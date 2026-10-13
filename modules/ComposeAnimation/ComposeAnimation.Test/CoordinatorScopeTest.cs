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
    public Task AncestorGetScopeDurationScale_DoesNotReachPropertyRuns() => OnUi(() =>
    {
        using var s = new Scene(Linear(160));
        AnimationScope.Get(s.Left).DurationScale = 2;
        s.Border.Opacity = 0;
        s.Step(0); s.Step(80);
        // CreateDetached: the coordinator's scope is parented to the window root, so an
        // ancestor's Get scope DurationScale is ignored (80 ms is half of the 160 ms tween).
        Assert.Equal(0.5, s.Border.Opacity, 6);
    });

    [Fact]
    public Task Reparent_MidSpring_SnapsToTheTarget() => OnUi(() =>
    {
        using var s = new Scene(new SpringSpec<double>(Spring.DampingRatioNoBouncy, Spring.StiffnessLow));
        s.Border.Opacity = 0;
        var samples = new List<double>();
        s.Step(0);
        for (var i = 0; i < 8; i++)
        {
            if (i == 4) s.Move();
            s.Step(16);
            samples.Add(s.Border.Opacity);
        }
        Assert.InRange(samples[3], 0.1, 0.9);
        // CreateDetached: detach ends the coordinator's scope and attach creates a new one; the
        // run is dropped and the property shows its target at the next frame.
        Assert.Equal(0, samples[4], 6);
        Assert.False(s.Border.IsAnimating(Visual.OpacityProperty));
    });

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
