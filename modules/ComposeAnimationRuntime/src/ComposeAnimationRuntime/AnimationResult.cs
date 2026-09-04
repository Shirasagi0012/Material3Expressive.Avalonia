namespace ComposeAnimationRuntime;

public enum AnimationEndReason
{
    Finished,
    Canceled,
    BoundReached,
}

public readonly record struct AnimationResult<T>(
    T Value,
    T Velocity,
    AnimationEndReason EndReason);
