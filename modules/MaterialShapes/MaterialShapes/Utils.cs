using Avalonia;
using Avalonia.Media;

namespace MaterialShapes;

internal static class Utils
{
    public const double DistanceEpsilon = 1e-4;
    public const double AngleEpsilon = 1e-6;

    internal static bool Convex(Point previous, Point current, Point next)
    {
        return (current - previous).Clockwise(next - current);
    }

    internal static double Square(double v)
    {
        return v * v;
    }

    internal static double DistanceSquared(Vector v)
    {
        return v.X * v.X + v.Y * v.Y;
    }

    internal static double DistanceSquared(double dx, double dy)
    {
        return dx * dx + dy * dy;
    }

    internal static double Distance(Vector v)
    {
        return Math.Sqrt(DistanceSquared(v));
    }

    internal static double Distance(double dx, double dy)
    {
        return Math.Sqrt(DistanceSquared(dx, dy));
    }

    internal static Point RadialToCartesian(double radius, double angleRadians)
    {
        return new Point(radius * Math.Cos(angleRadians), radius * Math.Sin(angleRadians));
    }

    internal static Point Interpolate(Point p0, Point p1, double t)
    {
        return new Point(p0.X + (p1.X - p0.X) * t, p0.Y + (p1.Y - p0.Y) * t);
    }

    internal static Point DirectionVector(double dx, double dy)
    {
        return new Point(dx, dy).GetDirection();
    }

    internal static Point DirectionVector(double angleRadians)
    {
        return new Point(Math.Cos(angleRadians), Math.Sin(angleRadians));
    }

    internal static double DistanceSqurared(Point point)
    {
        return point.X * point.X + point.Y * point.Y;
    }

    internal static double DistanceSquared(Point point)
    {
        return point.X * point.X + point.Y * point.Y;
    }

    public static double PositiveModulo(double num, double mod)
    {
        if (mod <= 0)
            throw new ArgumentOutOfRangeException(nameof(mod));

        var m = num % mod;
        return m < 0 ? m + mod : m;
    }

    public static Rect UnionBounds(Rect a, Rect b)
    {
        var left = Math.Min(a.Left, b.Left);
        var top = Math.Min(a.Top, b.Top);
        var right = Math.Max(a.Right, b.Right);
        var bottom = Math.Max(a.Bottom, b.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    public static Point RadicalToCartesian(double radius, double angleRadians, Point center = default)
    {
        return DirectionVector(angleRadians) * radius + center;
    }

    public static Matrix CreateTransformMatrix(
        double scaleX,
        double scaleY,
        double translateX,
        double translateY,
        double rotationDegree
    )
    {
        var matrix = new Matrix(
            scaleX,
            0,
            0,
            scaleY,
            translateX,
            translateY);
        if (rotationDegree != 0)
        {
            var radians = rotationDegree * Math.PI / 180.0;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            var offsetX = translateX * (1 - cos) + translateY * sin;
            var offsetY = translateY * (1 - cos) - translateX * sin;

            var rotationMatrix = new Matrix(
                cos, sin,
                -sin, cos,
                offsetX, offsetY);

            matrix *= rotationMatrix;
        }

        return matrix;
    }

    public static Transform? CreateFitTransform(Rect sourceBounds, Size availableSize, Stretch stretch, bool center)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;

        if (width <= 0 || height <= 0)
            return null;

        if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
            return null;

        var sx = width / sourceBounds.Width;
        var sy = height / sourceBounds.Height;

        var max = Math.Max(sx, sy);
        var min = Math.Min(sx, sy);

        var (scaleX, scaleY) = stretch switch
        {
            Stretch.None => (1.0, 1.0),
            Stretch.Fill => (sx, sy),
            Stretch.UniformToFill => (max, max),
            _ => (min, min)
        };

        var scaledWidth = sourceBounds.Width * scaleX;
        var scaledHeight = sourceBounds.Height * scaleY;

        var translateX = center ? (width - scaledWidth) / 2 : 0;
        var translateY = center ? (height - scaledHeight) / 2 : 0;

        var children = new Transforms();

        if (sourceBounds.X != 0 || sourceBounds.Y != 0)
            children.Add(new TranslateTransform(-sourceBounds.X, -sourceBounds.Y));

        if (scaleX != 1 || scaleY != 1)
            children.Add(new ScaleTransform(scaleX, scaleY));

        if (translateX != 0 || translateY != 0)
            children.Add(new TranslateTransform(translateX, translateY));

        return children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new TransformGroup { Children = children }
        };
    }
}