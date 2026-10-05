using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Keyframe configuration for <see cref="KeyframesWithSplineSpec{T}"/>.
/// </summary>
public sealed class KeyframesWithSplineSpecConfig<T> : KeyframesSpecBaseConfig<T>;

/// <summary>
/// Creates a keyframe-based animation using a monotone cubic Hermite spline to interpolate
/// between the values in all the intervals, producing a smooth, curvy motion.
/// </summary>
public sealed class KeyframesWithSplineSpec<T> : IDurationBasedAnimationSpec<T>
{
    public KeyframesWithSplineSpec(KeyframesWithSplineSpecConfig<T> config, double? periodicBias = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        if (periodicBias is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(periodicBias), "Periodic bias must be within [0, 1].");

        PeriodicBias = periodicBias ?? Double.NaN;
    }

    public KeyframesWithSplineSpecConfig<T> Config { get; }

    /// <summary>
    /// A value from 0 to 1 indicating how much the starting and final velocity are modified to
    /// achieve periodicity: 0 modifies only the starting velocity, 1 only the final velocity, and
    /// 0.5 modifies both equally. <see cref="double.NaN"/> for a non-periodic spline.
    /// </summary>
    public double PeriodicBias { get; }

    public TimeSpan Delay => Config.Delay;
    public TimeSpan Duration => Config.Duration;

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        var keyframes = new Dictionary<int, (double[] Value, Easing Easing)>(Config.Keyframes.Count);
        var timestamps = new List<int>(Config.Keyframes.Count + 2);

        foreach (var (timestamp, entity) in Config.Keyframes)
        {
            var value = new double[converter.Dimension];
            converter.To(entity.Value, value);
            keyframes[timestamp] = (value, entity.Easing);
            timestamps.Add(timestamp);
        }

        if (!keyframes.ContainsKey(0))
            timestamps.Add(0);
        if (!keyframes.ContainsKey((int)Duration.TotalMilliseconds))
            timestamps.Add((int)Duration.TotalMilliseconds);
        timestamps.Sort();

        return new VectorizedMonoSplineKeyframesSpec([.. timestamps], keyframes, Duration, Delay, PeriodicBias);
    }
}
