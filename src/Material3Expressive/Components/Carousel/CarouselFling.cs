// Fixed AndroidX SnapFlingBehavior.kt / PagerSnapLayoutInfoProvider.kt (Apache-2.0).
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Post-scroll behavior. Default selects NoSnap for Uncontained and SingleAdvance otherwise.</summary>
public enum CarouselFlingMode { Default, SingleAdvance, MultiBrowse, NoSnap }

internal static class CarouselFling
{
    internal static async Task<float> RunAsync(FrameScope scope, float velocity, IDecayAnimationSpec<float> decay,
        IAnimationSpec<float> snap, Func<float, float> consume, Func<float, float, float> approachOffset,
        Func<float, float> snapOffset, CancellationToken cancellationToken)
    {
        var decayTarget = decay.CalculateTargetValue(0, velocity);
        var approach = approachOffset(velocity, decayTarget);
        var remaining = MathF.Abs(approach) * Math.Sign(velocity);
        AnimationState<float> state;
        if (remaining == 0 || velocity == 0) state = State(remaining, velocity);
        else if (MathF.Abs(decayTarget) >= MathF.Abs(remaining))
        {
            state = State(0, velocity);
            var previous = 0f;
            await SuspendAnimation.AnimateDecayAsync(scope, state, decay, sequentialAnimation: state.Velocity != 0,
                block: frame =>
                {
                    var final = MathF.Abs(frame.Value) >= MathF.Abs(remaining);
                    var value = final ? CoerceToTarget(frame.Value, remaining) : frame.Value;
                    var delta = value - previous;
                    var consumed = consume(delta);
                    if (MathF.Abs(delta - consumed) > .5f || final) frame.CancelAnimation();
                    previous = value;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            var result = await AnimateTarget(remaining, State(0, velocity));
            state = result.State;
        }
        remaining = snapOffset(state.Velocity);
        var finalState = new AnimationState<float>(new FloatConverter(), 0,
            AnimationVector.From([state.Velocity]), state.LastFrameTime, state.FinishedTime);
        var finalResult = await AnimateTarget(remaining, finalState);
        return finalResult.Remaining == 0 ? 0 : finalResult.State.Velocity;

        async Task<(float Remaining, AnimationState<float> State)> AnimateTarget(float target, AnimationState<float> animation)
        {
            var consumedTotal = 0f;
            var initialVelocity = animation.Velocity;
            await SuspendAnimation.AnimateToAsync(scope, animation, target, snap,
                sequentialAnimation: animation.Velocity != 0, block: frame =>
                {
                    var realValue = CoerceToTarget(frame.Value, target);
                    var delta = realValue - consumedTotal;
                    var consumed = consume(delta);
                    if (MathF.Abs(delta - consumed) > .5f || realValue != frame.Value) frame.CancelAnimation();
                    consumedTotal += consumed;
                }, cancellationToken: cancellationToken);
            return (target - consumedTotal, new AnimationState<float>(new FloatConverter(), animation.Value,
                AnimationVector.From([CoerceToTarget(animation.Velocity, initialVelocity)]),
                animation.LastFrameTime, animation.FinishedTime, animation.IsRunning));
        }
    }

    private static AnimationState<float> State(float value, float velocity) =>
        new(new FloatConverter(), value, AnimationVector.From([velocity]));
    private static float CoerceToTarget(float value, float target) => target == 0 ? 0 : target > 0 ? MathF.Min(value, target) : MathF.Max(value, target);
}
