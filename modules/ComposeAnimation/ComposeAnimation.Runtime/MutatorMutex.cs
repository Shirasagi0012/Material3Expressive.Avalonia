namespace ComposeAnimation.Runtime;

/// <summary>Priority used to arbitrate long-running mutations.</summary>
public enum MutatePriority
{
    Default,
    UserInput,
    PreventUserInput,
}

/// <summary>
/// Serializes long-running mutations, adapted from AndroidX InternalMutatorMutex (Apache-2.0).
/// An equal or higher priority mutation interrupts the current one; a lower priority mutation is
/// rejected instead of being queued. The new mutation does not start until the interrupted one
/// has fully unwound, including its cancellation cleanup.
/// </summary>
public sealed class MutatorMutex
{
    private sealed class Current(MutatePriority priority, CancellationTokenSource cancellation)
    {
        public MutatePriority Priority { get; } = priority;
        public CancellationTokenSource Cancellation { get; } = cancellation;

        /// <summary>Task of the in-flight mutation body. Assigned before MutateAsync returns.</summary>
        public Task? Task { get; set; }
    }

    private readonly Lock _gate = new();
    private readonly Lock _sectionGate = new(); // TODO: Verify this Lock usage
    private readonly Queue<SectionWaiter> _sectionWaiters = new();
    private bool _sectionHeld;
    private Current? _current;

    /// <summary>
    /// Completion task of the mutation currently in flight, or null when idle. Callers that
    /// cancel via <see cref="CancelCurrent"/> can await this to observe the cleanup.
    /// </summary>
    public Task? CurrentCompletion
    {
        get
        {
            lock (_gate)
                return _current?.Task;
        }
    }

    /// <summary>Cancels the mutation currently in flight, if any, without waiting for it.</summary>
    public void CancelCurrent()
    {
        lock (_gate)
            _current?.Cancellation.Cancel();
    }

    /// <summary>Runs a mutation with exclusive access, subject to its priority.</summary>
    /// <param name="mutation">
    /// The operation to run. It must observe the supplied cancellation token so that an interrupting
    /// mutation can start after this operation and its cleanup have finished.
    /// </param>
    /// <param name="priority">Priority relative to the current mutation.</param>
    /// <param name="cancellationToken">Token used to cancel the requested mutation.</param>
    /// <param name="interruptedResult">Optional result for priority rejection or cancellation before
    /// entering the body. The body must also return its stop status. Without this factory, generic
    /// coroutine callers retain task cancellation semantics. Faults are never converted to a result.</param>
    /// <returns>
    /// The operation's result, or the interrupted result when supplied; real faults propagate.
    /// </returns>
    /// <typeparam name="TResult">The result returned by the mutation.</typeparam>
    public Task<TResult> MutateAsync<TResult>(
        Func<CancellationToken, Task<TResult>> mutation,
        MutatePriority priority = MutatePriority.Default,
        CancellationToken cancellationToken = default,
        Func<TResult>? interruptedResult = null)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        Current current;
        lock (_gate)
        {
            if (_current is { } old)
            {
                if (priority < old.Priority)
                    return interruptedResult is null ? Task.FromCanceled<TResult>(new CancellationToken(true)) : Task.FromResult(interruptedResult());
                old.Cancellation.Cancel();
            }

            current = new Current(priority, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            _current = current;
        }

        var task = interruptedResult is null
            ? ExecuteCoroutineAsync(current, mutation)
            : ExecuteResultAsync(current, mutation, interruptedResult);
        current.Task = task;
        return task;
    }

    private async Task<TResult> ExecuteCoroutineAsync<TResult>(Current current,
        Func<CancellationToken, Task<TResult>> mutation)
    {
        // Ordinary coroutine callers retain the original exception, cancellation token and
        // canceled task status. Explicit stop results use the nonthrowing management path below.
        try
        {
            var token = current.Cancellation.Token;
            if (!await WaitSectionAsync(token).ConfigureAwait(true))
                throw new OperationCanceledException(token);
            try
            {
                return await mutation(token).ConfigureAwait(true);
            }
            finally { ReleaseSection(); }
        }
        finally
        {
            lock (_gate) { if (ReferenceEquals(_current, current)) _current = null; }
            current.Cancellation.Dispose();
        }
    }

    private Task<TResult> ExecuteResultAsync<TResult>(Current current,
        Func<CancellationToken, Task<TResult>> mutation, Func<TResult> interruptedResult)
    {
        var completion = new TaskCompletionSource<TResult>();
        _ = Execute();
        return completion.Task;

        async Task Execute()
        {
            Task<TResult>? operation = null;
            Exception? failure = null;
            var entered = false;
            try
            {
                entered = await WaitSectionAsync(current.Cancellation.Token).ConfigureAwait(true);
                if (entered)
                {
                    operation = mutation(current.Cancellation.Token);
                    // Inspect status without rethrowing through management layers. In particular,
                    // a faulted OperationCanceledException remains a failure, not a stop result.
                    await ((Task)operation).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
                }
            }
            catch (Exception error) { failure = error; }
            finally
            {
                try { if (entered) ReleaseSection(); }
                catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
                lock (_gate) { if (ReferenceEquals(_current, current)) _current = null; }
            }
            current.Cancellation.Dispose();
            if (failure is not null) completion.TrySetException(failure);
            else if (operation?.IsFaulted == true) completion.TrySetException(operation.Exception!.InnerExceptions);
            else if (operation?.IsCompletedSuccessfully == true) completion.TrySetResult(operation.Result);
            else if (operation?.IsCanceled == true)
            {
                // An async body turns even an unrelated OCE into a canceled Task. Bodies using
                // the explicit-state contract return their stop result; never hide their throws.
                try { operation.GetAwaiter().GetResult(); }
                catch (Exception error) { completion.TrySetException(error); }
            }
            else
            {
                try { completion.TrySetResult(interruptedResult()); }
                catch (Exception error) { completion.TrySetException(error); }
            }
        }
    }

    private Task<bool> WaitSectionAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromResult(false);

        SectionWaiter waiter;
        lock (_sectionGate)
        {
            if (!_sectionHeld)
            {
                _sectionHeld = true;
                return Task.FromResult(true);
            }

            waiter = new SectionWaiter();
            _sectionWaiters.Enqueue(waiter);
            if (token.CanBeCanceled)
                waiter.Registration = token.Register(() => waiter.Completion.TrySetResult(false));
        }

        return waiter.Completion.Task;
    }

    private void ReleaseSection()
    {
        SectionWaiter? next = null;
        lock (_sectionGate)
        {
            while (_sectionWaiters.Count > 0)
            {
                var candidate = _sectionWaiters.Dequeue();
                candidate.Registration.Dispose();
                if (candidate.Completion.Task.IsCompleted)
                    continue;
                next = candidate;
                break;
            }

            if (next is null)
                _sectionHeld = false;
        }

        next?.Completion.TrySetResult(true);
    }

    private sealed class SectionWaiter
    {
        public readonly TaskCompletionSource<bool> Completion = new();
        public CancellationTokenRegistration Registration;
    }
}
