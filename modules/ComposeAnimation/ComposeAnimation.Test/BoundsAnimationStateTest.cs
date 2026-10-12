using Avalonia.Threading;
using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class BoundsAnimationStateTest
{

    private sealed class CallbackTransform(Func<Rect, Rect, IFiniteAnimationSpec<Rect>> create) : BoundsTransform
    {
        public override IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds)
            => create(initialBounds, targetBounds);
    }

    // The window's FrameClock resumes runs from a dispatcher post: drain it after each frame.
    private static void Advance(ManualFrameSource clock, TimeSpan by)
    {
        clock.AdvanceBy(by);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public Task SpringRetarget_PreservesSampleAndAllFourEdgeVelocities() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var errors = new List<Exception>();
        var state = new BoundsAnimationState(() => { }, errors.Add);
        var policy = new BoundsTransform
        {
            Spec = new SpringSpec<Rect>(stiffness: 50, visibilityThreshold: VisibilityThresholds.Rect)
        };
        var start = new Rect(10, 20, 100, 80);
        var first = new Rect(200, 150, 200, 120);
        state.UpdateTarget(start, scope, policy);
        state.UpdateTarget(first, scope, policy);
        Advance(clock, TimeSpan.FromMilliseconds(16));
        Advance(clock, TimeSpan.FromMilliseconds(100));
        var reference = new TargetBasedAnimation<Rect>(policy.Spec, new RectConverter(), start, first);
        var current = reference.GetValue(TimeSpan.FromMilliseconds(100));
        Assert.Equal(current, state.Value);
        var next = new Rect(-50, 70, 160, 200);
        var velocity = reference.GetVelocity(TimeSpan.FromMilliseconds(100));
        state.UpdateTarget(next, scope, policy);
        Dispatcher.UIThread.RunJobs(); // The interrupted run unwinds from the FrameClock's posted cancellation.
        Assert.Equal(current, state.Value);
        var resumed = new TargetBasedAnimation<Rect>(policy.Spec, new RectConverter(), current, next,
            initialVelocity: velocity);
        Advance(clock, TimeSpan.FromMilliseconds(1));
        var expected = resumed.GetValue(TimeSpan.FromMilliseconds(1));
        Assert.Equal(expected.X, state.Value!.Value.X, 7);
        Assert.Equal(expected.Y, state.Value.Value.Y, 7);
        Assert.Equal(expected.Width, state.Value.Value.Width, 7);
        Assert.Equal(expected.Height, state.Value.Value.Height, 7);
        Advance(clock, TimeSpan.FromSeconds(10));
        Assert.Equal(next, state.Value);
        Assert.False(state.IsRunning);
        Assert.Empty(errors);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task FailedPolicy_BlocksRepeatedTarget_AndAcceptsAnotherTarget(bool cancellation) => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var errors = new List<Exception>();
        var state = new BoundsAnimationState(() => { }, errors.Add);
        var calls = 0;
        var broken = new CallbackTransform((_, _) =>
        {
            calls++;
            throw cancellation ? new OperationCanceledException("policy") : new InvalidOperationException("policy");
        });
        var first = new Rect(10, 20, 100, 80);
        var second = new Rect(100, 200, 200, 100);
        state.UpdateTarget(first, scope, broken);
        state.UpdateTarget(second, scope, broken);
        for (var i = 0; i < 5; i++) state.UpdateTarget(second, scope, broken);
        Assert.Equal(1, calls);
        Assert.Single(errors);
        Assert.Equal(second, state.Value);
        Assert.False(state.IsRunning);
        state.UpdateTarget(first, scope, new BoundsTransform());
        Assert.True(state.IsRunning);
        state.Reset();
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task PolicyCanResetStateSynchronously_WithoutStartingStaleAnimation() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var errors = new List<Exception>();
        var state = new BoundsAnimationState(() => { }, errors.Add);
        var policy = new CallbackTransform((_, _) =>
        {
            state.Reset();
            return new SpringSpec<Rect>();
        });
        state.UpdateTarget(new Rect(0, 0, 100, 80), scope, policy);
        state.UpdateTarget(new Rect(200, 100, 120, 140), scope, policy);
        Assert.Null(state.Value);
        Assert.False(state.IsRunning);
        Assert.Empty(errors);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });
}
