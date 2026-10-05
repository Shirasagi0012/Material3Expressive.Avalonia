using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class FeatureMappingTest
{
    private readonly RoundedPolygon _triangleWithRoundings =
        new RoundedPolygon(3, rounding: new CornerRounding(0.2));

    private readonly RoundedPolygon _triangle = new RoundedPolygon(3);
    private readonly RoundedPolygon _square = new RoundedPolygon(4);

    [Fact]
    public void FeatureMappingTriangles()
    {
        VerifyMapping(_triangleWithRoundings, _triangle, distances =>
        {
            foreach (var d in distances)
                Assert.True(d < 0.1);
        });
    }

    [Fact]
    public void FeatureMappingTriangleToSquare()
    {
        VerifyMapping(_triangle, _square, distances =>
        {
            Assert.Equal(3, distances.Count);
            AssertEqualish(distances[0], distances[1]);
            Assert.True(distances[0] < 0.3);
            Assert.True(distances[2] < 1e-6);
        });
    }

    [Fact]
    public void FeatureMappingSquareToTriangle()
    {
        VerifyMapping(_square, _triangle, distances =>
        {
            Assert.Equal(3, distances.Count);
            AssertEqualish(distances[0], distances[1]);
            Assert.True(distances[0] < 0.3);
            Assert.True(distances[2] < 1e-6);
        });
    }

    [Fact]
    public void FeatureMappingDoesNotCrash()
    {
        var checkmark = new RoundedPolygon(
            [
                new Point(400, -304),
                new Point(240, -464),
                new Point(296, -520),
                new Point(400, -416),
                new Point(664, -680),
                new Point(720, -624),
                new Point(400, -304)
            ])
            .Normalized();

        var verySunny = RoundedPolygon.CreateStar(
                8,
                innerRadius: 0.65,
                rounding: new CornerRounding(0.15))
            .Normalized();

        VerifyMapping(checkmark, verySunny, distances =>
        {
            Assert.True(distances.Count >= 6);
            Assert.True(distances[0] < 0.15);
        });
    }

    protected void VerifyMapping(
        RoundedPolygon p1,
        RoundedPolygon p2,
        Action<List<double>> validator
    )
    {
        var f1 = MeasuredPolygon.MeasurePolygon(new LengthMeasurer(), p1).Features;
        var f2 = MeasuredPolygon.MeasurePolygon(new LengthMeasurer(), p2).Features;

        var map = FeatureMapping.DoMapping([.. f1], [.. f2]);

        var distances = new List<double>();
        foreach (var (progress1, progress2) in map)
        {
            var feature1 = f1.First(f => f.Progress == progress1);
            var feature2 = f2.First(f => f.Progress == progress2);
            distances.Add(FeatureMapping.FeatureDistSquared(feature1.Feature, feature2.Feature));
        }

        distances.Sort((a, b) => b.CompareTo(a));
        validator(distances);
    }
}
