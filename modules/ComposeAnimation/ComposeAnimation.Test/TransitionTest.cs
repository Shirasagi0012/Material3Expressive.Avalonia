using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class TransitionTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);
    private static TweenSpec<double> Tween(double duration = 100) => new(Ms(duration), easing: new LinearEasing());

    [Fact]
    public void SharedTimeline_WaitsForLongestAnimation()
    {
        var state = new MutableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, new ManualAnimationFrameClock());
        var alpha = transition.AnimateDouble(s => s ? 1 : 0, _ => Tween());
        var position = transition.AnimatePoint(s => s ? new Point(100, 200) : default,
            _ => new TweenSpec<Point>(Ms(200), easing: new LinearEasing()));

        state.TargetState = true;
        Assert.False(state.IsIdle);
        Assert.False(transition.IsRunning);
        transition.OnFrame(Ms(1000));
        transition.OnFrame(Ms(1050));
        Assert.Equal(0.5, alpha.Value, 8);
        Assert.Equal(new Point(25, 50), position.Value);
        Assert.Equal(Ms(200), transition.TotalDuration);
        Assert.Equal(Ms(50), transition.PlayTime);
        Assert.False(transition.CurrentState);

        transition.OnFrame(Ms(1100));
        Assert.True(alpha.IsFinished);
        Assert.False(position.IsFinished);
        Assert.True(transition.IsRunning);
        transition.OnFrame(Ms(1200));
        Assert.Equal(new Point(100, 200), position.Value);
        Assert.True(state.IsIdle);
        Assert.True(transition.CurrentState);
        Assert.False(transition.IsRunning);
        Assert.Equal(TimeSpan.Zero, transition.PlayTime);
    }

    [Fact]
    public void InitialTarget_CanBeSetBeforeConstruction()
    {
        var state = new MutableTransitionState<int>(0) { TargetState = 1 };
        using var transition = new Transition<int>(state, new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(s => s * 100, _ => Tween());
        Assert.Equal(new TransitionSegment<int>(0, 1), transition.Segment);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        Assert.Equal(50, value.Value, 8);
    }

    [Fact]
    public void InterruptedTween_UsesSpringAndPreservesValueVelocityAndTimebase()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(s => s * 100, _ => Tween(1000));
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(400));
        var oldValue = value.Value;
        var velocity = AnimationVector.From(value.VelocityVector);
        Assert.Equal(40, oldValue, 8);
        Assert.True(velocity[0] > 0);

        transition.UpdateTarget(2);
        var expected = new TargetBasedAnimation<double>(
            new SpringSpec<double>(visibilityThreshold: VisibilityThresholds.Double),
            new DoubleConverter(), oldValue, 200, initialVelocityVector: velocity);
        Assert.Equal(new TransitionSegment<int>(1, 2), transition.Segment);
        Assert.Equal(1, transition.CurrentState);
        Assert.Equal(oldValue, value.Value);
        Assert.Equal(velocity[0], value.VelocityVector[0]);
        Assert.Equal(Ms(400), transition.PlayTime);
        Assert.Equal(Ms(400) + expected.Duration, value.Duration);
        transition.OnFrame(Ms(416));
        Assert.Equal(expected.GetValue(Ms(16)), value.Value, 8);
        Assert.Equal(expected.GetVelocity(Ms(16)), value.Velocity, 8);
    }

    [Fact]
    public void InterruptedSpring_PreservesRequestedSpringAndIntegerVelocityPrecision()
    {
        var spring = new SpringSpec<int>(dampingRatio: 0.6, stiffness: 120, visibilityThreshold: 1);
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var value = transition.AnimateInt(s => s * 100, _ => spring);
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(73));
        var initial = value.Value;
        var velocity = AnimationVector.From(value.VelocityVector);
        Assert.NotEqual((double)value.Velocity, velocity[0]);
        transition.UpdateTarget(0);
        var expected = new TargetBasedAnimation<int>(spring, new Int32Converter(), initial, 0,
            initialVelocityVector: velocity);

        Assert.Equal(Ms(73) + expected.Duration, value.Duration);
        transition.OnFrame(Ms(89));
        var expectedVelocity = new double[1];
        expected.GetVelocityVector(Ms(16), expectedVelocity);
        Assert.Equal(expected.GetValue(Ms(16)), value.Value);
        Assert.Equal(expectedVelocity[0], value.VelocityVector[0], 8);
    }

    [Fact]
    public void FinishedChild_UsesNewTweenWhileOtherChildIsStillRunning()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var fast = transition.AnimateDouble(s => s * 10, _ => Tween(20));
        transition.AnimateDouble(s => s * 100, _ => Tween(1000));
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(40));
        Assert.True(fast.IsFinished);
        transition.UpdateTarget(2);
        Assert.Equal(Ms(60), fast.Duration);
        transition.OnFrame(Ms(50));
        Assert.Equal(15, fast.Value, 8);
    }

    [Fact]
    public void SameTarget_DoesNotRestartAndRefreshReevaluatesExternalInputs()
    {
        var distance = 100.0;
        using var transition = new Transition<bool>(false, new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(s => s ? distance : 0, _ => Tween());
        transition.UpdateTarget(true);
        var original = value.Animation;
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(100));
        transition.UpdateTarget(true);
        Assert.Same(original, value.Animation);
        distance = 200;
        transition.RefreshAnimations();
        Assert.NotSame(original, value.Animation);
        transition.OnFrame(Ms(200));
        transition.OnFrame(Ms(250));
        Assert.Equal(150, value.Value, 8);
        Assert.True(transition.IsRunning);
    }

    [Fact]
    public void NewState_WithUnchangedChannelTarget_ReconfiguresUsingSegmentSpec()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var alpha = transition.AnimateDouble(s => s == 0 ? 0 : 1,
            segment => segment.TargetState == 1 ? Tween(200) : new SnapSpec<double>());
        var changing = transition.AnimateDouble(s => s * 10, _ => Tween(200));
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(80));
        var original = alpha.Animation;
        var velocity = alpha.Velocity;
        var previousChanging = changing.Animation;

        transition.UpdateTarget(2);
        Assert.NotSame(original, alpha.Animation);
        Assert.Equal(0.4, alpha.Value, 8);
        Assert.Equal(velocity, alpha.Velocity);
        Assert.NotSame(previousChanging, changing.Animation);
        transition.OnFrame(Ms(120));
        var expected = new TargetBasedAnimation<double>(new SpringSpec<double>(visibilityThreshold: 0.01),
            new DoubleConverter(), 0.4, 1, velocity);
        Assert.Equal(expected.GetValue(Ms(40)), alpha.Value, 8);
        transition.OnFrame(Ms(2000));
        Assert.True(alpha.IsFinished);
        Assert.Equal(1, alpha.Value);
    }

    [Fact]
    public void LateRegistration_StartsAtCurrentParentTime()
    {
        using var transition = new Transition<bool>(false, new ManualAnimationFrameClock());
        transition.AnimateDouble(s => s ? 1 : 0, _ => Tween(200));
        transition.UpdateTarget(true);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(100));
        var late = transition.AnimateDouble(s => s ? 100 : 0, _ => Tween(300));
        Assert.Equal(Ms(400), transition.TotalDuration);
        transition.OnFrame(Ms(250));
        Assert.Equal(50, late.Value, 8);
        Assert.True(transition.IsRunning);
        late.Dispose();
        transition.OnFrame(Ms(260));
        Assert.False(transition.IsRunning);
        Assert.Equal(Ms(200), transition.TotalDuration);
        Assert.Single(transition.Animations);
    }

    [Fact]
    public void ChildTransition_MapsStateAndExtendsParentCompletion()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock(), "Parent");
        transition.AnimateDouble(s => s, _ => Tween(50));
        var child = transition.CreateChildTransition(s => s > 0, "Visible");
        var value = child.AnimateDouble(s => s ? 100 : 0, _ => Tween(200));
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(100));
        Assert.Equal(50, value.Value, 8);
        Assert.Equal(transition.PlayTime, child.PlayTime);
        Assert.Equal(Ms(200), transition.TotalDuration);
        var original = value.Animation;

        transition.UpdateTarget(2);
        Assert.Same(original, value.Animation);
        transition.OnFrame(Ms(150));
        Assert.Equal(75, value.Value, 8);
        transition.OnFrame(Ms(200));
        Assert.False(child.IsRunning);
        Assert.False(transition.IsRunning);
        Assert.Equal(2, transition.CurrentState);
        Assert.True(child.CurrentState);
        Assert.Same(transition, child.ParentTransition);
    }

    [Fact]
    public void DeferredAnimation_RegistersLazilyAndSupportsForcedHandoff()
    {
        var state = new DeferredTransitionState<bool>(false);
        using var transition = new DeferredTransition<bool>(state, new ManualAnimationFrameClock());
        using var deferred = transition.CreateDeferredAnimation(new DoubleConverter());
        Assert.Empty(transition.Animations);
        state.Defer(true);
        Assert.True(transition.HasPendingTargetState);
        Assert.False(transition.TargetState);
        state.AnimateTo(true);
        var velocity = AnimationVector.From(new double[] { 25 });
        var value = deferred.Animate(_ => new SpringSpec<double>(), 40, velocity, s => s ? 100 : 0);
        velocity[0] = -999;
        Assert.False(transition.HasPendingTargetState);
        Assert.Equal(40, value.Value);
        Assert.Equal(25, value.VelocityVector[0]);
        Assert.Single(transition.Animations);
        var expected = new TargetBasedAnimation<double>(new SpringSpec<double>(), new DoubleConverter(), 40, 100, 25);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(16));
        Assert.Equal(expected.GetValue(Ms(16)), value.Value, 8);
        Assert.Same(value, deferred.Animate(_ => new SpringSpec<double>(), s => s ? 100 : 0));
        Assert.Single(transition.Animations);
    }

    [Fact]
    public void DeferredTarget_PropagatesToChildAndClearsWithoutTransientRetarget()
    {
        var state = new DeferredTransitionState<int>(0);
        using var transition = new DeferredTransition<int>(state, new ManualAnimationFrameClock());
        var child = transition.CreateChildTransition(s => s > 0);
        var value = child.AnimateDouble(s => s ? 100 : 0, _ => Tween(100));
        state.Defer(1);
        Assert.True(child.HasPendingTargetState);
        Assert.True(child.PendingTargetState);
        state.AnimateTo(2);
        Assert.False(child.HasPendingTargetState);
        Assert.Equal(new TransitionSegment<int>(0, 2), transition.Segment);
        Assert.Equal(Ms(100), value.Duration);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        Assert.Equal(50, value.Value, 8);
    }

    [Fact]
    public void ManualSeek_SamplesWholeTreeAndRemainsInManualMode()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(s => s * 100, _ => Tween(1000));
        var child = transition.CreateChildTransition(s => s > 0);
        var childValue = child.AnimateDouble(s => s ? 200 : 0, _ => Tween(1000));
        transition.SetPlaytimeAfterInitialAndTargetStateEstablished(0, 1, Ms(250));
        Assert.Equal(25, value.Value, 8);
        Assert.Equal(50, childValue.Value, 8);
        Assert.True(transition.IsSeeking);
        Assert.True(child.IsSeeking);
        Assert.False(transition.IsRunning);
        transition.OnFrame(Ms(900));
        Assert.Equal(25, value.Value, 8);
        transition.SetPlaytimeAfterInitialAndTargetStateEstablished(0, 1, Ms(1500));
        Assert.Equal(100, value.Value);
        Assert.Equal(200, childValue.Value);
        transition.SetPlaytimeAfterInitialAndTargetStateEstablished(0, 1, Ms(100));
        Assert.Equal(10, value.Value, 8);
        Assert.False(value.IsFinished);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            transition.SetPlaytimeAfterInitialAndTargetStateEstablished(0, 1, Ms(-1)));
    }

    [Fact]
    public void Notifications_SeeAllSampledValuesAndAllowRetargetAndRemoval()
    {
        using var transition = new Transition<int>(0, new ManualAnimationFrameClock());
        var first = transition.AnimateDouble(s => s * 100, _ => Tween());
        var second = transition.AnimateDouble(s => s * 200, _ => Tween());
        var observed = false;
        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(first.Value) || observed) return;
            observed = true;
            Assert.Equal(first.Value * 2, second.Value, 8);
            second.Dispose();
            transition.UpdateTarget(2);
        };
        transition.UpdateTarget(1);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        Assert.True(observed);
        Assert.Single(transition.Animations);
        Assert.Equal(new TransitionSegment<int>(1, 2), transition.Segment);
        Assert.Equal(50, first.Value, 8);
        transition.OnFrame(Ms(66));
        Assert.True(first.Value > 50);
    }

    [Fact]
    public void DurationScale_SlowsTimeAndZeroFinishesAllChildren()
    {
        using var transition = new Transition<bool>(false, new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        var child = transition.CreateChildTransition(s => s);
        var childValue = child.AnimateDouble(s => s ? 200 : 0, _ => Tween(200));
        transition.DurationScale = 2;
        transition.UpdateTarget(true);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(100));
        Assert.Equal(50, value.Value, 8);
        transition.DurationScale = 0;
        transition.OnFrame(Ms(116));
        Assert.Equal(100, value.Value);
        Assert.Equal(200, childValue.Value);
        Assert.False(transition.IsRunning);
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.DurationScale = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.DurationScale = -1);
    }

    [Fact]
    public void Dispose_DetachesStateAndChildrenAndIsIdempotent()
    {
        var state = new MutableTransitionState<bool>(false);
        var transition = new Transition<bool>(state, new ManualAnimationFrameClock());
        var child = transition.CreateChildTransition(s => s);
        var value = child.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        state.TargetState = true;
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        transition.Dispose();
        transition.Dispose();
        child.Dispose();
        value.Dispose();
        state.TargetState = false;
        Assert.False(state.IsRunning);
        Assert.Equal(50, value.Value, 8);
        Assert.True(transition.TargetState);
        Assert.Empty(transition.Transitions);
        Assert.Throws<ObjectDisposedException>(() => transition.UpdateTarget(false));
        Assert.Throws<ObjectDisposedException>(() => value.Update(_ => 1));
    }

    [Fact]
    public async Task RunAsync_UsesOneClockWaitAndCancellationDoesNotPretendCompletion()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new MutableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        var child = transition.CreateChildTransition(s => s);
        child.AnimateDouble(s => s ? 200 : 0, _ => Tween());
        state.TargetState = true;
        using var cts = new CancellationTokenSource();
        var task = transition.RunAsync(cts.Token);
        Assert.Equal(1, clock.PendingFrameCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transition.RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => child.RunAsync());
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(50));
        Assert.Equal(50, value.Value, 8);
        Assert.True(transition.IsRunning);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(transition.IsRunning);
        Assert.False(child.IsRunning);
        Assert.False(state.IsIdle);
        Assert.False(transition.CurrentState);
        Assert.Equal(50, value.Value, 8);
    }

    [Fact]
    public async Task IdleLoop_WakesForTargetsAndDisposalCancelsIt()
    {
        var clock = new ManualAnimationFrameClock();
        var transition = new Transition<bool>(false, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        var task = transition.RunAsync();
        Assert.Equal(0, clock.PendingFrameCount);
        transition.UpdateTarget(true);
        for (var i = 0; i < 1000 && clock.PendingFrameCount == 0; i++) await Task.Yield();
        Assert.Equal(1, clock.PendingFrameCount);
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(100));
        Assert.Equal(100, value.Value);
        Assert.False(transition.IsRunning);
        Assert.False(task.IsCompleted);
        Assert.Equal(0, clock.PendingFrameCount);
        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task NotificationException_FaultsLoopAndClearsRunningFlags()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new Transition<bool>(false, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        transition.UpdateTarget(true);
        var error = new InvalidOperationException("observer failed");
        value.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(value.Value)) throw error;
        };
        var task = transition.RunAsync();
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(50));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.False(transition.IsRunning);
        Assert.False(transition.CurrentState);
    }
}
