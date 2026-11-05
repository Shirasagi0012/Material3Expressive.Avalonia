using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class PullToRefreshTests
{
    [AvaloniaFact]
    public async Task DerivedMotionPreservesOvershootReverseSnapAndCancellation()
    {
        using var clock = new MotionTestClockScope();
        PullToRefreshState state = new BouncyState();
        var indicator = new PullToRefreshIndicator { State = state };
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        var expand = state.AnimateToThresholdAsync();
        Frames(clock, window, 16, 144);
        Assert.True(state.DistanceFraction > 1); // Same high-bouncy spring as the pinned sample.
        Assert.True(state.IsAnimating);
        var beforeReverse = state.DistanceFraction;
        var hide = state.AnimateToHiddenAsync();
        Assert.Equal(beforeReverse, state.DistanceFraction);
        Frames(clock, window, 160, 192);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => expand);
        await state.SnapToAsync(1.5f); Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hide);
        Assert.Equal(1.5f, state.DistanceFraction);
        Assert.False(state.IsAnimating);
        Assert.Equal(80, Layout(indicator).TranslationY);
        using var cancellation = new CancellationTokenSource();
        var pending = state.AnimateToHiddenAsync(cancellation.Token);
        Frames(clock, window, 208, 240);
        cancellation.Cancel(); Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        var retained = state.DistanceFraction;
        Frames(clock, window, 256, 640);
        Assert.Equal(retained, state.DistanceFraction);
        Assert.False(state.IsAnimating);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task DerivedMotionUsesSharedConsumerLifetimeAndCanResumeAfterRemount()
    {
        using var clock = new MotionTestClockScope();
        var state = new BouncyState();
        var first = new PullToRefreshIndicator { State = state };
        var second = new PullToRefreshLoadingIndicator { State = state };
        var panel = new StackPanel { Children = { first, second } };
        var window = Show(panel); clock.SendFrame(TimeSpan.Zero);
        var pending = state.AnimateToThresholdAsync();
        Frames(clock, window, 16, 48);
        panel.Children.Remove(first); Flush(window);
        Assert.True(state.IsAnimating);
        Frames(clock, window, 64, 80);
        panel.Children.Remove(second); Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(state.IsAnimating);
        Assert.Equal(0, clock.PendingFrameCount);
        var retained = state.DistanceFraction;
        panel.Children.Add(first); Flush(window);
        Assert.Equal(retained, state.DistanceFraction);
        var finish = state.AnimateToHiddenAsync();
        Frames(clock, window, 96, 800); await finish;
        Assert.Equal(0, state.DistanceFraction);
        window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public async Task ScrollBehaviorDispatchesToDerivedStateOperations()
    {
        using var clock = new MotionTestClockScope();
        var state = new BouncyState();
        var behavior = new PullToRefreshScrollBehavior();
        var indicator = new PullToRefreshIndicator { State = state, ScrollBehavior = behavior };
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        Assert.Equal(1, state.Snaps);
        behavior.OnPostScroll(200); Flush(window);
        Assert.Equal(2, state.Snaps);
        var release = behavior.OnPreFlingAsync(200);
        Frames(clock, window, 16, 320);
        Assert.Equal(200, await release); Assert.Equal(1, state.Hides);
        indicator.IsRefreshing = true;
        Frames(clock, window, 336, 480);
        Assert.Equal(1, state.Expands); Assert.True(state.DistanceFraction > 1);
        Assert.Equal(0, behavior.OnPostScroll(80));
        indicator.IsRefreshing = false;
        Frames(clock, window, 496, 1008);
        Assert.Equal(2, state.Hides); Assert.Equal(0, state.DistanceFraction);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    private sealed class BouncyState : PullToRefreshState
    {
        public int Expands { get; private set; }
        public int Hides { get; private set; }
        public int Snaps { get; private set; }
        public override Task AnimateToThresholdAsync(CancellationToken cancellationToken = default)
        {
            Expands++;
            return AnimateToAsync(1, new SpringSpec<float>(.2f), cancellationToken);
        }
        public override Task AnimateToHiddenAsync(CancellationToken cancellationToken = default)
        {
            Hides++;
            return AnimateToAsync(0, new TweenSpec<float>(TimeSpan.FromMilliseconds(240)), cancellationToken);
        }
        public override Task SnapToAsync(float distanceFraction, CancellationToken cancellationToken = default)
        {
            Snaps++;
            return base.SnapToAsync(distanceFraction, cancellationToken);
        }
    }

    [AvaloniaFact]
    public async Task StateCanBeSeededWithoutMountingAndDoesNotClampOvershoot()
    {
        var state = new PullToRefreshState();
        Assert.Equal(0, state.DistanceFraction);
        await state.SnapToAsync(2.25f);
        Assert.Equal(2.25f, state.DistanceFraction);
        Assert.False(state.IsAnimating);
        await Assert.ThrowsAsync<InvalidOperationException>(() => state.AnimateToThresholdAsync());
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.SnapToAsync(.5f, canceled.Token));
        Assert.Equal(2.25f, state.DistanceFraction);
    }

    [AvaloniaTheory]
    [InlineData(false, 0, 40, -40, 0)]
    [InlineData(false, .5, 40, 0, 3)]
    [InlineData(false, 1, 40, 40, 3)]
    [InlineData(false, 1.5, 40, 80, 3)]
    [InlineData(true, 0, 48, -48, 0)]
    [InlineData(true, 1, 48, 32, 0)]
    public void ContainerPreservesLayoutAllocationAndMovesOnlyItsLayer(bool loading, float fraction,
        double size, double offset, double elevation)
    {
        var indicator = Create(loading, fraction);
        var window = Show(indicator);
        Assert.False(indicator.ClipToBounds, "owner clip");
        Assert.False(Layout(indicator).ClipToBounds, "layout clip");
        Assert.False(Surface(indicator).ClipToBounds, "surface clip");
        Assert.Equal(new Size(size, size), indicator.Bounds.Size);
        Assert.Equal(offset, Layout(indicator).TranslationY);
        Assert.Equal(elevation, Surface(indicator).ShadowElevation);
        Assert.False(Surface(indicator).ApplyTonalElevation);
        if (loading) Assert.Single(indicator.GetVisualDescendants().OfType<ContainedLoadingIndicator>());
        else Assert.Single(indicator.GetVisualDescendants().OfType<PullToRefreshArrow>());
        window.Close();
    }

    [AvaloniaFact]
    public async Task AnimationReversesFromCurrentValueAndDetachCancelsWithoutResettingState()
    {
        using var clock = new MotionTestClockScope();
        var indicator = Create(false, 0);
        var state = indicator.State!;
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        var expand = state.AnimateToThresholdAsync();
        Frames(clock, window, 16, 80);
        var partial = state.DistanceFraction;
        Assert.InRange(partial, .01f, .99f);
        var hide = state.AnimateToHiddenAsync();
        Assert.Equal(partial, state.DistanceFraction);
        Frames(clock, window, 96, 128);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => expand);
        window.Content = null; Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hide);
        var retained = state.DistanceFraction;
        Assert.False(state.IsAnimating);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = indicator; Flush(window);
        Assert.Equal(retained, state.DistanceFraction);
        var finish = state.AnimateToThresholdAsync();
        Frames(clock, window, 144, 1200); await finish;
        Assert.Equal(1, state.DistanceFraction);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task StateReplacementReleasesOldAnimationAndSharedConsumerKeepsItAlive()
    {
        using var clock = new MotionTestClockScope();
        var state = new PullToRefreshState(.5f);
        var first = new PullToRefreshIndicator { State = state };
        var second = new PullToRefreshLoadingIndicator { State = state };
        var panel = new StackPanel { Children = { first, second } };
        var window = Show(panel); clock.SendFrame(TimeSpan.Zero);
        var pending = state.AnimateToThresholdAsync();
        Frames(clock, window, 16, 48);
        first.State = new PullToRefreshState(2); Flush(window);
        Assert.Equal(120, Layout(first).TranslationY);
        Assert.True(state.IsAnimating);
        panel.Children.Remove(second); Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(state.IsAnimating);
        await state.SnapToAsync(0); Flush(window);
        Assert.Equal(120, Layout(first).TranslationY);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task RefreshCrossfadeRetainsOutgoingArrowAndCleansContinuousAnimation()
    {
        using var clock = new MotionTestClockScope();
        var indicator = Create(false, 1);
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        indicator.IsRefreshing = true; Flush(window);
        Frames(clock, window, 16, 64);
        Assert.Single(indicator.GetVisualDescendants().OfType<CircularProgressIndicator>());
        Assert.Single(indicator.GetVisualDescendants().OfType<PullToRefreshArrow>());
        indicator.IsRefreshing = false; Flush(window);
        Frames(clock, window, 80, 800);
        Assert.Empty(indicator.GetVisualDescendants().OfType<CircularProgressIndicator>());
        Assert.Single(indicator.GetVisualDescendants().OfType<PullToRefreshArrow>());
        await indicator.State!.SnapToAsync(.75f); Flush(window);
        Frames(clock, window, 816, 1200);
        Assert.Equal(.3f, indicator.GetVisualDescendants().OfType<PullToRefreshArrow>().Single().Alpha);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task ArrowAlphaStartsAtTargetAndRepeatedAboveThresholdUpdatesDoNotRestartIt()
    {
        using var clock = new MotionTestClockScope();
        var indicator = Create(false, .8f);
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        var arrow = indicator.GetVisualDescendants().OfType<PullToRefreshArrow>().Single();
        Assert.Equal(.3f, arrow.Alpha);
        await indicator.State!.SnapToAsync(1.1f); Flush(window);
        Frames(clock, window, 16, 64);
        var intermediate = arrow.Alpha;
        Assert.InRange(intermediate, .31f, .99f);
        await indicator.State.SnapToAsync(1.2f); Flush(window);
        Assert.Equal(intermediate, arrow.Alpha);
        Frames(clock, window, 80, 640);
        Assert.Equal(1, arrow.Alpha);
        var provider = Assert.IsAssignableFrom<IRangeValueProvider>(ControlAutomationPeer.CreatePeerForElement(arrow)!.GetProvider<IRangeValueProvider>());
        Assert.Equal((double)1.2f, provider.Value);
        await indicator.State.SnapToAsync(0); Flush(window);
        Assert.Null(ControlAutomationPeer.CreatePeerForElement(arrow)!.GetProvider<IRangeValueProvider>());
        window.Close();
    }

    [AvaloniaFact]
    public async Task ArrowAlphaConflatesTargetsBeforeTheNextCompositionFrame()
    {
        using var clock = new MotionTestClockScope();
        var indicator = Create(false, .8f);
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        var arrow = indicator.GetVisualDescendants().OfType<PullToRefreshArrow>().Single();
        await indicator.State!.SnapToAsync(1.2f); Flush(window);
        await indicator.State.SnapToAsync(.7f); Flush(window);
        Assert.Equal(.3f, arrow.Alpha);
        Frames(clock, window, 16, 64);
        Assert.Equal(.3f, arrow.Alpha);
        Assert.Equal(0, clock.PendingFrameCount);
        await indicator.State.SnapToAsync(1.2f); Flush(window);
        await indicator.State.SnapToAsync(.7f); Flush(window);
        await indicator.State.SnapToAsync(1.1f); Flush(window);
        Assert.Equal(.3f, arrow.Alpha);
        Frames(clock, window, 80, 720);
        Assert.Equal(1, arrow.Alpha);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void CustomContentAndDynamicTokensRetainTheirBindingsAndLayout()
    {
        var marker = new Border { Width = 12, Height = 12, Background = Brushes.Red };
        var indicator = new PullToRefreshIndicatorBox { State = new(1), Content = marker };
        var window = Show(indicator);
        Assert.Same(indicator, marker.Parent);
        Assert.Equal(40, indicator.Bounds.Width);
        indicator.Resources[Comp.PullToRefresh.ContainerSize] = 60d;
        indicator.Resources[Comp.PullToRefresh.MaxDistance] = 100d;
        indicator.Resources[Comp.PullToRefresh.Elevation] = 5d;
        Flush(window);
        Assert.Equal(new Size(60, 60), indicator.Bounds.Size);
        Assert.Equal(40, Layout(indicator).TranslationY);
        Assert.Equal(5, Surface(indicator).ShadowElevation);
        Assert.Same(marker, indicator.GetVisualDescendants().OfType<Border>().Single(b => ReferenceEquals(b, marker)));
        indicator.MaxWidth = 30; Flush(window);
        Assert.Equal(30, indicator.Bounds.Width);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshingDetachDisposesOwnedCrossfadeBeforeItsScopeAndRestartsCleanly(bool loading)
    {
        using var clock = new MotionTestClockScope();
        var indicator = Create(loading, 1); indicator.IsRefreshing = true;
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero);
        Frames(clock, window, 16, 160);
        window.Content = null; Flush(window);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = indicator; Flush(window);
        Frames(clock, window, 176, 400);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.True(clock.PendingFrameCount > 0);
        window.Close();
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ConstrainedLoadingKeepsItsRequiredCanvasCenteredInsideTheClippedContainer()
    {
        var indicator = Create(true, .65f);
        var window = Show(new Grid { Width = 24, Height = 24, Children = { indicator } });
        var loading = indicator.GetVisualDescendants().OfType<ContainedLoadingIndicator>().Single();
        Assert.Equal(new Size(24, 24), indicator.Bounds.Size);
        Assert.Equal(new Size(48, 48), loading.Bounds.Size);
        Assert.Equal(new Point(-12, -12), loading.Bounds.Position);
        window.Close();
    }

    private static PullToRefreshIndicator Create(bool loading, float fraction) => loading
        ? new PullToRefreshLoadingIndicator { State = new(fraction) }
        : new PullToRefreshIndicator { State = new(fraction) };
    private static PullToRefreshIndicatorLayout Layout(Control control) => control.GetVisualDescendants().OfType<PullToRefreshIndicatorLayout>().Single();
    private static MaterialSurface Surface(Control control) => control.GetVisualDescendants().OfType<MaterialSurface>().Single();
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 300 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    {
        for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
    }
}
