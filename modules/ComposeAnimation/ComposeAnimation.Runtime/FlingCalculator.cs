namespace ComposeAnimation.Runtime;

internal sealed class FlingCalculator
{
    private const float GravityEarth = 9.80665f;
    private const float InchesPerMeter = 39.37f;

    private static readonly float DecelerationRate = (float)(Math.Log(0.78) / Math.Log(0.9));

    private readonly float _friction;
    private readonly float _magicPhysicalCoefficient;

    public FlingCalculator(float friction, float density)
    {
        _friction = friction;
        _magicPhysicalCoefficient = ComputeDeceleration(0.84f, density);
    }

    public long FlingDuration(float velocity)
    {
        var l = GetSplineDeceleration(velocity);
        var decelMinusOne = DecelerationRate - 1.0;
        return (long)(1000.0 * Math.Exp(l / decelMinusOne));
    }

    public float FlingDistance(float velocity)
    {
        var l = GetSplineDeceleration(velocity);
        var decelMinusOne = DecelerationRate - 1.0;
        return (float)(_friction * _magicPhysicalCoefficient *
                       Math.Exp(DecelerationRate / decelMinusOne * l));
    }

    public FlingInfo GetFlingInfo(float velocity)
    {
        var l = GetSplineDeceleration(velocity);
        var decelMinusOne = DecelerationRate - 1.0;
        return new FlingInfo(
            velocity,
            (float)(_friction * _magicPhysicalCoefficient *
                    Math.Exp(DecelerationRate / decelMinusOne * l)),
            (long)(1000.0 * Math.Exp(l / decelMinusOne)));
    }

    private double GetSplineDeceleration(float velocity)
        => AndroidFlingSpline.Deceleration(velocity, _friction * _magicPhysicalCoefficient);

    private static float ComputeDeceleration(float friction, float density)
        => GravityEarth * InchesPerMeter * density * 160f * friction;

    internal readonly record struct FlingInfo(float InitialVelocity, float Distance, long Duration)
    {
        public float Position(long time)
        {
            var splinePos = Duration > 0 ? time / (float)Duration : 1f;
            return Distance *
                   Math.Sign(InitialVelocity) *
                   AndroidFlingSpline.FlingPosition(splinePos).DistanceCoefficient;
        }

        public float Velocity(long time)
        {
            // Compose divides by duration even when it is 0 (NaN). Zero-velocity flings are at rest.
            if (Duration <= 0)
                return 0f;

            var splinePos = time / (float)Duration;
            return AndroidFlingSpline.FlingPosition(splinePos).VelocityCoefficient *
                   Math.Sign(InitialVelocity) *
                   Distance / Duration * 1000.0f;
        }
    }
}
