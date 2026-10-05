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
    /// <returns>
    /// The operation's result. A rejected or cancelled mutation completes with cancellation;
    /// other exceptions from the operation propagate to the caller.
    /// </returns>
    /// <typeparam name="TResult">The result returned by the mutation.</typeparam>
    public Task<TResult> MutateAsync<TResult>(
        Func<CancellationToken, Task<TResult>> mutation,
        MutatePriority priority = MutatePriority.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        Current current;
        lock (_gate)
        {
            if (_current is { } old)
            {
                if (priority < old.Priority)
                    return Task.FromCanceled<TResult>(new CancellationToken(true));
                old.Cancellation.Cancel();
            }

            current = new Current(priority, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            _current = current;
        }

        var task = ExecuteAsync(current, mutation);
        current.Task = task;
        return task;
    }

    private async Task<TResult> ExecuteAsync<TResult>(
        Current current,
        Func<CancellationToken, Task<TResult>> mutation)
    {
        try
        {
            // Wait until the interrupted mutation has fully unwound before entering the critical
            // section; the counterpart of mutex.withLock in InternalMutatorMutex. Waiters are
            // resumed inline so a follow-up animateTo from the same frame can request the next
            // clock frame before that Step returns.
            await WaitSectionAsync(current.Cancellation.Token).ConfigureAwait(true);
            try
            {
                return await mutation(current.Cancellation.Token).ConfigureAwait(true);
            }
            finally
            {
                ReleaseSection();
            }
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_current, current))
                    _current = null;
            }
            current.Cancellation.Dispose();
        }
    }

    private Task WaitSectionAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromCanceled(token);

        SectionWaiter waiter;
        lock (_sectionGate)
        {
            if (!_sectionHeld)
            {
                _sectionHeld = true;
                return Task.CompletedTask;
            }

            waiter = new SectionWaiter();
            _sectionWaiters.Enqueue(waiter);
            if (token.CanBeCanceled)
                waiter.Registration = token.Register(() => waiter.Completion.TrySetCanceled(token));
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

        next?.Completion.TrySetResult();
    }

    private sealed class SectionWaiter
    {
        public readonly TaskCompletionSource Completion = new();
        public CancellationTokenRegistration Registration;
    }
}
