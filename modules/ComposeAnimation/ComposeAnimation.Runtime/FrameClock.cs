using Avalonia.Threading;

namespace ComposeAnimation.Runtime;

/// <summary>
/// The single per-window frame clock. It is the only consumer of the window's frame source and
/// multiplexes that source to any number of <see cref="WaitAsync"/> waiters.
/// </summary>
/// <remarks>
/// <para>Frame protocol: while at least one waiter is registered the clock keeps exactly one wait
/// pending on the source; with no waiters it requests nothing. When a frame arrives, the clock
/// (1) runs before-frame hooks, (2) snapshots the waiters, (3) resumes them in registration order
/// with that frame's timestamp, running after-waiter hooks after each one. Work started by a
/// before-frame hook that registers a waiter joins the snapshot; waiters registered while the
/// batch is resuming go to the next frame. Hooks run in registration order; the hook list is
/// snapshotted when the frame arrives, so registering or unregistering a hook during a frame
/// takes effect on the next frame.</para>
/// <para>Resumption policy (D4, the only policy implemented): the source's completion is never
/// continued inline or on whatever context the first waiter happened to capture. It is always
/// re-entered through one <see cref="Dispatcher.Post(Action, DispatcherPriority)"/> at
/// <see cref="ResumePriority"/>. Inside the batch waiters resume inline: before completing each
/// waiter the clock installs the <see cref="SynchronizationContext"/> that was current when the
/// waiter registered, which is the context its own <c>await</c> captured, so the TPL's
/// reference-equality check (<c>AvaloniaSynchronizationContext</c> has no <c>Equals</c>)
/// always passes and the continuation runs synchronously instead of being posted.</para>
/// <para>Failures: a source failure (anything but the clock's own cancellation) is reported once
/// through the failure callback and then fails every pending and future waiter. A throwing hook
/// fails the current batch (remaining snapshot waiters and any waiters registered during the
/// batch) with that exception; the clock itself stays usable for later waiters.</para>
/// </remarks>
internal sealed class FrameClock : IDisposable
{
    /// <summary>The single dispatcher priority at which every frame batch runs.</summary>
    internal static readonly DispatcherPriority ResumePriority = DispatcherPriority.Normal;

    private readonly IAnimationFrameClock _source;
    private readonly Action<Exception>? _onSourceFailure;
    private readonly object _gate = new();
    private readonly List<Waiter> _waiters = [];
    private readonly List<Hook> _beforeFrame = [];
    private readonly List<Hook> _afterWaiter = [];
    private CancellationTokenSource? _sourceWait;
    private bool _requested;
    private bool _inFrame;
    private bool _disposed;
    private Exception? _failure;

    /// <param name="source">The window's frame source. Only this clock may await it.</param>
    /// <param name="onSourceFailure">Invoked once when the source fails, before waiters observe the failure.</param>
    public FrameClock(IAnimationFrameClock source, Action<Exception>? onSourceFailure = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _onSourceFailure = onSourceFailure;
    }

    public void VerifyAccess() => _source.VerifyAccess();

    /// <summary>Waits for the next frame and returns its source timestamp.</summary>
    public ValueTask<TimeSpan> WaitAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<TimeSpan>(cancellationToken);

