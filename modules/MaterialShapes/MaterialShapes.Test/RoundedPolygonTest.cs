using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class RoundedPolygonTest
{
    private const double Epsilon = 1e-4;

    private readonly CornerRounding _rounding = new(0.1);
    private readonly List<CornerRounding> _perVtxRounded;

    public RoundedPolygonTest()
    {
        _perVtxRounded = [_rounding, _rounding, _rounding, _rounding];
    }

    [Fact]
    public void NumVertsConstructorTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoundedPolygon(2));

        var square = new RoundedPolygon(4);
        var min = new Point(-1, -1);
        var max = new Point(1, 1);
        AssertInBounds(square.Cubics, min, max);

        var doubleSquare = new RoundedPolygon(4, 2);
        min = Scale(min, 2);
        max = Scale(max, 2);
        AssertInBounds(doubleSquare.Cubics, min, max);

        var squareRounded = new RoundedPolygon(4, rounding: _rounding);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(squareRounded.Cubics, min, max);

        var squarePVRounded = new RoundedPolygon(4, perVertexRounding: _perVtxRounded);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(squarePVRounded.Cubics, min, max);
    }

    [Fact]
    public void VerticesConstructorTest()
    {
        var p0 = new Point(1, 0);
        var p1 = new Point(0, 1);
        var p2 = new Point(-1, 0);
        var p3 = new Point(0, -1);

        Assert.Throws<ArgumentException>(() => new RoundedPolygon([p0, p1]));

        var manualSquare = new RoundedPolygon([p0, p1, p2, p3]);
        var min = new Point(-1, -1);
        var max = new Point(1, 1);
        AssertInBounds(manualSquare.Cubics, min, max);

        var offset = new Point(1, 2);
        var manualSquareOffset = new RoundedPolygon(
            [Add(p0, offset), Add(p1, offset), Add(p2, offset), Add(p3, offset)],
            center: offset);
        min = new Point(0, 1);
        max = new Point(2, 3);
        AssertInBounds(manualSquareOffset.Cubics, min, max);

        var manualSquareRounded = new RoundedPolygon([p0, p1, p2, p3], _rounding);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(manualSquareRounded.Cubics, min, max);

        var manualSquarePVRounded = new RoundedPolygon([p0, p1, p2, p3], perVertexRounding: _perVtxRounded);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(manualSquarePVRounded.Cubics, min, max);
    }

    [Fact]
    public void FeaturesConstructorThrowsForTooFewFeatures()
    {
        Assert.Throws<ArgumentException>(() => new RoundedPolygon(new List<Feature>()));
        var corner = Feature.BuildConvexCorner([CubicBezier.Empty(0, 0)]);
        Assert.Throws<ArgumentException>(() => new RoundedPolygon([corner]));
    }

    [Fact]
    public void FeaturesConstructorThrowsForNonContinuousFeatures()
    {
        var cubic1 = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var cubic2 = CubicBezier.StraightLine(new Point(10, 10), new Point(20, 20));
        Assert.Throws<ArgumentException>(() =>
            new RoundedPolygon([Feature.BuildEdge(cubic1), Feature.BuildEdge(cubic2)]));
    }

    [Fact]
    public void FeaturesConstructorReconstructsSquare()
    {
        var basePolygon = RoundedPolygon.CreateRectangle();
        var actual = new RoundedPolygon(basePolygon.Features);
        AssertPolygonsEqualish(basePolygon, actual);
    }

    [Fact]
    public void FeaturesConstructorReconstructsRoundedSquare()
    {
        var basePolygon = RoundedPolygon.CreateRectangle(rounding: new CornerRounding(0.5, 0.2));
        var actual = new RoundedPolygon(basePolygon.Features);
        AssertPolygonsEqualish(basePolygon, actual);
    }

    [Fact]
    public void FeaturesConstructorReconstructsCircles()
    {
        for (var i = 3; i <= 20; i++)
        {
            var basePolygon = RoundedPolygon.CreateCircle(i);
            var actual = new RoundedPolygon(basePolygon.Features);
            AssertPolygonsEqualish(basePolygon, actual);
        }
    }

    [Fact]
    public void FeaturesConstructorReconstructsStars()
    {
        for (var i = 3; i <= 20; i++)
        {
            var basePolygon = RoundedPolygon.CreateStar(i);
            var actual = new RoundedPolygon(basePolygon.Features);
            AssertPolygonsEqualish(basePolygon, actual);
        }
    }

    [Fact]
    public void FeaturesConstructorReconstructsRoundedStars()
    {
        for (var i = 3; i <= 20; i++)
        {
            var basePolygon = RoundedPolygon.CreateStar(
                i,
                rounding: new CornerRounding(0.5, 0.2));
            var actual = new RoundedPolygon(basePolygon.Features);
            AssertPolygonsEqualish(basePolygon, actual);
        }
    }

    [Fact]
    public void FeaturesConstructorReconstructsPill()
    {
        var basePolygon = RoundedPolygon.CreatePill();
        var actual = new RoundedPolygon(basePolygon.Features);
        AssertPolygonsEqualish(basePolygon, actual);
    }

    [Fact]
    public void FeaturesConstructorReconstructsPillStar()
    {
        var basePolygon = RoundedPolygon.CreatePillStar(rounding: new CornerRounding(0.5, 0.2));
        var actual = new RoundedPolygon(basePolygon.Features);
        AssertPolygonsEqualish(basePolygon, actual);
    }

    [Fact]
    public void FeaturesConstructorDefensivelyCopiesInputList()
    {
        var basePolygon = RoundedPolygon.CreateRectangle();
        var sourceFeatures = basePolygon.Features.ToList();
        var actual = new RoundedPolygon(sourceFeatures);

        sourceFeatures[0] = Feature.BuildEdge(CubicBezier.StraightLine(new Point(0, 0), new Point(0, 0)));
        sourceFeatures.Clear();

        AssertPolygonsEqualish(basePolygon, actual);
    }

    [Fact]
    public void ComputeCenterTest()
    {
        var polygon = new RoundedPolygon(
            [new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1)]);

        AssertEqualish(0.5, polygon.CenterX);
        AssertEqualish(0.5, polygon.CenterY);
    }

    [Fact]
    public void RoundingSpaceUsageTest()
    {
        var p0 = new Point(0, 0);
        var p1 = new Point(1, 0);
        var p2 = new Point(0.5, 1);
        var pvRounding = new List<CornerRounding>
        {
            new(1, 0),
            new(1, 1),
            CornerRounding.Unrounded
        };
        var polygon = new RoundedPolygon([p0, p1, p2], perVertexRounding: pvRounding);

        var lowerEdgeFeature = polygon.Features.First(f => f.IsEdge);
        Assert.Single(lowerEdgeFeature.Cubics);

        var lowerEdge = lowerEdgeFeature.Cubics[0];
        AssertEqualish(0.5, lowerEdge.Anchor0.X);
        AssertEqualish(0.0, lowerEdge.Anchor0.Y);
        AssertEqualish(0.5, lowerEdge.Anchor1.X);
        AssertEqualish(0.0, lowerEdge.Anchor1.Y);
    }

    private const int Points = 20;

    [Fact]
    public void UnevenSmoothingTest()
    {
        for (var i = 0; i <= Points; i++)
        {
            var smooth = (double)i / Points;
            DoUnevenSmoothTest(
                new CornerRounding(0.4, smooth),
                0.4 * (1 + smooth),
                Math.Min(0.4 * (1 + smooth), 0.5),
                0.5);
        }
    }

    [Fact]
    public void UnevenSmoothingTest2()
    {
        for (var i = 0; i <= Points; i++)
        {
            var smooth = (double)i / Points;

            var smoothWantedV0 = 0.4 * smooth;
            var smoothWantedV3 = 0.2;
            var factor = Math.Min(0.4 / (smoothWantedV0 + smoothWantedV3), 1);

            DoUnevenSmoothTest(
                new CornerRounding(0.4, smooth),
                0.4 * (1 + smooth),
                0.4 + factor * smoothWantedV0,
                0.2 + factor * smoothWantedV3,
                new CornerRounding(0.2, 1));
        }
    }

    [Fact]
    public void UnevenSmoothingTest3()
    {
        for (var i = 0; i <= Points; i++)
        {
            var smooth = (double)i / Points;
            DoUnevenSmoothTest(
                new CornerRounding(0.4, smooth),
                0.4 * (1 + smooth),
                0.4,
                0.6,
                new CornerRounding(0.6));
        }
    }

    [Fact]
    public void CreatingFullSizeTest()
    {
        var radius = 400d;
        var innerRadiusFactor = 0.35d;
        var innerRadius = radius * innerRadiusFactor;
        var roundingFactor = 0.32d;

        var fullSizeShape = RoundedPolygon.CreateStar(
                4,
                radius,
                innerRadius,
                new CornerRounding(radius * roundingFactor),
                new CornerRounding(radius * roundingFactor),
                center: new Point(radius, radius))
            .Transformed(p => new Point((p.X - radius) / radius, (p.Y - radius) / radius));

        var canonicalShape = RoundedPolygon.CreateStar(
            4,
            1,
            innerRadiusFactor,
            new CornerRounding(roundingFactor),
            new CornerRounding(roundingFactor));

        var cubics = canonicalShape.Cubics;
        var cubics1 = fullSizeShape.Cubics;
        Assert.Equal(cubics.Count, cubics1.Count);
        for (var i = 0; i < cubics.Count; i++)
        {
            AssertEqualish(cubics[i].Anchor0.X, cubics1[i].Anchor0.X);
            AssertEqualish(cubics[i].Anchor0.Y, cubics1[i].Anchor0.Y);
            AssertEqualish(cubics[i].Anchor1.X, cubics1[i].Anchor1.X);
            AssertEqualish(cubics[i].Anchor1.Y, cubics1[i].Anchor1.Y);
            AssertEqualish(cubics[i].Control0.X, cubics1[i].Control0.X);
            AssertEqualish(cubics[i].Control0.Y, cubics1[i].Control0.Y);
            AssertEqualish(cubics[i].Control1.X, cubics1[i].Control1.X);
            AssertEqualish(cubics[i].Control1.Y, cubics1[i].Control1.Y);
        }
    }

    private void DoUnevenSmoothTest(
        CornerRounding rounding0,
        double expectedV0SX,
        double expectedV0SY,
        double expectedV3SY,
        CornerRounding? rounding3 = null
    )
    {
        var p0 = new Point(0, 0);
        var p1 = new Point(5, 0);
        var p2 = new Point(5, 1);
        var p3 = new Point(0, 1);

        var pvRounding = new List<CornerRounding>
        {
            rounding0,
            CornerRounding.Unrounded,
            CornerRounding.Unrounded,
            rounding3 ?? new CornerRounding(0.5)
        };

        var polygon = new RoundedPolygon([p0, p1, p2, p3], perVertexRounding: pvRounding);
        var edges = polygon.Features.Where(f => f.IsEdge).ToList();
        var e01 = edges[0];
        var e30 = edges[3];

        var msg = $"r0 = {Show(rounding0)}, r3 = {Show(rounding3 ?? new CornerRounding(0.5))}";
        AssertEqualish(expectedV0SX, e01.Cubics[0].Anchor0.X, msg);
        AssertEqualish(expectedV0SY, e30.Cubics[0].Anchor1.Y, msg);
        AssertEqualish(expectedV3SY, 1 - e30.Cubics[0].Anchor0.Y, msg);
    }

    private static string Show(CornerRounding cr)
    {
        return $"(r={cr.Radius}, s={cr.Smoothing})";
    }
}
