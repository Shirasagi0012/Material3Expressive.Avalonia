using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedTransitionHostTest
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static readonly Rect Start = new(10, 20, 100, 80);
    private static readonly Rect End = new(250, 100, 200, 160);

    private sealed class Scene : IDisposable
    {
        internal ManualFrameSource Clock { get; } = new();
        internal Transition<bool> Parent { get; }
        internal SharedTransitionHost Host { get; }
        internal SharedElement From { get; }
        internal SharedElement To { get; }
        internal SharedBounds? FromBounds { get; }
        internal SharedBounds? ToBounds { get; }
        internal AnimatedVisibility Source { get; }
        internal AnimatedVisibility Target { get; }
        internal Window Window { get; }
        internal Canvas Stage { get; }

        internal Scene(bool bounds = false, bool nested = false)
        {
            Parent = new Transition<bool>(true);
            var boundsSpec = new SharedBoundsTransform
            {
                Spec = new TweenSpec<Rect>(Ms(100), easing: new LinearEasing())
            };
            Host = new SharedTransitionHost
            {
                Transition = Parent,
                PreparationTimeout = Timeout.InfiniteTimeSpan,
                Width = 800, Height = 600
            };
            if (bounds)
            {
                FromBounds = new SharedBounds { Key = "photo", Content = Block(Brushes.Blue, 100, 80), BoundsTransform = boundsSpec };
                ToBounds = new SharedBounds { Key = "photo", Content = Block(Brushes.Red, 200, 160), BoundsTransform = boundsSpec };
                FromBounds.FadeSpec = new TweenSpec<double>(Ms(200), easing: new LinearEasing());
                ToBounds.FadeSpec = FromBounds.FadeSpec;
                Source = Visibility(FromBounds);
                Target = Visibility(ToBounds);
                From = null!; To = null!;
            }
            else
            {
                From = nested ? Nested("photo", Brushes.Blue, 100, 80, child: true) : Element("photo", Brushes.Blue, 100, 80);
                To = nested ? Nested("photo", Brushes.Red, 200, 160, child: false) : Element("photo", Brushes.Red, 200, 160);
                From.BoundsTransform = To.BoundsTransform = boundsSpec;
                Source = Visibility(From);
                Target = Visibility(To);
            }
            Source.Source = VisibilitySource.From(Parent, s => s);
            Target.Source = VisibilitySource.From(Parent, s => !s);


            Stage = new Canvas();
            Canvas.SetLeft(Source, 10); Canvas.SetTop(Source, 20);
            Canvas.SetLeft(Target, 250); Canvas.SetTop(Target, 100);
            Stage.Children.Add(Source); Stage.Children.Add(Target);
            Host.Content = Stage;
            Window = new AnimationTestWindow(Clock) { Width = 800, Height = 600, Content = Host };
            Window.Show();
            Layout();
            Parent.OnFrame(TimeSpan.Zero);
        }

        internal void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        internal SharedTransitionMotion Start(object? key = null)
        {
            Parent.UpdateTarget(false);
            Layout();
            var motions = Host.Coordinator!.Motions;
            return key is null ? Assert.Single(motions) : Assert.Single(motions, m => Equals(m.Pair.Source.Key, key));
        }

        public void Dispose()
        {
            Window.Close();
            Parent.Dispose();
        }
    }

    private static Border Block(IBrush brush, double width, double height)
        => new() { Width = width, Height = height, Background = brush };
    private static SharedElement Element(object key, IBrush brush, double width, double height)
        => new() { Key = key, Content = Block(brush, width, height) };
    private static SharedElement Nested(object key, IBrush brush, double width, double height, bool child)
        => new()
        {
            Key = key,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Content = new StackPanel
            {
                Children =
                {
                    child
                        ? new SharedElement { Key = "image", Content = Block(Brushes.Lime, width * 0.8, height * 0.5) }
                        : Block(Brushes.Lime, width * 0.8, height * 0.5),
                    Block(brush, width, height * 0.4)
                }
            }
        };
    private static AnimatedVisibility Visibility(Control content) => new()
    {
        Content = content,
        Enter = EnterTransition.None,
        Exit = ExitTransition.None
    };

    [Fact]
    public Task SharedElement_RegistersAndAnimatesWithoutManualRects() => OnUi(() =>
    {
        using var scene = new Scene();
        var motion = scene.Start();
        Assert.Equal(Start, motion.Pair.SourceBounds);
        Assert.Equal(End, motion.Pair.TargetBounds);
        Assert.True(scene.From.Suppress!.IsSuppressed);
        Assert.True(scene.To.Suppress!.IsSuppressed);
        Assert.True(scene.Host.Overlay!.Active);
        Assert.False(scene.Host.Overlay.IsHitTestVisible);
        scene.Parent.OnFrame(Ms(0));
        scene.Parent.OnFrame(Ms(16));
        scene.Layout();
        Assert.InRange(motion.Bounds.Value.X, Start.X + 1, End.X - 1);
        scene.Parent.OnFrame(motion.Bounds.Duration);
        scene.Layout();
        Assert.True(motion.Session.IsClosed);
        Assert.False(scene.Host.Overlay.Active);
        Assert.False(scene.To.Suppress!.IsSuppressed);
        Assert.True(scene.From.Suppress!.IsSuppressed);
        Assert.True(scene.Source.IsContentPresent);
        scene.Parent.OnFrame(motion.Bounds.Duration + Ms(16));
        scene.Layout();
        Assert.False(scene.Source.IsContentPresent);
        Assert.True(scene.Target.IsContentPresent);
        Assert.Null(scene.Host.LastPreparationError);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ReverseAndZeroDuration_KeepSessionAndSuppress(bool zeroDuration) => OnUi(() =>
    {
        using var scene = new Scene();
        scene.Parent.DurationScale = zeroDuration ? 0 : 1;
        var motion = scene.Start();
        scene.Parent.OnFrame(Ms(0));
        scene.Parent.OnFrame(Ms(40));
        scene.Layout();
        if (!zeroDuration)
        {
            var sampled = motion.Bounds.Value;
            scene.Parent.UpdateTarget(true);
            scene.Layout();
            Assert.Same(motion, Assert.Single(scene.Host.Coordinator!.Motions));
            Assert.Equal(sampled, motion.Bounds.Value);
            Assert.Equal(Start, motion.Pair.TargetBounds);
        }
        scene.Parent.OnFrame(motion.Bounds.Duration);
        scene.Layout();
        Assert.True(motion.Session.IsClosed);
        scene.Parent.OnFrame(motion.Bounds.Duration + Ms(16));
        scene.Layout();
        Assert.False(scene.Host.Overlay!.Active);
        Assert.False(scene.Parent.IsRunning);
    });

    [Fact]
    public Task SharedBounds_KeepsFadeAfterBoundsFinish_AndInvertsOnReverse() => OnUi(() =>
    {
        using var scene = new Scene(bounds: true);
        var motion = scene.Start();
        var fade = Assert.Single(scene.Host.Overlay!.Items).FadeAnimation;
        Assert.NotNull(fade);
        scene.Parent.OnFrame(Ms(0));
        scene.Parent.OnFrame(Ms(100));
        scene.Layout();
        Assert.True(motion.Bounds.IsFinished);
        Assert.False(fade!.IsFinished);
        Assert.Equal(0.5, fade.Value, 1);
        Assert.False(motion.Session.IsClosed);
        var before = fade.Value;
        scene.Parent.UpdateTarget(true);
        scene.Layout();
        Assert.Equal(1 - before, fade.Value, 1);
        scene.Parent.OnFrame(Ms(100) + fade.Duration);
        scene.Layout();
        Assert.False(scene.Host.Overlay.Active);
    });

    [Fact]
    public Task ReverseScaleBounds_UsesCurrentSourceLayoutWhenItMovesDuringFlight() => OnUi(() =>
    {
        using var scene = new Scene(bounds: true);
        var motion = scene.Start();
        scene.Parent.OnFrame(Ms(0)); scene.Parent.OnFrame(Ms(40)); scene.Layout();

        Canvas.SetLeft(scene.Source, 75);
        scene.Layout();
        scene.Parent.UpdateTarget(true);
        scene.Layout();

        Assert.Equal(75, motion.Pair.TargetBounds.X);
        Assert.Equal(Start.Size, motion.Pair.TargetBounds.Size);
    });

    [Fact]
    public Task ModeMismatch_FallsBackToOrdinaryDisplay() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.Target.Content = new SharedBounds { Key = "photo", Content = Block(Brushes.Red, 200, 160) };
        scene.Layout();
        scene.Parent.UpdateTarget(false);
        scene.Layout();
        Assert.Empty(scene.Host.Coordinator!.Motions);
        Assert.False(scene.From.Suppress!.IsSuppressed);
    });

    [Fact]
    public Task NestedChild_DoesNotLeaveAHoleWhenUnmatched() => OnUi(() =>
    {
        using var scene = new Scene(nested: true);
        scene.Parent.UpdateTarget(false);
        scene.Layout();
        Assert.False(scene.Host.Scope!.IsPreparing);
        Assert.Contains(scene.Host.Coordinator!.Motions, m => Equals(m.Pair.Source.Key, "photo"));
        Assert.DoesNotContain(scene.Host.Coordinator.Motions, m => Equals(m.Pair.Source.Key, "image"));
        var child = scene.From.GetVisualDescendants().OfType<SharedElement>().First(e => Equals(e.Key, "image"));
        Assert.False(child.Suppress!.IsSuppressed);
    });

    [Fact]
    public Task Overlay_DoesNotHitTest() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.Start();
        var overlay = scene.Host.Overlay!;
        Assert.False(overlay.IsHitTestVisible);
        var point = overlay.TranslatePoint(new Point(60, 60), scene.Window);
        Assert.NotNull(point);
        var hit = scene.Window.InputHitTest(point!.Value);
        Assert.False(ReferenceEquals(hit, overlay));
    });

    [Fact]
    public Task ElementPixels_AreTargetRedFromTheFirstOverlaySample() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.Start();
        var item = Assert.Single(scene.Host.Overlay!.Items);
        Assert.Same(scene.To.Capture, item.TargetBrush.Visual);
        Assert.Null(item.SourceBrush);
        Assert.Equal(Start, item.Rect);
        Assert.Equal(1, item.Fade);
    });

    [Fact]
    public Task DetachedEndpoint_DoesNotRegisterIntoRecreatedScope_AndCanReattach() => OnUi(() =>
    {
        using var scene = new Scene();
        var panel = new StackPanel();
        scene.Source.Content = panel;
        panel.Children.Add(scene.From);
        scene.Layout();
        var old = scene.From.Participant!;
        panel.Children.Remove(scene.From);
        Assert.False(old.IsActive);
        scene.Source.Source = VisibilitySource.From(scene.Parent, s => s);
        var replacement = Element("photo", Brushes.Blue, 100, 80);
        panel.Children.Add(replacement);
        scene.Layout();
        Assert.False(scene.From.Participant!.IsActive);
        Assert.Equal(1, scene.Host.RegisteredEndpointCount);
        scene.Start();
        Assert.DoesNotContain(scene.Host.Diagnostics, d => d.Contains("AmbiguousKey"));

        panel.Children.Remove(replacement);
        panel.Children.Add(scene.From);
        scene.Layout();
        Assert.True(scene.From.Participant!.IsActive);
        scene.Source.Source = VisibilitySource.From(scene.Parent, s => true);
        scene.Layout();
        Assert.True(scene.From.Participant!.IsActive);
        Assert.Same(scene.Source.Scope!.Transition, scene.From.Participant.Visibility);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DetachedEndpoint_IsRetainedDuringFlight_ThenReleased(bool changeKey) => OnUi(() =>
    {
        using var scene = new Scene();
        var panel = new StackPanel();
        scene.Source.Content = panel;
        panel.Children.Add(scene.From);
        scene.Layout();
        var motion = scene.Start();
        var participant = scene.From.Participant!;
        panel.Children.Remove(scene.From);
        Assert.True(participant.IsActive);
        Assert.False(motion.Session.IsClosed);
        if (changeKey) scene.From.Key = "recycled";
        else
        {
            scene.Parent.OnFrame(Ms(0));
            scene.Parent.OnFrame(Ms(100));
            scene.Layout();
        }
        Assert.True(motion.Session.IsClosed);
        Assert.False(participant.IsActive);
        Assert.Equal(1, scene.Host.RegisteredEndpointCount);
    });

    [Fact]
    public Task DeadlineWithoutLayout_ReleasesPreparation() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.Parent.UpdateTarget(false);
        Assert.True(scene.Host.Scope!.IsPreparing);
        Assert.True(scene.Host.Coordinator!.ExpirePreparation(scene.Host.Scope.Generation));
        Assert.False(scene.Host.Scope.IsPreparing);
        Assert.Empty(scene.Host.Coordinator.Motions);
    });
}
