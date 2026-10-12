using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimationLoopSchedulingTest
{
    private static ContentControl CreateHost(bool content) => content
        ? new AnimatedContent
        {
            Content = Card(),
            ContentTransform = new FadeIn { Spec = Tween() }.TogetherWith(new FadeOut { Spec = Tween() })
        }
        : new AnimatedVisibility
        {
            Content = Card(), Enter = new FadeIn { Spec = Tween() }, Exit = new FadeOut { Spec = Tween() }
        };

    private static Border Card() => new() { Width = 100, Height = 80 };
    private static TweenSpec<double> Tween() => new(TimeSpan.FromMilliseconds(100), easing: new LinearEasing());
    private static bool IsIdle(ContentControl host) => host is AnimatedVisibility visibility
        ? visibility.IsIdle : ((AnimatedContent)host).IsIdle;
    private static void ChangeTarget(ContentControl host)
    {
        if (host is AnimatedVisibility visibility) visibility.Visible = !visibility.Visible;
        else host.Content = Card();
    }

    private static void PumpUntil(Window window, Func<bool> completed, DispatcherPriority? priority = null)
    {
        Assert.True(SpinWait.SpinUntil(() =>
        {
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs(priority);
            return completed();
        }, TimeSpan.FromSeconds(5)), "The animation loop did not resume at the permitted dispatcher priority.");
    }

    private static void Initialize(Window window, ContentControl host, ManualAnimationFrameClock clock)
    {
        window.Show();
        for (var i = 0; i < 20; i++)
        {
            PumpUntil(window, () => clock.PendingFrameCount > 0 || IsIdle(host));
            if (clock.PendingFrameCount == 0 && IsIdle(host)) return;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(25));
        }
        Assert.Fail("Initial animation work did not settle.");
    }

    [Fact]
    public Task IdleTransition_RegistersNextFrameBeforeOwningMutationReturns() =>
        HeadlessApp.Session.Dispatch(() =>
        {
            using var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
            using var transition = new Transition<bool>(false, clock);
            var value = transition.AnimateDouble(visible => visible ? 1 : 0, _ => Tween());
            var loop = transition.RunAsync();
            try
            {
                Assert.Equal(0, clock.PendingFrameCount);
                for (var cycle = 0; cycle < 4; cycle++)
                {
                    var target = cycle % 2 == 0;
                    transition.UpdateTarget(target);
                    // The next clock batch must include this loop without waiting for a
                    // thread-pool continuation or a subsequent dispatcher pump.
                    Assert.Equal(1, clock.PendingFrameCount);
                    clock.AdvanceBy(TimeSpan.Zero);
                    Dispatcher.UIThread.RunJobs();
                    clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
                    Dispatcher.UIThread.RunJobs();
                    Assert.Equal(target ? 1 : 0, value.Value);
                    Assert.Equal(0, clock.PendingFrameCount);
                    Assert.False(transition.IsRunning);
                    Assert.False(loop.IsCompleted);
                }
            }
            finally
            {
                transition.Dispose();
                Dispatcher.UIThread.RunJobs();
            }
            Assert.True(loop.IsCanceled);
        }, CancellationToken.None);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InitializedLoop_WakesAndCompletesWithOnlyRenderOrHigherJobs(bool content) =>
        HeadlessApp.Session.Dispatch(() =>
        {
            var host = CreateHost(content);
            var clock = new ManualAnimationFrameClock();

            var window = new AnimationTestWindow(clock) { Content = host, Width = 500, Height = 500 };
            try
            {
                Initialize(window, host, clock);
                var loadedRan = false;
                Dispatcher.UIThread.Post(() => loadedRan = true, DispatcherPriority.Loaded);
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    ChangeTarget(host);
                    PumpUntil(window, () => clock.PendingFrameCount == 1, DispatcherPriority.Render);
                    // Deliver from Render so the captured loop context must schedule its continuation.
                    for (var frame = 0; frame < 10; frame++)
                    {
                        Dispatcher.UIThread.Post(() => clock.AdvanceBy(TimeSpan.FromMilliseconds(25)), DispatcherPriority.Render);
                        PumpUntil(window, () => IsIdle(host) || clock.PendingFrameCount == 1, DispatcherPriority.Render);
                        if (frame == 1) Assert.False(IsIdle(host));
                        if (IsIdle(host) && clock.PendingFrameCount == 0) break;
                    }
                    Assert.True(IsIdle(host));
                    Assert.Equal(0, clock.PendingFrameCount);
                    Assert.False(loadedRan);
                    Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(host)!).AnimationRoot.Diagnostics.LastError);
                }
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }, CancellationToken.None);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task QueuedStartup_UsesOnlyTheCurrentConnection(bool content, bool detach) =>
        HeadlessApp.Session.Dispatch(() =>
        {
            var host = CreateHost(content);
            var clock = new ManualAnimationFrameClock();

            var window = new AnimationTestWindow(clock) { Content = host, Width = 500, Height = 500 };
            try
            {
                window.Show();
                if (detach) window.Content = null;

                Dispatcher.UIThread.RunJobs();
                if (detach) window.Content = host;
                Initialize(window, host, clock);
                ChangeTarget(host);
                PumpUntil(window, () => clock.PendingFrameCount == 1);
                Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(host)!).AnimationRoot.Diagnostics.LastError);
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal(0, clock.PendingFrameCount);
        }, CancellationToken.None);
}
