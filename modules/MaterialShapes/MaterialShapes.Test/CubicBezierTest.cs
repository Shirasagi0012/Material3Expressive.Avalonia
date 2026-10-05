using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class CubicBezierTest
{
    private static readonly Point Zero = new(0, 0);
    private static readonly Point P0 = new(1, 0);
    private static readonly Point P1 = new(1, 0.5);
    private static readonly Point P2 = new(0.5, 1);
    private static readonly Point P3 = new(0, 1);

    private readonly CubicBezier _cubic = new(P0, P1, P2, P3);

    [Fact]
    public void ConstructionTest()
    {
        Assert.Equal(P0, _cubic.Anchor0);
        Assert.Equal(P1, _cubic.Control0);
        Assert.Equal(P2, _cubic.Control1);
        Assert.Equal(P3, _cubic.Anchor1);
    }

    [Fact]
    public void CircularArcTest()
    {
        var arcCubic = CubicBezier.CircularArc(Zero, P0, P3);
        Assert.Equal(P0, arcCubic.Anchor0);
        Assert.Equal(P3, arcCubic.Anchor1);
    }

    [Fact]
    public void DivTest()
    {
        var divCubic = _cubic / 1d;
        AssertCubicsEqualish(_cubic, divCubic);

        divCubic = _cubic / 1;
        AssertCubicsEqualish(_cubic, divCubic);

        divCubic = _cubic / 2d;
        AssertPointEqualish(DividePoint(P0, 2d), divCubic.Anchor0);
        AssertPointEqualish(DividePoint(P1, 2d), divCubic.Control0);
        AssertPointEqualish(DividePoint(P2, 2d), divCubic.Control1);
        AssertPointEqualish(DividePoint(P3, 2d), divCubic.Anchor1);

        divCubic = _cubic / 2;
        AssertPointEqualish(DividePoint(P0, 2d), divCubic.Anchor0);
        AssertPointEqualish(DividePoint(P1, 2d), divCubic.Control0);
        AssertPointEqualish(DividePoint(P2, 2d), divCubic.Control1);
        AssertPointEqualish(DividePoint(P3, 2d), divCubic.Anchor1);
    }

    [Fact]
    public void TimesTest()
    {
        var timesCubic = _cubic * 1d;
        Assert.Equal(P0, timesCubic.Anchor0);
        Assert.Equal(P1, timesCubic.Control0);
        Assert.Equal(P2, timesCubic.Control1);
        Assert.Equal(P3, timesCubic.Anchor1);

        timesCubic = _cubic * 1;
        Assert.Equal(P0, timesCubic.Anchor0);
        Assert.Equal(P1, timesCubic.Control0);
        Assert.Equal(P2, timesCubic.Control1);
        Assert.Equal(P3, timesCubic.Anchor1);

        timesCubic = _cubic * 2d;
        AssertPointEqualish(ScalePoint(P0, 2d), timesCubic.Anchor0);
        AssertPointEqualish(ScalePoint(P1, 2d), timesCubic.Control0);
        AssertPointEqualish(ScalePoint(P2, 2d), timesCubic.Control1);
        AssertPointEqualish(ScalePoint(P3, 2d), timesCubic.Anchor1);

        timesCubic = _cubic * 2;
        AssertPointEqualish(ScalePoint(P0, 2d), timesCubic.Anchor0);
        AssertPointEqualish(ScalePoint(P1, 2d), timesCubic.Control0);
        AssertPointEqualish(ScalePoint(P2, 2d), timesCubic.Control1);
        AssertPointEqualish(ScalePoint(P3, 2d), timesCubic.Anchor1);
    }

    [Fact]
    public void PlusTest()
    {
        var offsetCubic = _cubic * 2d;
        var plusCubic = _cubic + offsetCubic;

        AssertPointEqualish(Add(P0, offsetCubic.Anchor0), plusCubic.Anchor0);
        AssertPointEqualish(Add(P1, offsetCubic.Control0), plusCubic.Control0);
        AssertPointEqualish(Add(P2, offsetCubic.Control1), plusCubic.Control1);
        AssertPointEqualish(Add(P3, offsetCubic.Anchor1), plusCubic.Anchor1);
    }

    [Fact]
    public void ReverseTest()
    {
        var reverseCubic = _cubic.Reversed();
        Assert.Equal(P3, reverseCubic.Anchor0);
        Assert.Equal(P2, reverseCubic.Control0);
        Assert.Equal(P1, reverseCubic.Control1);
        Assert.Equal(P0, reverseCubic.Anchor1);
    }

    [Fact]
    public void StraightLineTest()
    {
        var lineCubic = CubicBezier.StraightLine(P0, P3);
        Assert.Equal(P0, lineCubic.Anchor0);
        Assert.Equal(P3, lineCubic.Anchor1);
        AssertBetween(P0, P3, lineCubic.Control0);
        AssertBetween(P0, P3, lineCubic.Control1);
    }

    [Fact]
    public void SplitTest()
    {
        var (split0, split1) = _cubic.Split(0.5);
        Assert.Equal(_cubic.Anchor0, split0.Anchor0);
        Assert.Equal(_cubic.Anchor1, split1.Anchor1);
        AssertBetween(_cubic.Anchor0, _cubic.Anchor1, split0.Anchor1);
        AssertBetween(_cubic.Anchor0, _cubic.Anchor1, split1.Anchor0);
    }

    [Fact]
    public void PointOnCurveTest()
    {
        var halfway = _cubic.PointOnCurve(0.5);
        AssertBetween(_cubic.Anchor0, _cubic.Anchor1, halfway);

        var straightLineCubic = CubicBezier.StraightLine(P0, P3);
        halfway = straightLineCubic.PointOnCurve(0.5);
        var computedHalfway = new Point(P0.X + 0.5 * (P3.X - P0.X), P0.Y + 0.5 * (P3.Y - P0.Y));
        AssertPointEqualish(computedHalfway, halfway);
    }

    [Fact]
    public void TransformTest()
    {
        var transform = IdentityTransform();
        var transformedCubic = _cubic.Transformed(transform);
        AssertCubicsEqualish(_cubic, transformedCubic);

        transform = ScaleTransform(3d, 3d);
        transformedCubic = _cubic.Transformed(transform);
        AssertCubicsEqualish(_cubic * 3d, transformedCubic);

        var tx = 200d;
        var ty = 300d;
        var translationVector = new Point(tx, ty);
        transform = TranslateTransform(tx, ty);
        transformedCubic = _cubic.Transformed(transform);

        AssertPointEqualish(Add(_cubic.Anchor0, translationVector), transformedCubic.Anchor0);
        AssertPointEqualish(Add(_cubic.Control0, translationVector), transformedCubic.Control0);
        AssertPointEqualish(Add(_cubic.Control1, translationVector), transformedCubic.Control1);
        AssertPointEqualish(Add(_cubic.Anchor1, translationVector), transformedCubic.Anchor1);
    }

    [Fact]
    public void EmptyCubicHasZeroLength()
    {
        Assert.True(CubicBezier.Empty(10d, 10d).IsZeroLength);
    }
}