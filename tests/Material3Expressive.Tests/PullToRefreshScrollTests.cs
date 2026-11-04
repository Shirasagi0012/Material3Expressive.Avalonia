using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class PullToRefreshScrollTests
{
    [AvaloniaFact]
    public void PrePostOrderPreservesDragMultiplierTensionAndSourceGates()
    {
        using var clock = new MotionTestClockScope();
        var (window, indicator, behavior) = Show();
        Assert.Equal(0, indicator.State!.DistanceFraction);
        Assert.Equal(0, behavior.OnPreScroll(80));
        Assert.Equal(80, behavior.OnPostScroll(80)); Flush(window);
        Assert.Equal(.5f, indicator.State.DistanceFraction);
        Assert.Equal(-20, behavior.OnPreScroll(-20));
        Assert.Equal(.5f, indicator.State.DistanceFraction); // pre changes accumulation, post publishes it.
        Assert.Equal(0, behavior.OnPostScroll(0)); Flush(window);
        Assert.Equal(.375f, indicator.State.DistanceFraction);
        Assert.Equal(0, behavior.OnPostScroll(200, false));
        Assert.Equal(0, behavior.OnPreScroll(-200, false));
        Assert.Equal(420, behavior.OnPostScroll(420)); Flush(window);
        Assert.Equal(2, indicator.State.DistanceFraction); // 200% extra drag reaches the tension cap.
        Assert.Equal(-480, behavior.OnPreScroll(-600));
        behavior.OnPostScroll(0); Flush(window);
        Assert.Equal(0, indicator.State.DistanceFraction);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(159, 200, false, 0, 200)]
    [InlineData(160, 200, false, 0, 200)]
    [InlineData(161, 200, false, 1, 200)]
    [InlineData(161, -200, false, 1, 0)]
    [InlineData(161, 200, true, 1, 200)]
    [InlineData(0, 200, false, 0, 0)]
    public async Task ReleaseUsesStrictThresholdAndConsumesOnlyDownwardDraggedVelocity(float drag, float velocity,
        bool disableBeforeRelease, int requests, float expectedVelocity)
    {
        using var clock = new MotionTestClockScope();
        var (window, indicator, behavior) = Show(); clock.SendFrame(TimeSpan.Zero);
        var actualRequests = 0; behavior.RefreshRequested += (_, _) => actualRequests++;
        behavior.OnPostScroll(drag); Flush(window);
        behavior.IsEnabled = !disableBeforeRelease;
        var release = behavior.OnPreFlingAsync(velocity);
        Assert.Equal(requests, actualRequests);
        Assert.Equal(0, behavior.OnPostScroll(30)); // settling blocks new pulls.
        Frames(clock, window, 16, 1008);
        Assert.Equal(expectedVelocity, await release);
        Assert.Equal(0, indicator.State!.DistanceFraction);
        Assert.Equal(0, behavior.DistancePulled);
        window.Close();
    }

    [AvaloniaFact]
    public async Task RefreshCallbackCanInterruptReleaseAndRefreshingBlocksFurtherConsumption()
    {
        using var clock = new MotionTestClockScope();
        var (window, indicator, behavior) = Show(); clock.SendFrame(TimeSpan.Zero);
        behavior.RefreshRequested += (_, _) => indicator.IsRefreshing = true;
        behavior.OnPostScroll(200); Flush(window);
        var release = behavior.OnPreFlingAsync(300);
        Frames(clock, window, 16, 1008);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => release);
        Assert.Equal(1, indicator.State!.DistanceFraction);
        Assert.Equal(80, behavior.DistancePulled); // upstream finally uses threshold, not twice threshold.
        Assert.Equal(0, behavior.OnPreScroll(-40));
        Assert.Equal(0, behavior.OnPostScroll(100));
        Assert.Equal(0, await behavior.OnPreFlingAsync(300));
        indicator.IsRefreshing = false;
        Frames(clock, window, 1024, 2000);
        Assert.Equal(0, indicator.State.DistanceFraction);
        Assert.Equal(0, behavior.DistancePulled);
        window.Close();
    }

    [AvaloniaFact]
    public async Task BehaviorReplacementCancelsReleaseEvenWhenStateHasAnotherConsumer()
    {
        using var clock = new MotionTestClockScope();
        var (window, indicator, behavior) = Show(); clock.SendFrame(TimeSpan.Zero);
        var peer = new PullToRefreshLoadingIndicator { State = indicator.State };
        window.Content = null; Flush(window);
        window.Content = new StackPanel { Children = { indicator, peer } }; Flush(window);
        behavior.OnPostScroll(120); Flush(window);
        var release = behavior.OnPreFlingAsync(300);
        Frames(clock, window, 16, 48);
        indicator.ScrollBehavior = new(); Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => release);
        Assert.Throws<InvalidOperationException>(() => behavior.OnPostScroll(10));
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(0, indicator.State!.DistanceFraction);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void RejectedSecondOwnerCannotDetachTheOriginalBehavior()
    {
        using var clock = new MotionTestClockScope();
        var (window, indicator, behavior) = Show();
        var other = new PullToRefreshIndicator();
        Assert.Throws<InvalidOperationException>(() => behavior.Attach(other));
        behavior.Detach(other);
        Assert.Equal(80, behavior.OnPostScroll(80)); Flush(window);
        Assert.Equal(.5f, indicator.State!.DistanceFraction);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    private static (MotionTestWindow, PullToRefreshIndicator, PullToRefreshScrollBehavior) Show()
    {
        var behavior = new PullToRefreshScrollBehavior();
        var indicator = new PullToRefreshIndicator { State = new(2), ScrollBehavior = behavior };
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = indicator; window.Show(); Flush(window);
        return (window, indicator, behavior);
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    {
        for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
    }
}
