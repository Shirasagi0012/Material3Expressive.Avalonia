using Avalonia.Threading;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Multiplexes one inner <see cref="IAnimationFrameClock"/> to many concurrent
/// <see cref="NextFrameAsync"/> waiters. The inner clock sees at most one pending wait;
/// each inner frame wakes every waiter registered for that frame, in registration order.
/// A waiter canceled before it is woken is not given a timestamp.
/// </summary>
internal sealed class SharedAnimationFrameClock : IAnimationFrameClock, IAnimationLifetimeClock, IDisposable
{
    private readonly IAnimationFrameClock _inner;
    private readonly bool _normalizeDispatcherContext;
    private readonly Action? _beforeEachFrame;
    private readonly Action? _afterEachWaiter;
    private readonly Action<Exception>? _onInnerFailure;
    private readonly object _gate = new();
    private readonly List<Waiter> _waiters = new();
    private CancellationTokenSource? _innerWait;
    private bool _pumping;
    private bool _disposed;
    private Exception? _failure;

    /// <param name="afterEachWaiter">
    /// Invoked after each waiter is resumed or canceled as part of a frame batch.
    /// Used by hosts that need to drain deferred work between consumers.
    /// </param>
    /// <param name="onInnerFailure">
    /// Invoked once when the inner clock throws from <see cref="IAnimationFrameClock.NextFrameAsync"/>
    /// (not for cancellation), before the same failure is delivered to consumers.
    /// </param>
    /// <param name="beforeEachFrame">
    /// Invoked after the inner frame arrives and before waiters are resumed, so a host can
    /// retarget animations and have the replacement join this frame's batch.
    /// </param>
    public SharedAnimationFrameClock(
        IAnimationFrameClock inner,
        Action? afterEachWaiter = null,
        Action<Exception>? onInnerFailure = null,
        Action? beforeEachFrame = null, bool normalizeDispatcherContext = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _normalizeDispatcherContext = normalizeDispatcherContext;
        _afterEachWaiter = afterEachWaiter;
        _onInnerFailure = onInnerFailure;
        _beforeEachFrame = beforeEachFrame;
    }

    AnimationLifetime? IAnimationLifetimeClock.AnimationLifetime => (_inner as IAnimationLifetimeClock)?.AnimationLifetime;
    double IAnimationLifetimeClock.DurationScale => (_inner as IAnimationLifetimeClock)?.DurationScale ?? MotionDurationScale.ScaleFactor;

    public void VerifyAccess() => _inner.VerifyAccess();

    public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<TimeSpan>(cancellationToken);

        var waiter = new Waiter();
        bool startPump;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_failure is not null) return ValueTask.FromException<TimeSpan>(_failure);
            _waiters.Add(waiter);
            startPump = !_pumping;
            if (startPump)
                _pumping = true;
            if (cancellationToken.CanBeCanceled)
                waiter.Registration = cancellationToken.Register(() => CancelWaiter(waiter));
        }

        if (startPump)
            StartPump();
        return new ValueTask<TimeSpan>(waiter.Completion.Task);
    }

    public void Dispose()
    {
        Waiter[] pending;
        CancellationTokenSource? innerWait;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _waiters.ToArray();
            _waiters.Clear();
            innerWait = _innerWait;
        }

        innerWait?.Cancel();
        foreach (var waiter in pending)
        {
            waiter.Registration.Dispose();
            waiter.TrySetCanceled();
        }
    }

    private void CancelWaiter(Waiter waiter)
    {
        CancellationTokenSource? innerWait = null;
        lock (_gate)
        {
            if (_waiters.Remove(waiter) && _waiters.Count == 0)
                innerWait = _innerWait;
        }

        waiter.Registration.Dispose();
        waiter.TrySetCanceled();
        innerWait?.Cancel();
    }

    private void StartPump()
    {
        var previous = SynchronizationContext.Current;
        try
        {
            if (_normalizeDispatcherContext)
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext(Dispatcher.UIThread, DispatcherPriority.Normal));
            _ = PumpAsync();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private async Task PumpAsync()
    {
        try
        {
            await PumpLoopAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            try
            {
                _onInnerFailure?.Invoke(error);
            }
            catch
            {
                // Host callbacks must not become unobserved pump faults.
            }
        }
        finally
        {
            var restart = false;
            lock (_gate)
            {
                _pumping = false;
                if (!_disposed && _failure is null && _waiters.Count > 0)
                {
                    _pumping = true;
                    restart = true;
                }
            }

            if (restart)
                StartPump();
        }
    }

    private async Task PumpLoopAsync()
    {
        while (true)
        {
            var innerWait = new CancellationTokenSource();
            lock (_gate)
            {
                if (_disposed || _waiters.Count == 0)
                {
                    innerWait.Dispose();
                    return;
                }

                _innerWait = innerWait;
            }

            TimeSpan timestamp;
            try
            {
                timestamp = await _inner.NextFrameAsync(innerWait.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (innerWait.IsCancellationRequested)
            {
                continue;
            }
            catch (OperationCanceledException) when ((_inner as IAnimationLifetimeClock)?.AnimationLifetime?.IsEnding == true)
            {
                Dispose();
                return;
            }
            catch (Exception error)
            {
                _failure = error;
                // Diagnose before waiter continuations propagate this same source failure.
                try { _onInnerFailure?.Invoke(error); } catch { }
                FailWaiters(error);
                return;
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_innerWait, innerWait))
                        _innerWait = null;
                }

                innerWait.Dispose();
            }

            try { _beforeEachFrame?.Invoke(); }
            catch (Exception error) { FailWaiters(error); return; }

            Waiter[] batch;
            lock (_gate)
            {
                batch = _waiters.ToArray();
                _waiters.Clear();
            }

            _inner.VerifyAccess();
            foreach (var waiter in batch)
            {
                waiter.Registration.Dispose();
                if (!waiter.IsCompleted)
                    waiter.TrySetResult(timestamp);
                try { _afterEachWaiter?.Invoke(); }
                catch (Exception error)
                {
                    foreach (var remaining in batch)
                    {
                        remaining.Registration.Dispose();
                        remaining.TrySetException(error);
                    }
                    FailWaiters(error);
                    return;
                }
            }
        }
    }

    private void FailWaiters(Exception error)
    {
        Waiter[] failed;
        lock (_gate)
        {
            failed = _waiters.ToArray();
            _waiters.Clear();
        }

        List<Exception>? callbackErrors = null;
        foreach (var waiter in failed)
        {
            waiter.Registration.Dispose();
            waiter.TrySetException(error);
            try { _afterEachWaiter?.Invoke(); }
            catch (Exception callbackError) { (callbackErrors ??= []).Add(callbackError); }
        }
        if (callbackErrors is not null) throw new AggregateException(callbackErrors);
    }

    private sealed class Waiter
    {
        // Inline continuations: Animatable must resume on the clock thread.
        public readonly TaskCompletionSource<TimeSpan> Completion = new();
        public CancellationTokenRegistration Registration;

        public bool IsCompleted => Completion.Task.IsCompleted;
        public bool TrySetResult(TimeSpan timestamp) => Completion.TrySetResult(timestamp);
        public bool TrySetCanceled() => Completion.TrySetCanceled();
        public bool TrySetException(Exception error) => Completion.TrySetException(error);
    }
}
