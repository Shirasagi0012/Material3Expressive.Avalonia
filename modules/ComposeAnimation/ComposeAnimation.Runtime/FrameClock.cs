using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using Avalonia.Threading;

namespace ComposeAnimation.Runtime;

/// <summary>
/// The single per-window frame clock. It is the only consumer of the window's frame source and
/// multiplexes that source to any number of <see cref="WaitAsync"/> waiters. UI-thread only.
/// </summary>
/// <remarks>
/// <para>Frame protocol: one async pump loop runs while at least one waiter is registered. Each
/// iteration awaits the source (so the source never sees overlapping waits), makes one explicit
/// hop to the dispatcher, then (1) runs before-frame hooks, (2) snapshots the waiters, (3) resumes
/// them in registration order with that frame's timestamp, running after-waiter hooks after each
/// one. Work started by a before-frame hook that registers a waiter joins the snapshot; waiters
/// registered while the batch is resuming go to the next frame. With no waiters the loop exits and
/// nothing is requested. Hook lists are snapshotted when a frame arrives, so registering or
/// unregistering a hook during a frame takes effect on the next frame.</para>
/// <para>Resumption policy (D4, the only policy implemented): the source's completion is always
/// continued through exactly one <see cref="Dispatcher.Post(Action, DispatcherPriority)"/> at
/// <see cref="ResumePriority"/>, posted from the thread that completed the source. This is the
/// single decision point; no captured context is consulted.</para>
/// <para>Inline waiters: a wait is an <see cref="IValueTaskSource{TResult}"/> that stores its
/// continuation and ignores the scheduling context the awaiter asks for (it strips
/// <see cref="ValueTaskSourceOnCompletedFlags.UseSchedulingContext"/>). The batch completes each
/// waiter, which invokes its continuation directly on the UI thread, inside the batch, with the
/// batch's own dispatcher <see cref="SynchronizationContext"/> left current, so the continuation's
/// later ordinary awaits behave normally. This holds for <see cref="FrameAwaitable"/> and for the
/// <see cref="ValueTask{TResult}"/> adapter alike, whatever context the awaiting code started on.</para>
/// <para>Cancellation, failure and dispose complete waiters through the same path: inline when
/// already on the UI thread, otherwise posted to it at <see cref="ResumePriority"/>.</para>
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
    private readonly List<Waiter> _waiters = [];
    private readonly List<Hook> _beforeFrame = [];
    private readonly List<Hook> _afterWaiter = [];
    private CancellationTokenSource? _sourceWait;
    private bool _pumping;
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

    /// <summary>
    /// Waits for the next frame and returns its source timestamp. Cancelled when either token is.
    /// <paramref name="onFault"/>, if given, observes the completing exception (cancellation
    /// included) before the waiter's continuation runs.
    /// </summary>
    public FrameAwaitable WaitAsync(CancellationToken cancellationToken,
        CancellationToken secondToken = default, Action<Exception>? onFault = null)
    {
        VerifyAccess();
        var waiter = new Waiter(this, onFault);
        var canceled = cancellationToken.IsCancellationRequested ? cancellationToken
            : secondToken.IsCancellationRequested ? secondToken : (CancellationToken?)null;
        if (canceled is { } token)
        {
            waiter.Complete(default, new OperationCanceledException(token));
            return new FrameAwaitable(waiter);
        }
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_failure is not null)
            waiter.Complete(default, _failure);
        else
        {
            _waiters.Add(waiter);
            waiter.Register(cancellationToken, secondToken);
            if (!_pumping)
            {
                _pumping = true;
                _ = PumpAsync();
            }
        }
        return new FrameAwaitable(waiter);
    }

    /// <summary>Runs <paramref name="hook"/> when a frame arrives, before the waiter snapshot.</summary>
    public IDisposable RegisterBeforeFrame(Action hook) => AddHook(_beforeFrame, hook);

    /// <summary>Runs <paramref name="hook"/> after each waiter of a batch is resumed.</summary>
    public IDisposable RegisterAfterWaiter(Action hook) => AddHook(_afterWaiter, hook);

    private IDisposable AddHook(List<Hook> hooks, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        VerifyAccess();
        var hook = new Hook(hooks, action);
        hooks.Add(hook);
        return hook;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var pending = _waiters.ToArray();
        _waiters.Clear();
        _beforeFrame.Clear();
        _afterWaiter.Clear();
        _sourceWait?.Cancel();
        foreach (var waiter in pending)
            waiter.Complete(default, new OperationCanceledException());
    }

    // Runs on the UI thread (inline or posted by Waiter's token callback).
    private void CancelWaiter(Waiter waiter, CancellationToken token)
    {
        if (waiter.IsCompleted) return;
        var wasLast = _waiters.Remove(waiter) && _waiters.Count == 0;
        waiter.Complete(default, new OperationCanceledException(token));
        if (wasLast) _sourceWait?.Cancel();
    }

    private async Task PumpAsync()
    {
        try
        {
            while (!_disposed && _failure is null && _waiters.Count > 0)
            {
                var sourceWait = new CancellationTokenSource();
                _sourceWait = sourceWait;
                Task<TimeSpan> frame;
                try { frame = _source.NextFrameAsync(sourceWait.Token).AsTask(); }
                catch (Exception error) { frame = Task.FromException<TimeSpan>(error); }

                // The single resumption decision point: one post at ResumePriority.
                await new DispatcherHop(frame);

                _sourceWait = null;
                var canceledByClock = sourceWait.IsCancellationRequested;
                sourceWait.Dispose();
                if (_disposed) return;
                if (frame.IsCanceled && canceledByClock) continue;
                if (!frame.IsCompletedSuccessfully)
                {
                    var error = frame.IsCanceled
                        ? new OperationCanceledException("The frame source canceled a frame wait.")
                        : frame.Exception!.InnerExceptions.Count == 1 ? frame.Exception.InnerException! : frame.Exception;
                    _failure = error;
                    try { _onSourceFailure?.Invoke(error); } catch { /* reporting must not strand waiters */ }
                    FailWaiters(error, _afterWaiter.ToArray());
                    return;
                }
                RunFrame(frame.Result);
            }
        }
        catch (Exception error)
        {
            // Defensive: nothing above should throw; never leave an unobserved pump fault.
            Dispatcher.UIThread.Post(() => throw new InvalidOperationException("FrameClock pump failed.", error));
        }
        finally
        {
            _pumping = false;
        }
    }

    private void RunFrame(TimeSpan timestamp)
    {
        var before = _beforeFrame.ToArray();
        var after = _afterWaiter.ToArray();
        try
        {
            foreach (var hook in before) hook.Action();
        }
        catch (Exception error)
        {
            FailWaiters(error, after);
            return;
        }

        var batch = _waiters.ToArray();
        _waiters.Clear();
        VerifyAccess();
        for (var i = 0; i < batch.Length; i++)
        {
            batch[i].Complete(timestamp, null);
            try
            {
                foreach (var hook in after) hook.Action();
            }
            catch (Exception error)
            {
                for (var j = i + 1; j < batch.Length; j++)
                    batch[j].Complete(default, error);
                FailWaiters(error, after);
                return;
            }
        }
    }

    private void FailWaiters(Exception error, Hook[] after)
    {
        var failed = _waiters.ToArray();
        _waiters.Clear();
        foreach (var waiter in failed)
        {
            waiter.Complete(default, error);
            foreach (var hook in after)
            {
                // The batch is already failing with `error`; a second hook fault must not
                // strand the remaining waiters or be reported as a new failure.
                try { hook.Action(); } catch { }
            }
        }
    }

    /// <summary>Awaiter-side view of one frame wait. Await it once.</summary>
    internal readonly struct FrameAwaitable
    {
        private readonly Waiter _waiter;
        private readonly short _token;

        internal FrameAwaitable(Waiter waiter) => (_waiter, _token) = (waiter, waiter.Version);

        public Awaiter GetAwaiter() => new(_waiter, _token);

        /// <summary>Adapter for callers that need a <see cref="ValueTask{TResult}"/>; same inline resumption.</summary>
        public ValueTask<TimeSpan> AsValueTask() => new(_waiter, _token);

        internal readonly struct Awaiter(Waiter waiter, short token) : ICriticalNotifyCompletion
        {
            public bool IsCompleted => waiter.GetStatus(token) != ValueTaskSourceStatus.Pending;
            public TimeSpan GetResult() => waiter.GetResult(token);
            public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation)
                => waiter.OnCompleted(static state => ((Action)state!)(), continuation, token,
                    ValueTaskSourceOnCompletedFlags.None);
        }
    }

    /// <summary>
    /// One frame wait. Allocated per wait for now; the version-token protocol keeps it ready for
    /// pooling (O4) without changing callers.
    /// </summary>
    internal sealed class Waiter(FrameClock owner, Action<Exception>? onFault) : IValueTaskSource<TimeSpan>
    {
        // Synchronous: completing the waiter runs its continuation right here, inside the batch.
        private ManualResetValueTaskSourceCore<TimeSpan> _core = new() { RunContinuationsAsynchronously = false };
        private CancellationTokenRegistration _first, _second;
#if DEBUG
        private bool _awaited;
#endif

        public short Version => _core.Version;
        public bool IsCompleted { get; private set; }

        public void Register(CancellationToken first, CancellationToken second)
        {
            if (first.CanBeCanceled) _first = first.UnsafeRegister(OnCanceled, (this, first));
            if (second.CanBeCanceled) _second = second.UnsafeRegister(OnCanceled, (this, second));
        }

        private static void OnCanceled(object? state)
        {
            var (waiter, token) = ((Waiter, CancellationToken))state!;
            if (Dispatcher.UIThread.CheckAccess()) waiter.CancelOnUiThread(token);
            else Dispatcher.UIThread.Post(() => waiter.CancelOnUiThread(token), ResumePriority);
        }

        private void CancelOnUiThread(CancellationToken token) => owner.CancelWaiter(this, token);

        /// <summary>Completes on the UI thread; the stored continuation runs inline.</summary>
        public void Complete(TimeSpan timestamp, Exception? error)
        {
            if (IsCompleted) return;
            IsCompleted = true;
            _first.Dispose();
            _second.Dispose();
            if (error is null)
            {
                _core.SetResult(timestamp);
                return;
            }
            try { onFault?.Invoke(error); } catch { /* observers must not strand the waiter */ }
            _core.SetException(error);
        }

        public TimeSpan GetResult(short token) => _core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
#if DEBUG
            Debug.Assert(!_awaited, "A FrameClock wait can be awaited only once.");
            _awaited = true;
#endif
            // Never capture a SynchronizationContext or TaskScheduler: the batch calls the
            // continuation directly. ExecutionContext flow is honoured if the awaiter asks for it.
            _core.OnCompleted(continuation, state, token, flags & ~ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
        }
    }

    // Awaits the source and resumes with one dispatcher post, issued on the completing thread
    // (ExecuteSynchronously), so a synchronous source deterministically queues the batch.
    private readonly struct DispatcherHop(Task source) : ICriticalNotifyCompletion
    {
        public DispatcherHop GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
            => source.ContinueWith(static (_, state) => Dispatcher.UIThread.Post((Action)state!, ResumePriority),
                continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private sealed class Hook(List<Hook> list, Action action) : IDisposable
    {
        public Action Action { get; } = action;
        public void Dispose() => list.Remove(this);
    }
}

/// <summary>
/// A frame clock that can hand out FrameClock waits directly, letting pass-through wrappers
/// observe faults without an async layer (which would reintroduce context-dependent resumption).
/// </summary>
internal interface IFrameWaitProvider
{
    ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault);
}

internal static class FrameWait
{
    /// <summary>
    /// Forwards a wait to <paramref name="provider"/>; <paramref name="onFault"/> sees any exception
    /// (cancellation included) before the caller does.
    /// </summary>
    public static ValueTask<TimeSpan> Observe(IAnimationFrameClock provider, CancellationToken cancellationToken,
        Action<Exception> onFault)
    {
        if (provider is not IFrameWaitProvider direct) return Slow(provider, cancellationToken, onFault);
        try
        {
            provider.VerifyAccess();
            return direct.NextFrameAsync(cancellationToken, onFault);
        }
        catch (Exception error)
        {
            onFault(error);
            return ValueTask.FromException<TimeSpan>(error);
        }

        static async ValueTask<TimeSpan> Slow(IAnimationFrameClock provider, CancellationToken token, Action<Exception> onFault)
        {
            try
            {
                provider.VerifyAccess();
                return await provider.NextFrameAsync(token).ConfigureAwait(true);
            }
            catch (Exception error)
            {
                onFault(error);
                throw;
            }
        }
    }
}
