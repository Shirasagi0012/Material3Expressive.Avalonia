using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

public sealed class CoroutineDelayTest
{
    static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public Task Delay_UsesElapsedTimeWithoutFramesOrDurationScaling(double scale) => OnUi(async () =>
    {
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        using var frames = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(frames, time);
        window.AnimationRoot.DurationScale = scale;
        var child = window.AnimationRoot.CreateChild();
        Assert.Same(time, child.TimeProvider);
        var continued = false;
        var task = child.Launch(async ct =>
        {
            await child.DelayAsync(TimeSpan.FromMilliseconds(650), ct);
            Dispatcher.UIThread.VerifyAccess(); continued = true;
        });
        time.AdvanceBy(TimeSpan.FromMilliseconds(649)); Dispatcher.UIThread.RunJobs();
        Assert.False(continued); Assert.False(task.IsCompleted); Assert.Equal(0, frames.PendingFrameCount);
        time.AdvanceBy(TimeSpan.FromMilliseconds(1)); Dispatcher.UIThread.RunJobs();
        await task;
        Assert.True(continued); Assert.Equal(0, time.ActiveTimerCount);
        await child.Diagnostics.EndAsync(); window.Close(); Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    });

    [Fact]
    public Task Delay_CallerCancellationAndScopeDisposalReleaseTimers() => OnUi(async () =>
    {
        var time = new ManualTimeProvider();
        var window = new AnimationTestWindow(new ManualAnimationFrameClock(), time);
        var child = window.AnimationRoot.CreateChild();
        using var cancel = new CancellationTokenSource();
        var a = child.DelayAsync(TimeSpan.FromSeconds(1), cancel.Token);
        var b = child.DelayAsync(Timeout.InfiniteTimeSpan);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => a);
        child.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b);
        await child.Diagnostics.EndAsync();
        Assert.Equal(0, time.ActiveTimerCount); Assert.Equal(0, child.Diagnostics.ActiveTaskCount);
        Assert.Throws<ObjectDisposedException>(() => { _ = child.DelayAsync(TimeSpan.Zero); });
        var rootWait = window.AnimationRoot.DelayAsync(TimeSpan.FromSeconds(1));
        window.Close();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rootWait);
        await window.AnimationRoot.Diagnostics.EndAsync(); Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    });

    [Fact]
    public Task Delay_DetachingOwnerCancelsOnlyItsScope() => OnUi(async () =>
    {
        var time = new ManualTimeProvider();
        var owner = new Border(); var content = new Grid { Children = { owner } };
        var window = new AnimationTestWindow(null, time) { Content = content }; window.Show();
        var child = AnimationScope.Get(owner);
        var waiting = child.DelayAsync(TimeSpan.FromSeconds(1));
        var sibling = window.AnimationRoot.DelayAsync(TimeSpan.FromSeconds(1));
        content.Children.Remove(owner);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        await child.Diagnostics.EndAsync(); Assert.False(sibling.IsCompleted);
        time.AdvanceBy(TimeSpan.FromSeconds(1)); Dispatcher.UIThread.RunJobs(); await sibling;
        window.Close(); Assert.Equal(0, time.ActiveTimerCount);
    });

    [Fact]
    public Task Delay_CancellationWinsWhileCompletedTimerContinuationIsQueued() => OnUi(async () =>
    {
        var time = new ManualTimeProvider();
        var window = new AnimationTestWindow(null, time);
        using var cancel = new CancellationTokenSource();
        var waiting = window.AnimationRoot.DelayAsync(TimeSpan.FromMilliseconds(650), cancel.Token);
        // Complete the timer off-thread without pumping the captured UI continuation.
        Task.Run(() => time.AdvanceBy(TimeSpan.FromMilliseconds(650))).GetAwaiter().GetResult();
        cancel.Cancel(); Dispatcher.UIThread.RunJobs();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        window.Close(); Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Popup_InheritsDelayTimeAndReleasesWaitOnClose(bool overlay) => OnUi(async () =>
    {
        var time = new ManualTimeProvider();
        var anchor = new Border { Width = 40, Height = 40 };
        var panel = new StackPanel { Children = { anchor } };
        var window = new AnimationTestWindow(null, time) { Content = panel };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var content = new Border { Width = 30, Height = 30 };
        var popup = new Popup { PlacementTarget = anchor, Child = content, ShouldUseOverlayLayer = overlay };
        panel.Children.Add(popup); popup.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var scope = AnimationScope.Get(content); Assert.Same(time, scope.TimeProvider);
        var waiting = scope.DelayAsync(TimeSpan.FromSeconds(1));
        popup.IsOpen = false; Dispatcher.UIThread.RunJobs();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        await scope.Diagnostics.EndAsync(); Assert.Equal(0, time.ActiveTimerCount);
        window.Close(); Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    });

    [Fact]
    public void ManualTime_FiresChronologicallyAndSupportsChangeDisposeAndPeriodicTimers()
    {
        var time = new ManualTimeProvider(); var events = new List<double>();
        using var periodic = time.CreateTimer(_ => events.Add(time.Elapsed.TotalMilliseconds), null,
            TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        using var once = time.CreateTimer(_ => events.Add(time.Elapsed.TotalMilliseconds), null,
            TimeSpan.FromMilliseconds(150), Timeout.InfiniteTimeSpan);
        time.AdvanceBy(TimeSpan.FromMilliseconds(250));
        Assert.Equal(new double[] { 100, 150, 200 }, events);
        periodic.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        time.AdvanceBy(TimeSpan.FromMilliseconds(100)); Assert.Equal(3, events.Count);
        once.Dispose(); Assert.False(once.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => time.AdvanceBy(TimeSpan.FromTicks(-1)));
        Assert.Equal(0, time.ActiveTimerCount);
    }
}
