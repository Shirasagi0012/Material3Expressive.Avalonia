namespace ComposeAnimation.Runtime;

/// <summary>
/// Ported from Compose <c>SplineBasedFloatDecayAnimationSpec</c>.
/// </summary>
public sealed class SplineBasedFloatDecayAnimationSpec : IFloatDecayAnimationSpec
{
    private readonly FlingCalculator _flingCalculator;

    /// <param name="density">
    /// Compose <c>Density.density</c> (dpi / 160). Pass Avalonia <c>TopLevel.RenderScaling</c>.
    /// </param>
    public SplineBasedFloatDecayAnimationSpec(double density)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(density);
        _flingCalculator = new FlingCalculator((float)DecayAnimationSpec.ScrollFriction, (float)density);
    }

    /// <summary>
    /// Spline decay stops at the computed fling duration rather than a velocity threshold.
    /// </summary>
    public double AbsVelocityThreshold => 0;

    public double GetTargetValue(double initialValue, double initialVelocity)
        => initialValue + FlingDistance((float)initialVelocity);

    public double GetValue(TimeSpan playTime, double initialValue, double initialVelocity)
    {
        var playTimeMillis = ToMillis(playTime);
        return initialValue + _flingCalculator.GetFlingInfo((float)initialVelocity).Position(playTimeMillis);
    }

    public TimeSpan GetDuration(double initialValue, double initialVelocity)
    {
        var millis = _flingCalculator.FlingDuration((float)initialVelocity);
        return TimeSpan.FromTicks(checked(millis * TimeSpan.TicksPerMillisecond));
    }

    public double GetVelocity(TimeSpan playTime, double initialValue, double initialVelocity)
    {
        var playTimeMillis = ToMillis(playTime);
        return _flingCalculator.GetFlingInfo((float)initialVelocity).Velocity(playTimeMillis);
    }

    private float FlingDistance(float startVelocity)
        => _flingCalculator.FlingDistance(startVelocity) * Math.Sign(startVelocity);

    private static long ToMillis(TimeSpan playTime)
    {
        if (playTime < TimeSpan.Zero)
            playTime = TimeSpan.Zero;
        return playTime.Ticks / TimeSpan.TicksPerMillisecond;
    }
}
