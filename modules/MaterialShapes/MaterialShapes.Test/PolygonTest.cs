using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class PolygonTest
{
    private readonly RoundedPolygon _square = new RoundedPolygon(4);

    private readonly RoundedPolygon _roundedSquare =
        new RoundedPolygon(4, rounding: new CornerRounding(0.2));

    private readonly RoundedPolygon _pentagon = new RoundedPolygon(5);

    [Fact]
    public void ConstructionTest()
    {
        var min = new Point(-1, -1);
        var max = new Point(1, 1);
        AssertInBounds(_square.Cubics, min, max);

        var doubleSquare = new RoundedPolygon(4, 2);
        min = ScalePoint(min, 2);
        max = ScalePoint(max, 2);
        AssertInBounds(doubleSquare.Cubics, min, max);

        var offsetSquare = new RoundedPolygon(4, center: new Point(1, 2));
        min = new Point(0, 1);
        max = new Point(2, 3);
        AssertInBounds(offsetSquare.Cubics, min, max);

        var squareCopy = new RoundedPolygon(_square);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(squareCopy.Cubics, min, max);

        var p0 = new Point(1, 0);
        var p1 = new Point(0, 1);
        var p2 = new Point(-1, 0);
        var p3 = new Point(0, -1);
        var manualSquare = new RoundedPolygon([p0, p1, p2, p3]);
        min = new Point(-1, -1);
        max = new Point(1, 1);
        AssertInBounds(manualSquare.Cubics, min, max);

        var offset = new Point(1, 2);
        var manualSquareOffset = new RoundedPolygon(
            [
                Add(p0, offset),
                Add(p1, offset),
                Add(p2, offset),
                Add(p3, offset)
            ],
            center: offset);
        min = new Point(0, 1);
        max = new Point(2, 3);
        AssertInBounds(manualSquareOffset.Cubics, min, max);
    }

    [Fact]
    public void BoundsTest()
    {
        var bounds = _square.CalculateBounds();
        AssertEqualish(-1, bounds.Left);
        AssertEqualish(-1, bounds.Top);
        AssertEqualish(1, bounds.Right);
        AssertEqualish(1, bounds.Bottom);

        var betterBounds = _square.CalculateBounds(false);
        AssertEqualish(-1, betterBounds.Left);
        AssertEqualish(-1, betterBounds.Top);
        AssertEqualish(1, betterBounds.Right);
        AssertEqualish(1, betterBounds.Bottom);

        bounds = _roundedSquare.CalculateBounds();
        betterBounds = _roundedSquare.CalculateBounds(false);
        Assert.True(
            betterBounds.Width < bounds.Width,
            $"bounds {bounds.Left}, {bounds.Top}, {bounds.Right}, {bounds.Bottom}, " +
            $"betterBounds = {betterBounds.Left}, {betterBounds.Top}, {betterBounds.Right}, {betterBounds.Bottom}");

        bounds = _pentagon.CalculateBounds();
        var maxBounds = _pentagon.CalculateMaxBounds();
        Assert.True(maxBounds.Width > bounds.Width);
    }

    [Fact]
    public void CenterTest()
    {
        AssertPointEqualish(new Point(0, 0), new Point(_square.CenterX, _square.CenterY));
    }

    [Fact]
    public void TransformTest()
    {
        var squareCopy = _square.Transformed(IdentityTransform());
        var n = _square.Cubics.Count;

        Assert.Equal(n, squareCopy.Cubics.Count);
        for (var i = 0; i < n; i++)
            AssertCubicsEqualish(_square.Cubics[i], squareCopy.Cubics[i]);

        var offset = new Point(1, 2);
        var squareCubics = _square.Cubics;
        var translator = TranslateTransform(offset.X, offset.Y);
        var translatedSquareCubics = _square.Transformed(translator).Cubics;

        for (var i = 0; i < squareCubics.Count; i++)
        {
            AssertPointEqualish(Add(squareCubics[i].Anchor0, offset), translatedSquareCubics[i].Anchor0);
            AssertPointEqualish(Add(squareCubics[i].Control0, offset), translatedSquareCubics[i].Control0);
            AssertPointEqualish(Add(squareCubics[i].Control1, offset), translatedSquareCubics[i].Control1);
            AssertPointEqualish(Add(squareCubics[i].Anchor1, offset), translatedSquareCubics[i].Anchor1);
        }
    }

    [Fact]
    public void FeaturesTest()
    {
        var squareFeatures = _square.Features;
        var nonzero = NonZeroCubics(squareFeatures.SelectMany(f => f.Cubics).ToList());
        AssertCubicListsEqualish(_square.Cubics, nonzero);

        var squareCopy = new RoundedPolygon(_square);
        var squareCopyFeatures = squareCopy.Features;
        nonzero = NonZeroCubics(squareCopyFeatures.SelectMany(f => f.Cubics).ToList());
        AssertCubicListsEqualish(squareCopy.Cubics, nonzero);
    }

    [Fact]
    public void EmptyPolygonTest()
    {
        var poly = new RoundedPolygon(6, 0, rounding: new CornerRounding(0.1));
        Assert.Single(poly.Cubics);

        var stillEmpty = poly.Transformed(ScaleTransform(10, 20));
        Assert.Single(stillEmpty.Cubics);
        Assert.True(stillEmpty.Cubics[0].IsZeroLength);
    }

    [Fact]
    public void EmptySideTest()
    {
        var poly1 = new RoundedPolygon(
        [
            new Point(0, 0),
            new Point(1, 0),
            new Point(1, 0),
            new Point(0, 1)
        ]);

        var poly2 = new RoundedPolygon(
        [
            new Point(0, 0),
            new Point(1, 0),
            new Point(0, 1)
        ]);

        AssertCubicListsEqualish(poly1.Cubics, poly2.Cubics);
    }

    private static List<CubicBezier> NonZeroCubics(IReadOnlyList<CubicBezier> original)
    {
        var result = new List<CubicBezier>();
        for (var i = 0; i < original.Count; i++)
            if (!original[i].IsZeroLength)
                result.Add(original[i]);
        return result;
    }
}
