using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

public interface IAnimationSpec<T>
{
    IVectorizedAnimationSpec Vectorize(ITwoWayConverter<T> converter);
}

public interface IFiniteAnimationSpec<T> : IAnimationSpec<T>
{
    new IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter);

    IVectorizedAnimationSpec IAnimationSpec<T>.Vectorize(ITwoWayConverter<T> converter)
        => Vectorize(converter);
}

/// <summary>
/// An <see cref="IAnimationSpec{T}"/> whose animation duration is known upfront, such as
/// <see cref="TweenSpec{T}"/>, <see cref="SnapSpec{T}"/>, <see cref="KeyframesSpec{T}"/> or
/// <see cref="RepeatableSpec{T}"/>.
/// </summary>
public interface IDurationBasedAnimationSpec<T> : IFiniteAnimationSpec<T>
{
    TimeSpan Delay { get; }
    TimeSpan Duration { get; }

    new IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter);

    IVectorizedAnimationSpec IAnimationSpec<T>.Vectorize(ITwoWayConverter<T> converter)
        => Vectorize(converter);

    IVectorizedFiniteAnimationSpec IFiniteAnimationSpec<T>.Vectorize(ITwoWayConverter<T> converter)
        => Vectorize(converter);
}

public class TweenSpec<T> : IDurationBasedAnimationSpec<T>
{
    public TweenSpec(TimeSpan? duration = null, TimeSpan? delay = null, Easing? easing = null)
    {
        if (duration is { } d) Duration = d;
        if (delay is { } l) Delay = l;
        if (easing is { }) Easing = easing;
    }

    public TimeSpan Duration
    {
        get;
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            field = value;
        }
    } = AnimationConstants.DefaultDuration;

    public TimeSpan Delay
    {
        get;
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            field = value;
        }
    } = TimeSpan.Zero;

    public Easing Easing { get; set; } = ComposeEasings.FastOutSlowIn;

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        return new VectorizedTweenSpec(Duration, Delay, Easing);
    }
}
