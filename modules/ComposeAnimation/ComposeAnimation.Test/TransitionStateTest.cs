using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

using Avalonia = global::Avalonia;

public class TransitionStateTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);

    [Fact]
    public void MutableTarget_UsesSharedStorageAndNotifiesIdle()
    {
        var state = new MutableTransitionState<string>("A");
        TransitionState<string> baseState = state;
        var notifications = new List<string?>();
        state.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        Assert.True(state.IsIdle);
        state.TargetState = "B";

        Assert.Equal("A", state.CurrentState);
        Assert.Equal("B", baseState.TargetState);
        Assert.False(state.IsIdle);
        Assert.Equal(new[] { "TargetState", "IsIdle" }, notifications);

        notifications.Clear();
        baseState.TargetState = "C";
        Assert.Equal("C", state.TargetState);
        Assert.Equal(new[] { "TargetState", "IsIdle" }, notifications);
    }

    [Fact]
    public void EqualAssignments_DoNotNotify()
    {
        var state = new MutableTransitionState<string>("A");
        var notifications = new List<string?>();
        state.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        state.TargetState = new string('A', 1);
        state.CurrentState = new string('A', 1);
        state.IsRunning = false;

        Assert.Empty(notifications);
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void Idle_RequiresEqualStatesAndStoppedRuntime()
    {
        var state = new MutableTransitionState<int>(0);
        var idleValues = new List<bool>();
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(state.IsIdle)) idleValues.Add(state.IsIdle);
        };

        state.TargetState = 1;
        state.IsRunning = true;
        state.CurrentState = 1;
        Assert.False(state.IsIdle);

        state.IsRunning = false;
        Assert.True(state.IsIdle);
        Assert.Equal(new[] { false, false, false, true }, idleValues);
    }

    [Fact]
    public void Defer_LeavesCurrentRunUnchanged_AndCanReplaceOrClearPendingTarget()
    {
        var state = new DeferredTransitionState<string>("A");
        state.AnimateTo("B");
        state.IsRunning = true;

        state.Defer("C");
        Assert.Equal("A", state.CurrentState);
        Assert.Equal("B", state.TargetState);
        Assert.True(state.IsRunning);
        Assert.True(state.HasPendingTargetState);
        Assert.Equal("C", state.PendingTargetState);

        state.Defer("D");
        Assert.Equal("D", state.PendingTargetState);

        state.Defer("B");
        Assert.False(state.HasPendingTargetState);
        Assert.Null(state.PendingTargetState);
        Assert.Equal("B", state.TargetState);
        Assert.True(state.IsRunning);
    }

    [Fact]
    public void AnimateTo_ClearsDeferredTargetBeforeRequestingDifferentTarget()
    {
        var state = new DeferredTransitionState<string>("A");
        state.Defer("B");
        var notifications = new List<string?>();
        state.PropertyChanged += (_, e) =>
        {
            notifications.Add(e.PropertyName);
            Assert.False(state.HasPendingTargetState);
            Assert.Null(state.PendingTargetState);
        };

        state.AnimateTo("C");

        Assert.Equal("A", state.CurrentState);
        Assert.Equal("C", state.TargetState);
        Assert.False(state.IsRunning);
        Assert.Equal(new[] { "PendingTargetState", "HasPendingTargetState", "TargetState" }, notifications);
    }

    [Fact]
    public void DeferredDefaultValue_IsDistinctFromNoPendingTarget()
    {
        var state = new DeferredTransitionState<int>(1);
        var observed = new List<(bool HasValue, int Value)>();
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(state.PendingTargetState))
                observed.Add((state.HasPendingTargetState, state.PendingTargetState));
        };

        Assert.False(state.HasPendingTargetState);
        state.Defer(0);
        Assert.True(state.HasPendingTargetState);
        Assert.Equal(0, state.PendingTargetState);
        state.Defer(0);
        state.AnimateTo(0);

        Assert.False(state.HasPendingTargetState);
        Assert.Equal(0, state.TargetState);
        Assert.Equal(new[] { (true, 0), (false, 0) }, observed);
    }

    [Fact]
    public void NullableStates_SupportNullAsARealTarget()
    {
        var mutable = new MutableTransitionState<string?>(null);
        Assert.True(mutable.IsIdle);
        mutable.TargetState = "A";
        Assert.False(mutable.IsIdle);
        mutable.TargetState = null;
        Assert.True(mutable.IsIdle);

        var deferred = new DeferredTransitionState<string?>("A");
        deferred.Defer(null);
        Assert.True(deferred.HasPendingTargetState);
        Assert.Null(deferred.PendingTargetState);
        deferred.AnimateTo(null);
        Assert.False(deferred.HasPendingTargetState);
        Assert.Null(deferred.TargetState);
    }

    [Fact]
    public async Task SeekableSeek_SamplesTheWholeTransitionTree()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ =>
            new TweenSpec<double>(Ms(100), easing: new Avalonia.Animation.Easings.LinearEasing()));

        await state.SeekToAsync(true, 0.25);

        Assert.Equal(0.25, state.Fraction);
        Assert.Equal(25, value.Value, 8);
        Assert.True(transition.IsSeeking);
        Assert.False(transition.IsRunning);
    }

    [Fact]
    public async Task SeekableSeek_UpdatesChildTransitionTimeline()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var child = transition.CreateChildTransition(s => s);
        var value = child.AnimateDouble(s => s ? 200 : 0, _ =>
            new TweenSpec<double>(Ms(200), easing: new Avalonia.Animation.Easings.LinearEasing()));

        await state.SeekToAsync(true, 0.25);

        Assert.Equal(50, value.Value, 8);
        Assert.Equal(transition.PlayTime, child.PlayTime);
        Assert.True(child.IsSeeking);
    }

    [Fact]
    public async Task SeekableAnimate_UsesClockAndReturnsToIdle()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ =>
            new TweenSpec<double>(Ms(100), easing: new Avalonia.Animation.Easings.LinearEasing()));

        var running = state.AnimateToAsync(true);
        Assert.Equal(1, clock.PendingFrameCount);
        clock.AdvanceBy(Ms(0));
        Assert.False(transition.IsRunning);
        clock.AdvanceBy(Ms(50));
        Assert.Equal(50, value.Value, 8);
        Assert.True(state.Fraction > 0 && state.Fraction < 1);
        Assert.True(transition.IsRunning);
        clock.AdvanceBy(Ms(25));
        Assert.Equal(75, value.Value, 8);
        Assert.True(transition.IsRunning);
        clock.AdvanceBy(Ms(100));
        await running;

        Assert.Equal(100, value.Value, 8);
        Assert.Equal(0, state.Fraction);
        Assert.True(state.CurrentState);
        Assert.True(state.TargetState);
        Assert.False(transition.IsSeeking);
        Assert.False(transition.IsRunning);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeekablePlayback_PreservesRunningUntilManualSeekOrCancellation(bool cancel)
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var child = transition.CreateChildTransition(s => s);
        child.AnimateDouble(s => s ? 100 : 0, _ => new TweenSpec<double>(Ms(100)));
        using var cancellation = new CancellationTokenSource();
        var playback = state.AnimateToAsync(true, cancellationToken: cancellation.Token);
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(25));
        var stopped = new List<string>();
        transition.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(transition.IsRunning) && !transition.IsRunning) stopped.Add("parent");
        };
        child.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(child.IsRunning) && !child.IsRunning) stopped.Add("child");
        };
        clock.AdvanceBy(Ms(50));
        transition.RefreshAnimations();
        Assert.True(transition.IsRunning);
        Assert.True(child.IsRunning);
        Assert.Empty(stopped);

        if (cancel) cancellation.Cancel();
        else await state.SeekToAsync(0.4);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => playback);
        Assert.False(transition.IsRunning);
        Assert.False(child.IsRunning);
        Assert.True(transition.IsSeeking);
        Assert.True(child.IsSeeking);
    }

    [Fact]
    public async Task SeekableSnap_StopsAndSnapsAnimations()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ =>
            new TweenSpec<double>(Ms(100), easing: new Avalonia.Animation.Easings.LinearEasing()));

        await state.SeekToAsync(true, 0.5);
        await state.SnapToAsync(false);

        Assert.Equal(0, value.Value, 8);
        Assert.False(state.CurrentState);
        Assert.False(state.TargetState);
        Assert.False(transition.IsSeeking);
        Assert.False(transition.IsRunning);
    }

    [Fact]
    public async Task SeekableOperations_CancelThePreviousOperation()
    {
        var clock = new ManualAnimationFrameClock();
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state, clock);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ =>
            new TweenSpec<double>(Ms(100), easing: new Avalonia.Animation.Easings.LinearEasing()));

        var first = state.AnimateToAsync(true);
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(25));
        var second = state.AnimateToAsync(false);
        clock.AdvanceBy(Ms(25));
        clock.AdvanceBy(Ms(50));
        clock.AdvanceBy(Ms(150));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await second;
        Assert.Equal(0, value.Value, 8);
        Assert.False(transition.IsSeeking);
    }
}
