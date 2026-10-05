using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedContentSizeBehaviorTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static TweenSpec<Size> Tween(int ms = 100) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());
    private sealed class Fixture : IDisposable
    {
        public ManualAnimationFrameClock Clock { get; } = new();
        public Border Leaf { get; } = new() { Width = 100, Height = 40 };
        public AnimatedContentSize Host { get; }
        public Grid Surface { get; } = new() { Width = 400, Height = 350 };
        public Window Window { get; }
        public List<AnimatedContentSizeChangedEventArgs> Completions { get; } = [];
        public Control TemplateRoot => Host.GetVisualChildren().OfType<Control>().Single();
        public Fixture(Action<AnimatedContentSize>? configure = null, IAnimationFrameClock? clock = null)
        {
            Host = new AnimatedContentSize
            {
                Content = Leaf, AnimationSpec = Tween(), HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            configure?.Invoke(Host);
            Host.AnimationCompleted += (_, e) => Completions.Add(e);
            Surface.Children.Add(Host);
            Window = new AnimationTestWindow(clock ?? Clock) { Width = 600, Height = 500, Content = Surface };
            Window.Show(); Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Clock.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() => Window.Close();
    }

    public static IEnumerable<object[]> Alignments()
    {
        var entries = new[]
        {
            (VisibilityAlignment.TopStart, 0d, 0d), (VisibilityAlignment.Top, .5, 0), (VisibilityAlignment.TopEnd, 1, 0),
            (VisibilityAlignment.Start, 0, .5), (VisibilityAlignment.Center, .5, .5), (VisibilityAlignment.End, 1, .5),
            (VisibilityAlignment.BottomStart, 0, 1), (VisibilityAlignment.Bottom, .5, 1), (VisibilityAlignment.BottomEnd, 1, 1)
        };
        foreach (var (alignment, x, y) in entries)
        foreach (var rtl in new[] { false, true }) yield return [alignment, rtl, x, y];
    }

    [Theory]
    [MemberData(nameof(Alignments))]
    public Task Alignment_MatchesUpstreamPlacementDuringResize(VisibilityAlignment alignment, bool rtl, double x, double y) => OnUi(() =>
    {
        using var f = new Fixture(host =>
        {
            host.ContentAlignment = alignment;
            host.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        });
        f.Leaf.Width = 200; f.Leaf.Height = 140;
        f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(new Size(150, 90), f.Host.Bounds.Size);
        Assert.Equal(new Size(200, 140), f.TemplateRoot.Bounds.Size);
        Assert.Equal(-50 * (rtl ? 1 - x : x), f.TemplateRoot.Bounds.X, 5);
        Assert.Equal(-50 * y, f.TemplateRoot.Bounds.Y, 5);
        Assert.True(f.Host.ClipToBounds);
    });

    [Fact]
    public Task AlignmentAndFlowDirectionChanges_RearrangeWhileIdle() => OnUi(() =>
    {
        using var f = new Fixture(host => { host.Width = 300; host.Height = 200; });
        f.Host.ContentAlignment = VisibilityAlignment.BottomEnd;
        Assert.False(f.Host.IsArrangeValid);
        f.Layout();
        Assert.Equal(new Point(200, 160), f.TemplateRoot.Bounds.TopLeft);
        f.Host.FlowDirection = FlowDirection.RightToLeft;
        f.Layout();
        Assert.Equal(new Point(0, 160), f.TemplateRoot.Bounds.TopLeft);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task DefaultTemplate_ForwardsDecorationAndAnimatesItsMeasuredSize() => OnUi(() =>
    {
        using var f = new Fixture(host =>
        {
            host.Padding = new Thickness(10); host.BorderThickness = new Thickness(2);
            host.Background = Brushes.Red; host.BorderBrush = Brushes.Blue; host.CornerRadius = new CornerRadius(6);
            host.BackgroundSizing = BackgroundSizing.InnerBorderEdge;
        });
        var presenter = Assert.IsAssignableFrom<ContentPresenter>(f.TemplateRoot);
        Assert.Same(Brushes.Red, presenter.Background);
        Assert.Same(Brushes.Blue, presenter.BorderBrush);
        Assert.Equal(BackgroundSizing.InnerBorderEdge, presenter.BackgroundSizing);
        Assert.Equal(new CornerRadius(6), presenter.CornerRadius);
        Assert.Equal(new Size(124, 64), f.Host.Bounds.Size);
        f.Host.Padding = new Thickness(20);
        f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(new Size(134, 74), f.Host.Bounds.Size);
        Assert.Equal(new Size(144, 84), presenter.Bounds.Size);
        Assert.Equal(new Point(22, 22), f.Leaf.Bounds.TopLeft);
        f.Step(50);
        Assert.Equal(new Size(144, 84), f.Completions.Single().TargetSize);
    });

    [Fact]
    public Task CustomTemplate_ArrangesWholeTree_AndTemplateRemovalDropsOldRoot() => OnUi(() =>
    {
        using var f = new Fixture(host => host.Template = new FuncControlTemplate<AnimatedContentSize>((owner, names) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty);
            names.Register(presenter.Name, presenter);
            return new Grid { Children = { new Border { Padding = new Thickness(7), Child = presenter } } };
        }));
        var root = f.TemplateRoot;
        Assert.Equal(new Size(114, 54), root.Bounds.Size);
        f.Leaf.Height = 140; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(new Size(114, 104), f.Host.Bounds.Size);
        Assert.Equal(new Size(114, 154), root.Bounds.Size);
        Assert.All(root.GetVisualDescendants().OfType<Control>(), child => Assert.True(child.IsArrangeValid));
        Assert.Equal(new Point(7, 7), f.Leaf.TranslatePoint(default, root));
        f.Host.Template = null; f.Layout(); f.Step(100);
        Assert.Empty(f.Host.GetVisualChildren());
        Assert.Null(root.GetVisualParent());
        Assert.Equal(default, f.Host.Bounds.Size);
    });

    [Fact]
    public Task DataTemplate_AndEmptyContent_WorkWithoutLookaheadOptIn() => OnUi(() =>
    {
        using var f = new Fixture(host =>
        {
            host.Content = "data";
            host.ContentTemplate = new FuncDataTemplate<string>((_, _) => new Border { Width = 120, Height = 72 });
        });
        Assert.Equal(new Size(120, 72), f.Host.Bounds.Size);
        f.Host.Content = null; f.Layout();
        Assert.Equal(new Size(120, 72), f.Host.Bounds.Size); // Explicit template builds even for null.
        var template = f.Host.ContentTemplate;
        f.Host.ContentTemplate = null; f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(default, f.Host.Bounds.Size);
        f.Host.ContentTemplate = template;
        f.Host.Content = "again"; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(new Size(60, 36), f.Host.Bounds.Size);
        f.Step(50);
        Assert.Equal(new Size(120, 72), f.Host.Bounds.Size);
        Assert.Equal(2, f.Completions.Count);
    });

    [Fact]
    public Task Reattach_ResumesSameTarget_WhileExplicitResetInitializesForReuse() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Leaf.Height = 140; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(90, f.Host.Bounds.Height);
        f.Surface.Children.Remove(f.Host);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        f.Clock.AdvanceBy(TimeSpan.FromSeconds(10));
        f.Surface.Children.Add(f.Host); f.Layout();
        Assert.Equal(90, f.Host.Bounds.Height);
        f.Step(16); f.Step(50);
        Assert.Equal(115, f.Host.Bounds.Height);
        f.Step(50);
        Assert.Equal(90, f.Completions.Single().InitialSize.Height);
        Assert.Equal(140, f.Completions.Single().TargetSize.Height);
        f.Leaf.Height = 40; f.Layout(); f.Step(16); f.Step(50);
        f.Host.ResetAnimation(); f.Layout();
        Assert.Equal(40, f.Host.Bounds.Height);
        Assert.Single(f.Completions);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task OffTreeMeasure_DoesNotStartFirstVisibleAnimation() => OnUi(() =>
    {
        using var f = new Fixture(host =>
        {
            host.Measure(new Size(400, 350));
            ((Border)host.Content!).Height = 140;
        });
        Assert.Equal(140, f.Host.Bounds.Height);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        Assert.Empty(f.Completions);
    });

    [Fact]
    public Task ReparentOutOfLookahead_DropsOldTargetConstraints() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Surface.Children.Remove(f.Host);
        f.Leaf.Width = 300;
        f.Host.ResetAnimation();
        var root = new LookaheadRoot { Width = 200, Height = 250, Child = f.Host };
        f.Surface.Children.Add(root); f.Layout();
        Assert.Equal(200, f.Host.Bounds.Width);
        f.Leaf.Width = 140; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(170, f.Host.Bounds.Width);
        root.Child = null;
        f.Surface.Children.Remove(root);
        f.Leaf.Width = 300;
        f.Surface.Children.Add(f.Host); f.Layout();
        Assert.Equal(170, f.Host.Bounds.Width);
        f.Step(16); f.Step(100);
        Assert.Equal(300, f.Host.Bounds.Width);
        Assert.Equal(300, f.Completions.Single().TargetSize.Width);
    });


    [Fact]
    public Task RetargetCompletion_IsCurrentRunOnly_AndAllowsReentrantContentChanges() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Leaf.Height = 140; f.Layout(); f.Step(16); f.Step(50);
        f.Leaf.Height = 60; f.Layout(); f.Step(50);
        Assert.Equal(75, f.Host.Bounds.Height);
        Assert.Empty(f.Completions);
        f.Host.AnimationCompleted += (_, _) =>
        {
            if (f.Completions.Count == 1) { f.Leaf.Height = 120; f.Window.UpdateLayout(); }
            else f.Surface.Children.Remove(f.Host);
        };
        f.Step(50); f.Step(16); f.Step(100);
        Assert.Equal(2, f.Completions.Count);
        Assert.Equal(new Size(100, 90), f.Completions[0].InitialSize);
        Assert.Equal(new Size(100, 60), f.Completions[0].TargetSize);
        Assert.Equal(new Size(100, 60), f.Completions[1].InitialSize);
        Assert.Equal(new Size(100, 120), f.Completions[1].TargetSize);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task SpecChange_DoesNotRetimeCurrentRun_ButAppliesToNextTarget() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Leaf.Height = 140; f.Layout(); f.Step(16); f.Step(50);
        f.Host.AnimationSpec = Tween(200);
        f.Step(50);
        Assert.Equal(140, f.Host.Bounds.Height);
        Assert.Single(f.Completions);
        f.Leaf.Height = 40; f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(90, f.Host.Bounds.Height);
        f.Step(100);
        Assert.Equal(40, f.Host.Bounds.Height);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ZeroDurationOrMotionScale_CompletesOnFirstFrame(bool zeroScale) => OnUi(() =>
    {
        var previous = MotionDurationScale.ScaleFactor;
        try
        {
            MotionDurationScale.ScaleFactor = zeroScale ? 0 : 1;
            using var f = new Fixture(host => host.AnimationSpec = Tween(zeroScale ? 100 : 0));
            f.Leaf.Height = 140; f.Layout(); f.Step(16);
            Assert.Equal(140, f.Host.Bounds.Height);
            Assert.Single(f.Completions);
            Assert.Equal(0, f.Clock.PendingFrameCount);
        }
        finally { MotionDurationScale.ScaleFactor = previous; }
    });


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SynchronousCompletionOrError_CanResetDuringMeasure(bool fail) => OnUi(() =>
    {
        using var f = new Fixture(host =>
        {

            host.AnimationSpec = fail ? new BrokenSpec(new InvalidOperationException("spec")) : Tween(0);
        }, clock: new ImmediateClock());
        f.Host.AnimationCompleted += (_, _) => f.Host.ResetAnimation();
        ((AnimationTestWindow)f.Window).AnimationRoot.AnimationFailed += (_, _) => f.Host.ResetAnimation();
        f.Leaf.Height = 140; f.Layout();
        Assert.Equal(140, f.Host.Bounds.Height);
        Assert.Equal(fail ? 0 : 1, f.Completions.Count);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SpecAndListenerFailures_AreReported_AndLaterTargetsStillWork(bool cancellation) => OnUi(() =>
    {
        Exception failure = cancellation ? new OperationCanceledException("spec") : new InvalidOperationException("spec");
        using var f = new Fixture(host => host.AnimationSpec = new BrokenSpec(failure));
        f.Leaf.Height = 140; f.Layout();
        Assert.Equal(140, f.Host.Bounds.Height);
        Assert.Same(failure, ((AnimationTestWindow)TopLevel.GetTopLevel(f.Host)!).AnimationRoot.LastError);
        Assert.Empty(f.Completions);
        f.Host.AnimationSpec = Tween();
        f.Leaf.Height = 60; f.Layout(); f.Step(16);
        var listenerFailure = new InvalidOperationException("listener");
        EventHandler<AnimatedContentSizeChangedEventArgs> listener = (_, _) => throw listenerFailure;
        f.Host.AnimationCompleted += listener;
        f.Step(100);
        Assert.Same(listenerFailure, ((AnimationTestWindow)TopLevel.GetTopLevel(f.Host)!).AnimationRoot.LastError);
        Assert.Equal(60, f.Host.Bounds.Height);
        f.Host.AnimationCompleted -= listener;
        f.Leaf.Height = 120; f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(120, f.Host.Bounds.Height);
        Assert.Equal(2, f.Completions.Count);
    });

    [Fact]
    public Task MinMaxAndParentConstraints_ConstrainOutputWithoutLosingAnimationTarget() => OnUi(() =>
    {
        using var f = new Fixture(host => { host.MinHeight = 80; host.MaxHeight = 160; });
        Assert.Equal(80, f.Host.Bounds.Height);
        f.Leaf.Height = 200; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(120, f.Host.Bounds.Height); // Endpoint begins at the actual minimum (80).
        Assert.Equal(160, f.TemplateRoot.DesiredSize.Height);
        f.Surface.Height = 100; f.Layout();
        Assert.Equal(100, f.Host.Bounds.Height); // The visible result obeys the new parent limit.
        f.Step(50);
        Assert.Equal(100, f.Host.Bounds.Height);
        f.Step(50);
        Assert.Equal(100, f.Completions.Single().TargetSize.Height);
        f.Surface.Height = 350; f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(160, f.Host.Bounds.Height);
        Assert.Equal(160, f.Completions.Last().TargetSize.Height);
    });

    [Fact]
    public Task SpringOvershootAndRetarget_PreserveRuntimeSamplesAndVelocity() => OnUi(() =>
    {
        var spec = new SpringSpec<Size>(dampingRatio: .25, stiffness: 300, visibilityThreshold: new Size(.5, .5));
        using var f = new Fixture(host => { host.AnimationSpec = spec; host.UseLayoutRounding = false; });
        var initial = new Size(100, 40); var target = new Size(100, 140);
        var reference = new TargetBasedAnimation<Size>(spec, new SizeConverter(), initial, target);
        f.Leaf.Height = 140; f.Layout(); f.Step(16);
        f.Step(200);
        var current = reference.GetValue(TimeSpan.FromMilliseconds(200));
        Assert.True(current.Height > 140);
        Assert.Equal(current.Height, f.Host.Bounds.Height, 5);
        var velocity = reference.GetVelocity(TimeSpan.FromMilliseconds(200));
        f.Leaf.Height = 0; f.Layout();
        Assert.Equal(current.Height, f.Host.Bounds.Height, 5);
        var redirected = new TargetBasedAnimation<Size>(spec, new SizeConverter(), current, new Size(100, 0), velocity);
        var observedNegative = false;
        for (var ms = 50; ms <= 400; ms += 50)
        {
            f.Step(50);
            var expected = redirected.GetValue(TimeSpan.FromMilliseconds(ms)).Height;
            observedNegative |= expected < 0;
            Assert.Equal(Math.Max(0, expected), f.Host.Bounds.Height, 5);
        }
        Assert.True(observedNegative); // Negative spring samples must never become negative layout sizes.
        f.Step(10000);
        Assert.Equal(0, f.Host.Bounds.Height);
        Assert.Single(f.Completions);
        Assert.Equal(current.Height, f.Completions[0].InitialSize.Height, 5);
        Assert.Equal(0, f.Completions[0].TargetSize.Height);
    });

    private sealed class FailingClock(bool cancellation) : IAnimationFrameClock
    {
        public Exception Error { get; } = cancellation ? new OperationCanceledException("clock") : new InvalidOperationException("clock");
        public int Calls { get; private set; }
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken token) { Calls++; return ValueTask.FromException<TimeSpan>(Error); }
    }
    private sealed class ImmediateClock : IAnimationFrameClock
    {
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken token) => ValueTask.FromResult(TimeSpan.Zero);
    }
    [Fact]
    public Task NonFiniteAnimationSample_FallsBackBeforeInvalidLayoutIsPublished() => OnUi(() =>
    {
        using var f = new Fixture(host => host.AnimationSpec = new TweenSpec<Size>(
            TimeSpan.FromMilliseconds(100), easing: new NonFiniteEasing()));
        f.Leaf.Height = 140; f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(140, f.Host.Bounds.Height);
        Assert.IsType<InvalidOperationException>(((AnimationTestWindow)TopLevel.GetTopLevel(f.Host)!).AnimationRoot.LastError);
        Assert.Empty(f.Completions);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });
    private sealed class NonFiniteEasing : Easing
    {
        public override double Ease(double progress) => double.NaN;
    }
    private sealed class BrokenSpec(Exception error) : IFiniteAnimationSpec<Size>
    {
        public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Size> converter) => throw error;
    }
}
