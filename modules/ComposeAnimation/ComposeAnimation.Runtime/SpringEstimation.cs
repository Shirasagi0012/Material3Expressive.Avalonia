namespace ComposeAnimation.Runtime;

/// <summary>Estimates the duration of spring animations.</summary>
internal static class SpringEstimation
{
    /// <summary>
    /// Maximum duration, taking into account that the original API converts milliseconds to
    /// nanoseconds in a 64-bit value.
    /// </summary>
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMilliseconds(Int64.MaxValue / 1_000_000.0);

    /// <summary>Returns the estimated time that the spring will last be at <paramref name="delta"/>.</summary>
    public static TimeSpan EstimateDuration(
        double stiffness,
        double dampingRatio,
        double initialVelocity,
        double initialDisplacement,
        double delta)
    {
        if (!double.IsFinite(stiffness) || !double.IsFinite(dampingRatio) ||
            !double.IsFinite(initialVelocity) || !double.IsFinite(initialDisplacement) ||
            !double.IsFinite(delta) || stiffness <= 0 || delta <= 0)
            return TimeSpan.Zero;
        if (dampingRatio == 0)
            return MaxDuration;

        var dampingCoefficient = 2.0 * dampingRatio * Math.Sqrt(stiffness);

        // Compute the roots of the polynomial a*x^2 + b*x + c = 0 which may be complex.
        // Here a is set to the constant 1.0, and folded into the other computations.
        var partialRoot = dampingCoefficient * dampingCoefficient - 4.0 * stiffness;
        var partialRootReal = partialRoot < 0 ? 0.0 : Math.Sqrt(partialRoot);
        var partialRootImaginary = partialRoot < 0 ? Math.Sqrt(Math.Abs(partialRoot)) : 0.0;

        var firstRootReal = (-dampingCoefficient + partialRootReal) * 0.5;
        var firstRootImaginary = partialRootImaginary * 0.5;
        var secondRootReal = (-dampingCoefficient - partialRootReal) * 0.5;

        var seconds = EstimateDurationInternal(
            firstRootReal,
            firstRootImaginary,
            secondRootReal,
            dampingRatio,
            initialVelocity,
            initialDisplacement,
            delta);

        if (!double.IsFinite(seconds) || seconds <= 0)
            return TimeSpan.Zero;
        var milliseconds = seconds * 1000;
        if (!double.IsFinite(milliseconds))
            return MaxDuration;
        // Compose estimateAnimationDurationMillis returns a Long (truncating fractional
        // milliseconds) before FloatSpringSpec converts it to nanoseconds. Keeping the
        // fraction can postpone the final snap to the next frame.
        return TimeSpan.FromMilliseconds((long)milliseconds);
    }

    /// <summary>Returns the estimated duration in seconds that the spring will last be at <paramref name="delta"/>.</summary>
    private static double EstimateDurationInternal(
        double firstRootReal,
        double firstRootImaginary,
        double secondRootReal,
        double dampingRatio,
        double initialVelocity,
        double initialPosition,
        double delta)
    {
        if (initialPosition == 0.0 && initialVelocity == 0.0)
            return 0.0;

        var v0 = initialPosition < 0 ? -initialVelocity : initialVelocity;
        var p0 = Math.Abs(initialPosition);

        var t = dampingRatio switch
        {
            > 1.0 => EstimateOverDamped(firstRootReal, secondRootReal, p0, v0, delta),
            < 1.0 => EstimateUnderDamped(firstRootReal, firstRootImaginary, p0, v0, delta),
            _ => EstimateCriticallyDamped(firstRootReal, p0, v0, delta),
        };
        return Math.Max(t, 0.0);
    }

    /// <summary>
    /// In the under-damped case we simply calculate the envelope of the function. The general
    /// solution is of the form x(t) = c1*e^(r*t)*cos(...) + c2*e^(r*t)*sin(...) which simplifies to
    /// x(t) = c*e^(r*t)*cos(...) where c*e^(r*t) is the envelope of x(t).
    /// </summary>
    private static double EstimateUnderDamped(double firstRootReal, double firstRootImaginary, double p0, double v0, double delta)
    {
        var r = firstRootReal;
        var c1 = p0;
        var c2 = (v0 - r * c1) / firstRootImaginary;
        var c = Math.Sqrt(c1 * c1 + c2 * c2);

        return Math.Log(delta / c) / r;
    }

