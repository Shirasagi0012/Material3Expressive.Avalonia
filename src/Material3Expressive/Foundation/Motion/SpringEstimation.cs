using System;

namespace Material3Expressive.Foundation.Motion;

/// <summary>
/// Estimates when a spring last passes within a given distance of its target.
/// </summary>
/// <remarks>
/// Ported from androidx <c>animation-core/SpringEstimation.kt</c>. A spring's closed form never
/// literally reaches its target, so "finished" has to be defined as "closer than the visibility
/// threshold, and staying there". Solving for the last crossing is what makes an animation end
/// deterministically instead of at whatever frame a sampled check happens to notice.
/// <para>
/// Under-damping has a closed-form answer — the decaying envelope. The other two cases need
/// Newton-Raphson, and the fiddly part in both is picking a starting guess on the right side of the
/// inflection point, so the iteration converges on the <i>last</i> crossing rather than the first.
/// </para>
/// </remarks>
public static class SpringEstimation
{
    private const double NewtonTolerance = 0.001;
    private const int MaxIterations = 100;

    /// <summary>
    /// Seconds until the spring is last at <paramref name="delta" /> from its target.
    /// </summary>
    /// <remarks>
    /// Displacement and velocity are expected in units of the visibility threshold, with
    /// <paramref name="delta" /> then being 1 — that is how the caller folds the threshold in.
    /// An undamped spring oscillates forever and returns <see cref="double.PositiveInfinity" />.
    /// </remarks>
    public static double DurationSeconds(
        double dampingRatio,
        double stiffness,
        double initialDisplacement,
        double initialVelocity,
        double delta)
    {
        if (dampingRatio == 0)
            return double.PositiveInfinity;

        if (initialDisplacement == 0 && initialVelocity == 0)
            return 0;

        var dampingCoefficient = 2.0 * dampingRatio * Math.Sqrt(stiffness);

        // Roots of x² + (damping)x + stiffness = 0, which may be complex.
        var partialRoot = dampingCoefficient * dampingCoefficient - 4.0 * stiffness;
        var partialRootReal = partialRoot < 0 ? 0 : Math.Sqrt(partialRoot);
        var partialRootImaginary = partialRoot < 0 ? Math.Sqrt(Math.Abs(partialRoot)) : 0;

        var firstRootReal = (-dampingCoefficient + partialRootReal) * 0.5;
        var firstRootImaginary = partialRootImaginary * 0.5;
        var secondRootReal = (-dampingCoefficient - partialRootReal) * 0.5;

        // The estimators assume a positive starting displacement, so mirror the system if needed.
        var velocity = initialDisplacement < 0 ? -initialVelocity : initialVelocity;
        var displacement = Math.Abs(initialDisplacement);

        return dampingRatio switch
        {
            > 1.0 => EstimateOverDamped(firstRootReal, secondRootReal, displacement, velocity, delta),
            < 1.0 => EstimateUnderDamped(firstRootReal, firstRootImaginary, displacement, velocity, delta),
            _ => EstimateCriticallyDamped(firstRootReal, displacement, velocity, delta)
        };
    }

    /// <summary>
    /// The envelope of an under-damped spring is <c>c·e^(rt)</c>, so the crossing solves directly.
    /// </summary>
    private static double EstimateUnderDamped(
        double firstRootReal, double firstRootImaginary, double p0, double v0, double delta)
    {
        var r = firstRootReal;
        var c1 = p0;
        var c2 = (v0 - r * c1) / firstRootImaginary;
        var c = Math.Sqrt(c1 * c1 + c2 * c2);

        return Math.Log(delta / c) / r;
    }

