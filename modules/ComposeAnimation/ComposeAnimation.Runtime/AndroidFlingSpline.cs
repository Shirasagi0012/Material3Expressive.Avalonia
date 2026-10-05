namespace ComposeAnimation.Runtime;

/// <summary>
/// Native Android fling scroll spline, ported from Compose <c>AndroidFlingSpline</c> /
/// <c>android.widget.Scroller</c>.
/// </summary>
internal static class AndroidFlingSpline
{
    private const float Inflection = 0.35f;
    private const float StartTension = 0.5f;
    private const float EndTension = 1.0f;
    private const float P1 = StartTension * Inflection;
    private const float P2 = 1.0f - EndTension * (1.0f - Inflection);
    private const int NbSamples = 100;

    private static readonly float[] SplinePositions = new float[NbSamples + 1];
    private static readonly float[] SplineTimes = new float[NbSamples + 1];

    static AndroidFlingSpline() => ComputeSplineInfo(SplinePositions, SplineTimes, NbSamples);

    /// <summary>Instantaneous fling position along the scroller spline. <paramref name="time"/> is 0–1 progress.</summary>
    public static FlingResult FlingPosition(float time)
    {
        var clampedTime = Math.Clamp(time, 0f, 1f);
        var index = (int)(NbSamples * clampedTime);
        var distanceCoef = 1f;
        var velocityCoef = 0f;
        if (index < NbSamples)
        {
            var tInf = index / (float)NbSamples;
            var tSup = (index + 1) / (float)NbSamples;
            var dInf = SplinePositions[index];
            var dSup = SplinePositions[index + 1];
            velocityCoef = (dSup - dInf) / (tSup - tInf);
            distanceCoef = dInf + (clampedTime - tInf) * velocityCoef;
        }

        return new FlingResult(distanceCoef, velocityCoef);
    }

    /// <summary>Rate of deceleration along the spline given <paramref name="velocity"/> and <paramref name="friction"/>.</summary>
    public static double Deceleration(float velocity, float friction)
        => Math.Log(Inflection * Math.Abs(velocity) / (double)friction);

    internal readonly record struct FlingResult(float DistanceCoefficient, float VelocityCoefficient);

    private static void ComputeSplineInfo(float[] splinePositions, float[] splineTimes, int nbSamples)
    {
        var xMin = 0.0f;
        var yMin = 0.0f;
        for (var i = 0; i < nbSamples; i++)
        {
            var alpha = i / (float)nbSamples;
            var xMax = 1.0f;
            float x;
            float coef;
            while (true)
            {
                x = xMin + (xMax - xMin) / 2.0f;
                coef = 3.0f * x * (1.0f - x);
                var tx = coef * ((1.0f - x) * P1 + x * P2) + x * x * x;
                if (Math.Abs(tx - alpha) < 1E-5) break;
                if (tx > alpha) xMax = x;
                else xMin = x;
            }

            splinePositions[i] = coef * ((1.0f - x) * StartTension + x) + x * x * x;
            var yMax = 1.0f;
            float y;
            while (true)
            {
                y = yMin + (yMax - yMin) / 2.0f;
                coef = 3.0f * y * (1.0f - y);
                var dy = coef * ((1.0f - y) * StartTension + y) + y * y * y;
                if (Math.Abs(dy - alpha) < 1E-5) break;
                if (dy > alpha) yMax = y;
                else yMin = y;
            }

            splineTimes[i] = coef * ((1.0f - y) * P1 + y * P2) + y * y * y;
        }

        splineTimes[nbSamples] = 1.0f;
        splinePositions[nbSamples] = splineTimes[nbSamples];
    }
}
