using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class TransitionBoundaryTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);
    private static TweenSpec<double> Tween(double duration = 100) => new(Ms(duration), easing: new LinearEasing());

    [Fact]
    public void StartDelay_PreservesInitialVelocityBeforeOffsetAndComposesWithTweenDelay()
    {
        var spec = new StartDelayAnimationSpec<double>(
            new TweenSpec<double>(Ms(100), Ms(20), new LinearEasing()), Ms(50));
        var animation = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 2, 12, 7);
        Assert.Equal(Ms(170), animation.Duration);
        Assert.Equal(2, animation.GetValue(Ms(49)));
        Assert.Equal(7, animation.GetVelocity(Ms(49)));
        Assert.Equal(2, animation.GetValue(Ms(50)));
        Assert.Equal(2, animation.GetValue(Ms(70)));
        Assert.Equal(7, animation.GetValue(Ms(120)), 8);
        Assert.False(animation.IsFinished(Ms(169)));
        Assert.True(animation.IsFinished(Ms(170)));
        Assert.Equal(12, animation.GetValue(Ms(170)));
        Assert.Equal(100, animation.GetVelocity(Ms(170)), 8);
    }

    [Fact]
    public void FiniteSpecs_DispatchThroughBothInterfaces()
    {
        IFiniteAnimationSpec<double>[] specs =
        [Tween(), new SpringSpec<double>(), new RepeatableSpec<double>(2, Tween())];
        foreach (var spec in specs)
        {
            IAnimationSpec<double> untyped = spec;
            var converter = new DoubleConverter();
            Assert.IsAssignableFrom<IVectorizedFiniteAnimationSpec>(untyped.Vectorize(converter));
            Assert.False(spec.Vectorize(converter).IsInfinite);
            Assert.Equal(spec.Vectorize(converter).GetDuration([0], [1], [0]),
                untyped.Vectorize(converter).GetDuration([0], [1], [0]));
        }
        IAnimationSpec<double> infinite = new InfiniteRepeatableSpec<double>(Tween());
        Assert.False(infinite is IFiniteAnimationSpec<double>);
    }

    [Fact]
    public void NestedSeek_AndDeferredRefreshUseCurrentManualTime()
    {
        using var root = new Transition<bool>(false);
        var child = root.CreateChildTransition(s => s);
        var grandchild = child.CreateChildTransition(s => s);
        var distance = 100.0;
        var value = grandchild.AnimateDouble(s => s ? distance : 0, _ => Tween());
        root.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, Ms(25));
        Assert.Equal(25, value.Value, 8);
        distance = 200;
        root.RefreshAnimations();
        Assert.Equal(50, value.Value, 8);
        Assert.Equal(Ms(25), grandchild.PlayTime);
        using var deferred = root.CreateDeferredAnimation(new DoubleConverter());
        var late = deferred.Animate(_ => Tween(), s => s ? 400 : 0);
        Assert.Equal(100, late.Value, 8);
        root.UpdateTarget(false);
        Assert.True(root.TargetState);
        Assert.True(root.IsSeeking);
    }

    [Fact]
    public void ReturningFromDeferredPhase_UsesManualValueInsteadOfMappedInitialState()
    {
        var state = new DeferredTransitionState<bool>(false);
        using var transition = new DeferredTransition<bool>(state);
        using var deferred = transition.CreateDeferredAnimation(new DoubleConverter());
        var value = deferred.Animate(_ => new SpringSpec<double>(), s => s ? 100 : 0);
        state.Defer(true);
        deferred.Animate(_ => new SpringSpec<double>(), 40, null, s => s ? 100 : 0);
        state.AnimateTo(false);
        Assert.Equal(new TransitionSegment<bool>(true, false), transition.Segment);
        Assert.Equal(40, value.Value);
        Assert.False(transition.HasPendingTargetState);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(16));
        Assert.InRange(value.Value, 0.01, 39.99);
        deferred.Dispose();
        Assert.Empty(transition.Animations);
        Assert.Throws<ObjectDisposedException>(() => deferred.Animate(_ => Tween(), _ => 10));
    }

    [Fact]
    public void EmptyTransitionAndRemovalOfLastChild_CanComplete()
    {
        using var root = new Transition<int>(0);
        root.UpdateTarget(1);
        root.OnFrame(Ms(0));
        Assert.Equal(1, root.CurrentState);
        Assert.False(root.IsRunning);
        var child = root.CreateChildTransition(s => s);
        child.AnimateDouble(s => s * 100, _ => Tween(1000));
        root.UpdateTarget(2);
        root.OnFrame(Ms(100));
        root.OnFrame(Ms(200));
        Assert.True(root.IsRunning);
        child.Dispose();
        root.OnFrame(Ms(216));
        Assert.Equal(2, root.CurrentState);
        Assert.False(root.IsRunning);
        Assert.Empty(root.Transitions);
    }

    [Fact]
    public void ScaleChange_RecalculatesPlayTimeFromStartAsUpstreamDoes()
    {
        using var root = new Transition<bool>(false);
        var value = root.AnimateDouble(s => s ? 100 : 0, _ => Tween(1000));
        root.DurationScale = 2;
        root.UpdateTarget(true);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(200));
        Assert.Equal(10, value.Value, 8);
        root.DurationScale = 1;
        root.OnFrame(Ms(300));
        Assert.Equal(30, value.Value, 8);
    }

    [Fact]
    public Task CancelledLoop_CanResumeWithoutCountingInactiveTime() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var root = new Transition<bool>(false);
        var value = root.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        root.UpdateTarget(true);
        using var firstCancellation = new CancellationTokenSource();
        var first = root.RunAsync(window.AnimationRoot, firstCancellation.Token);
        clock.Pump(Ms(0));
        clock.Pump(Ms(50));
        firstCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        clock.PumpTo(Ms(1000));

        using var secondCancellation = new CancellationTokenSource();
        var second = root.RunAsync(window.AnimationRoot, secondCancellation.Token);
        clock.Pump(Ms(0));
        Assert.Equal(50, value.Value, 8);
        clock.Pump(Ms(25));
        Assert.Equal(75, value.Value, 8);
        secondCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        window.Close();
    });

    [Fact]
    public void Disposal_CompletesLogicalStateWithoutSnappingAnimatedValue()
    {
        var state = new MutableTransitionState<bool>(false);
        var root = new Transition<bool>(state);
        var value = root.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        root.UpdateTarget(true);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(25));
        root.Dispose();
        Assert.Equal(25, value.Value, 8);
        Assert.True(state.CurrentState);
        Assert.True(state.IsIdle);
        Assert.Equal(TimeSpan.Zero, root.PlayTime);
    }

    [Fact]
    public void StateNotification_CanChainNextTargetAtCompletion()
    {
        var state = new MutableTransitionState<int>(0);
        using var root = new Transition<int>(state);
        var value = root.AnimateDouble(s => s * 100, _ => Tween());
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(state.CurrentState) && state.CurrentState == 1)
                state.TargetState = 2;
        };
        state.TargetState = 1;
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(100));
        Assert.Equal(2, root.TargetState);
        Assert.Equal(new TransitionSegment<int>(1, 2), root.Segment);
        Assert.False(state.IsIdle);
        root.OnFrame(Ms(116));
        root.OnFrame(Ms(166));
        Assert.Equal(150, value.Value, 8);
        root.OnFrame(Ms(216));
        Assert.True(state.IsIdle);
        Assert.Equal(2, root.CurrentState);
    }

    [Fact]
    public void RunningNotification_CanRetargetThroughMutableState()
    {
        var state = new MutableTransitionState<int>(0);
        using var root = new Transition<int>(state);
        var value = root.AnimateDouble(s => s * 100, _ => Tween());
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(state.IsRunning) && state.IsRunning)
                state.TargetState = 2;
        };
        state.TargetState = 1;
        root.OnFrame(Ms(0));
        Assert.Equal(2, root.TargetState);
        Assert.Equal(200, value.TargetValue);
        root.DurationScale = 0;
        root.OnFrame(Ms(16));
        Assert.Equal(200, value.Value);
        Assert.True(state.IsIdle);
    }
}
