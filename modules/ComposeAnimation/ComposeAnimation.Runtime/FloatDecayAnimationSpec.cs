namespace ComposeAnimation.Runtime;

/// <summary>
/// Like <see cref="IVectorizedDecayAnimationSpec"/>, but deals exclusively with scalar values.
/// </summary>
public interface IFloatDecayAnimationSpec
{
    /// <summary>
    /// The absolute value of the velocity threshold, below which the animation is considered
    /// finished.
    /// </summary>
    double AbsVelocityThreshold { get; }

    double GetValue(TimeSpan playTime, double initialValue, double initialVelocity);

    double GetVelocity(TimeSpan playTime, double initialValue, double initialVelocity);

    TimeSpan GetDuration(double initialValue, double initialVelocity);

    /// <summary>Returns the target value where the animation will come to a natural stop.</summary>
    double GetTargetValue(double initialValue, double initialVelocity);
}

/// <summary>
/// A decay animation where the friction/deceleration is always proportional to the velocity. As a
/// result, the velocity goes under an exponential decay. The higher the friction multiplier, the
/// higher the friction, the sooner the animation will stop, and the shorter distance the animation
/// will travel with the same starting condition.
/// </summary>
public sealed class FloatExponentialDecaySpec : IFloatDecayAnimationSpec
{
    private const double ExponentialDecayFriction = -4.2;

    private readonly double _friction;

    public FloatExponentialDecaySpec(double frictionMultiplier = 1, double absVelocityThreshold = 0.1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frictionMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(absVelocityThreshold);

        AbsVelocityThreshold = Math.Max(0.0000001, Math.Abs(absVelocityThreshold));
        _friction = ExponentialDecayFriction * Math.Max(0.0001, frictionMultiplier);
    }

    public double AbsVelocityThreshold { get; }

    public double GetValue(TimeSpan playTime, double initialValue, double initialVelocity)
    {
        var seconds = playTime.TotalSeconds;
        return initialValue - initialVelocity / _friction +
            initialVelocity / _friction * Math.Exp(_friction * seconds);
    }

    public double GetVelocity(TimeSpan playTime, double initialValue, double initialVelocity)
        => initialVelocity * Math.Exp(playTime.TotalSeconds * _friction);

    public TimeSpan GetDuration(double initialValue, double initialVelocity)
    {
        if (Math.Abs(initialVelocity) <= AbsVelocityThreshold)
            return TimeSpan.Zero;

        return TimeSpan.FromMilliseconds(1000.0 * Math.Log(AbsVelocityThreshold / Math.Abs(initialVelocity)) / _friction);
    }

    public double GetTargetValue(double initialValue, double initialVelocity)
    {
        if (Math.Abs(initialVelocity) <= AbsVelocityThreshold)
            return initialValue;

        var duration = Math.Log(Math.Abs(AbsVelocityThreshold / initialVelocity)) / _friction * 1000;
        return initialValue - initialVelocity / _friction +
            initialVelocity / _friction * Math.Exp(_friction * duration / 1000);
    }
}