    /// <summary>Newton-Raphson on <c>x(t) = (c1 + c2·t)·e^(rt)</c>.</summary>
    private static double EstimateCriticallyDamped(double firstRootReal, double p0, double v0, double delta)
    {
        var r = firstRootReal;
        var c1 = p0;
        var c2 = v0 - r * c1;

        // Start from whichever of the two terms reaches delta later.
        var t1 = Math.Log(Math.Abs(delta / c1)) / r;

        var guess = Math.Log(Math.Abs(delta / c2));
        var t = guess;
        for (var i = 0; i <= 5; i++)
            t = guess - Math.Log(Math.Abs(t / r));

        var t2 = t / r;

        var current = !IsFinite(t1) ? t2 : !IsFinite(t2) ? t1 : Math.Max(t1, t2);

        var inflectionTime = -(r * c1 + c2) / (r * c2);
        var inflectionValue = c1 * Math.Exp(r * inflectionTime) + c2 * inflectionTime * Math.Exp(r * inflectionTime);

        var signedDelta = SignedDelta(delta, inflectionTime, -inflectionValue, ref current,
            resetToZero: c2 < 0 && c1 > 0,
            lastCrossingGuess: -(2.0 / r) - c1 / c2);

        return Solve(current,
            x => (c1 + c2 * x) * Math.Exp(r * x) + signedDelta,
            x => (c2 * (r * x + 1) + c1 * r) * Math.Exp(r * x));
    }

    /// <summary>Newton-Raphson on <c>x(t) = c1·e^(r1·t) + c2·e^(r2·t)</c>.</summary>
    private static double EstimateOverDamped(
        double firstRootReal, double secondRootReal, double p0, double v0, double delta)
    {
        var r1 = firstRootReal;
        var r2 = secondRootReal;
        var c2 = (r1 * p0 - v0) / (r1 - r2);
        var c1 = p0 - c2;

        var t1 = Math.Log(Math.Abs(delta / c1)) / r1;
        var t2 = Math.Log(Math.Abs(delta / c2)) / r2;

        var current = !IsFinite(t1) ? t2 : !IsFinite(t2) ? t1 : Math.Max(t1, t2);

        var inflectionTime = Math.Log(c1 * r1 / (-c2 * r2)) / (r2 - r1);
        var inflectionValue = c1 * Math.Exp(r1 * inflectionTime) + c2 * Math.Exp(r2 * inflectionTime);

        var signedDelta = SignedDelta(delta, inflectionTime, -inflectionValue, ref current,
            resetToZero: c2 > 0 && c1 < 0,
            lastCrossingGuess: Math.Log(-(c2 * r2 * r2) / (c1 * r1 * r1)) / (r1 - r2));

        // A guess this flat is already the answer.
        if (Math.Abs(c1 * r1 * Math.Exp(r1 * current) + c2 * r2 * Math.Exp(r2 * current)) < 0.0001)
            return current;

        return Solve(current,
            x => c1 * Math.Exp(r1 * x) + c2 * Math.Exp(r2 * x) + signedDelta,
            x => c1 * r1 * Math.Exp(r1 * x) + c2 * r2 * Math.Exp(r2 * x));
    }

    /// <summary>
    /// Chooses which side of the threshold to solve for, and moves the starting guess so Newton's
    /// method converges on the correct crossing.
    /// </summary>
    /// <remarks>
    /// Three cases. With no inflection in positive time the curve crosses the threshold once, from
    /// above. With an inflection that stays inside the threshold, the first crossing is the answer
    /// and the search runs from the left. Otherwise the curve crosses three times — down, back out
    /// past the threshold, and finally down for good — and only the last one counts, so the guess
    /// starts beyond the change in concavity.
    /// </remarks>
    private static double SignedDelta(
        double delta,
        double inflectionTime,
        double negatedInflectionValue,
        ref double current,
        bool resetToZero,
        double lastCrossingGuess)
    {
        if (double.IsNaN(inflectionTime) || inflectionTime <= 0.0)
            return -delta;

        if (negatedInflectionValue < delta)
        {
            if (resetToZero)
                current = 0.0;

            return -delta;
        }

        current = lastCrossingGuess;
        return delta;
    }

    private static double Solve(double start, Func<double, double> f, Func<double, double> fPrime)
    {
        var current = start;
        var change = double.MaxValue;

        for (var i = 0; i < MaxIterations && change > NewtonTolerance; i++)
        {
            var last = current;
            current -= f(current) / fPrime(current);
            change = Math.Abs(last - current);
        }

        return current;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
