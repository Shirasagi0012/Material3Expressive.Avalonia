using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Like <see cref="IVectorizedAnimationSpec"/>, but deals exclusively with scalar values. A
/// <see cref="IFloatAnimationSpec"/> animates every dimension of a vector independently.
/// </summary>
public interface IFloatAnimationSpec
{
    double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity);

    double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity);

    /// <summary>
    /// The end velocity of the animation. Duration-based animations return the velocity at the
    /// duration time by default; physics-based animations snap the transient trailing velocity to
    /// zero.
    /// </summary>
    double GetEndVelocity(double initialValue, double targetValue, double initialVelocity)
        => GetVelocity(GetDuration(initialValue, targetValue, initialVelocity), initialValue, targetValue, initialVelocity);

    TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity);
}

/// <summary>
/// Uses spring physics to animate a scalar value. The configuration can be tuned by adjusting the
/// damping ratio and stiffness.
/// </summary>
public sealed class FloatSpringSpec : IFloatAnimationSpec
{
    private readonly SpringSimulation _spring;
    private readonly bool _singlePrecision;

    public FloatSpringSpec(
        double dampingRatio = Spring.DampingRatioNoBouncy,
        double stiffness = Spring.StiffnessMedium,
        double visibilityThreshold = Spring.DefaultDisplacementThreshold)
        : this(dampingRatio, stiffness, visibilityThreshold, singlePrecision: false)
    {
    }

    internal FloatSpringSpec(double dampingRatio, double stiffness, double visibilityThreshold, bool singlePrecision)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dampingRatio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stiffness);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(visibilityThreshold);

        _singlePrecision = singlePrecision;
        DampingRatio = singlePrecision ? (float)dampingRatio : dampingRatio;
        Stiffness = singlePrecision ? (float)stiffness : stiffness;
        VisibilityThreshold = singlePrecision ? (float)visibilityThreshold : visibilityThreshold;
        _spring = new SpringSimulation { DampingRatio = DampingRatio, Stiffness = Stiffness };
    }

    public double DampingRatio { get; }
    public double Stiffness { get; }

    /// <summary>The value threshold below which the animation is no longer considered significant.</summary>
    public double VisibilityThreshold { get; }

    public double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        _spring.FinalPosition = targetValue;
        // Fixed FloatSpringSpec truncates nanos to whole milliseconds before SpringSimulation.
        var seconds = _singlePrecision ? (playTime.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0 : playTime.TotalSeconds;
        return _spring.UpdateValues(initialValue, initialVelocity, seconds, _singlePrecision).Value;
    }

    public double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        _spring.FinalPosition = targetValue;
        var seconds = _singlePrecision ? (playTime.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0 : playTime.TotalSeconds;
        return _spring.UpdateValues(initialValue, initialVelocity, seconds, _singlePrecision).Velocity;
    }

    public double GetEndVelocity(double initialValue, double targetValue, double initialVelocity)
        => 0;

    public TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity)
    {
        return SpringEstimation.EstimateDuration(
            Stiffness,
            DampingRatio,
            _singlePrecision ? (float)initialVelocity / (float)VisibilityThreshold : initialVelocity / VisibilityThreshold,
            _singlePrecision ? ((float)initialValue - (float)targetValue) / (float)VisibilityThreshold : (initialValue - targetValue) / VisibilityThreshold,
            1);
    }
}

/// <summary>
/// Animates a scalar value from any start value to any end value using a provided easing function.
/// The animation finishes within <see cref="Duration"/> unless a <see cref="Delay"/> is specified.
/// </summary>
public sealed class FloatTweenSpec : IFloatAnimationSpec
{
    private readonly Easing _easing;
    private readonly bool _singlePrecision;

    public FloatTweenSpec(
        TimeSpan duration,
        TimeSpan delay = default,
        Easing? easing = null)
        : this(duration, delay, easing, singlePrecision: false)
    {
    }

    internal FloatTweenSpec(TimeSpan duration, TimeSpan delay, Easing? easing, bool singlePrecision)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        Duration = duration;
        Delay = delay;
        _easing = easing ?? ComposeEasings.FastOutSlowIn;
        _singlePrecision = singlePrecision;
    }

    public TimeSpan Duration { get; }
    public TimeSpan Delay { get; }

    public double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        var clampedPlayTime = ClampPlayTime(playTime);
        if (_singlePrecision)
        {
            // Compose divides nanoseconds as Float and uses ui-util's weighted lerp.
            // Keep each arithmetic boundary; narrowing only the final result is different.
            var raw = Duration == TimeSpan.Zero ? 1f
                : (float)(clampedPlayTime.Ticks * 100.0) / (float)(Duration.Ticks * 100.0);
            var eased = (float)_easing.Ease(raw);
            return (1f - eased) * (float)initialValue + eased * (float)targetValue;
        }
        var rawFraction = Duration == TimeSpan.Zero
            ? 1.0
            : clampedPlayTime.TotalSeconds / Duration.TotalSeconds;
        var fraction = _easing.Ease(rawFraction);
        return initialValue + (targetValue - initialValue) * fraction;
    }

    public TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity)
        => Delay + Duration;

    /// <summary>
    /// Calculates velocity by difference between the current value and the value 1 ms ago. This is
    /// a preliminary way of calculating velocity used by easing curve based animations, and
    /// keyframe animations. Physics-based animations give a much more accurate velocity.
    /// </summary>
    public double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        var clampedPlayTime = ClampPlayTime(playTime);
        if (clampedPlayTime == TimeSpan.Zero)
            return initialVelocity;

        // Match Compose FloatTweenSpec: pass already-clamped times back into GetValue (which clamps again).
        var startNum = GetValue(clampedPlayTime - TimeSpan.FromMilliseconds(1), initialValue, targetValue, initialVelocity);
        var endNum = GetValue(clampedPlayTime, initialValue, targetValue, initialVelocity);
        return _singlePrecision ? ((float)endNum - (float)startNum) * 1000f
            : (endNum - startNum) * 1000.0;
    }

    private TimeSpan ClampPlayTime(TimeSpan playTime)
    {
        var t = playTime - Delay;
        if (t < TimeSpan.Zero)
            return TimeSpan.Zero;
        if (t > Duration)
            return Duration;
        return t;
    }
}
