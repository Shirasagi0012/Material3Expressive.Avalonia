using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class PolygonMeasureTest
{
    private readonly LengthMeasurer _measurer = new();

    [Fact]
    public void MeasureSharpTriangle()
    {
        RegularPolygonMeasure(3);
    }

    [Fact]
    public void MeasureSharpPentagon()
    {
        RegularPolygonMeasure(5);
    }

    [Fact]
    public void MeasureSharpOctagon()
    {
        RegularPolygonMeasure(8);
    }

    [Fact]
    public void MeasureSharpDodecagon()
    {
        RegularPolygonMeasure(12);
    }

    [Fact]
    public void MeasureSharpIcosagon()
    {
        RegularPolygonMeasure(20);
    }

    [Fact]
    public void MeasureSlightlyRoundedHexagon()
    {
        IrregularPolygonMeasure(new RoundedPolygon(6, rounding: new CornerRounding(0.15)));
    }

    [Fact]
    public void MeasureMediumRoundedHexagon()
    {
        IrregularPolygonMeasure(new RoundedPolygon(6, rounding: new CornerRounding(0.5)));
    }

    [Fact]
    public void MeasureMaximumRoundedHexagon()
    {
        IrregularPolygonMeasure(new RoundedPolygon(6, rounding: new CornerRounding(1)));
    }

    [Fact]
    public void MeasureCircle()
    {
        var vertices = 4;
        var polygon = RoundedPolygon.CreateCircle(vertices);

        var actualLength = polygon.Cubics.Sum(c => new LengthMeasurer().MeasureCubic(c));
        var expectedLength = 2 * Math.PI;

        Assert.Equal(expectedLength, actualLength, 0.015 * expectedLength);
    }

    [Fact]
    public void IrregularTriangleAngleMeasure()
    {
        IrregularPolygonMeasure(
            new RoundedPolygon(
                [
                    new Point(0, -1),
                    new Point(1, 1),
                    new Point(0, 0.5),
                    new Point(-1, 1)
                ],
                perVertexRounding:
                [
                    new CornerRounding(0.2, 0.5),
                    new CornerRounding(0.2, 0.5),
                    new CornerRounding(0.4, 0),
                    new CornerRounding(0.2, 0.5)
                ]));
    }

    [Fact]
    public void QuarterAngleMeasure()
    {
        IrregularPolygonMeasure(
            new RoundedPolygon(
                [
                    new Point(-1, -1),
                    new Point(1, -1),
                    new Point(1, 1),
                    new Point(-1, 1)
                ],
                perVertexRounding:
                [
                    CornerRounding.Unrounded,
                    CornerRounding.Unrounded,
                    new CornerRounding(0.5, 0.5),
                    CornerRounding.Unrounded
                ]));
    }

    [Fact]
    public void HourGlassMeasure()
    {
        var unit = 1d;
        var coordinates = new[]
        {
            new Point(0, 0),
            new Point(unit, unit),
            new Point(-unit, unit),
            new Point(0, 0),
            new Point(-unit, -unit),
            new Point(unit, -unit)
        };

        var diagonal = Math.Sqrt(unit * unit + unit * unit);
        var horizontal = 2 * unit;
        var total = 4 * diagonal + 2 * horizontal;

        var polygon = new RoundedPolygon(coordinates);
        CustomPolygonMeasure(
            polygon,
            [
                diagonal / total,
                horizontal / total,
                diagonal / total,
                diagonal / total,
                horizontal / total,
                diagonal / total
            ]);
    }

    [Fact]
    public void HandlesEmptyFeatureLast()
    {
        var triangle = new RoundedPolygon(
            new List<Feature>
            {
                Feature.BuildConvexCorner(
                    [CubicBezier.StraightLine(new Point(0, 0), new Point(1, 1))]),
                Feature.BuildConvexCorner(
                    [CubicBezier.StraightLine(new Point(1, 1), new Point(1, 0))]),
                Feature.BuildConvexCorner(
                    [CubicBezier.StraightLine(new Point(1, 0), new Point(0, 0))]),
                Feature.BuildConvexCorner(
                    [CubicBezier.StraightLine(new Point(0, 0), new Point(0, 0))])
            });

        IrregularPolygonMeasure(triangle);
    }

    private void RegularPolygonMeasure(int sides, CornerRounding? rounding = null)
    {
        IrregularPolygonMeasure(
            new RoundedPolygon(sides, rounding: rounding ?? CornerRounding.Unrounded),
            measuredPolygon =>
            {
                Assert.Equal(sides, measuredPolygon.Count);
                for (var index = 0; index < measuredPolygon.Count; index++)
                    AssertEqualish((double)index / sides, measuredPolygon[index].StartOutlineProgress);
            });
    }

    private void CustomPolygonMeasure(RoundedPolygon polygon, double[] progresses)
    {
        IrregularPolygonMeasure(
            polygon,
            measuredPolygon =>
            {
                if (measuredPolygon.Count != progresses.Length)
                    throw new InvalidOperationException("Measured polygon size mismatch.");

                for (var index = 0; index < measuredPolygon.Count; index++)
                    AssertEqualish(
                        progresses[index],
                        measuredPolygon[index].EndOutlineProgress - measuredPolygon[index].StartOutlineProgress);
            });
    }

    private void IrregularPolygonMeasure(RoundedPolygon polygon, Action<MeasuredPolygon>? extraChecks = null)
    {
        var measuredPolygon = MeasuredPolygon.MeasurePolygon(_measurer, polygon);

        AssertEqualish(0d, measuredPolygon[0].StartOutlineProgress);
        AssertEqualish(1d, measuredPolygon[measuredPolygon.Count - 1].EndOutlineProgress);

        for (var index = 0; index < measuredPolygon.Count; index++)
        {
            var measuredCubic = measuredPolygon[index];
            if (index > 0)
                AssertEqualish(measuredPolygon[index - 1].EndOutlineProgress, measuredCubic.StartOutlineProgress);

            Assert.True(measuredCubic.EndOutlineProgress >= measuredCubic.StartOutlineProgress);
        }

        for (var index = 0; index < measuredPolygon.Features.Count; index++)
        {
            var progressableFeature = measuredPolygon.Features[index];
            Assert.True(
                progressableFeature.Progress >= 0d && progressableFeature.Progress < 1d,
                $"Feature #{index} has invalid progress: {progressableFeature.Progress}");
        }

        extraChecks?.Invoke(measuredPolygon);
    }
}
