namespace Material3Expressive.VisualTesting.Compose;

public sealed record ComposeParityMetrics(
    bool DimensionsMatch,
    int ReferenceWidth,
    int ReferenceHeight,
    int AvaloniaWidth,
    int AvaloniaHeight,
    int AlignmentX,
    int AlignmentY,
    double RawRootMeanSquareError,
    double AlignedRootMeanSquareError,
    double MeanAbsoluteChannelError,
    double MeanDeltaE76,
    double Percentile95DeltaE76,
    double MaximumDeltaE76,
    double BackgroundDeltaE76,
    double ForegroundDominantDeltaE76,
    double EdgePrecision,
    double EdgeRecall,
    double EdgeF1,
    PixelBounds? ReferenceForegroundBounds,
    PixelBounds? AvaloniaForegroundBounds,
    IReadOnlyList<ComposeParityRegionMetrics> Regions,
    string Summary);

public sealed record ComposeParityRegionMetrics(
    string Id,
    ComposeParityRegionKind Kind,
    PixelBounds Bounds,
    double MeanDeltaE76,
    double Percentile95DeltaE76,
    double AverageColorDeltaE76,
    double EdgeF1,
    PixelBounds? ReferenceInkBounds,
    PixelBounds? AvaloniaInkBounds,
    int? InkBottomDelta);

public readonly record struct PixelBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public override string ToString() => $"{X},{Y} {Width}x{Height}";
}
