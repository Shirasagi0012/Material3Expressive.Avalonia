using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

public interface IVectorizedAnimationSpec
{
    bool IsInfinite { get; }

    TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity
    );

    void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    );

    void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    );

    /// <summary>
    /// The velocity at the end of the animation, which is the velocity at the duration time by
    /// default. Physics-based animations may override this to snap the trailing velocity to zero.
    /// </summary>
    void GetEndVelocity(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    ) => GetVelocity(
        GetDuration(initial, target, initialVelocity),
        initial,
        target,
        initialVelocity,
        destination
    );
}

public interface IVectorizedFiniteAnimationSpec : IVectorizedAnimationSpec
{
    bool IVectorizedAnimationSpec.IsInfinite => false;
}

public interface IVectorizedDurationBasedAnimationSpec : IVectorizedFiniteAnimationSpec
{
    TimeSpan Delay { get; }
    TimeSpan Duration { get; }

    TimeSpan IVectorizedAnimationSpec.GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity
    )
    {
        return Delay + Duration;
    }
}

public sealed class VectorizedTweenSpec : IVectorizedDurationBasedAnimationSpec
{
    private readonly VectorizedFloatAnimationSpec _anim;

    public TimeSpan Delay { get; }
    public TimeSpan Duration { get; }

    public VectorizedTweenSpec(TimeSpan duration, TimeSpan delay = default, Easing? easing = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        Duration = duration;
        Delay = delay;
        _anim = new VectorizedFloatAnimationSpec(new FloatTweenSpec(duration, delay, easing));
    }

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    ) => _anim.GetValue(playTime, initial, target, initialVelocity, destination);

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    ) => _anim.GetVelocity(playTime, initial, target, initialVelocity, destination);
}