    /// <summary>
    /// In the critically-damped case we apply Newton-Raphson's iterative numerical method of
    /// solving the equation x(t) = c1*e^(r*t) + c2*t*e^(r*t).
    /// </summary>
    private static double EstimateCriticallyDamped(double firstRootReal, double p0, double v0, double delta)
    {
        var r = firstRootReal;
        var c1 = p0;
        var c2 = v0 - r * c1;

        // For the initial guess, determine the max t of c1*e^(r*t) = delta and c2*t*e^(r*t) = delta.
        var t1 = Math.Log(Math.Abs(delta / c1)) / r;
        var t2 = SolveTExpT(Math.Log(Math.Abs(delta / c2)), r) / r;
        var tCurr = !Double.IsFinite(t1) ? t2 : !Double.IsFinite(t2) ? t1 : Math.Max(t1, t2);

        // Calculate the inflection time. This is important if the inflection is in t > 0.
        var tInflection = -(r * c1 + c2) / (r * c2);
        var xInflection = c1 * Math.Exp(r * tInflection) + c2 * tInflection * Math.Exp(r * tInflection);

        // For an inflection that does not exist in real time, we always solve for x(t) = delta.
        // Note the system is manipulated such that p0 is always positive.
        var signedDelta = tInflection is Double.NaN or <= 0.0
            ? -delta
            : tInflection > 0.0 && -xInflection < delta
                // The first crossing with the threshold is to be found: the inflection does not
                // exceed delta, so we search from the left.
                ? -delta
                // There are three total crossings of the threshold; find the final one by
                // starting between the concavity change and the inflection point.
                : delta;

        if (tInflection > 0.0 && -xInflection < delta && c2 < 0 && c1 > 0)
            tCurr = 0.0;
        else if (tInflection > 0.0 && -xInflection >= delta)
            tCurr = -(2.0 / r) - c1 / c2;

        var iterations = 0;
        var tDelta = Double.MaxValue;
        while (tDelta > 0.001 && iterations < 100)
        {
            iterations++;
            var tLast = tCurr;
            tCurr = IterateNewtonsMethod(
                tCurr,
                t => (c1 + c2 * t) * Math.Exp(r * t) + signedDelta,
                t => (c2 * (r * t + 1) + c1 * r) * Math.Exp(r * t));
            tDelta = Math.Abs(tLast - tCurr);
        }

        return tCurr;
    }

    /// <summary>
    /// In the over-damped case we apply Newton-Raphson's iterative numerical method of solving the
    /// equation x(t) = c1*e^(r1*t) + c2*e^(r2*t).
    /// </summary>
    private static double EstimateOverDamped(double firstRootReal, double secondRootReal, double p0, double v0, double delta)
    {
        var r1 = firstRootReal;
        var r2 = secondRootReal;
        var c2 = (r1 * p0 - v0) / (r1 - r2);
        var c1 = p0 - c2;

        // For the initial guess, determine the max t of c1*e^(r1*t) = delta and c2*e^(r2*t) = delta.
        var t1 = Math.Log(Math.Abs(delta / c1)) / r1;
        var t2 = Math.Log(Math.Abs(delta / c2)) / r2;
        var tCurr = !Double.IsFinite(t1) ? t2 : !Double.IsFinite(t2) ? t1 : Math.Max(t1, t2);

        // Calculate the inflection time. This is important if the inflection is in t > 0.
        var tInflection = Math.Log(c1 * r1 / (-c2 * r2)) / (r2 - r1);
        var xInflection = c1 * Math.Exp(r1 * tInflection) + c2 * Math.Exp(r2 * tInflection);

        var signedDelta = tInflection is Double.NaN or <= 0.0
            ? -delta
            : tInflection > 0.0 && -xInflection < delta
                // The first crossing with the threshold is to be found: the inflection does not
                // exceed delta, so we search from the left.
                ? -delta
                // There are three total crossings of the threshold; find the final one.
                : delta;

        if (tInflection > 0.0 && -xInflection < delta && c2 > 0.0 && c1 < 0.0)
            tCurr = 0.0;
        else if (tInflection > 0.0 && -xInflection >= delta)
            tCurr = Math.Log(-(c2 * r2 * r2) / (c1 * r1 * r1)) / (r1 - r2);

        // For a good initial guess, simply return.
        if (Math.Abs(c1 * r1 * Math.Exp(r1 * tCurr) + c2 * r2 * Math.Exp(r2 * tCurr)) < 0.0001)
            return tCurr;

        var iterations = 0;
        var tDelta = Double.MaxValue;
        while (tDelta > 0.001 && iterations < 100)
        {
            iterations++;
            var tLast = tCurr;
            tCurr = IterateNewtonsMethod(
                tCurr,
                t => c1 * Math.Exp(r1 * t) + c2 * Math.Exp(r2 * t) + signedDelta,
                t => c1 * r1 * Math.Exp(r1 * t) + c2 * r2 * Math.Exp(r2 * t));
            tDelta = Math.Abs(tLast - tCurr);
        }

        return tCurr;
    }

    /// <summary>Applies Lambert's W function to solve t*e^t.</summary>
    private static double SolveTExpT(double guess, double r)
    {
        var t = guess;
        for (var i = 0; i <= 5; i++)
            t = guess - Math.Log(Math.Abs(t / r));
        return t;
    }

    /// <summary>Applies Newton-Raphson's method to solve for the estimated time.</summary>
    private static double IterateNewtonsMethod(double x, Func<double, double> fn, Func<double, double> fnPrime)
        => x - fn(x) / fnPrime(x);
}
