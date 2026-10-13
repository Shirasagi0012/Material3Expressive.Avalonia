using ComposeAnimation.Runtime;

namespace ComposeAnimation.TestSupport;

/// <summary>A manually advanced, multi-consumer clock. One send publishes one fixed batch.</summary>
public sealed class ManualFrameSource : IFrameSource, IDisposable
{
    private readonly object _gate = new();
    private readonly HashSet<Waiter> _waiters = [];
    private readonly Action? _verifyAccess;
    private bool _disposed;
    public ManualFrameSource(Action? verifyAccess = null) => _verifyAccess = verifyAccess;
    public TimeSpan Now { get; private set; }
    public int PendingFrameCount { get { lock (_gate) return _waiters.Count; } }
    public void VerifyAccess() => _verifyAccess?.Invoke();

    public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (cancellationToken.IsCancellationRequested) return ValueTask.FromCanceled<TimeSpan>(cancellationToken);
        var waiter = new Waiter(cancellationToken);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _waiters.Add(waiter);
        }
        if (cancellationToken.CanBeCanceled)
            waiter.Registration = cancellationToken.Register(() => Cancel(waiter));
        if (waiter.Completion.Task.IsCompleted) waiter.Registration.Dispose();
        return new ValueTask<TimeSpan>(waiter.Completion.Task);
    }

    public void AdvanceBy(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        SendFrame(Now + delta);
    }

    public void SendFrame(TimeSpan timestamp)
    {
        VerifyAccess();
        Waiter[] batch;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentOutOfRangeException.ThrowIfLessThan(timestamp, Now);
            Now = timestamp;
            batch = _waiters.ToArray();
        }
        foreach (var waiter in batch)
        {
            // Keep cancellation live for the rest of the batch during inline continuations.
            lock (_gate) _waiters.Remove(waiter);
            waiter.Registration.Dispose();
            if (_disposed || waiter.Token.IsCancellationRequested) waiter.Completion.TrySetCanceled(waiter.Token);
            else waiter.Completion.TrySetResult(timestamp);
        }
    }

    private void Cancel(Waiter waiter)
    {
        lock (_gate) _waiters.Remove(waiter);
        waiter.Completion.TrySetCanceled(waiter.Token);
    }

    public void Dispose()
    {
        VerifyAccess();
        Waiter[] pending;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _waiters.ToArray();
            _waiters.Clear();
        }
        foreach (var waiter in pending)
        {
            waiter.Registration.Dispose();
            waiter.Completion.TrySetCanceled();
        }
    }

    private sealed class Waiter(CancellationToken token)
    {
        internal CancellationToken Token { get; } = token;
        internal TaskCompletionSource<TimeSpan> Completion { get; } = new();
        internal CancellationTokenRegistration Registration;
    }
}
