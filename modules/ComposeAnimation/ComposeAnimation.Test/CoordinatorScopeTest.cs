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
    private sealed class Scene : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public Decorator Left { get; } = new();
        public Decorator Right { get; } = new();
        public Border Border { get; }
        public Window Window { get; }
        public Scene(IFiniteAnimationSpec<double> spec)
        {
            Border = new Border { Width = 40, Height = 40, Background = Brushes.Red };
            Animated.SetProperties(Border, new AnimatedPropertyCollection
            {
                new AnimatedDouble { Property = Visual.OpacityProperty, Spec = spec }
            });
            Window = new AnimationTestWindow(Frames) { Width = 200, Height = 200, Content = new StackPanel { Children = { Left, Right } } };
            Window.Show();
            Dispatcher.UIThread.RunJobs();
            // The ancestors own Get scopes before the host arrives, so a host Get scope parents to Left's.
            AnimationScope.Get(Left); AnimationScope.Get(Right);
            Left.Child = Border;
            Dispatcher.UIThread.RunJobs();
        }
        public void Step(int ms) { Dispatcher.UIThread.RunJobs(); Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Dispatcher.UIThread.RunJobs(); }
        public void Move() { Left.Child = null; Right.Child = Border; }
        public void Dispose() => Window.Close();
    }

    private static TweenSpec<double> Linear(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    [Fact]
    public Task AncestorGetScopeDurationScale_ScalesPropertyRuns() => OnUi(() =>
    {
        using var s = new Scene(Linear(160));
        AnimationScope.Get(s.Left).DurationScale = 2;
        s.Border.Opacity = 0;
        s.Step(0); s.Step(80);
        // O5: the coordinator runs in the host's Get scope, a child of Left's, so 80 ms at
        // scale 2 is a quarter of the 160 ms tween.
        Assert.Equal(0.75, s.Border.Opacity, 6);
    });

    [Fact]
    public Task Reparent_MidSpring_ResumesWithVelocity() => OnUi(() =>
    {
        var still = SpringRun(move: false);
        var moved = SpringRun(move: true);
        Assert.True(moved[2] - moved[3] > 0.01);
        // D17: the run moves into the rebuilt scope like an in-place interruption: no hold, no
        // jump, velocity kept; the trajectory matches the run that was never moved.
        for (var i = 0; i < moved.Count; i++) Assert.Equal(still[i], moved[i], 6);
    });

    [Fact]
    public Task DetachWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        using var s = new Scene(Linear(160));
        s.Border.Opacity = 0;
        s.Step(0); s.Step(48);
        s.Left.Child = null;
        Assert.True(s.Border.Opacity > 0); // kept until the drain
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, s.Border.Opacity);
        Assert.False(s.Border.IsAnimating(Visual.OpacityProperty));
    });

    [Fact]
    public Task AnimationScopeHost_WithProperties_EachResumesOnce() => OnUi(() =>
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
        using var s = new Scene(new SpringSpec<double>(Spring.DampingRatioNoBouncy, Spring.StiffnessLow));
        s.Border.Opacity = 0;
        var samples = new List<double>();
        s.Step(0);
        for (var i = 0; i < 8; i++)
        {
            if (i == 4 && move) s.Move();
            s.Step(16);
            samples.Add(s.Border.Opacity);
        }
        return samples;
    }

    [Fact]
    public Task AncestorCreateChildDisposal_DoesNotCancelPropertyRuns() => OnUi(() =>
    {
        using var s = new Scene(Linear(160));
        var child = AnimationScope.Get(s.Left).CreateChild();
        s.Border.Opacity = 0;
        s.Step(0); s.Step(16);
        child.Dispose();
        s.Step(16);
        Assert.Equal(0.8, s.Border.Opacity, 6);
        Assert.True(s.Border.IsAnimating(Visual.OpacityProperty));
    });
}