        var waiter = new Waiter(SynchronizationContext.Current);
        bool request;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_failure is not null) return ValueTask.FromException<TimeSpan>(_failure);
            _waiters.Add(waiter);
            request = !_requested && !_inFrame;
            if (request) _requested = true;
            if (cancellationToken.CanBeCanceled)
                waiter.Registration = cancellationToken.Register(static state =>
                {
                    var (clock, w) = ((FrameClock, Waiter))state!;
                    clock.CancelWaiter(w);
                }, (this, waiter));
        }

        if (request) RequestFrame();
        return new ValueTask<TimeSpan>(waiter.Completion.Task);
    }

    /// <summary>Runs <paramref name="hook"/> when a frame arrives, before the waiter snapshot.</summary>
    public IDisposable RegisterBeforeFrame(Action hook) => AddHook(_beforeFrame, hook);

    /// <summary>Runs <paramref name="hook"/> after each waiter of a batch is resumed.</summary>
    public IDisposable RegisterAfterWaiter(Action hook) => AddHook(_afterWaiter, hook);

    private IDisposable AddHook(List<Hook> hooks, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var hook = new Hook(this, hooks, action);
        lock (_gate) hooks.Add(hook);
        return hook;
    }

    public void Dispose()
    {
        Waiter[] pending;
        CancellationTokenSource? sourceWait;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _waiters.ToArray();
            _waiters.Clear();
            _beforeFrame.Clear();
            _afterWaiter.Clear();
            sourceWait = _sourceWait;
        }

        sourceWait?.Cancel();
        foreach (var waiter in pending)
        {
            waiter.Registration.Dispose();
            waiter.Completion.TrySetCanceled();
        }
    }

    private void CancelWaiter(Waiter waiter)
    {
        CancellationTokenSource? sourceWait = null;
        lock (_gate)
        {
            if (_waiters.Remove(waiter) && _waiters.Count == 0)
                sourceWait = _sourceWait;
        }

        waiter.Registration.Dispose();
        waiter.Completion.TrySetCanceled();
        sourceWait?.Cancel();
    }

    // Exactly one source wait is outstanding while _requested is set. It is cleared only when that
    // wait's completion has been observed, so the source never sees overlapping waits.
    private void RequestFrame()
    {
        var sourceWait = new CancellationTokenSource();
        lock (_gate) _sourceWait = sourceWait;
        Task<TimeSpan> frame;
        try { frame = _source.NextFrameAsync(sourceWait.Token).AsTask(); }
        catch (Exception error) { frame = Task.FromException<TimeSpan>(error); }
        // Resumption policy: one fixed dispatcher hop, independent of any captured context.
        // ExecuteSynchronously (not an awaiter): the post is queued on the completing thread, even
        // when that thread has a SynchronizationContext, so a synchronous source deterministically
        // queues the batch before it returns instead of bouncing through the thread pool.
        frame.ContinueWith(static (_, state) =>
        {
            var (clock, task, wait) = ((FrameClock, Task<TimeSpan>, CancellationTokenSource))state!;
            Dispatcher.UIThread.Post(() => clock.OnFrame(task, wait), ResumePriority);
        }, (this, frame, sourceWait), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void OnFrame(Task<TimeSpan> frame, CancellationTokenSource sourceWait)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_sourceWait, sourceWait)) _sourceWait = null;
            _requested = false;
        }
        var canceledByClock = sourceWait.IsCancellationRequested;
        sourceWait.Dispose();

        if (frame.IsCanceled && canceledByClock)
        {
            RequestIfWaiting();
            return;
        }

        if (!frame.IsCompletedSuccessfully)
        {
            if (_disposed) return;
            var error = frame.IsCanceled
                ? new OperationCanceledException("The frame source canceled a frame wait.")
                : frame.Exception!.InnerExceptions.Count == 1 ? frame.Exception.InnerException! : frame.Exception;
            lock (_gate) _failure = error;
            try { _onSourceFailure?.Invoke(error); } catch { /* reporting must not strand waiters */ }
            Hook[] after;
            lock (_gate) after = _afterWaiter.ToArray();
            FailWaiters(error, after);
            return;
        }

        RunFrame(frame.Result);
        RequestIfWaiting();
    }

    private void RunFrame(TimeSpan timestamp)
    {
        Hook[] before, after;
        lock (_gate)
        {
            if (_disposed) return;
            _inFrame = true;
            before = _beforeFrame.ToArray();
            after = _afterWaiter.ToArray();
        }

        try
        {
            try
            {
                foreach (var hook in before) hook.Action();
            }
            catch (Exception error)
            {
                FailWaiters(error, after);
                return;
            }

            Waiter[] batch;
            lock (_gate)
            {
                batch = _waiters.ToArray();
                _waiters.Clear();
            }

            VerifyAccess();
            for (var i = 0; i < batch.Length; i++)
            {
                var waiter = batch[i];
                waiter.Registration.Dispose();
                Resume(waiter, timestamp);
                try
                {
                    foreach (var hook in after) hook.Action();
                }
                catch (Exception error)
                {
                    for (var j = i + 1; j < batch.Length; j++)
                    {
                        batch[j].Registration.Dispose();
                        batch[j].Completion.TrySetException(error);
                    }
                    FailWaiters(error, after);
                    return;
                }
            }
        }
        finally
        {
            lock (_gate) _inFrame = false;
        }
    }

    // Install the waiter's own captured context so its continuation passes the TPL's
    // same-context check and runs inline here rather than being posted at its priority.
    private static void Resume(Waiter waiter, TimeSpan timestamp)
    {
        if (waiter.Completion.Task.IsCompleted) return;
        var previous = SynchronizationContext.Current;
        if (ReferenceEquals(previous, waiter.Context))
        {
            waiter.Completion.TrySetResult(timestamp);
            return;
        }
        SynchronizationContext.SetSynchronizationContext(waiter.Context);
        try { waiter.Completion.TrySetResult(timestamp); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private void RequestIfWaiting()
    {
        bool request;
        lock (_gate)
        {
            request = !_disposed && _failure is null && !_requested && !_inFrame && _waiters.Count > 0;
            if (request) _requested = true;
        }
        if (request) RequestFrame();
    }

    private void FailWaiters(Exception error, Hook[] after)
    {
        Waiter[] failed;
        lock (_gate)
        {
            failed = _waiters.ToArray();
            _waiters.Clear();
        }

        foreach (var waiter in failed)
        {
            waiter.Registration.Dispose();
            waiter.Completion.TrySetException(error);
            foreach (var hook in after)
            {
                // The batch is already failing with `error`; a second hook fault must not
                // strand the remaining waiters or be reported as a new failure.
                try { hook.Action(); } catch { }
            }
        }
    }

    private sealed class Waiter(SynchronizationContext? context)
    {
        // Synchronous continuations: waiters resume inline inside the batch.
        public readonly TaskCompletionSource<TimeSpan> Completion = new();
        public readonly SynchronizationContext? Context = context;
        public CancellationTokenRegistration Registration;
    }

    private sealed class Hook(FrameClock owner, List<Hook> list, Action action) : IDisposable
    {
        public Action Action { get; } = action;
        public void Dispose()
        {
            lock (owner._gate) list.Remove(this);
        }
    }
}
