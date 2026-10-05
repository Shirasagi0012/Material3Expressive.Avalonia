using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>Holder class for building a keyframes animation.</summary>
public sealed class KeyframeEntity<T>
{
    internal KeyframeEntity(T value)
    {
        Value = value;
    }

    public T Value { get; }
    public Easing Easing { get; private set; } = ComposeEasings.Linear;
    public ArcMode ArcMode { get; private set; } = ArcMode.ArcLinear;

    /// <summary>Sets the <see cref="IEasing"/> used for the interval starting at this keyframe.</summary>
    public KeyframeEntity<T> Using(Easing easing)
    {
        Easing = easing ?? throw new ArgumentNullException(nameof(easing));
        return this;
    }

    /// <summary>Sets the <see cref="ArcMode"/> used for the interval starting at this keyframe.</summary>
    public KeyframeEntity<T> Using(ArcMode arcMode)
    {
        ArcMode = arcMode;
        return this;
    }
}

/// <summary>
/// Mutable configuration of a keyframe-based animation, including the duration, delay and all the
/// keyframes. Each keyframe defines what the animation value should be at a particular time.
/// </summary>
public class KeyframesSpecBaseConfig<T>
{
    private readonly SortedDictionary<int, KeyframeEntity<T>> _keyframes = [];
    private TimeSpan _duration = AnimationConstants.DefaultDuration;
    private TimeSpan _delay = TimeSpan.Zero;

    /// <summary>Duration of the animation. Defaults to <see cref="AnimationConstants.DefaultDuration"/>.</summary>
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _duration = value;
        }
    }

    /// <summary>Amount of time the animation is delayed. Defaults to 0.</summary>
    public TimeSpan Delay
    {
        get => _delay;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _delay = value;
        }
    }

    internal IReadOnlyDictionary<int, KeyframeEntity<T>> Keyframes => _keyframes;

    /// <summary>
    /// Adds a keyframe so that the animation value will be <paramref name="value"/> at
    /// <paramref name="timestamp"/> milliseconds into the animation.
    /// </summary>
    public KeyframeEntity<T> At(int timestamp, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timestamp);

        var entity = new KeyframeEntity<T>(value);
        _keyframes[timestamp] = entity;
        return entity;
    }

    /// <summary>
    /// Adds a keyframe so that the animation value will be <paramref name="value"/> at
    /// <paramref name="fraction"/> of the total <see cref="Duration"/>.
    /// </summary>
    public KeyframeEntity<T> AtFraction(double fraction, T value)
    {
        if (fraction is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be within [0, 1].");
        return At((int)Math.Round(Duration.TotalMilliseconds * fraction, MidpointRounding.AwayFromZero), value);
    }
}

/// <summary>Keyframe configuration for <see cref="KeyframesSpec{T}"/>.</summary>
public sealed class KeyframesSpecConfig<T> : KeyframesSpecBaseConfig<T>;

/// <summary>
/// Creates a keyframe-based duration animation. Values are interpolated linearly between
/// keyframes by default; a custom <see cref="IEasing"/> or <see cref="ArcMode"/> can be attached
/// to any keyframe to shape the following interval.
/// </summary>
public sealed class KeyframesSpec<T> : IDurationBasedAnimationSpec<T>
{
    public KeyframesSpec(KeyframesSpecConfig<T> config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public KeyframesSpecConfig<T> Config { get; }
    public TimeSpan Delay => Config.Delay;
    public TimeSpan Duration => Config.Duration;

    public IVectorizedDurationBasedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        var keyframes = new Dictionary<int, VectorizedKeyframeSpecElementInfo>(Config.Keyframes.Count);
        var timestamps = new List<int>(Config.Keyframes.Count + 2);

        foreach (var (timestamp, entity) in Config.Keyframes)
        {
            var value = new double[converter.Dimension];
            converter.To(entity.Value, value);
            keyframes[timestamp] = new VectorizedKeyframeSpecElementInfo(value, entity.Easing, entity.ArcMode);
            timestamps.Add(timestamp);
        }

        if (!keyframes.ContainsKey(0))
            timestamps.Add(0);
        if (!keyframes.ContainsKey((int)Duration.TotalMilliseconds))
            timestamps.Add((int)Duration.TotalMilliseconds);
        timestamps.Sort();

        return new VectorizedKeyframesSpec(
            [.. timestamps],
            keyframes,
            Duration,
            Delay,
            ComposeEasings.Linear,
            ArcMode.ArcLinear);
    }
}
