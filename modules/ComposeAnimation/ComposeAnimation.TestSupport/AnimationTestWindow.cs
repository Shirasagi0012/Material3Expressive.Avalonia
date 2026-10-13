using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.TestSupport;

/// <summary>Initializes the animation environment before any Content setter or XAML loading.</summary>
public class AnimationTestWindow : Window
{
    public AnimationTestWindow(IFrameSource? clock = null)
        : this(clock, TimeProvider.System) { }

    public AnimationTestWindow(IFrameSource? clock, TimeProvider timeProvider)
    {
        FrameSource = clock;
        AnimationRoot = AnimationScope.Initialize(this, clock, timeProvider);
    }
    public IFrameSource? FrameSource { get; }
    public AnimationScope AnimationRoot { get; }
}

public static class AnimationTestDispatcher
{
    /// <summary>Bounded dispatcher progress. This is not a promise that layout/render is idle.</summary>
    public static async Task WaitUntilAsync(Func<bool> completed, int maximumTurns = 1000)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTurns);
        Dispatcher.UIThread.VerifyAccess();
        for (var turn = 0; turn < maximumTurns; turn++)
        {
            if (completed()) return;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
        throw new TimeoutException($"Animation condition did not complete in {maximumTurns} dispatcher turns.");
    }
}
