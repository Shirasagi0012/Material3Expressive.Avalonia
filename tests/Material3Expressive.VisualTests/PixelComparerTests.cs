using Material3Expressive.VisualTesting.Comparison;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class PixelComparerTests
{
    private readonly PixelComparer _comparer = new();

    [Fact]
    public void ExactComparisonAcceptsIdenticalPixels()
    {
        var image = Solid(4, 4, 12, 34, 56, 255);
        var result = _comparer.Compare(image, image, ComparisonProfile.Exact);

        Assert.True(result.Passed);
        Assert.Equal(0, result.ChangedPixels);
        Assert.Equal(0, result.RootMeanSquareError);
    }

    [Fact]
    public void ExactComparisonRejectsASingleChangedPixel()
    {
        var expected = Solid(4, 4, 12, 34, 56, 255);
        var actual = expected with { Pixels = (byte[])expected.Pixels.Clone() };
        actual.Pixels[0] = 13;

        var result = _comparer.Compare(expected, actual, ComparisonProfile.Exact);

        Assert.False(result.Passed);
        Assert.Equal(1, result.ChangedPixels);
        Assert.Equal(1, result.LargestChangedComponent);
    }

    [Fact]
    public void AOnePixelTranslationProducesAConnectedDifference()
    {
        var expected = Solid(5, 3, 0, 0, 0, 255);
        var actual = expected with { Pixels = (byte[])expected.Pixels.Clone() };
        Paint(expected.Pixels, 5, 1, 1, 255);
        Paint(actual.Pixels, 5, 2, 1, 255);

        var result = _comparer.Compare(expected, actual, ComparisonProfile.Exact);

        Assert.False(result.Passed);
        Assert.Equal(2, result.ChangedPixels);
        Assert.Equal(2, result.LargestChangedComponent);
    }

    [Fact]
    public void DifferentDimensionsAlwaysFail()
    {
        var result = _comparer.Compare(
            Solid(4, 4, 0, 0, 0, 255),
            Solid(5, 4, 0, 0, 0, 255),
            ComparisonProfile.Shadow);

        Assert.False(result.Passed);
        Assert.False(result.DimensionsMatch);
    }

    private static RenderedImage Solid(
        int width,
        int height,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = red;
            pixels[index + 1] = green;
            pixels[index + 2] = blue;
            pixels[index + 3] = alpha;
        }

        return new RenderedImage(width, height, pixels);
    }

    private static void Paint(byte[] pixels, int width, int x, int y, byte value)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = value;
        pixels[offset + 1] = value;
        pixels[offset + 2] = value;
    }
}
