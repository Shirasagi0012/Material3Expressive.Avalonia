using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class FeaturesTest
{
    [Fact]
    public void CannotBuildEmptyFeatures()
    {
        Assert.Throws<ArgumentException>(() => Feature.BuildConvexCorner([]));
        Assert.Throws<ArgumentException>(() => Feature.BuildConcaveCorner([]));
        Assert.Throws<ArgumentException>(() => Feature.BuildIgnorableFeature([]));
    }

    [Fact]
    public void CannotBuildNonContinuousFeatures()
    {
        var cubic1 = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 1));
        var cubic2 = CubicBezier.StraightLine(new Point(10, 10), new Point(11, 11));

        Assert.Throws<ArgumentException>(() => Feature.BuildConvexCorner([cubic1, cubic2]));
        Assert.Throws<ArgumentException>(() => Feature.BuildConcaveCorner([cubic1, cubic2]));
        Assert.Throws<ArgumentException>(() => Feature.BuildIgnorableFeature([cubic1, cubic2]));
    }

    [Fact]
    public void BuildsConcaveCorner()
    {
        var cubic = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var actual = Feature.BuildConcaveCorner([cubic]);

        Assert.True(actual.IsConcaveCorner);
        Assert.False(actual.IsConvexCorner);
        Assert.False(actual.IsEdge);
        Assert.False(actual.IsIgnorableFeature);
        AssertCubicsEqualish([cubic], actual.Cubics);
    }

    [Fact]
    public void BuildsConvexCorner()
    {
        var cubic = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var actual = Feature.BuildConvexCorner([cubic]);

        Assert.True(actual.IsConvexCorner);
        Assert.False(actual.IsConcaveCorner);
        Assert.False(actual.IsEdge);
        Assert.False(actual.IsIgnorableFeature);
        AssertCubicsEqualish([cubic], actual.Cubics);
    }

    [Fact]
    public void BuildsEdge()
    {
        var cubic = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var actual = Feature.BuildEdge(cubic);

        Assert.True(actual.IsEdge);
        Assert.True(actual.IsIgnorableFeature);
        Assert.False(actual.IsConvexCorner);
        Assert.False(actual.IsConcaveCorner);
        AssertCubicsEqualish([cubic], actual.Cubics);
    }

    [Fact]
    public void BuildsIgnorableAsEdge()
    {
        var cubic = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var actual = Feature.BuildIgnorableFeature([cubic]);

        Assert.True(actual.IsEdge);
        Assert.True(actual.IsIgnorableFeature);
        Assert.False(actual.IsConvexCorner);
        Assert.False(actual.IsConcaveCorner);
        AssertCubicsEqualish([cubic], actual.Cubics);
    }

    [Fact]
    public void FeatureDefensivelyCopiesInputCubics()
    {
        var cubic = CubicBezier.StraightLine(new Point(0, 0), new Point(1, 0));
        var sourceCubics = new List<CubicBezier> { cubic };
        var actual = Feature.BuildConvexCorner(sourceCubics);

        sourceCubics[0] = CubicBezier.StraightLine(new Point(10, 10), new Point(11, 11));
        sourceCubics.Add(CubicBezier.StraightLine(new Point(20, 20), new Point(21, 21)));

        AssertCubicsEqualish([cubic], actual.Cubics);
    }
}
