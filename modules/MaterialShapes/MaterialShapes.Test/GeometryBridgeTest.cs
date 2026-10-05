using Avalonia;

namespace MaterialShapes.Test;

/// <summary>
/// Covers the Polygon/Morph → geometry bridge ported from androidx
/// <c>compose/material3/internal/ShapeUtil.kt</c>.
/// </summary>
public class GeometryBridgeTest
{
    [Fact]
    public void OvalMatchesTheUpstreamCatalogGeometry()
    {
        var oval = MaterialShapeCatalog.Oval;
        var maxBounds = oval.CalculateMaxBounds();

        // org.jetbrains.compose.material3:material3-desktop:1.12.0-alpha03 uses the default
        // eight-vertex graphics-shapes circle for MaterialShapes.Oval.
        Assert.Equal(9, oval.Cubics.Count);
        Assert.Equal(1.1552892, maxBounds.Width, 6);
        Assert.Equal(1.1552892, maxBounds.Height, 6);
    }

    [Fact]
    public void WithoutAStartAngleTheCubicsAreEmittedUntouched()
    {
        var polygon = MaterialShapeCatalog.Cookie9Sided;
        var (figure, repeatIndex) = AvaloniaExtensions.BuildFigure(polygon.Cubics, 0, false, polygon.Center);

        Assert.Equal(polygon.Cubics.Count, figure.Count);
        Assert.Equal(-1, repeatIndex);
        Assert.Equal(polygon.Cubics[0].Anchor0, figure[0].Anchor0);
        Assert.Equal(polygon.Cubics[^1].Anchor1, figure[^1].Anchor1);
    }

    [Fact]
    public void StartAngleIsMeasuredFromThePivotAndAppliedAboutTheOrigin()
    {
        var polygon = MaterialShapeCatalog.Cookie9Sided;
        var (figure, _) = AvaloniaExtensions.BuildFigure(
            polygon.Cubics,
            AvaloniaExtensions.StartAngleTop,
            false,
            polygon.Center);

        // The rotation is about the origin, so the centre moves with the shape; what the start
        // angle pins down is the direction from the rotated centre to the first anchor.
        var transform = AvaloniaExtensions.CreateStartAngleTransform(
            polygon.Cubics,
            AvaloniaExtensions.StartAngleTop,
            polygon.Center);
        var rotatedCentre = polygon.Center.Transform(transform);
        var offset = figure[0].Anchor0 - rotatedCentre;

        // 270° in screen coordinates is straight up, which atan2 reports as -90.
        Assert.Equal(-90, Math.Atan2(offset.Y, offset.X) * 180.0 / Math.PI, 6);
    }

    [Fact]
    public void AStartAngleOfZeroIsTheIdentity()
    {
        var polygon = MaterialShapeCatalog.Pentagon;

        Assert.Equal(
            Matrix.Identity,
            AvaloniaExtensions.CreateStartAngleTransform(polygon.Cubics, 0, polygon.Center));
    }

    [Fact]
    public void RotationIsRigid()
    {
        var polygon = MaterialShapeCatalog.Pentagon;
        var transform = AvaloniaExtensions.CreateStartAngleTransform(
            polygon.Cubics,
            AvaloniaExtensions.StartAngleTop,
            polygon.Center);
        var rotatedCentre = polygon.Center.Transform(transform);

        var (figure, _) = AvaloniaExtensions.BuildFigure(
            polygon.Cubics,
            AvaloniaExtensions.StartAngleTop,
            false,
            polygon.Center);

        for (var i = 0; i < polygon.Cubics.Count; i++)
            Assert.Equal(
                Distance(polygon.Cubics[i].Anchor0, polygon.Center),
                Distance(figure[i].Anchor0, rotatedCentre),
                6);
    }

    [Fact]
    public void RepeatPathEmitsEveryCubicTwice()
    {
        var polygon = MaterialShapeCatalog.Circle;
        var (figure, repeatIndex) = AvaloniaExtensions.BuildFigure(polygon.Cubics, 0, true, polygon.Center);

        Assert.Equal(polygon.Cubics.Count * 2, figure.Count);
        Assert.Equal(polygon.Cubics.Count, repeatIndex);
        Assert.Equal(figure[0].Anchor0, figure[repeatIndex].Anchor0);
        Assert.Equal(figure[0].Anchor1, figure[repeatIndex].Anchor1);
    }

    [Fact]
    public void AMorphMeasuresTheStartAngleFromTheGivenPivot()
    {
        var morph = new Morph(MaterialShapeCatalog.Circle, MaterialShapeCatalog.Square);
        var cubics = morph.EnumerateCubics(0.5).ToList();

        // With the pivot at the origin — the upstream default for morphs — the first anchor lands
        // exactly on the requested angle.
        var (figure, _) = AvaloniaExtensions.BuildFigure(cubics, AvaloniaExtensions.StartAngleTop, false, default);

        Assert.Equal(-90, Math.Atan2(figure[0].Anchor0.Y, figure[0].Anchor0.X) * 180.0 / Math.PI, 6);
    }

    [Fact]
    public void AnEmptyCubicListProducesAnEmptyFigure()
    {
        var (figure, repeatIndex) = AvaloniaExtensions.BuildFigure([], AvaloniaExtensions.StartAngleTop, true, default);

        Assert.Empty(figure);
        Assert.Equal(-1, repeatIndex);
    }

    // The emission wrapper itself is not covered here: StreamGeometry needs a render platform,
    // which this project deliberately does not pull in, and a StreamGeometry cannot be read back
    // anyway. All of the geometry that could be wrong lives in BuildFigure, which is covered above.

    private static double Distance(Point a, Point b)
    {
        var d = a - b;
        return Math.Sqrt(d.X * d.X + d.Y * d.Y);
    }
}
