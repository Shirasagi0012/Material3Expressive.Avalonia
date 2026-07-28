using MaterialSkiaShadow.Internal;
using SkiaSharp;
using Xunit;

namespace MaterialSkiaShadow.Tests;

public sealed class ConvexShadowTessellatorTests
{
    [Fact]
    public void CircleAmbientUsesBlurFallback()
    {
        using var path = CreateCircle();

        var mesh = ConvexShadowTessellator.MakeAmbient(
            path,
            SKMatrix.CreateScale(2, 2),
            elevationPx: 12,
            transparent: false);

        Assert.Null(mesh);
    }

    [Fact]
    public void RoundedRectangleAmbientMeshIsFinite()
    {
        using var path = CreateRoundedRectangle();

        var mesh = ConvexShadowTessellator.MakeAmbient(
            path,
            SKMatrix.CreateScale(2, 2),
            elevationPx: 12,
            transparent: false);

        Assert.NotNull(mesh);
        Assert.NotEmpty(mesh.Positions);
        Assert.NotEmpty(mesh.Indices);
        Assert.All(mesh.Positions, point =>
        {
            Assert.True(float.IsFinite(point.X));
            Assert.True(float.IsFinite(point.Y));
        });
    }

    private static SKPath CreateRoundedRectangle()
    {
        using var rounded = new SKRoundRect();
        rounded.SetRectRadii(
            new SKRect(0, 0, 96, 64),
            [new(20, 20), new(20, 20), new(20, 20), new(20, 20)]);
        using var builder = new SKPathBuilder();
        builder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        return builder.Detach();
    }

    private static SKPath CreateCircle()
    {
        using var rounded = new SKRoundRect();
        rounded.SetRectRadii(
            new SKRect(0, 0, 72, 72),
            [new(36, 36), new(36, 36), new(36, 36), new(36, 36)]);
        using var builder = new SKPathBuilder();
        builder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        return builder.Detach();
    }
}
