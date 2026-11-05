/*
 * Copyright 2019, 2024 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Adapted from the pinned Compose Easing.kt, Bezier.kt and MathHelpers.kt.
 */
using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;


public sealed class CubicBezierEasing : Easing, IEquatable<CubicBezierEasing>
{
    private const double Epsilon = 1e-7;
    private const float FloatEpsilon = 1.05e-6f;
    private const float OneUlpAt1 = 1.1920929e-7f;
    private readonly float _min;
    private readonly float _max;

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

        (_min, _max) = ComputeVerticalBounds((float)b, (float)d);
    }

    public double A { get; }
    public double B { get; }
    public double C { get; }
    public double D { get; }

    public override double Ease(double progress)
    {
        if (!(progress > 0 && progress < 1))
            return progress;
        var fraction = (float)progress;
        if (fraction is <= 0 or >= 1)
            return fraction;
        // Fixed Compose Easing.kt / ui-graphics Bezier.kt: preserve Float translations,
        // Cardano root ordering, fastCbrt and the evaluation's Float rounding boundaries.
        var f = Math.Max(fraction, OneUlpAt1);
        var t = FindFirstCubicRoot(-f, (float)A - f, (float)C - f, 1f - f);
        if (float.IsNaN(t))
            throw new ArgumentException($"The cubic curve with parameters ({A}, {B}, {C}, {D}) has no solution at {fraction}");
        var a = 1f / 3f + ((float)B - (float)D);
        var b = (float)D - 2f * (float)B;
        return Math.Clamp(3f * ((a * t + b) * t + (float)B) * t, _min, _max);
    }

    public bool Equals(CubicBezierEasing? other) =>
        other is not null && A == other.A && B == other.B && C == other.C && D == other.D;

    public override bool Equals(object? obj) => obj is CubicBezierEasing other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(A, B, C, D);

    public override string ToString() => $"CubicBezierEasing(a={A}, b={B}, c={C}, d={D})";

    private static float FindFirstCubicRoot(float p0, float p1, float p2, float p3)
    {
        var a = 3.0 * (p0 - 2.0 * p1 + p2);
        var b = 3.0 * (p1 - p0);
        var c = (double)p0;
        var d = -p0 + 3.0 * (p1 - p2) + p3;
        if (Math.Abs(d) < Epsilon)
        {
            if (Math.Abs(a) < Epsilon)
                return Math.Abs(b) < Epsilon ? float.NaN : ClampRoot((float)(-c / b));
            var q = Math.Sqrt(b * b - 4.0 * a * c);
            var a2 = 2.0 * a;
            var root = ClampRoot((float)((q - b) / a2));
            return !float.IsNaN(root) ? root : ClampRoot((float)((-b - q) / a2));
        }
        a /= d;
        b /= d;
        c /= d;
        var o3 = (3.0 * b - a * a) / 9.0;
        var q2 = (2.0 * a * a * a - 9.0 * a * b + 27.0 * c) / 54.0;
        var discriminant = q2 * q2 + o3 * o3 * o3;
        var a3 = a / 3.0;
        if (discriminant < 0.0)
        {
            var r = Math.Sqrt(-(o3 * o3 * o3));
            var phi = Math.Acos(Math.Clamp(-q2 / r, -1.0, 1.0));
            var t1 = 2f * FastCbrt((float)r);
            var root = ClampRoot((float)(t1 * Math.Cos(phi / 3.0) - a3));
            if (!float.IsNaN(root)) return root;
            root = ClampRoot((float)(t1 * Math.Cos((phi + Math.Tau) / 3.0) - a3));
            if (!float.IsNaN(root)) return root;
            return ClampRoot((float)(t1 * Math.Cos((phi + 2.0 * Math.Tau) / 3.0) - a3));
        }
        if (discriminant == 0.0)
        {
            var u = -FastCbrt((float)q2);
            var root = ClampRoot(2f * u - (float)a3);
            return !float.IsNaN(root) ? root : ClampRoot(-u - (float)a3);
        }
        var sd = Math.Sqrt(discriminant);
        var u1 = FastCbrt((float)(-q2 + sd));
        var v1 = FastCbrt((float)(q2 + sd));
        return ClampRoot((float)(u1 - v1 - a3));
    }

    private static float FastCbrt(float x)
    {
        // ui-util MathHelpers.fastCbrt, including the signed-bit extension mask.
        var bits = (long)BitConverter.SingleToInt32Bits(x) & 0x1ffffffffL;
        var estimate = BitConverter.Int32BitsToSingle(unchecked(0x2a510554 + (int)(bits / 3)));
        estimate -= (estimate - x / (estimate * estimate)) * (1f / 3f);
        estimate -= (estimate - x / (estimate * estimate)) * (1f / 3f);
        return estimate;
    }

    private static float ClampRoot(float root)
    {
        var clamped = Math.Clamp(root, 0f, 1f);
        return Math.Abs(clamped - root) > FloatEpsilon ? float.NaN : clamped;
    }

    private static (float Min, float Max) ComputeVerticalBounds(float p1, float p2)
    {
        // The first and second derivatives give the same candidate extrema as
        // computeCubicVerticalBounds(0, p1, p2, 1); duplicate roots do not change bounds.
        var d0 = 3f * p1;
        var d1 = 3f * (p2 - p1);
        var d2 = 3f * (1f - p2);
        var min = 0f;
        var max = 1f;
        void Include(float candidate)
        {
            var t = ClampRoot(candidate);
            if (float.IsNaN(t)) return;
            var a = 1f + 3f * (p1 - p2);
            var b = 3f * (p2 - 2f * p1);
            var c = 3f * p1;
            var y = ((a * t + b) * t + c) * t;
            min = Math.Min(min, y);
            max = Math.Max(max, y);
        }
        var a = (double)d0;
        var b = (double)d1;
        var c = (double)d2;
        var d = a - 2.0 * b + c;
        if (d != 0.0)
        {
            var v1 = -Math.Sqrt(b * b - a * c);
            var v2 = -a + b;
            Include((float)(-(v1 + v2) / d));
            Include((float)((v1 - v2) / d));
        }
        else if (b != c)
            Include((float)((2.0 * b - c) / (2.0 * b - 2.0 * c)));
        var dd0 = 2f * (d1 - d0);
        var dd1 = 2f * (d2 - d1);
        Include(-dd0 / (dd1 - dd0));
        return (min, max);
    }
}
