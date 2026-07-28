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

    [Fact]
    public void OpaqueSpotUsesSkiaCanonicalTranslationRoute()
    {
        using var path = CreatePill();
        const float lightZ = 737.7778f;
        const float lightRadius = 1600f;
        const float elevation = 2f;
        var translated = new SKMatrix(2, 0, 150, 0, 2, 40, 0, 0, 1);
        var canonical = SKMatrix.CreateScale(2, 2);

        var actual = ConvexShadowTessellator.MakeSpot(
            path,
            translated,
            elevation,
            new SKPoint(240, 0),
            lightZ,
            lightRadius,
            transparent: false);
        var canonicalMesh = ConvexShadowTessellator.MakeSpot(
            path,
            canonical,
            elevation,
            new SKPoint(90.5f, 56),
            lightZ,
            lightRadius,
            transparent: false);

        Assert.NotNull(actual);
        Assert.NotNull(canonicalMesh);
        Assert.Equal(canonicalMesh.Positions.Count, actual.Positions.Count);

        var relative = ShadowMetrics.GetSpotParameters(
            elevation,
            lightX: -0.5f,
            lightY: -96f,
            lightZ,
            lightRadius);
        var offset = new SKPoint(relative.Translation.X + 150, relative.Translation.Y + 40);
        for (var index = 0; index < actual.Positions.Count; index++)
        {
            Assert.InRange(
                actual.Positions[index].X - canonicalMesh.Positions[index].X - offset.X,
                -1e-4f,
                1e-4f);
            Assert.InRange(
                actual.Positions[index].Y - canonicalMesh.Positions[index].Y - offset.Y,
                -1e-4f,
                1e-4f);
        }
    }

    [Fact]
    public void TransparentRoundedRectangleMeshesRemainFiniteAndLocal()
    {
        using var path = CreateRoundedRectangle();
        var matrix = new SKMatrix(2, 0, 144, 0, 2, 116, 0, 0, 1);

        var ambient = ConvexShadowTessellator.MakeAmbient(
            path,
            matrix,
            elevationPx: 12,
            transparent: true);
        var spot = ConvexShadowTessellator.MakeSpot(
            path,
            matrix,
            elevationPx: 12,
            new SKPoint(240, 0),
            lightZ: 800,
            lightRadius: 1600,
            transparent: true);

        AssertValidViewportMesh(ambient);
        AssertValidViewportMesh(spot);
    }

    private static void AssertValidViewportMesh(ShadowMesh? mesh)
    {
        Assert.NotNull(mesh);
        Assert.NotEmpty(mesh.Positions);
        Assert.NotEmpty(mesh.Indices);
        Assert.All(mesh.Positions, point =>
        {
            Assert.True(float.IsFinite(point.X));
            Assert.True(float.IsFinite(point.Y));
            Assert.InRange(point.X, -100f, 580f);
            Assert.InRange(point.Y, -100f, 460f);
        });
        Assert.All(mesh.Indices, index => Assert.InRange(index, (ushort)0, (ushort)(mesh.Positions.Count - 1)));
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

    private static SKPath CreatePill()
    {
        using var rounded = new SKRoundRect();
        rounded.SetRectRadii(
            new SKRect(0, 0, 90.5f, 56),
            [new(28, 28), new(28, 28), new(28, 28), new(28, 28)]);
        using var builder = new SKPathBuilder();
        builder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        return builder.Detach();
    }
}
