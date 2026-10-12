using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class BoundsAnimationStateTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private sealed class CallbackTransform(Func<Rect, Rect, IFiniteAnimationSpec<Rect>> create) : BoundsTransform
    {
        public override IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds)
            => create(initialBounds, targetBounds);
    }

    [Fact]
    public Task SpringRetarget_PreservesSampleAndAllFourEdgeVelocities() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var errors = new List<Exception>();
        var state = new BoundsAnimationState(() => { }, errors.Add);
        var policy = new BoundsTransform
        {
            Spec = new SpringSpec<Rect>(stiffness: 50, visibilityThreshold: VisibilityThresholds.Rect)
        };
        var start = new Rect(10, 20, 100, 80);
        var first = new Rect(200, 150, 200, 120);
        state.UpdateTarget(start, clock, policy);
        state.UpdateTarget(first, clock, policy);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        var reference = new TargetBasedAnimation<Rect>(policy.Spec, new RectConverter(), start, first);
        var current = reference.GetValue(TimeSpan.FromMilliseconds(100));
        Assert.Equal(current, state.Value);
        var next = new Rect(-50, 70, 160, 200);
        var velocity = reference.GetVelocity(TimeSpan.FromMilliseconds(100));
        state.UpdateTarget(next, clock, policy);
        Assert.Equal(current, state.Value);
        var resumed = new TargetBasedAnimation<Rect>(policy.Spec, new RectConverter(), current, next,
            initialVelocity: velocity);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        var expected = resumed.GetValue(TimeSpan.FromMilliseconds(1));
        Assert.Equal(expected.X, state.Value!.Value.X, 7);
        Assert.Equal(expected.Y, state.Value.Value.Y, 7);
        Assert.Equal(expected.Width, state.Value.Value.Width, 7);
        Assert.Equal(expected.Height, state.Value.Value.Height, 7);
        clock.AdvanceBy(TimeSpan.FromSeconds(10));
        Assert.Equal(next, state.Value);
        Assert.False(state.IsRunning);
        Assert.Empty(errors);
        Assert.Equal(0, clock.PendingFrameCount);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task FailedPolicy_BlocksRepeatedTarget_AndAcceptsAnotherTarget(bool cancellation) => OnUi(() =>
    {
        var clock = new ManualFrameSource();
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
        state.UpdateTarget(first, clock, broken);
        state.UpdateTarget(second, clock, broken);
        for (var i = 0; i < 5; i++) state.UpdateTarget(second, clock, broken);
        Assert.Equal(1, calls);
        Assert.Single(errors);
        Assert.Equal(second, state.Value);
        Assert.False(state.IsRunning);
        state.UpdateTarget(first, clock, new BoundsTransform());
        Assert.True(state.IsRunning);
        state.Reset();
        Assert.Equal(0, clock.PendingFrameCount);
    });

    [Fact]
    public Task PolicyCanResetStateSynchronously_WithoutStartingStaleAnimation() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var errors = new List<Exception>();
        var state = new BoundsAnimationState(() => { }, errors.Add);
        var policy = new CallbackTransform((_, _) =>
        {
            state.Reset();
            return new SpringSpec<Rect>();
        });
        state.UpdateTarget(new Rect(0, 0, 100, 80), clock, policy);
        state.UpdateTarget(new Rect(200, 100, 120, 140), clock, policy);
        Assert.Null(state.Value);
        Assert.False(state.IsRunning);
        Assert.Empty(errors);
        Assert.Equal(0, clock.PendingFrameCount);
    });
}
