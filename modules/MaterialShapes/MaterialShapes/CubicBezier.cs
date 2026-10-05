/*
 * Copyright 2022 The Android Open Source Project
 * Copyright 2026 @Shirasagi0012
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\Cubic.kt by Shirasagi0012

using Avalonia;

namespace MaterialShapes;

public readonly struct CubicBezier(Point anchor0, Point control0, Point control1, Point anchor1)
    : IEquatable<CubicBezier>
{
    public readonly Point Anchor0 { get; init; } = anchor0;
    public readonly Point Control0 { get; init; } = control0;
    public readonly Point Control1 { get; init; } = control1;
    public readonly Point Anchor1 { get; init; } = anchor1;


    public CubicBezier(CubicBezier cubic) : this(cubic.Anchor0, cubic.Control0, cubic.Control1, cubic.Anchor1)
    {
    }

    /// <summary>
    /// Returns a point on the curve for parameter t, representing the proportional distance along
    /// the curve between its starting point at anchor0 and ending point at anchor1.
    /// </summary>
    /// <param name="t">
    /// The distance along the curve between the anchor points, where 0 is at anchor0 and 1
    /// is at anchor1
    /// </param>
    internal Point PointOnCurve(double t)
    {
        var u = 1 - t;
        return new Point(Anchor0.X * (u * u * u) +
                         Control0.X * (3 * t * u * u) +
                         Control1.X * (3 * t * t * u) +
                         Anchor1.X * (t * t * t),
            Anchor0.Y * (u * u * u) +
            Control0.Y * (3 * t * u * u) +
            Control1.Y * (3 * t * t * u) +
            Anchor1.Y * (t * t * t));
    }

    internal bool IsZeroLength => Math.Abs(Anchor0.X - Anchor1.X) < Utils.DistanceEpsilon &&
                                  Math.Abs(Anchor0.Y - Anchor1.Y) < Utils.DistanceEpsilon;

    internal bool ConvexTo(CubicBezier next)
    {
        var prevVertex = new Point(Anchor0.X, Anchor0.Y);
        var currVertex = new Point(Anchor1.X, Anchor1.Y);
        var nextVertex = new Point(next.Anchor1.X, next.Anchor1.Y);
        return Utils.Convex(prevVertex, currVertex, nextVertex);
    }

    /// <summary>
    /// This function returns the true bounds of this curve.
    /// </summary>
    internal Rect CalculateBounds(bool approximate = false)
    {
        // A curve might be of zero-length, with both anchors co-lated.
        // Just return the point itself.

        if (IsZeroLength)
            return new Rect(Anchor0, Anchor0);

        var minX = Math.Min(Anchor0.X, Anchor1.X);
        var minY = Math.Min(Anchor0.Y, Anchor1.Y);
        var maxX = Math.Max(Anchor0.X, Anchor1.X);
        var maxY = Math.Max(Anchor0.Y, Anchor1.Y);

        if (approximate)
        {
            // Approximate bounds use the bounding box of all anchors and controls
            var topLeft = new Point(
                Math.Min(minX, Math.Min(Control0.X, Control1.X)),
                Math.Min(minY, Math.Min(Control0.Y, Control1.Y)));
            var bottomRight = new Point(
                Math.Max(maxX, Math.Max(Control0.X, Control1.X)),
                Math.Max(maxY, Math.Max(Control0.Y, Control1.Y)));
            return new Rect(topLeft, bottomRight);
        }

        // Find the derivative, which is a quadratic Bezier. Then we can solve for t using
        // the quadratic formula
        var xa = -Anchor0.X + 3 * Control0.X - 3 * Control1.X + Anchor1.X;
        var xb = 2 * Anchor0.X - 4 * Control0.X + 2 * Control1.X;
        var xc = -Anchor0.X + Control0.X;

        if (ZeroIsh(xa))
        {
            // Try Muller's method instead; it can find a single root when a is 0
            if (xb != 0)
            {
                var t = 2 * xc / (-2 * xb);
                if (t is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t).X;
                    if (it < minX)
                        minX = it;
                    if (it > maxX)
                        maxX = it;
                }
            }
        }
        else
        {
            var xs = xb * xb - 4 * xa * xc;
            if (xs >= 0)
            {
                var sqrtXs = Math.Sqrt(xs);
                var t1 = (-xb + sqrtXs) / (2 * xa);
                if (t1 is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t1).X;
                    if (it < minX)
                        minX = it;
                    if (it > maxX)
                        maxX = it;
                }

                var t2 = (-xb - sqrtXs) / (2 * xa);
                if (t2 is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t2).X;
                    if (it < minX)
                        minX = it;
                    if (it > maxX)
                        maxX = it;
                }
            }
        }

        // Repeat for Y coordinate
        var ya = -Anchor0.Y + 3 * Control0.Y - 3 * Control1.Y + Anchor1.Y;
        var yb = 2 * Anchor0.Y - 4 * Control0.Y + 2 * Control1.Y;
        var yc = -Anchor0.Y + Control0.Y;

        if (ZeroIsh(ya))
        {
            if (yb != 0)
            {
                var t = 2 * yc / (-2 * yb);
                if (t is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t).Y;
                    if (it < minY)
                        minY = it;
                    if (it > maxY)
                        maxY = it;
                }
            }
        }
        else
        {
            var ys = yb * yb - 4 * ya * yc;
            if (ys >= 0)
            {
                var sqrtYs = Math.Sqrt(ys);
                var t1 = (-yb + sqrtYs) / (2 * ya);
                if (t1 is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t1).Y;
                    if (it < minY)
                        minY = it;
                    if (it > maxY)
                        maxY = it;
                }

                var t2 = (-yb - sqrtYs) / (2 * ya);
                if (t2 is >= 0 and <= 1)
                {
                    var it = PointOnCurve(t2).Y;
                    if (it < minY)
                        minY = it;
                    if (it > maxY)
                        maxY = it;
                }
            }
        }

        return new Rect(new Point(minX, minY), new Point(maxX, maxY));

        static bool ZeroIsh(double v)
        {
            return Math.Abs(v) < Utils.DistanceEpsilon;
        }
    }

    /// <summary>
    /// Returns two Cubics, created by splitting this curve at the given distance of [t] between the
    /// original starting and ending anchor points.
    /// </summary>
    public readonly (CubicBezier, CubicBezier) Split(double t)
    {
        var u = 1 - t;
        var pointOnCurve = PointOnCurve(t);
        return (
            new CubicBezier(
                Anchor0,
                new Point(
                    Anchor0.X * u + Control0.X * t,
                    Anchor0.Y * u + Control0.Y * t),
                new Point(
                    Anchor0.X * (u * u) + Control0.X * (2 * u * t) + Control1.X * (t * t),
                    Anchor0.Y * (u * u) + Control0.Y * (2 * u * t) + Control1.Y * (t * t)),
                pointOnCurve
            ),
            new CubicBezier(
                pointOnCurve,
                new Point(
                    Control0.X * (u * u) + Control1.X * (2 * u * t) + Anchor1.X * (t * t),
                    Control0.Y * (u * u) + Control1.Y * (2 * u * t) + Anchor1.Y * (t * t)),
                new Point(
                    Control1.X * u + Anchor1.X * t,
                    Control1.Y * u + Anchor1.Y * t),
                Anchor1
            )
        );
    }

    public CubicBezier Reversed()
    {
        return new CubicBezier(Anchor1, Control1, Control0, Anchor0);
    }

    public static CubicBezier operator +(CubicBezier a, CubicBezier b)
    {
        return new CubicBezier(
            new Point(a.Anchor0.X + b.Anchor0.X, a.Anchor0.Y + b.Anchor0.Y),
            new Point(a.Control0.X + b.Control0.X, a.Control0.Y + b.Control0.Y),
            new Point(a.Control1.X + b.Control1.X, a.Control1.Y + b.Control1.Y),
            new Point(a.Anchor1.X + b.Anchor1.X, a.Anchor1.Y + b.Anchor1.Y)
        );
    }

    public static CubicBezier operator *(CubicBezier a, double scalar)
    {
        return new CubicBezier(
            new Point(a.Anchor0.X * scalar, a.Anchor0.Y * scalar),
            new Point(a.Control0.X * scalar, a.Control0.Y * scalar),
            new Point(a.Control1.X * scalar, a.Control1.Y * scalar),
            new Point(a.Anchor1.X * scalar, a.Anchor1.Y * scalar)
        );
    }

    public static CubicBezier operator /(CubicBezier a, double scalar)
    {
        return new CubicBezier(
            new Point(a.Anchor0.X / scalar, a.Anchor0.Y / scalar),
            new Point(a.Control0.X / scalar, a.Control0.Y / scalar),
            new Point(a.Control1.X / scalar, a.Control1.Y / scalar),
            new Point(a.Anchor1.X / scalar, a.Anchor1.Y / scalar)
        );
    }

    public override string ToString()
    {
        return $"anchor0: ({Anchor0.X}, {Anchor0.Y}) control0: ({Control0.X}, {Control0.Y}), " +
               $"control1: ({Control1.X}, {Control1.Y}), anchor1: ({Anchor1.X}, {Anchor1.Y})";
    }

    internal readonly CubicBezier Transformed(Func<Point, Point> f)
    {
        var newCubic = new CubicBezier(
            f(Anchor0),
            f(Control0),
            f(Control1),
            f(Anchor1)
        );

        return newCubic;
    }

    public bool Equals(CubicBezier other)
    {
        return Anchor0.Equals(other.Anchor0) &&
               Control0.Equals(other.Control0) &&
               Control1.Equals(other.Control1) &&
               Anchor1.Equals(other.Anchor1);
    }

    public override bool Equals(object? obj)
    {
        return obj is CubicBezier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Anchor0, Control0, Control1, Anchor1);
    }

    public static CubicBezier StraightLine(Point p0, Point p1)
    {
        return new CubicBezier(
            p0,
            InterpolatePoint(p0, p1, 1.0 / 3.0),
            InterpolatePoint(p0, p1, 2.0 / 3.0),
            p1
        );
    }

    public static CubicBezier CircularArc(Point center, Point p0, Point p1)
    {
        var v0 = p0 - center;
        var v1 = p1 - center;

        var rotatedV0 = new Point(-v0.Y, v0.X);
        var rotatedV1 = new Point(-v1.Y, v1.X);

        var clockwise = rotatedV0.X * v1.X + rotatedV0.Y * v1.Y >= 0;

        var len0 = Math.Sqrt(v0.X * v0.X + v0.Y * v0.Y);
        var len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
        var cosa = (v0.X * v1.X + v0.Y * v1.Y) / (len0 * len1);

        if (cosa > 0.999)
            return StraightLine(p0, p1);

        var k = len0 * 4.0 / 3.0 * (Math.Sqrt(2 * (1 - cosa)) - Math.Sqrt(1 - cosa * cosa)) / (1 - cosa);
        if (!clockwise)
            k = -k;

        return new CubicBezier(
            p0,
            new Point(p0.X + rotatedV0.X / len0 * k, p0.Y + rotatedV0.Y / len0 * k),
            new Point(p1.X - rotatedV1.X / len1 * k, p1.Y - rotatedV1.Y / len1 * k),
            p1
        );
    }

    internal static CubicBezier Empty(double x, double y)
    {
        return new CubicBezier(new Point(x, y), new Point(x, y), new Point(x, y), new Point(x, y));
    }

    public static CubicBezier Interpolate(CubicBezier start, CubicBezier end, double progress)
    {
        CubicBezier newCubic = new(
            InterpolatePoint(start.Anchor0, end.Anchor0, progress),
            InterpolatePoint(start.Control0, end.Control0, progress),
            InterpolatePoint(start.Control1, end.Control1, progress),
            InterpolatePoint(start.Anchor1, end.Anchor1, progress)
        );
        return newCubic;
    }

    private static Point InterpolatePoint(Point p1, Point p2, double t)
    {
        return new Point(p1.X + (p2.X - p1.X) * t, p1.Y + (p2.Y - p1.Y) * t);
    }
}