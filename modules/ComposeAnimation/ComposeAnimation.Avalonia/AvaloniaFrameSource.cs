using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Drives animations using Avalonia's UI-thread animation frames. This is the default
/// <see cref="IFrameSource"/> for a window; it is the only place the engine touches a control.
/// </summary>
internal sealed class AvaloniaFrameSource(TopLevel topLevel) : IFrameSource
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
