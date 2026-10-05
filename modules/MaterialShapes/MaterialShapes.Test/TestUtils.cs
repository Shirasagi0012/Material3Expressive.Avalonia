using Avalonia;

namespace MaterialShapes.Test;

internal static class TestUtils
{
    public const double Epsilon = 1e-4;

    public static void AssertEqualish(double expected, double actual, string? message = null)
    {
        var ok = Math.Abs(expected - actual) <= Epsilon;
        if (message is null)
            Assert.True(ok);
        else
            Assert.True(ok, message);
    }

    public static void AssertPointEqualish(Point expected, Point actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) <= Epsilon);
        Assert.True(Math.Abs(expected.Y - actual.Y) <= Epsilon);
    }

    public static bool PointsEqualish(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) <= Epsilon && Math.Abs(a.Y - b.Y) <= Epsilon;
    }

    public static void AssertPointInBounds(Point point, Point min, Point max)
    {
        Assert.True(point.X >= min.X && point.X <= max.X);
        Assert.True(point.Y >= min.Y && point.Y <= max.Y);
    }

    public static void AssertBetween(Point end0, Point end1, Point actual)
    {
        var minX = Math.Min(end0.X, end1.X);
        var minY = Math.Min(end0.Y, end1.Y);
        var maxX = Math.Max(end0.X, end1.X);
        var maxY = Math.Max(end0.Y, end1.Y);
        Assert.True(minX <= actual.X);
        Assert.True(minY <= actual.Y);
        Assert.True(maxX >= actual.X);
        Assert.True(maxY >= actual.Y);
    }

    public static Point Add(Point a, Point b)
    {
        return new Point(a.X + b.X, a.Y + b.Y);
    }

    public static Point Scale(Point p, double scalar)
    {
        return new Point(p.X * scalar, p.Y * scalar);
    }

    public static Point DividePoint(Point p, double scalar)
    {
        return new Point(p.X / scalar, p.Y / scalar);
    }

    public static Point ScalePoint(Point p, double scalar)
    {
        return new Point(p.X * scalar, p.Y * scalar);
    }

    public static Func<Point, Point> IdentityTransform()
    {
        return p => p;
    }

    public static Func<Point, Point> TranslateTransform(double tx, double ty)
    {
        return p => new Point(p.X + tx, p.Y + ty);
    }

    public static Func<Point, Point> ScaleTransform(double sx, double sy)
    {
        return p => new Point(p.X * sx, p.Y * sy);
    }

    public static void AssertPolygonsEqualish(RoundedPolygon expected, RoundedPolygon actual)
    {
        AssertCubicListsEqualish(expected.Cubics, actual.Cubics);
    }

    public static void AssertCubicListsEqualish(IReadOnlyList<CubicBezier> expected, IReadOnlyList<CubicBezier> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
            AssertCubicsEqualish(expected[i], actual[i]);
    }

    public static void AssertCubicsEqualish(IReadOnlyList<CubicBezier> expected, IReadOnlyList<CubicBezier> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
            AssertCubicEqualish(expected[i], actual[i]);
    }

    public static void AssertCubicEqualish(CubicBezier expected, CubicBezier actual)
    {
        AssertPointEqualish(expected.Anchor0, actual.Anchor0);
        AssertPointEqualish(expected.Control0, actual.Control0);
        AssertPointEqualish(expected.Control1, actual.Control1);
        AssertPointEqualish(expected.Anchor1, actual.Anchor1);
    }

    public static void AssertCubicsEqualish(CubicBezier expected, CubicBezier actual)
    {
        AssertPointEqualish(expected.Anchor0, actual.Anchor0);
        AssertPointEqualish(expected.Control0, actual.Control0);
        AssertPointEqualish(expected.Control1, actual.Control1);
        AssertPointEqualish(expected.Anchor1, actual.Anchor1);
    }

    public static bool CubicsEqualish(CubicBezier a, CubicBezier b)
    {
        return PointsEqualish(a.Anchor0, b.Anchor0) && PointsEqualish(a.Control0, b.Control0) &&
               PointsEqualish(a.Control1, b.Control1) && PointsEqualish(a.Anchor1, b.Anchor1);
    }

    public static void AssertInBounds(IReadOnlyList<CubicBezier> cubics, Point min, Point max)
    {
        foreach (var cubic in cubics)
        {
            AssertPointInBounds(cubic.Anchor0, min, max);
            AssertPointInBounds(cubic.Control0, min, max);
            AssertPointInBounds(cubic.Control1, min, max);
            AssertPointInBounds(cubic.Anchor1, min, max);
        }
    }
}