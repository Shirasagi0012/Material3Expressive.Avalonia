using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ComposeParityComparerTests
{
    private readonly ComposeParityComparer _comparer = new();

    [Fact]
    public void IdenticalImagesProducePerfectDiagnosticMetrics()
    {
        var image = Rectangle(32, 20, 8, 5, 16, 10, 90, 60, 200);

        var result = _comparer.Compare(image, image);

        Assert.True(result.DimensionsMatch);
        Assert.Equal(0, result.AlignmentX);
        Assert.Equal(0, result.AlignmentY);
        Assert.Equal(0, result.RawRootMeanSquareError);
        Assert.Equal(0, result.MeanDeltaE76);
        Assert.Equal(1, result.EdgeF1);
    }

    [Fact]
    public void AlignmentReportsAWholeComponentTranslationWithoutHidingRawError()
    {
        var reference = Rectangle(40, 24, 8, 6, 18, 12, 90, 60, 200);
        var translated = Rectangle(40, 24, 10, 7, 18, 12, 90, 60, 200);

        var result = _comparer.Compare(reference, translated);

        Assert.Equal(2, result.AlignmentX);
        Assert.Equal(1, result.AlignmentY);
        Assert.True(result.RawRootMeanSquareError > result.AlignedRootMeanSquareError);
        Assert.Equal(0, result.AlignedRootMeanSquareError);
    }

    [Fact]
    public void ACenteredSizeDifferenceIsNotMisreportedAsTranslation()
    {
        var reference = Rectangle(40, 24, 8, 6, 18, 12, 90, 60, 200);
        var narrower = Rectangle(40, 24, 10, 7, 14, 10, 90, 60, 200);

        var result = _comparer.Compare(reference, narrower);

        Assert.Equal(0, result.AlignmentX);
        Assert.Equal(0, result.AlignmentY);
    }

    [Fact]
    public void AnAsymmetricShadowIsNotAcceptedAsAComponentTranslation()
    {
        var reference = Rectangle(40, 24, 10, 6, 14, 12, 90, 60, 200);
        var shadowedPixels = (byte[])reference.Pixels.Clone();
        for (var row = 8; row < 16; row++)
        {
            for (var column = 6; column < 10; column++)
            {
                var offset = (row * reference.Width + column) * 4;
                shadowedPixels[offset] = 210;
                shadowedPixels[offset + 1] = 210;
                shadowedPixels[offset + 2] = 220;
            }
        }
        var shadowed = new RenderedImage(reference.Width, reference.Height, shadowedPixels);

        var result = _comparer.Compare(shadowed, reference);

        Assert.Equal(0, result.AlignmentX);
        Assert.Equal(0, result.AlignmentY);
        Assert.Equal(result.RawRootMeanSquareError, result.AlignedRootMeanSquareError);
    }

    [Fact]
    public void PerceptualColorMetricsDetectAColorOnlyDifference()
    {
        var reference = Rectangle(32, 20, 8, 5, 16, 10, 90, 60, 200);
        var changed = Rectangle(32, 20, 8, 5, 16, 10, 110, 70, 190);

        var result = _comparer.Compare(reference, changed);

        Assert.Equal(0, result.AlignmentX);
        Assert.Equal(0, result.AlignmentY);
        Assert.True(result.MeanDeltaE76 > 0);
        Assert.True(result.ForegroundDominantDeltaE76 > 0);
        Assert.Equal(1, result.EdgeF1);
    }

    [Fact]
    public void DeclaredTextRegionReportsRawInkBounds()
    {
        var reference = Rectangle(32, 20, 10, 8, 8, 3, 10, 10, 10);
        var shifted = Rectangle(32, 20, 10, 9, 8, 3, 10, 10, 10);
        var region = new ComposeParityRegion(
            "label",
            ComposeParityRegionKind.Text,
            8,
            6,
            12,
            8);

        var result = _comparer.Compare(reference, shifted, [region]);

        Assert.Single(result.Regions);
        Assert.NotNull(result.Regions[0].ReferenceInkBounds);
        Assert.NotNull(result.Regions[0].AvaloniaInkBounds);
    }

    private static RenderedImage Rectangle(
        int width,
        int height,
        int x,
        int y,
        int rectangleWidth,
        int rectangleHeight,
        byte red,
        byte green,
        byte blue)
    {
        var pixels = new byte[width * height * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 250;
            pixels[offset + 1] = 250;
            pixels[offset + 2] = 250;
            pixels[offset + 3] = 255;
        }

        for (var row = y; row < y + rectangleHeight; row++)
        {
            for (var column = x; column < x + rectangleWidth; column++)
            {
                var offset = (row * width + column) * 4;
                pixels[offset] = red;
                pixels[offset + 1] = green;
                pixels[offset + 2] = blue;
            }
        }

        return new RenderedImage(width, height, pixels);
    }
}
