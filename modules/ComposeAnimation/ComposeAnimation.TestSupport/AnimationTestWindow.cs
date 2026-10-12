using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.TestSupport;

/// <summary>Initializes the animation environment before any Content setter or XAML loading.</summary>
public class AnimationTestWindow : Window
{
    public AnimationTestWindow(IAnimationFrameClock? clock = null)
        : this(clock, TimeProvider.System) { }

    public AnimationTestWindow(IAnimationFrameClock? clock, TimeProvider timeProvider)
    {
        TestClock = clock;
        AnimationRoot = AnimationScope.Initialize(this, clock, timeProvider);
    }
    public IAnimationFrameClock? TestClock { get; }
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
