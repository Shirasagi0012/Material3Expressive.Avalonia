using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedVisibilityTest
{
    private static TweenSpec<T> Tween<T>(int milliseconds = 100)
        => new(TimeSpan.FromMilliseconds(milliseconds), easing: new LinearEasing());

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public AnimatedVisibility Host { get; }
        public Window Window { get; }
        public Fixture(AnimatedVisibility host)
        {
            Host = host;

            Window = new AnimationTestWindow(Frames) { Width = 500, Height = 500, Content = host };
            Window.Show();
            Layout();
        }
        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }
        public void Step(int milliseconds)
        {
            Layout();
            // Runtime wakes its idle loop asynchronously. Deliver a frame only after it has
            // subscribed, rather than racing the thread-pool continuation with simulated time.
            Assert.True(SpinWait.SpinUntil(() =>
            {
                Layout();
                return Host.IsIdle || Frames.PendingFrameCount > 0;
            }, TimeSpan.FromSeconds(5)), "The animation loop did not request its next frame.");
            Frames.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }
        public void Finish()
        {
            for (var i = 0; i < 200 && (!Host.IsIdle || Frames.PendingFrameCount > 0); i++) Step(16);
            Assert.True(Host.IsIdle, $"Target={Host.TargetVisible}, Scope={Host.Scope?.Transition.CurrentState}->{Host.Scope?.Transition.TargetState}, PlayTime={Host.Scope?.Transition.PlayTime}, Nodes={string.Join("; ", Host.Scope?.Transition.Animations.Select(a => $"{a.Label} finished={a.IsFinished} duration={a.Duration}") ?? [])}");
            Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(Host)!).AnimationRoot.Diagnostics.LastError);
        }
        public void Dispose() => Window.Close();
    }

    private static AnimatedVisibility Host(bool visible = true) => new()
    {
        Visible = visible,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Content = new Border { Width = 100, Height = 80, Background = Brushes.Red },
        Enter = new FadeIn { Spec = Tween<double>() },
        Exit = new FadeOut { Spec = Tween<double>() }
    };

    private static VisibilityLayer Layer(AnimatedVisibility host)
        => host.GetVisualDescendants().OfType<VisibilityLayer>().First();

    [Fact]
    public Task EnterInterruptedByExitNone_UsesDefaultSpringsAndPivotFractions() => OnUi(() =>
    {
        var host = Host(false);
        host.Enter = new FadeIn { Spec = new SpringSpec<double>(stiffness: 1600, visibilityThreshold: 0.01) }
            + new ScaleIn { InitialScale = 0, TransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative),
                Spec = new SpringSpec<double>(dampingRatio: 0.6, stiffness: 800, visibilityThreshold: 0.01) };
        host.Exit = ExitTransition.None;
        using var fixture = new Fixture(host);
        host.Visible = true;
        fixture.Step(16);
        fixture.Step(64);
        var scope = host.Scope!;
        var alpha = scope.Transition.Animations.Single(a => a.Label.EndsWith(" alpha"));
        var scale = scope.Transition.Animations.Single(a => a.Label.EndsWith(" scale"));
        var originalAlpha = ((Transition<EnterExitState>.TransitionAnimationState<double>)alpha).Animation;
        var originalScale = ((Transition<EnterExitState>.TransitionAnimationState<double>)scale).Animation;
        host.Visible = false;
        fixture.Layout();
        var retargetedAlpha = ((Transition<EnterExitState>.TransitionAnimationState<double>)alpha);
        var retargetedScale = ((Transition<EnterExitState>.TransitionAnimationState<double>)scale);
        Assert.NotSame(originalAlpha, retargetedAlpha.Animation);
        Assert.NotSame(originalScale, retargetedScale.Animation);
        var alphaSpec = Assert.IsType<SpringSpec<double>>(retargetedAlpha.AnimationSpec);
        var scaleSpec = Assert.IsType<SpringSpec<double>>(retargetedScale.AnimationSpec);
        Assert.Equal(400, alphaSpec.Stiffness);
        Assert.Equal(400, scaleSpec.Stiffness);
        Assert.Equal(0.002, scaleSpec.VisibilityThreshold);
        var expectedAlpha = new TargetBasedAnimation<double>(alphaSpec, new DoubleConverter(), retargetedAlpha.Value, 1, retargetedAlpha.Velocity);
        var expectedScaleAnimation = new TargetBasedAnimation<double>(scaleSpec, new DoubleConverter(), retargetedScale.Value, 1, retargetedScale.Velocity);
        fixture.Step(16);

        var layer = Layer(host);
        var matrix = ((MatrixTransform)layer.Surface.RenderTransform!).Matrix;
        var expectedScale = expectedScaleAnimation.GetValue(TimeSpan.FromMilliseconds(16));
        var expectedPivot = new TargetBasedAnimation<Point>(
            new SpringSpec<Point>(visibilityThreshold: new Point(0.01, 0.01)), new PointConverter(),
            new Point(0, 1), new Point(0.5, 0.5)).GetValue(TimeSpan.FromMilliseconds(16));
        Assert.Equal(expectedAlpha.GetValue(TimeSpan.FromMilliseconds(16)), layer.Surface.Opacity, 8);
        Assert.Equal(expectedScale, matrix.M11, 8);
        Assert.Equal(expectedPivot.X * 100 * (1 - expectedScale), matrix.M31, 8);
        Assert.Equal(expectedPivot.Y * 80 * (1 - expectedScale), matrix.M32, 8);
        Assert.True(host.IsContentPresent);
        fixture.Finish();
        Assert.False(host.IsContentPresent);
        Assert.Equal(0, fixture.Frames.PendingFrameCount);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SharedParent_AutomaticPreparationRetainsExitNoneWithoutManualLease(bool zeroDuration) => OnUi(() =>
    {
        using var parent = new Transition<bool>(true);
        using var shared = SharedTransitionScope.Create(parent);
        var host = Host();
        host.Enter = EnterTransition.None;
        host.Exit = ExitTransition.None;
        host.Source = VisibilitySource.From(parent, value => value);
        host.ScopeCreated += (_, e) => e.Scope.Own(shared.RegisterParticipant("photo", e.Scope.Transition));
        using var fixture = new Fixture(host);
        parent.OnFrame(TimeSpan.Zero);
        if (zeroDuration) parent.DurationScale = 0;
        parent.UpdateTarget(false);
        parent.OnFrame(TimeSpan.FromMilliseconds(16));
        fixture.Layout();
        Assert.True(shared.IsPreparing);
        Assert.True(host.IsContentPresent);
        shared.CancelPreparation(shared.Generation);
        parent.OnFrame(TimeSpan.FromMilliseconds(32));
        fixture.Layout();
        Assert.False(host.IsContentPresent);
        Assert.False(shared.IsPreparing);
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public Task SharedSession_TwoHostsRetainContentThroughHandoffAndReversal(bool reverse, bool zeroDuration) => OnUi(() =>
    {
        using var parent = new Transition<bool>(true);
        using var shared = SharedTransitionScope.Create(parent);
        var source = Host();
        var target = Host(false);
        source.Enter = target.Enter = EnterTransition.None;
        source.Exit = target.Exit = ExitTransition.None;
        source.Source = VisibilitySource.From(parent, s => s);
        target.Source = VisibilitySource.From(parent, s => !s);
        SharedTransitionParticipant from = null!, to = null!;
        source.ScopeCreated += (_, e) => e.Scope.Own(from = shared.RegisterParticipant("photo", e.Scope.Transition));
        target.ScopeCreated += (_, e) => e.Scope.Own(to = shared.RegisterParticipant("photo", e.Scope.Transition));
        using var first = new Fixture(source);
        using var second = new Fixture(target);
        parent.OnFrame(TimeSpan.Zero);
        parent.DurationScale = zeroDuration ? 0 : 1;
        parent.UpdateTarget(false);
        first.Layout(); second.Layout();
        Assert.NotNull(from);
        Assert.NotNull(to);
        SharedTransitionSession session = null!;
        Transition<int>.TransitionAnimationState<double> motion = null!;
        var handedOff = false;
        Assert.True(shared.TryCommitSessions(shared.Generation, batch => session = batch.Start(from, to, s =>
        {
            motion = s.Transition.CreateDeferredAnimation(new DoubleConverter()).Animate(
                _ => Tween<double>(), 0, null, _ => 1);
        }, (_, reason) =>
        {
            Assert.Equal(SharedSessionEndReason.Completed, reason);
            Assert.True(source.IsContentPresent);
            Assert.True(target.IsContentPresent);
            Assert.Same(first.Window, TopLevel.GetTopLevel((Control)source.Content!));
            Assert.Same(second.Window, TopLevel.GetTopLevel((Control)target.Content!));
            Assert.Equal(reverse ? 0 : 1, motion.Value);
            handedOff = true;
        })));
        var sourceScope = source.Scope;
        var targetScope = target.Scope;
        parent.OnFrame(TimeSpan.FromMilliseconds(16));
        parent.OnFrame(TimeSpan.FromMilliseconds(56));
        if (reverse)
        {
            var value = motion.Value;
            var velocity = motion.VelocityVector.ToArray();
            parent.UpdateTarget(true);
            Assert.True(shared.TryCommitSessions(shared.Generation,
                batch => batch.Retarget(session, to, from, () => motion.Update(_ => 0))));
            Assert.Equal(value, motion.Value);
            Assert.Equal(velocity, motion.VelocityVector.ToArray());
        }
        var finishTime = TimeSpan.FromMilliseconds(16) + motion.Duration;
        parent.OnFrame(finishTime);
        first.Layout(); second.Layout();
        Assert.Same(sourceScope, source.Scope);
        Assert.Same(targetScope, target.Scope);
        Assert.True(source.IsContentPresent);
        Assert.True(target.IsContentPresent);
        Assert.False(handedOff);
        Assert.True(session.TryComplete(session.Generation));
        Assert.True(handedOff);
        parent.OnFrame(finishTime + TimeSpan.FromMilliseconds(16));
        first.Layout(); second.Layout();
        Assert.Equal(reverse, source.IsContentPresent);
        Assert.Equal(!reverse, target.IsContentPresent);
        Assert.False(parent.IsRunning);
        Assert.Equal(0, first.Frames.PendingFrameCount);
        Assert.Equal(0, second.Frames.PendingFrameCount);
        Assert.Null(shared.LastSessionError);
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(source)!).AnimationRoot.Diagnostics.LastError);
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(target)!).AnimationRoot.Diagnostics.LastError);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Preparation_KeepsExitNoneContentUntilReleased(bool zeroDuration) => OnUi(() =>
    {
        var host = Host();
        host.Enter = EnterTransition.None;
        host.Exit = ExitTransition.None;
        using var f = new Fixture(host);
        f.Finish();
        var scope = host.Scope!;
        if (zeroDuration) scope.Transition.DurationScale = 0;
        using var preparation = scope.BeginPreparation();
        host.Visible = false;
        f.Step(16);
        f.Step(60000);
        Assert.Same(scope, host.Scope);
        Assert.True(host.IsContentPresent);
        Assert.False(host.IsIdle);
        Assert.Equal(EnterExitState.Visible, scope.Transition.CurrentState);
        preparation.Dispose();
        f.Finish();
        Assert.False(host.IsContentPresent);
        Assert.Null(host.Scope);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SharedBounds_ScopeCleanupCanCommitFinalSampleBeforeContentRemoval(bool zeroDuration) => OnUi(() =>
    {
        var host = Host();
        host.Enter = EnterTransition.None;
        host.Exit = ExitTransition.None;
        using var f = new Fixture(host);
        f.Finish();
        var scope = host.Scope!;
        if (zeroDuration) scope.Transition.DurationScale = 0;
        using var preparation = scope.BeginPreparation();
        host.Visible = false;
        f.Step(16);
        f.Step(2000);
        using var deferred = scope.Transition.CreateDeferredAnimation(new RectConverter());
        var from = new Rect(0, 0, 100, 80);
        var target = new Rect(200, 100, 220, 180);
        var bounds = deferred.Animate(_ => Tween<Rect>(), from, null, _ => target);
        var handedOff = false;
        scope.Own(new ActionDisposable(() =>
        {
            Assert.True(bounds.IsFinished);
            Assert.Equal(target, bounds.Value);
            Assert.True(host.IsContentPresent);
            Assert.Same(f.Window, TopLevel.GetTopLevel((Control)host.Content!));
            handedOff = true;
        }));
        preparation.Dispose();
        Assert.False(scope.Transition.IsPreparing);
        if (!zeroDuration)
        {
            f.Step(40);
            Assert.True(host.IsContentPresent);
            Assert.False(handedOff);
            var current = bounds.Value;
            var velocity = bounds.VelocityVector.ToArray();
            target = new Rect(60, 20, 130, 90);
            bounds.Update(_ => target);
            Assert.Equal(current, bounds.Value);
            Assert.Equal(velocity, bounds.VelocityVector.ToArray());
        }
        f.Finish();
        Assert.True(handedOff);
        Assert.False(host.IsContentPresent);
    });

    [Fact]
    public Task SharedBounds_DetachCancelsMotionAndReleasesScopeResourceOnce() => OnUi(() =>
    {
        var host = Host();
        host.Enter = EnterTransition.None;
        host.Exit = ExitTransition.None;
        using var f = new Fixture(host);
        f.Finish();
        var scope = host.Scope!;
        using var preparation = scope.BeginPreparation();
        host.Visible = false;
        f.Step(16);
        using var deferred = scope.Transition.CreateDeferredAnimation(new RectConverter());
        var bounds = deferred.Animate(_ => Tween<Rect>(4000), new Rect(0, 0, 100, 80), null,
            _ => new Rect(200, 100, 220, 180));
        var releases = 0;
        scope.Own(new ActionDisposable(() =>
        {
            Assert.False(bounds.IsFinished); // Teardown is cancellation, not a successful handoff.
            releases++;
        }));
        preparation.Dispose();
        f.Step(40);
        f.Window.Content = null;
        f.Layout();
        Assert.Equal(1, releases);
        Assert.Null(host.Scope);
        Assert.False(host.IsContentPresent);
        Assert.Throws<ObjectDisposedException>(() => bounds.Update(_ => default));
        deferred.Dispose();
        Assert.Equal(1, releases);
    });

    [Fact]
    public Task Preparation_DetachRevokesLease_AndReattachUsesNewScope() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        f.Finish();
        var scope = f.Host.Scope!;
        var lease = scope.BeginPreparation();
        f.Host.Visible = false;
        f.Step(16);
        f.Window.Content = null;
        f.Layout();
        Assert.False(lease.IsActive);
        lease.Dispose();
        f.Host.Visible = true;
        f.Window.Content = f.Host;
        f.Layout();
        f.Finish();
        Assert.NotSame(scope, f.Host.Scope);
        Assert.True(f.Host.IsContentPresent);
    });

    [Fact]
    public Task InitiallyVisible_DoesNotFadeIn_AndInitiallyHiddenTakesNoSpace() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        Assert.True(f.Host.IsContentPresent);
        Assert.Equal(1, Layer(f.Host).Surface.Opacity);
        f.Host.Visible = false;
        f.Finish();
        Assert.False(f.Host.IsContentPresent);
        Assert.Null(f.Host.Scope);
        Assert.Equal(default, f.Host.DesiredSize);
        using var hidden = new Fixture(new AnimatedVisibility
        {
            Visible = false, Margin = new Thickness(20), MinHeight = 100,
            Content = new Border { Width = 100, Height = 100 }
        });
        Assert.Equal(default, hidden.Host.DesiredSize);
        Assert.Empty(hidden.Host.GetVisualDescendants());
    });

    [Fact]
    public Task ExitKeepsLiveContent_UntilLastFrame_ThenReentryCreatesNewScope() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        var original = f.Host.Scope;
        var content = Assert.IsType<Border>(f.Host.Content);
        f.Host.Visible = false;
        f.Step(16);
        f.Step(50);
        Assert.True(f.Host.IsContentPresent);
        Assert.False(f.Host.TargetVisible);
        Assert.InRange(Layer(f.Host).Surface.Opacity, 0.45, 0.55);
        Assert.Contains(content, f.Host.GetVisualDescendants());
        content.Background = Brushes.Blue;
        f.Finish();
        Assert.DoesNotContain(content, f.Host.GetVisualDescendants());
        f.Host.Visible = true;
        f.Layout();
        Assert.NotSame(original, f.Host.Scope);
        Assert.Same(content, f.Host.Content);
        Assert.Equal(0, Layer(f.Host).Surface.Opacity);
        f.Finish();
        Assert.Equal(1, Layer(f.Host).Surface.Opacity);
    });

    [Fact]
    public Task ReversingExit_PreservesScopeAndCurrentValue() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        var scope = f.Host.Scope;
        f.Host.Visible = false;
        f.Step(16);
        f.Step(40);
        var opacity = Layer(f.Host).Surface.Opacity;
        f.Host.Visible = true;
        Assert.Same(scope, f.Host.Scope);
        Assert.Equal(opacity, Layer(f.Host).Surface.Opacity);
        f.Finish();
        Assert.True(f.Host.IsContentPresent);
        Assert.Equal(1, Layer(f.Host).Surface.Opacity);
    });

    [Fact]
    public Task CustomAnimation_ExtendsExit_AndReleasesAnimationBinding() => OnUi(() =>
    {
        var host = Host();
        host.Exit = ExitTransition.None;
        var card = (Border)host.Content!;
        card.CornerRadius = new CornerRadius(4);
        host.ScopeCreated += (_, e) => e.Scope.Bind(card, Border.CornerRadiusProperty,
            e.Scope.Transition.AnimateCornerRadius(
                s => new CornerRadius(s == EnterExitState.PostExit ? 24 : 4),
                _ => Tween<CornerRadius>(300)));
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16);
        f.Step(150);
        Assert.True(host.IsContentPresent);
        Assert.InRange(card.CornerRadius.TopLeft, 13.9, 14.1);
        Assert.True(card.IsAnimating(Border.CornerRadiusProperty));
        f.Finish();
        Assert.False(host.IsContentPresent);
        Assert.False(card.IsAnimating(Border.CornerRadiusProperty));
        Assert.Equal(new CornerRadius(4), card.CornerRadius);
    });

    [Fact]
    public Task ChildAnimation_IsMeasuredIndependently_AndExtendsParentExit() => OnUi(() =>
    {
        var child = new AnimateEnterExit
        {
            Enter = EnterTransition.None,
            Exit = new SlideOutHorizontally { TargetOffsetRatio = 1, Spec = Tween<Vector>(300) },
            Child = new Border { Width = 60, Height = 40 }
        };
        var host = Host();
        host.Content = new StackPanel { Children = { new Border { Width = 100, Height = 40 }, child } };
        using var f = new Fixture(host);
        Assert.Same(child, child.Child!.Parent);
        host.Visible = false;
        f.Step(16);
        f.Step(150);
        Assert.True(host.IsContentPresent);
        var surface = child.GetVisualDescendants().OfType<VisibilitySurface>().First();
        var transform = Assert.IsType<MatrixTransform>(surface.RenderTransform);
        Assert.InRange(transform.Matrix.M31, 29.9, 30.1);
        f.Finish();
        Assert.False(host.IsContentPresent);
    });

    [Fact]
    public Task ShrinkChangesLayoutWithoutRemeasuringContentAtAnimatedWidth() => OnUi(() =>
    {
        var child = new RecordingControl();
        var host = Host();
        host.Content = child;
        host.Exit = new ShrinkHorizontally
        {
            ShrinkTowards = VisibilityHorizontalAlignment.Start, Spec = Tween<Size>()
        };
        using var f = new Fixture(host);
        var originalConstraint = child.LastConstraint;
        host.Visible = false;
        f.Step(16);
        f.Step(50);
        Assert.InRange(host.DesiredSize.Width, 49, 51);
        Assert.Equal(80, host.DesiredSize.Height);
        Assert.Equal(originalConstraint, child.LastConstraint);
        Assert.Equal(100, child.Bounds.Width);
        Assert.True(Layer(host).ClipToBounds);
        f.Finish();
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task DefaultAxisSlides_UseHalfNegativeDimensionAsCompose(bool vertical, bool entering) => OnUi(() =>
    {
        var host = Host(!entering);
        host.Enter = vertical
            ? new SlideInVertically { Spec = Tween<Vector>() }
            : new SlideInHorizontally { Spec = Tween<Vector>() };
        host.Exit = vertical
            ? new SlideOutVertically { Spec = Tween<Vector>() }
            : new SlideOutHorizontally { Spec = Tween<Vector>() };
        using var f = new Fixture(host);
        host.Visible = entering;
        f.Step(16);
        f.Step(50);
        // 内容 100×80；时间中点位移为默认终点 -50/-40 的一半。
        var transform = Assert.IsType<MatrixTransform>(Layer(host).Surface.RenderTransform);
        Assert.InRange(transform.Matrix.M31, vertical ? -0.01 : -25.01, vertical ? 0.01 : -24.99);
        Assert.InRange(transform.Matrix.M32, vertical ? -20.01 : -0.01, vertical ? -19.99 : 0.01);
        f.Finish();
    });

    private sealed class RecordingControl : Control
    {
        public Size LastConstraint { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            LastConstraint = availableSize;
            return new Size(100, 80);
        }
    }

    [Fact]
    public Task MutableState_AllowsInitialEntry_AndReportsCompletion() => OnUi(() =>
    {
        var state = new MutableTransitionState<bool>(false) { TargetState = true };
        var host = Host();
        host.Source = VisibilitySource.From(state);
        using var f = new Fixture(host);
        Assert.Equal(0, Layer(host).Surface.Opacity);
        Assert.False(state.IsIdle);
        f.Finish();
        Assert.True(state.IsIdle);
        Assert.True(state.CurrentState);
        host.Visible = false; // Source owns the target.
        Assert.True(host.TargetVisible);
        state.TargetState = false;
        f.Finish();
        Assert.False(state.CurrentState);
        Assert.True(state.IsIdle);
    });

    [Fact]
    public Task ParentTransition_WaitsForVisibility_AndReleasesAtTheDrainAfterHostDetach() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var parent = new Transition<int>(1);
        var loop = parent.RunAsync(window.AnimationRoot);
        var host = Host();
        host.Source = VisibilitySource.From(parent, s => s == 1);
        using var f = new Fixture(host);
        parent.UpdateTarget(0);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs();
        f.Layout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        f.Layout();
        Assert.True(host.IsContentPresent);
        Assert.True(parent.IsRunning);
        for (var i = 0; i < 50 && host.IsContentPresent; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs();
            f.Layout();
        }
        Assert.False(host.IsContentPresent);
        Assert.Equal(0, parent.CurrentState);
        f.Window.Content = null;
        Assert.NotEmpty(parent.Transitions); // still connected until the detached scope ends
        Dispatcher.UIThread.RunJobs(); // the drain (fallback post) ends the scope and releases
        Assert.Empty(parent.Transitions);
        parent.UpdateTarget(2);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, parent.CurrentState);
        Assert.False(loop.IsFaulted);
        window.Close();
    });

    [Fact]
    public Task DetachReleasesResources_AndReattachStartsFresh() => OnUi(() =>
    {
        var host = Host();
        var released = 0;
        host.ScopeCreated += (_, e) => e.Scope.Own(new ActionDisposable(() => released++));
        using var f = new Fixture(host);
        var scope = host.Scope;
        host.Visible = false;
        f.Step(16);
        f.Window.Content = null;
        f.Layout();
        Assert.Null(host.Scope);
        Assert.Equal(1, released);
        Assert.Equal(0, f.Frames.PendingFrameCount);
        host.Visible = true;
        f.Window.Content = host;
        f.Layout();
        Assert.NotSame(scope, host.Scope);
        Assert.Equal(1, Layer(host).Surface.Opacity);
    });

    [Fact]
    public Task InternalEffects_PreserveContentOpacityAndTransform() => OnUi(() =>
    {
        var host = Host();
        var content = (Border)host.Content!;
        var transform = new RotateTransform(30);
        content.RenderTransform = transform;
        content.Opacity = 0.4;
        host.Exit = new ScaleOut { Spec = Tween<double>() } + new FadeOut { Spec = Tween<double>() };
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16);
        f.Step(50);
        Assert.Equal(0.4, content.Opacity);
        Assert.Same(transform, content.RenderTransform);
        Assert.InRange(Layer(host).Surface.Opacity, 0.49, 0.51);
        Assert.InRange(Assert.IsType<MatrixTransform>(Layer(host).Surface.RenderTransform).Matrix.M11, 0.49, 0.51);
    });

    [Fact]
    public Task Xaml_CanDeclareGroupsSpecsAndChildEffects() => OnUi(() =>
    {
        var host = AvaloniaRuntimeXamlLoader.Parse<AnimatedVisibility>("""
            <ca:AnimatedVisibility xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation" Visible="False">
                <ca:AnimatedVisibility.Enter>
                    <ca:EnterTransitionGroup>
                        <ca:FadeIn>
                            <ca:FadeIn.Spec><ca:DoubleTweenSpec Duration="0:0:0.2" /></ca:FadeIn.Spec>
                        </ca:FadeIn>
                        <ca:ExpandVertically ExpandFrom="Top" />
                    </ca:EnterTransitionGroup>
                </ca:AnimatedVisibility.Enter>
                <ca:AnimatedVisibility.Exit>
                    <ca:ExitTransitionGroup><ca:FadeOut /><ca:ShrinkVertically /></ca:ExitTransitionGroup>
                </ca:AnimatedVisibility.Exit>
                <ca:AnimateEnterExit><Border Width="100" Height="80" /></ca:AnimateEnterExit>
            </ca:AnimatedVisibility>
            """);
        Assert.Equal(2, Assert.IsType<EnterTransitionGroup>(host.Enter).Children.Count);
        using var f = new Fixture(host);
        host.Visible = true;
        f.Layout();
        f.Finish();
        Assert.True(host.IsContentPresent);
    });

    [Fact]
    public Task SharedEffects_HaveIndependentPlayback_AndLastSameKindWins() => OnUi(() =>
    {
        var exit = new FadeOut { TargetAlpha = 0.25, Spec = Tween<double>() }
            + new FadeOut { TargetAlpha = 0.75, Spec = Tween<double>() };
        using var first = new Fixture(Host());
        using var second = new Fixture(Host());
        first.Host.Exit = second.Host.Exit = exit;
        first.Host.Visible = false;
        first.Step(16);
        first.Step(50);
        Assert.InRange(Layer(first.Host).Surface.Opacity, 0.874, 0.876);
        Assert.Equal(1, Layer(second.Host).Surface.Opacity);
        Assert.True(second.Host.TargetVisible);
    });

    [Fact]
    public Task ZeroDurationScale_CompletesOnNextFrame_AndNoneStillRegistersCustomAnimation() => OnUi(() =>
    {
        var previous = MotionDurationScale.ScaleFactor;
        MotionDurationScale.ScaleFactor = 0;
        try
        {
            var host = Host(false);
            host.Enter = EnterTransition.None;
            host.Exit = ExitTransition.None;
            var count = 0;
            host.ScopeCreated += (_, e) =>
            {
                count++;
                e.Scope.Transition.AnimateDouble(s => s == EnterExitState.Visible ? 1 : 0, _ => Tween<double>(500));
            };
            using var f = new Fixture(host);
            host.Visible = true;
            f.Layout();
            Assert.Equal(1, count);
            f.Step(16);
            Assert.True(host.IsIdle);
            host.Visible = false;
            Assert.True(host.IsContentPresent);
            f.Step(16);
            Assert.False(host.IsContentPresent);
        }
        finally { MotionDurationScale.ScaleFactor = previous; }
    });

    [Fact]
    public Task ContentTemplate_IsNotBuiltWhileInitiallyHidden() => OnUi(() =>
    {
        var built = 0;
        var host = Host(false);
        host.Content = "data";
        host.ContentTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            built++;
            return new Border { Width = 100, Height = 80 };
        });
        using var f = new Fixture(host);
        Assert.Equal(0, built);
        host.Visible = true;
        f.Layout();
        Assert.Equal(1, built);
        f.Finish();
    });

    [Fact]
    public Task ScopeCreated_CanReverseTargetBeforeFirstLayout() => OnUi(() =>
    {
        var host = Host(false);
        host.Enter = EnterTransition.None;
        host.Exit = ExitTransition.None;
        host.ScopeCreated += (_, _) => host.Visible = false;
        using var f = new Fixture(host);
        host.Visible = true;
        f.Layout();
        f.Finish();
        Assert.False(host.TargetVisible);
        Assert.False(host.IsContentPresent);
        Assert.Null(host.Scope);
    });

    [Fact]
    public Task EnterThenStepThenReverse_UsesPostExitNotPreEnter() => OnUi(() =>
    {
        var host = Host(false);
        host.Enter = new FadeIn { Spec = Tween<double>(200) };
        host.Exit = new FadeOut { Spec = Tween<double>(200) };
        using var f = new Fixture(host);
        host.Visible = true;
        f.Step(16);
        f.Step(32);
        Assert.True(host.IsContentPresent);
        Assert.InRange(Layer(host).Surface.Opacity, 0.1, 0.3);
        host.Visible = false;
        f.Finish();
        Assert.False(host.IsContentPresent);
        Assert.Null(host.Scope);
    });

    [Fact]
    public Task KeepUntilTransitionsFinished_IsNoOpOnAnimatedVisibility() => OnUi(() =>
    {
        var host = Host();
        host.Exit = new FadeOut { Spec = Tween<double>() } + ExitTransition.KeepUntilTransitionsFinished;
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16);
        f.Step(16);
        Assert.True(host.IsContentPresent);
        Assert.InRange(Layer(host).Surface.Opacity, 0.8, 0.9);
        f.Finish();
        Assert.False(host.IsContentPresent);
        Assert.Null(host.Scope);
    });

    [Fact]
    public Task RapidToggleBeforeLayout_DoesNotDisposeCurrentPresentation() => OnUi(() =>
    {
        using var f = new Fixture(Host(false));
        f.Host.Visible = true;
        var scope = f.Host.Scope;
        f.Host.Visible = false;
        f.Host.Visible = true;
        f.Layout();
        f.Finish();
        Assert.True(f.Host.IsContentPresent);
        Assert.Same(scope, f.Host.Scope);
    });

    [Fact]
    public Task NativeVisibilityAndContentReplacement_ReleaseTheOldScope() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        var released = 0;
        f.Host.Scope!.Own(new ActionDisposable(() => released++));
        var scope = f.Host.Scope;
        f.Host.Content = new Border { Width = 40, Height = 30 };
        f.Layout();
        Assert.NotSame(scope, f.Host.Scope);
        Assert.Equal(1, released);
        f.Host.IsVisible = false;
        Assert.False(f.Host.IsContentPresent);
        Assert.Null(f.Host.Scope);
        f.Host.IsVisible = true;
        f.Layout();
        Assert.True(f.Host.IsContentPresent);
        Assert.Equal(1, Layer(f.Host).Surface.Opacity);
    });

    [Fact]
    public Task ParentCanBeDisposedWhileHostIsAttached() => OnUi(() =>
    {
        var parent = new Transition<bool>(true);
        var host = Host();
        host.Source = VisibilitySource.From(parent, s => s);
        using var f = new Fixture(host);
        parent.Dispose();
        f.Layout();
        Assert.Null(host.Scope);
        Assert.False(host.IsContentPresent);
        Assert.Empty(parent.Transitions);
    });

    [Fact]
    public Task SharedParentWaitsForLongestHostWithoutDrivingOwnClocks() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var parent = new Transition<bool>(true);
        _ = parent.RunAsync(window.AnimationRoot);
        var a = Host();
        var b = Host();
        a.Source = b.Source = VisibilitySource.From(parent, s => s);
        b.Exit = new FadeOut { Spec = Tween<double>(300) };
        using var first = new Fixture(a);
        using var second = new Fixture(b);
        parent.UpdateTarget(false);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        first.Layout(); second.Layout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        first.Layout(); second.Layout();
        Assert.False(a.IsContentPresent);
        Assert.True(b.IsContentPresent);
        Assert.True(parent.IsRunning);
        Assert.Equal(0, first.Frames.PendingFrameCount);
        Assert.Equal(0, second.Frames.PendingFrameCount);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
        first.Layout(); second.Layout();
        Assert.False(b.IsContentPresent);
        Assert.False(parent.IsRunning);
        window.Close();
    });

    [Fact]
    public Task ResizingWhileEntering_RetargetsFullSizeAndEndsAtNewSize() => OnUi(() =>
    {
        var host = Host(false);
        host.Enter = new ExpandVertically { Spec = Tween<Size>(300) };
        using var f = new Fixture(host);
        host.Visible = true;
        f.Step(16);
        f.Step(100);
        var before = host.DesiredSize.Height;
        ((Border)host.Content!).Height = 160;
        f.Layout();
        Assert.InRange(host.DesiredSize.Height, before - 1, before + 1);
        f.Finish();
        Assert.Equal(160, host.DesiredSize.Height);
        Assert.Equal(160, ((Border)host.Content!).Bounds.Height);
    });

    [Fact]
    public Task ReplacingChildWhileExiting_UnregistersRemovedAnimation() => OnUi(() =>
    {
        var child = new AnimateEnterExit
        {
            Exit = new FadeOut { Spec = Tween<double>(2000) },
            Child = new Border { Width = 40, Height = 40 }
        };
        var panel = new StackPanel { Children = { child } };
        var host = Host();
        host.Content = panel;
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16);
        f.Step(50);
        panel.Children.Clear();
        f.Step(100);
        Assert.False(host.IsContentPresent);
    });

    [Fact]
    public Task ChildReplacementPreservesLogicalParentAndInheritedDataContext() => OnUi(() =>
    {
        var decorator = new AnimateEnterExit { Child = new Border() };
        var host = Host();
        host.DataContext = "inherited";
        host.Content = decorator;
        using var f = new Fixture(host);
        var old = decorator.Child!;
        decorator.Child = new Border { Width = 30, Height = 20 };
        f.Layout();
        Assert.Null(old.Parent);
        Assert.Null(old.GetVisualParent());
        Assert.Same(decorator, decorator.Child.Parent);
        Assert.Equal("inherited", decorator.Child.DataContext);
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight, -50)]
    [InlineData(FlowDirection.RightToLeft, 0)]
    public Task EndAnchorRespectsFlowDirection(FlowDirection direction, double expectedX) => OnUi(() =>
    {
        var host = Host();
        host.FlowDirection = direction;
        host.Enter = new ExpandHorizontally();
        host.Exit = new ShrinkHorizontally { Spec = Tween<Size>() };
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16); f.Step(50);
        Assert.InRange(Layer(host).Surface.Bounds.X, expectedX - 1, expectedX + 1);
    });

    [Fact]
    public Task ChangingAnchorOnInterruption_DoesNotJumpContentPosition() => OnUi(() =>
    {
        var host = Host(false);
        host.Enter = new ExpandHorizontally { ExpandFrom = VisibilityHorizontalAlignment.Start, Spec = Tween<Size>(300) };
        host.Exit = new ShrinkHorizontally { ShrinkTowards = VisibilityHorizontalAlignment.End, Spec = Tween<Size>(300) };
        using var f = new Fixture(host);
        host.Visible = true;
        f.Step(16); f.Step(100);
        var position = Layer(host).Surface.Bounds.X;
        host.Visible = false;
        f.Layout();
        Assert.Equal(position, Layer(host).Surface.Bounds.X);
        f.Finish();
        Assert.False(host.IsContentPresent);
    });


    private sealed class FailedClock : IFrameSource
    {
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
            => ValueTask.FromException<TimeSpan>(new InvalidOperationException("Test clock failed."));
    }

    [Fact]
    public Task BindRejectsAnimationFromDifferentScope() => OnUi(() =>
    {
        using var first = new Fixture(Host());
        using var second = new Fixture(Host());
        var animation = first.Host.Scope!.Transition.AnimateDouble(_ => 0.5);
        Assert.Throws<ArgumentException>(() => second.Host.Scope!.Bind(
            (Border)second.Host.Content!, Visual.OpacityProperty, animation));
    });

    [Fact]
    public Task ParentSeekingSamplesTheWholePresentationAndKeepsHiddenEndpoint() => OnUi(() =>
    {
        using var parent = new Transition<bool>(true);
        var host = Host();
        host.Source = VisibilitySource.From(parent, s => s);
        using var f = new Fixture(host);
        parent.SetPlaytimeAfterInitialAndTargetStateEstablished(true, false, TimeSpan.FromMilliseconds(50));
        f.Layout();
        Assert.InRange(Layer(host).Surface.Opacity, 0.49, 0.51);
        parent.SetPlaytimeAfterInitialAndTargetStateEstablished(true, false, TimeSpan.FromMilliseconds(100));
        f.Layout();
        Assert.True(host.IsContentPresent);
        Assert.False(host.IsIdle);
        Assert.Equal(0, Layer(host).Surface.Opacity);
        parent.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, TimeSpan.FromMilliseconds(25));
        f.Layout();
        Assert.InRange(Layer(host).Surface.Opacity, 0.24, 0.26);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task ScopeBindingRetainsOriginalOneWayBindingDuringAnimationAndAfterRelease() => OnUi(() =>
    {
        var source = new Border { CornerRadius = new CornerRadius(4) };
        var host = Host();
        var card = (Border)host.Content!;
        card.Bind(Border.CornerRadiusProperty, new Binding(nameof(Border.CornerRadius))
        {
            Source = source, Mode = BindingMode.OneWay
        });
        host.ScopeCreated += (_, e) => e.Scope.Bind(card, Border.CornerRadiusProperty,
            e.Scope.Transition.AnimateCornerRadius(s => new CornerRadius(s == EnterExitState.PostExit ? 24 : 4),
                _ => Tween<CornerRadius>(300)));
        using var f = new Fixture(host);
        host.Visible = false;
        f.Step(16); f.Step(150);
        source.CornerRadius = new CornerRadius(8);
        Assert.InRange(card.CornerRadius.TopLeft, 13.9, 14.1);
        Assert.Equal(new CornerRadius(8), source.CornerRadius);
        f.Finish();
        Assert.Equal(new CornerRadius(8), card.CornerRadius);
        source.CornerRadius = new CornerRadius(12);
        Assert.Equal(new CornerRadius(12), card.CornerRadius);
    });

    [Fact]
    public Task NestedVisibilityUsesItsOwnScopeAndDoesNotDelayOuterExit() => OnUi(() =>
    {
        var inner = Host();
        inner.Exit = new FadeOut { Spec = Tween<double>(3000) };
        var outer = Host();
        outer.Content = inner;
        using var f = new Fixture(outer);
        Assert.NotSame(inner.Scope, outer.Scope);
        inner.Visible = false;
        outer.Visible = false;
        f.Finish();
        Assert.False(outer.IsContentPresent);
        Assert.Null(inner.Scope);
    });

    [Fact]
    public Task ThrowingResourceDoesNotPreventOtherBindingsBeingReleased() => OnUi(() =>
    {
        var host = Host();
        var card = (Border)host.Content!;
        host.ScopeCreated += (_, e) =>
        {
            e.Scope.Own(new ActionDisposable(() => throw new InvalidOperationException("cleanup")));
            e.Scope.Bind(card, Border.CornerRadiusProperty,
                e.Scope.Transition.AnimateCornerRadius(_ => new CornerRadius(10)));
        };
        using var f = new Fixture(host);
        host.Visible = false;
        for (var i = 0; i < 100 && host.IsContentPresent; i++) f.Step(16);
        Assert.False(host.IsContentPresent);
        Assert.False(card.IsAnimating(Border.CornerRadiusProperty));
        Assert.NotNull(((AnimationTestWindow)TopLevel.GetTopLevel(host)!).AnimationRoot.Diagnostics.LastError);
    });

    [Fact]
    public Task EnterConfigurationCanBeReplacedBetweenPresentations() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        f.Host.Visible = false;
        f.Finish();
        f.Host.Enter = new FadeIn { InitialAlpha = 0.3, Spec = Tween<double>() };
        f.Host.Visible = true;
        f.Layout();
        Assert.InRange(Layer(f.Host).Surface.Opacity, 0.299, 0.301);
        f.Finish();
        Assert.Equal(1, Layer(f.Host).Surface.Opacity);
    });
}
