using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Comparison;

/// <summary>Metrics and the changed-pixel mask produced by one image comparison.</summary>
public sealed record PixelComparisonResult(
    bool DimensionsMatch,
    int ExpectedWidth,
    int ExpectedHeight,
    int ActualWidth,
    int ActualHeight,
    long ChangedPixels,
    double ChangedPixelRatio,
    byte MaximumChannelDelta,
    double RootMeanSquareError,
    int LargestChangedComponent,
    bool Passed,
    string Summary,
    bool[] ChangedMask)
{
    public static PixelComparisonResult DimensionMismatch(
        RenderedImage expected,
        RenderedImage actual)
    {
        return new PixelComparisonResult(
            false,
            expected.Width,
            expected.Height,
            actual.Width,
            actual.Height,
            0,
            1,
            byte.MaxValue,
            double.PositiveInfinity,
            0,
            false,
            $"Dimensions differ: expected {expected.Width}x{expected.Height}, " +
            $"actual {actual.Width}x{actual.Height}.",
            []);
    }
}
