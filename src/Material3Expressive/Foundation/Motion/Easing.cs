using System;

namespace Material3Expressive.Foundation.Motion;

/// <summary>An easing curve: maps a 0–1 fraction of elapsed time to a 0–1 fraction of the change.</summary>
public delegate double Easing(double fraction);

/// <summary>
/// The easing curves Material's duration-based animations use.
/// </summary>
/// <remarks>
/// Transcribed from androidx <c>animation-core/Easing.kt</c>. Expressive moved most motion to
/// springs, so these survive only where upstream still uses a tween — state layer fades and the
/// ripple's radius growth.
/// </remarks>
public static class Easings
{
    public static Easing Linear { get; } = static fraction => fraction;

    /// <summary>The standard curve: accelerates quickly, decelerates gently.</summary>
    public static Easing FastOutSlowIn { get; } = CubicBezier(0.4, 0.0, 0.2, 1.0);

    /// <summary>Elements entering the screen.</summary>
    public static Easing LinearOutSlowIn { get; } = CubicBezier(0.0, 0.0, 0.2, 1.0);

    /// <summary>Elements leaving the screen.</summary>
    public static Easing FastOutLinearIn { get; } = CubicBezier(0.4, 0.0, 1.0, 1.0);

    /// <summary>
    /// A cubic Bézier easing through (0,0), (<paramref name="a" />,<paramref name="b" />),
    /// (<paramref name="c" />,<paramref name="d" />) and (1,1).
    /// </summary>
    /// <remarks>
    /// The curve is parametric in <c>t</c>, but easing needs it as a function of <c>x</c>, so the
    /// <c>x(t) = fraction</c> root has to be found before <c>y(t)</c> can be evaluated. Bisection
    /// is used rather than Newton's method: the curve can be arbitrarily flat, which starves the
    /// derivative, and at this precision the cost is a handful of iterations either way.
    /// </remarks>
    public static Easing CubicBezier(double a, double b, double c, double d)
    {
        return fraction =>
        {
            if (fraction <= 0 || fraction >= 1)
                return fraction;

            var start = 0.0;
            var end = 1.0;

            for (var i = 0; i < 50; i++)
            {
                var midpoint = (start + end) / 2;
                var x = EvaluateCubic(a, c, midpoint);

                if (Math.Abs(fraction - x) < 1e-6)
                    return EvaluateCubic(b, d, midpoint);

                if (x < fraction)
                    start = midpoint;
                else
                    end = midpoint;
            }

            return EvaluateCubic(b, d, (start + end) / 2);
        };
    }

    /// <summary>One axis of a cubic Bézier whose outer control points are pinned to 0 and 1.</summary>
    private static double EvaluateCubic(double p1, double p2, double t)
    {
        var inverse = 1 - t;
        return 3 * inverse * inverse * t * p1
               + 3 * inverse * t * t * p2
               + t * t * t;
    }
}
