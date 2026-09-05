using ComposeAnimationRuntime;
using System.Numerics;
using Xunit;

namespace ComposeAnimationRuntime.Tests;

public sealed class AnimatableTests
{
    [Fact]
    public void TweenReachesTarget()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(10, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        animation.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(5, animation.Value, 3);
        animation.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(10, animation.Value);
        Assert.False(animation.IsRunning);
    }

    [Fact]
    public void SpringRetargetsFromCurrentValue()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        var spec = new SpringAnimationSpec<float>(new ScalarVectorConverter(), 0.8f, 380);
        animation.AnimateTo(1, spec);
        animation.Tick(TimeSpan.FromMilliseconds(100));
        var before = animation.Value;
        animation.AnimateTo(0, spec);
        animation.Tick(TimeSpan.FromMilliseconds(1));
        Assert.True(animation.Value > 0);
        Assert.NotEqual(before, animation.Value);
    }

    [Fact]
    public void SnapStopsImmediately()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        animation.SnapTo(0.25f);
        Assert.Equal(0.25f, animation.Value);
        Assert.False(animation.IsRunning);
    }

    [Fact]
    public void PauseAndResumePreserveAnimationState()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        animation.Tick(TimeSpan.FromMilliseconds(250));
        var pausedValue = animation.Value;

        animation.Pause();
        Assert.True(animation.IsPaused);
        Assert.False(animation.IsRunning);
        animation.Tick(TimeSpan.FromMilliseconds(250));
        Assert.Equal(pausedValue, animation.Value);

        animation.Resume();
        animation.Tick(TimeSpan.FromMilliseconds(250));
        Assert.True(animation.Value > pausedValue);
    }

    [Fact]
    public void DecaySlowsToRest()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateDecay(10, new DecaySpec<float>(new ScalarVectorConverter(), friction: 4));

        animation.Tick(TimeSpan.FromSeconds(1));

        Assert.True(animation.Value > 2);
        Assert.True(animation.Velocity < 1);
        Assert.True(animation.IsRunning);
    }

    [Fact]
    public void ManualClockAdvancesRegisteredAnimationsTogether()
    {
        var first = new Animatable<float>(0, new ScalarVectorConverter());
        var second = new Animatable<float>(10, new ScalarVectorConverter());
        var spec = new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1));
        first.AnimateTo(1, spec);
        second.AnimateTo(0, spec);
        var clock = new ManualAnimationClock();
        clock.Register(first);
        clock.Register(second);

        Assert.True(clock.AdvanceBy(TimeSpan.FromMilliseconds(250)));
        Assert.Equal(0.25f, first.Value, 3);
        Assert.Equal(7.5f, second.Value, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(250), clock.Time);
    }

    [Fact]
    public void InvalidSpecsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpringAnimationSpec<float>(new ScalarVectorConverter(), stiffness: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecaySpec<float>(new ScalarVectorConverter(), friction: 0));
    }

    [Fact]
    public void KeyframesUsePerSegmentEasing()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(10, new KeyframesSpec<float>(converter, TimeSpan.FromSeconds(1), new(TimeSpan.Zero, 0), new(TimeSpan.FromMilliseconds(500), 10, Easings.EaseIn), new(TimeSpan.FromSeconds(1), 0)));
        animation.Tick(TimeSpan.FromMilliseconds(250));
        Assert.Equal(2.5f, animation.Value, 2);
    }

    [Fact]
    public void RepeatableSpecFinishesAfterRequestedIterations()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(1, new RepeatableSpec<float>(new TweenSpec<float>(converter, TimeSpan.FromMilliseconds(100)), 2, reverse: true));
        animation.Tick(TimeSpan.FromMilliseconds(200));
        Assert.False(animation.IsRunning);
        Assert.Equal(1, animation.Value);
    }

    [Fact]
    public void TransitionCoordinatesRegisteredTracks()
    {
        var converter = new ScalarVectorConverter();
        var value = new Animatable<float>(0, converter);
        var transition = new MotionTransition<bool>(false);
        transition.Register("opacity", value, state => state ? 1 : 0,
            _ => new TweenSpec<float>(converter, TimeSpan.FromSeconds(1)));

        transition.AnimateTo(true);
        Assert.True(transition.IsRunning);
        transition.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0.5f, value.Value, 3);
        transition.Tick(TimeSpan.FromMilliseconds(500));
        Assert.False(transition.IsRunning);
        Assert.True(transition.CurrentState);
    }

    [Fact]
    public void ArcReachesTarget()
    {
        var converter = VectorConverter.Create(
            value => new AnimationVector2D((float)value.X, (float)value.Y),
            vector => new System.Numerics.Vector2(vector[0], vector[1]),
            new System.Numerics.Vector2(0.001f));
        var animation = new Animatable<System.Numerics.Vector2>(new(0, 0), converter);
        animation.AnimateTo(new(10, 20), new ArcSpec<System.Numerics.Vector2>(converter, TimeSpan.FromSeconds(1)));
        animation.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(new System.Numerics.Vector2(10, 20), animation.Value);
    }

    [Fact]
    public void VisibilityRemainsVisibleUntilExitCompletes()
    {
        var spec = new TweenSpec<double>(new DoubleVectorConverter(), TimeSpan.FromMilliseconds(100));
        var visibility = new VisibilityMotion(true, spec, spec);
        visibility.Hide();
        visibility.Tick(TimeSpan.FromMilliseconds(50));
        Assert.Equal(VisibilityState.Visible, visibility.State);
        visibility.Tick(TimeSpan.FromMilliseconds(50));
        Assert.Equal(VisibilityState.Hidden, visibility.State);
    }

    [Fact]
    public void TransitionSessionCoordinatesLifecycle()
    {
        var clock = new ManualAnimationClock();
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        var session = new TransitionSession(clock);
        session.Add(animation.Tick, () => animation.IsRunning);
        session.Start();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0.25f, animation.Value, 3);
        session.Pause();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0.25f, animation.Value, 3);
        session.Resume();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(750));
        Assert.Equal(TransitionSessionState.Completed, session.State);
    }

    [Fact]
    public void GestureControllerClampsDragAndSettles()
    {
        var gesture = new GestureController();
        gesture.SetBounds(new System.Numerics.Vector2(-10), new System.Numerics.Vector2(10));
        gesture.Begin(default, TimeSpan.Zero);
        gesture.Update(new System.Numerics.Vector2(20), TimeSpan.FromMilliseconds(100));
        Assert.Equal(new System.Numerics.Vector2(10), gesture.Position);
        gesture.End(fling: false);
        gesture.Tick(TimeSpan.Zero);
        Assert.Equal(GestureState.Completed, gesture.State);
    }

    [Fact]
    public void GestureControllerSupportsSpringSettle()
    {
        var gesture = new GestureController(new System.Numerics.Vector2(10, 0));
        gesture.Begin(default, TimeSpan.Zero);
        gesture.End(fling: false, settleTarget: new System.Numerics.Vector2(0, 0));
        gesture.Tick(TimeSpan.FromMilliseconds(100));
        Assert.True(gesture.Position.X < 10);
    }

    [Fact]
    public void InteractiveTransitionSettlesByThresholdAndVelocity()
    {
        var transition = new InteractiveTransition(TimeSpan.FromMilliseconds(100));
        transition.SetProgress(0.6);
        transition.Release();
        transition.Tick(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, transition.Progress);

        transition.SetProgress(0.2);
        transition.Release(velocity: -1);
        transition.Tick(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, transition.Progress);
    }

    [Fact]
    public void TransitionCoordinatorControlsGenericTracks()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        var coordinator = new TransitionCoordinator();
        coordinator.Add(animation);
        coordinator.Start();
        coordinator.Tick(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0.25f, animation.Value, 3);
        coordinator.Pause();
        coordinator.Tick(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0.25f, animation.Value, 3);
        coordinator.Complete();
        Assert.Equal(1, animation.Value);
    }

    [Fact]
    public void GestureFlingRemainsInsideBounds()
    {
        var gesture = new GestureController(new Vector2(10, 0));
        gesture.SetBounds(Vector2.Zero, new Vector2(20, 20));
        gesture.Begin(Vector2.Zero, TimeSpan.Zero);
        gesture.Update(new Vector2(10, 0), TimeSpan.FromMilliseconds(10));
        gesture.End();

        for (var i = 0; i < 120 && gesture.IsRunning; i++)
            gesture.Tick(TimeSpan.FromMilliseconds(16));

        Assert.InRange(gesture.Position.X, 0, 20);
        Assert.InRange(gesture.Position.Y, 0, 20);
    }

    [Fact]
    public void GestureIgnoresOutOfOrderTimestamps()
    {
        var gesture = new GestureController();
        gesture.SetBounds(Vector2.Zero, new Vector2(100, 100));
        gesture.Begin(Vector2.Zero, TimeSpan.FromMilliseconds(20));
        gesture.Update(new Vector2(10, 0), TimeSpan.FromMilliseconds(10));

        Assert.Equal(Vector2.Zero, gesture.Velocity);
        Assert.Equal(new Vector2(10, 0), gesture.Position);
    }

    [Fact]
    public void CubicBezierAndKeySplineProduceStableEasing()
    {
        var easing = Easings.CubicBezier(0.25, 0.1, 0.25, 1);
        Assert.Equal(0, easing(0), 6);
        Assert.Equal(1, easing(1), 6);
        Assert.InRange(easing(0.5), 0, 1);
        Assert.Equal(easing(0.5), new KeySpline(0.25, 0.1, 0.25, 1).Transform(0.5), 6);
    }

    [Fact]
    public void CubicBezierSpecInterpolatesVectorValues()
    {
        var converter = VectorConverter.Create<Vector2>(
            value => new AnimationVector2D(value.X, value.Y),
            vector => new Vector2(vector[0], vector[1]),
            new Vector2(0.001f));
        var animation = new Animatable<Vector2>(Vector2.Zero, converter);
        animation.AnimateTo(new Vector2(10, 0), new CubicBezierSpec<Vector2>(converter,
            TimeSpan.FromSeconds(1), new Vector2(0, 10), new Vector2(10, 10)));
        animation.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(5, animation.Value.X, 3);
        Assert.Equal(7.5f, animation.Value.Y, 3);
    }

    [Fact]
    public void DeferredTargetAnimationResolvesOnlyOnce()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        var deferred = new DeferredTargetAnimation<float>(animation,
            new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        var target = 10f;
        deferred.SetTarget(() => target);

        Assert.False(deferred.IsResolved);
        Assert.True(deferred.Resolve());
        target = 20;
        Assert.False(deferred.Resolve());
        deferred.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(5, deferred.Value, 3);
        deferred.Cancel();
    }

    [Fact]
    public void TransitionCoordinatorAcceptsPriorityForTrackReplacement()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        var coordinator = new TransitionCoordinator();
        coordinator.Add((ITransitionTrack)new TestTrack(animation), 1);
        coordinator.Add((ITransitionTrack)new TestTrack(animation), 0);
        coordinator.Start();
        Assert.True(coordinator.IsRunning);
    }

    [Fact]
    public void TransitionCoordinatorRejectsDuplicateTrackWhenConfigured()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        var coordinator = new TransitionCoordinator(conflictPolicy: TransitionConflictPolicy.Reject);
        coordinator.Add(animation);

        Assert.Throws<InvalidOperationException>(() => coordinator.Add(animation));
    }

    private sealed class TestTrack(Animatable<float> animation) : ITransitionTrack
    {
        public bool IsRunning => animation.IsRunning;
        public bool Tick(TimeSpan delta) => animation.Tick(delta);
        public void Pause() => animation.Pause();
        public void Resume() => animation.Resume();
        public void Complete() => animation.Settle();
        public void Cancel() => animation.Stop();
    }

    [Fact]
    public void PathSpecInterpolatesMultipleSegments()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(20, new PathSpec<float>(converter, TimeSpan.FromSeconds(1),
            new[] { 10f, 15f, 20f }));
        animation.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(12.5, animation.Value, 3);
    }

    [Fact]
    public void PathSpecAcceptsOneEasingPerGeneratedSegment()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(20, new PathSpec<float>(converter, TimeSpan.FromSeconds(1),
            new[] { 10f, 15f, 20f }, new[] { Easings.Linear, Easings.EaseIn, Easings.EaseOut }));
        animation.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(20, animation.Value, 3);
    }

    [Fact]
    public void SettleNotifiesFinalValue()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        var observed = -1f;
        animation.ValueChanged += value => observed = value;

        animation.AnimateTo(42, new TweenSpec<float>(converter, TimeSpan.FromSeconds(1)));
        animation.Settle();

        Assert.Equal(42, animation.Value);
        Assert.Equal(42, observed);
    }

}
