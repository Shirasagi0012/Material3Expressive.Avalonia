using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>
/// O5 characterization: which scope drives <see cref="Animated"/> property runs (the
/// <see cref="AnimatedHostCoordinator"/>'s scope), seen from ancestor scopes and reparenting.
/// </summary>
public sealed class CoordinatorScopeTest
{
    // The ancestors own Get scopes before the host arrives, so a host Get scope parents to Left's.
    private static ReparentFixture Scene(IFiniteAnimationSpec<double> spec)
    {
        var border = new Border { Width = 40, Height = 40, Background = Brushes.Red };
        Animated.SetProperties(border, new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Visual.OpacityProperty, Spec = spec }
        });
        return new ReparentFixture(border, addContentAfterSlotScopes: true);
    }

    private static TweenSpec<double> Linear(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    [Fact]
    public Task AnimatedProperties_AncestorGetScopeDurationScale_ScalesRuns() => OnUi(() =>
    {
        using var s = Scene(Linear(160));
        AnimationScope.Get(s.Left).DurationScale = 2;
        s.Content.Opacity = 0;
        s.Step(0); s.Step(80);
        // O5: the coordinator runs in the host's Get scope, a child of Left's, so 80 ms at
        // scale 2 is a quarter of the 160 ms tween.
        Assert.Equal(0.75, s.Content.Opacity, 6);
    });

    [Fact]
    public Task AnimatedProperties_ReparentedMidSpring_KeepsVelocity() => OnUi(() =>
    {
        var still = SpringRun(move: false);
        var moved = SpringRun(move: true);
        Assert.True(moved[2] - moved[3] > 0.01);
        // D17: the run moves into the rebuilt scope like an in-place interruption: no hold, no
        // jump, velocity kept; the trajectory matches the run that was never moved.
        for (var i = 0; i < moved.Count; i++) Assert.Equal(still[i], moved[i], 6);
    });

    [Fact]
    public Task AnimatedProperties_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        using var s = Scene(Linear(160));
        s.Content.Opacity = 0;
        s.Step(0); s.Step(48);
        s.Detach();
        Assert.True(s.Content.Opacity > 0); // kept until the drain
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, s.Content.Opacity);
        Assert.False(s.Content.IsAnimating(Visual.OpacityProperty));
    });

    [Fact]
    public Task AnimatedProperties_OnAnAnimationScopeHost_EachResumesOnce() => OnUi(() =>
    {
        // AnimatedContentSize is an IAnimationScopeHost; its own D17 reaction and the
        // coordinator's share the Get scope and each re-runs only its own state.
        var frames = new ManualFrameSource();
        var left = new Decorator(); var right = new Decorator();
        var window = new AnimationTestWindow(frames) { Width = 200, Height = 200, Content = new StackPanel { Children = { left, right } } };
        window.Show(); Dispatcher.UIThread.RunJobs();
        AnimationScope.Get(left); AnimationScope.Get(right);
        var host = new AnimatedContentSize { Content = new Border { Width = 20, Height = 20 } };
        Animated.SetProperties(host, new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Visual.OpacityProperty, Spec = Linear(160) }
        });
        left.Child = host;
        Dispatcher.UIThread.RunJobs();
        var scope = AnimationScope.Get(host);
        var changes = 0;
        AnimationScope.AddScopeChangedHandler(host, (_, _) => changes++);
        host.Opacity = 0;
        void Step(int ms) { Dispatcher.UIThread.RunJobs(); frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Dispatcher.UIThread.RunJobs(); }
        Step(0); Step(48);
        var before = host.Opacity;
        left.Child = null; right.Child = host;
        Step(16);
        Assert.Equal(1, changes);
        Assert.NotSame(scope, AnimationScope.Get(host));
        // One frame of the resumed run: a tween re-run restarts its duration from the current
        // value (upstream animateTo semantics), 0.7 -> 0 over 160 ms.
        Assert.Equal(before * (1 - 16.0 / 160), host.Opacity, 6);
        Assert.Equal(1, AnimationScope.Get(host).Diagnostics.PendingAnimationCount);
        for (var i = 0; i < 20; i++) Step(16);
        Assert.Equal(0, host.Opacity);
        window.Close();
    });

    private static List<double> SpringRun(bool move)
    {
        using var s = Scene(new SpringSpec<double>(Spring.DampingRatioNoBouncy, Spring.StiffnessLow));
        s.Content.Opacity = 0;
        var samples = new List<double>();
        s.Step(0);
        for (var i = 0; i < 8; i++)
        {
            if (i == 4 && move) s.Move();
            s.Step(16);
            samples.Add(s.Content.Opacity);
        }
        return samples;
    }

    [Fact]
    public Task AnimatedProperties_AncestorChildScopeDisposed_RunsContinue() => OnUi(() =>
    {
        using var s = Scene(Linear(160));
        var child = AnimationScope.Get(s.Left).CreateChild();
        s.Content.Opacity = 0;
        s.Step(0); s.Step(16);
        child.Dispose();
        s.Step(16);
        Assert.Equal(0.8, s.Content.Opacity, 6);
        Assert.True(s.Content.IsAnimating(Visual.OpacityProperty));
    });
}
