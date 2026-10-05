using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class ShapesTest
{
    private static readonly Point Zero = new(0, 0);
    private const double Epsilon = 0.01;

    private static double Distance(Point start, Point end)
    {
        var vector = end - start;
        return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    }

    private static void AssertPointOnRadii(Point point, double radius1, double radius2, Point center)
    {
        var dist = Distance(center, point);
        try
        {
            Assert.Equal(radius1, dist, Epsilon);
        }
        catch (Exception)
        {
            Assert.Equal(radius2, dist, Epsilon);
        }
    }

    private static void AssertPointOnRadii(Point point, double radius1, double radius2)
    {
        AssertPointOnRadii(point, radius1, radius2, Zero);
    }

    private static void AssertPointOnRadii(Point point, double radius1)
    {
        AssertPointOnRadii(point, radius1, radius1, Zero);
    }

    private static void AssertCubicOnRadii(CubicBezier cubic, double radius1, double radius2, Point center)
    {
        AssertPointOnRadii(cubic.Anchor0, radius1, radius2, center);
        AssertPointOnRadii(cubic.Anchor1, radius1, radius2, center);
    }

    private static void AssertCubicOnRadii(CubicBezier cubic, double radius1, double radius2)
    {
        AssertCubicOnRadii(cubic, radius1, radius2, Zero);
    }

    private static void AssertCircularCubic(CubicBezier cubic, double radius, Point center)
    {
        var t = 0d;
        while (t <= 1d)
        {
            var pointOnCurve = cubic.PointOnCurve(t);
            var distanceToPoint = Distance(center, pointOnCurve);
            Assert.Equal(radius, distanceToPoint, Epsilon);
            t += 0.1d;
        }
    }

    private static void AssertCircleShape(IReadOnlyList<CubicBezier> shape, double radius, Point center)
    {
        foreach (var cubic in shape)
            AssertCircularCubic(cubic, radius, center);
    }

    private static void AssertCircleShape(IReadOnlyList<CubicBezier> shape)
    {
        AssertCircleShape(shape, 1, Zero);
    }


    [Fact]
    public void CircleTest()
    {
        Assert.Throws<ArgumentException>(() => RoundedPolygon.CreateCircle(2));

        var circle = RoundedPolygon.CreateCircle();
        AssertCircleShape(circle.Cubics);

        var simpleCircle = RoundedPolygon.CreateCircle(3);
        AssertCircleShape(simpleCircle.Cubics);

        var complexCircle = RoundedPolygon.CreateCircle(20);
        AssertCircleShape(complexCircle.Cubics);

        var bigCircle = RoundedPolygon.CreateCircle(radius: 3);
        AssertCircleShape(bigCircle.Cubics, 3, Zero);

        var center = new Point(1, 2);
        var offsetCircle = RoundedPolygon.CreateCircle(center: center);
        AssertCircleShape(offsetCircle.Cubics, 1, center);
    }

    [Fact]
    public void StarTest()
    {
        var star = RoundedPolygon.CreateStar(4, innerRadius: 0.5);
        var shape = star.Cubics;
        var radius = 1d;
        var innerRadius = 0.5d;
        foreach (var cubic in shape)
            AssertCubicOnRadii(cubic, radius, innerRadius);

        var center = new Point(1, 2);
        star = RoundedPolygon.CreateStar(4, innerRadius: innerRadius, center: center);
        shape = star.Cubics;
        foreach (var cubic in shape)
            AssertCubicOnRadii(cubic, radius, innerRadius, center);

        radius = 4d;
        innerRadius = 2d;
        star = RoundedPolygon.CreateStar(4, radius, innerRadius);
        shape = star.Cubics;
        foreach (var cubic in shape)
            AssertCubicOnRadii(cubic, radius, innerRadius);
    }

    [Fact]
    public void RoundedStarTest()
    {
        var rounding = new CornerRounding(0.1);
        var innerRounding = new CornerRounding(0.2);
        var perVtxRounded = new List<CornerRounding>
        {
            rounding, innerRounding, rounding, innerRounding, rounding, innerRounding, rounding, innerRounding
        };

        var star = RoundedPolygon.CreateStar(4, innerRadius: 0.5, rounding: rounding);
        var min = new Point(-1, -1);
        var max = new Point(1, 1);
        AssertInBounds(star.Cubics, min, max);

        star = RoundedPolygon.CreateStar(4, innerRadius: 0.5, innerRounding: innerRounding);
        AssertInBounds(star.Cubics, min, max);

        star = RoundedPolygon.CreateStar(4, innerRadius: 0.5, rounding: rounding, innerRounding: innerRounding);
        AssertInBounds(star.Cubics, min, max);

        star = RoundedPolygon.CreateStar(4, innerRadius: 0.5, perVertexRounding: perVtxRounded);
        AssertInBounds(star.Cubics, min, max);

        Assert.Throws<ArgumentException>(() =>
            RoundedPolygon.CreateStar(6, innerRadius: 0.5, perVertexRounding: perVtxRounded));
    }
}