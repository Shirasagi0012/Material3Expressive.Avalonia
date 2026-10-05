namespace ComposeAnimation.Runtime;

/// <summary>
/// Describes a jump-cut animation that immediately snaps the animating value to the end value.
/// </summary>
public sealed class SnapSpec<T> : IDurationBasedAnimationSpec<T>
{
    public SnapSpec(TimeSpan? delay = null)
    {
        Delay = delay ?? TimeSpan.Zero;
        if (Delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
    }

    public TimeSpan Delay { get; }
    public TimeSpan Duration => TimeSpan.Zero;

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        return new VectorizedSnapSpec(Delay);
    }
}

public sealed class VectorizedSnapSpec : IVectorizedDurationBasedAnimationSpec
{
    public VectorizedSnapSpec(TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        Delay = delay;
    }

    public TimeSpan Delay { get; }
    public TimeSpan Duration => TimeSpan.Zero;

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        (playTime < Delay ? initial : target).CopyTo(destination);
    }

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        initialVelocity.CopyTo(destination);
    }
}
