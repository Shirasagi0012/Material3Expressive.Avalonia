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

    public FloatSpringSpec(
        double dampingRatio = Spring.DampingRatioNoBouncy,
        double stiffness = Spring.StiffnessMedium,
        double visibilityThreshold = Spring.DefaultDisplacementThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dampingRatio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stiffness);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(visibilityThreshold);

        DampingRatio = dampingRatio;
        Stiffness = stiffness;
        VisibilityThreshold = visibilityThreshold;
        _spring = new SpringSimulation { DampingRatio = dampingRatio, Stiffness = stiffness };
    }

    public double DampingRatio { get; }
    public double Stiffness { get; }

    /// <summary>The value threshold below which the animation is no longer considered significant.</summary>
    public double VisibilityThreshold { get; }

    public double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        _spring.FinalPosition = targetValue;
        return _spring.UpdateValues(initialValue, initialVelocity, playTime.TotalSeconds).Value;
    }

    public double GetVelocity(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        _spring.FinalPosition = targetValue;
        return _spring.UpdateValues(initialValue, initialVelocity, playTime.TotalSeconds).Velocity;
    }

    public double GetEndVelocity(double initialValue, double targetValue, double initialVelocity)
        => 0;

    public TimeSpan GetDuration(double initialValue, double targetValue, double initialVelocity)
    {
        return SpringEstimation.EstimateDuration(
            Stiffness,
            DampingRatio,
            initialVelocity / VisibilityThreshold,
            (initialValue - targetValue) / VisibilityThreshold,
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

    public FloatTweenSpec(
        TimeSpan duration,
        TimeSpan delay = default,
        Easing? easing = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        Duration = duration;
        Delay = delay;
        _easing = easing ?? ComposeEasings.FastOutSlowIn;
    }

    public TimeSpan Duration { get; }
    public TimeSpan Delay { get; }

    public double GetValue(TimeSpan playTime, double initialValue, double targetValue, double initialVelocity)
    {
        var clampedPlayTime = ClampPlayTime(playTime);
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
        return (endNum - startNum) * 1000.0;
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
