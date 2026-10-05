using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>
/// A duration-based animation spec that interpolates 2-dimensional values using arcs of a quarter
/// of an ellipse. Recommended only for positional values such as <see cref="Avalonia.Point"/>,
/// <see cref="Avalonia.Vector"/> or offsets. One-dimensional values always interpolate linearly.
/// </summary>
public sealed class ArcAnimationSpec<T> : IDurationBasedAnimationSpec<T>
{
    public ArcAnimationSpec(
        ArcMode mode = ArcMode.ArcBelow,
        TimeSpan? duration = null,
        TimeSpan? delay = null,
        Easing? easing = null)
    {
        Mode = mode;
        Duration = duration ?? AnimationConstants.DefaultDuration;
        Delay = delay ?? TimeSpan.Zero;
        Easing = easing ?? ComposeEasings.FastOutSlowIn;

        if (Duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));
        if (Delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
    }

    public ArcMode Mode { get; }
    public TimeSpan Delay { get; }
    public TimeSpan Duration { get; }
    public Easing Easing { get; }

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        var durationMillis = (int)(Duration.Ticks / TimeSpan.TicksPerMillisecond);
        return new VectorizedKeyframesSpec(
            [0, durationMillis],
            new Dictionary<int, VectorizedKeyframeSpecElementInfo>(),
            Duration,
            Delay,
            Easing,
            Mode);
    }
}
