using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;


public sealed class CubicBezierEasing : Easing, IEquatable<CubicBezierEasing>
{
    private const double Epsilon = 1e-7;

    private readonly double _ax;
    private readonly double _bx;
    private readonly double _cx;
    private readonly double _ay;
    private readonly double _by;
    private readonly double _cy;

    /// <param name="a">X of the first control point (tangent at 0,0).</param>
    /// <param name="b">Y of the first control point (tangent at 0,0).</param>
    /// <param name="c">X of the second control point (tangent at 1,1).</param>
    /// <param name="d">Y of the second control point (tangent at 1,1).</param>
    public CubicBezierEasing(double a, double b, double c, double d)
    {
        if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(c) || double.IsNaN(d))
        {
            throw new ArgumentException(
                $"Parameters to CubicBezierEasing cannot be NaN. Actual parameters are: {a}, {b}, {c}, {d}.");
        }

        A = a;
        B = b;
        C = c;
        D = d;

        _cx = 3 * a;
        _bx = 3 * (c - a) - _cx;
        _ax = 1 - _cx - _bx;

        _cy = 3 * b;
        _by = 3 * (d - b) - _cy;
        _ay = 1 - _cy - _by;
    }

    public double A { get; }
    public double B { get; }
    public double C { get; }
    public double D { get; }

    public override double Ease(double progress)
    {
        if (progress <= 0 || progress >= 1)
            return progress;
        return SampleY(SolveX(progress));
    }

    public bool Equals(CubicBezierEasing? other) =>
        other is not null && A == other.A && B == other.B && C == other.C && D == other.D;

    public override bool Equals(object? obj) => obj is CubicBezierEasing other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(A, B, C, D);

    public override string ToString() => $"CubicBezierEasing(a={A}, b={B}, c={C}, d={D})";

    private double SampleX(double t) => ((_ax * t + _bx) * t + _cx) * t;

    private double SampleY(double t) => ((_ay * t + _by) * t + _cy) * t;

    private double SampleDerivativeX(double t) => (3 * _ax * t + 2 * _bx) * t + _cx;

    private double SolveX(double x)
    {
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var delta = SampleX(t) - x;
            if (Math.Abs(delta) < Epsilon)
                return t;
            var derivative = SampleDerivativeX(t);
            if (Math.Abs(derivative) < Epsilon)
                break;
            var next = t - delta / derivative;
            if (next is < 0 or > 1)
                break;
            t = next;
        }

        var lo = 0.0;
        var hi = 1.0;
        t = Math.Clamp(t, 0, 1);
        for (var i = 0; i < 20; i++)
        {
            var sampled = SampleX(t);
            if (Math.Abs(sampled - x) < Epsilon)
                return t;
            if (sampled < x)
                lo = t;
            else
                hi = t;
            t = (lo + hi) * 0.5;
        }

        return t;
    }
}
