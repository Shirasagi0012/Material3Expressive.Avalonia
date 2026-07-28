using Avalonia.Media;
using Material3Expressive.Foundation;
using Xunit;

namespace Material3Expressive.Tests;

public class ElevationTests
{
    [Fact]
    public void LevelZero_HasNoShadow()
    {
        var shadows = Elevation.ToBoxShadows(0, Colors.Black);

        Assert.Equal(0, shadows.Count);
    }

    [Fact]
    public void CanonicalElevation_HasAmbientAndSpotLayers()
    {
        var shadows = Elevation.ToBoxShadows(6, Color.FromRgb(18, 52, 86));

        Assert.Equal(2, shadows.Count);

        var ambient = shadows[0];
        Assert.Equal(0.5, ambient.OffsetY);
        Assert.Equal(20, ambient.Blur);
        Assert.Equal(Color.FromArgb(31, 18, 52, 86), ambient.Color);

        var spot = shadows[1];
        Assert.Equal(0.5, spot.OffsetY);
        Assert.Equal(4, spot.Blur);
        Assert.Equal(Color.FromArgb(26, 18, 52, 86), spot.Color);
    }

    [Fact]
    public void IntermediateElevation_InterpolatesTheTwoLayers()
    {
        var shadows = Elevation.ToBoxShadows(2, Colors.Black);

        Assert.Equal(2, shadows.Count);
        Assert.Equal(5.65, shadows[0].Blur);
        Assert.Equal(Color.FromArgb(27, 0, 0, 0), shadows[0].Color);
        Assert.Equal(0.7, shadows[1].Blur);
        Assert.Equal(Color.FromArgb(25, 0, 0, 0), shadows[1].Color);
    }

    [Fact]
    public void InputShadowAlpha_ModulatesTheLayerOpacity()
    {
        var shadows = Elevation.ToBoxShadows(6, Color.FromArgb(128, 18, 52, 86));

        Assert.Equal(Color.FromArgb(15, 18, 52, 86), shadows[0].Color);
        Assert.Equal(Color.FromArgb(13, 18, 52, 86), shadows[1].Color);
    }
}
