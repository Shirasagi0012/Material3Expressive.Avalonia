using Avalonia.Controls;
using Avalonia.Threading;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Time source for animation frame loops. Pull model: the consumer awaits one frame at a time
/// via <see cref="NextFrameAsync"/>, so cancellation of the supplied token unwinds the waiting
/// loop with an <see cref="OperationCanceledException"/>. All members must be called on the
/// clock's owning thread.
/// </summary>
public interface IAnimationFrameClock
{
    /// <summary>Throws if called from a thread other than the clock's owning thread.</summary>
    void VerifyAccess();

    /// <summary>
    /// Suspends until the next frame and returns its timestamp in the clock's timebase (this is
    /// not elapsed play time). Typical implementations allow at most one pending call; when used as a window's frame
    /// source, the window's internal frame clock is its only consumer and multiplexes it.
    /// </summary>
    ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken);
}

/// <summary>Drives animations using Avalonia's UI-thread animation frames.</summary>
internal sealed class AvaloniaAnimationFrameClock(TopLevel topLevel) : IAnimationFrameClock
{
    private readonly TopLevel _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));

    public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

    public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<TimeSpan>(cancellationToken);
        return new ValueTask<TimeSpan>(new FrameWaiter(_topLevel, cancellationToken).Task);
    }

    private sealed class FrameWaiter
    {
        private readonly TaskCompletionSource<TimeSpan> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _registration;
        private int _completed;

        public FrameWaiter(TopLevel topLevel, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
                _registration = cancellationToken.Register(
                    static state => ((FrameWaiter)state!).Cancel(), this);
            topLevel.RequestAnimationFrame(OnFrame);
        }

        public Task<TimeSpan> Task => _completion.Task;

        private void OnFrame(TimeSpan timestamp)
        {
            // Avalonia cannot unqueue a pending frame callback; swallow it if cancellation won.
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;
            _registration.Dispose();
            _completion.TrySetResult(timestamp);
        }

        private void Cancel()
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;
            _completion.TrySetCanceled();
        }
    }
}
