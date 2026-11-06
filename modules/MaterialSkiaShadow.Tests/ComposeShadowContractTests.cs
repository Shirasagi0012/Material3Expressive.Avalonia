using Avalonia;
using Avalonia.Media;
using MaterialSkiaShadow;
using SkiaSharp;
using Xunit;

namespace MaterialSkiaShadow.Tests;

public sealed class ComposeShadowContractTests
{
    [Fact]
    public void PathSnapshotRetainsConicsAndSurvivesNativePathDisposal()
    {
        ComposeShadowShape shape;
        using (var builder = new SKPathBuilder())
        {
            builder.MoveTo(0, 0); builder.ConicTo(10, 0, 10, 10, .70710677f);
            builder.LineTo(0, 10); builder.Close();
            using var path = builder.Detach();
            shape = ComposeShadowShape.FromPath(path);
        }
        Assert.Equal(new Rect(0, 0, 10, 10), shape.Bounds);
        Assert.Equal(SKPathVerb.Conic, shape.PathData![1].Verb);
        Assert.Equal(.70710677f, shape.PathData[1].Weight);
        Assert.Equal(new SKPoint(10, 0), shape.PathData[1].First);
        Assert.Equal(new SKPoint(10, 10), shape.PathData[1].Second);
    }

    [Fact]
    public void MultipleContoursAreRetainedForWholePathBlurFallback()
    {
        using var builder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
        builder.AddRect(new SKRect(0, 0, 20, 20));
        builder.AddRect(new SKRect(30, 0, 40, 10));
        using var path = builder.Detach();
        var shape = ComposeShadowShape.FromPath(path);
        Assert.Equal(2, shape.PathData!.Count(segment => segment.Verb == SKPathVerb.Move));
        Assert.Equal(SKPathFillType.EvenOdd, shape.PathFillType);
        Assert.Equal(new Rect(0, 0, 40, 20), shape.Bounds);
    }

    [Fact]
    public void OpenAndUnsupportedContoursFailAtSnapshotTime()
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(0, 0); builder.LineTo(10, 10);
        using var open = builder.Snapshot();
        Assert.Throws<ArgumentException>(() => ComposeShadowShape.FromPath(open));
        builder.CubicTo(10, 20, 0, 20, 0, 0); builder.Close();
        using var cubic = builder.Detach();
        Assert.Throws<NotSupportedException>(() => ComposeShadowShape.FromPath(cubic));

        using var mixedBuilder = new SKPathBuilder();
        mixedBuilder.MoveTo(0, 0); mixedBuilder.LineTo(10, 10);
        mixedBuilder.AddRect(new SKRect(20, 0, 30, 10));
        using var mixed = mixedBuilder.Detach();
        Assert.Throws<ArgumentException>(() => ComposeShadowShape.FromPath(mixed));
    }

    private static readonly ComposeShadowShape Shape =
        ComposeShadowShape.RoundedRectangle(
            new Rect(new Size(96, 64)),
            new CornerRadius(20));

    private static readonly ComposeShadowLighting Lighting =
        ComposeShadowLighting.ForViewport(new Size(240, 180), 2);

    [Fact]
    public void InheritedLayerOpacityDoesNotMakeAnOpaqueOccluderTransparent()
    {
        var resolved = ComposeShadowContract.Resolve(
            Shape,
            ComposeShadowParameters.Material(6, Colors.Black),
            Lighting,
            renderScaling: 2,
            inheritedOpacity: .5);

        Assert.Equal(.5f, resolved.Opacity);
        Assert.False(resolved.TransparentOccluder);
    }

    [Fact]
    public void ShadowOwnerOpacityMakesItsOccluderTransparent()
    {
        var resolved = ComposeShadowContract.Resolve(
            Shape,
            new ComposeShadowParameters(
                6,
                Colors.Black,
                Colors.Black,
                Opacity: .5),
            Lighting,
            renderScaling: 2);

        Assert.Equal(.5f, resolved.Opacity);
        Assert.True(resolved.TransparentOccluder);
    }

    [Fact]
    public void DrawOperationBoundsUseLocalShadowSupportInsteadOfTheViewport()
    {
        var shape = ComposeShadowShape.RoundedRectangle(
            new Rect(0, 0, 320, 48),
            new CornerRadius(4));

        var bounds = ComposeShadowRenderer.GetOperationBounds(
            shape,
            ComposeShadowParameters.Material(6, Colors.Black));

        Assert.Equal(new Rect(-24, -24, 368, 96), bounds);
    }
}
