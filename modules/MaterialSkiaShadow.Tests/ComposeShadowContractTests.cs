using Avalonia;
using Avalonia.Media;
using MaterialSkiaShadow;
using Xunit;

namespace MaterialSkiaShadow.Tests;

public sealed class ComposeShadowContractTests
{
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
