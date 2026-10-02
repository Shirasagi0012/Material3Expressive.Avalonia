namespace Material3Expressive.Parity;

public sealed record ComparisonIssue(string Code, string[] Members);

public sealed record FieldDifference(
    string Name,
    string Status,
    Measurement? Expected,
    Measurement? Actual,
    double[]? Delta = null,
    string? Reason = null,
    ComparisonIssue? Issue = null
);

public sealed record PointDifference(
    string Id,
    string? Role,
    string? ParentId,
    string Status,
    FieldDifference[] Fields,
    string? Reason = null,
    string? ExpectedRole = null,
    string? ActualRole = null,
    string? ExpectedParentId = null,
    string? ActualParentId = null,
    ComparisonIssue? Issue = null
);

public sealed record PixelMetrics(long DifferentPixels, int[] MaximumChannelDelta, double[] MeanChannelDelta, int[]? DifferenceBounds);

public sealed record RasterRegion(
    string PointId,
    int[] Bounds,
    PixelMetrics Metrics,
    string Basis = "union of expected/actual layout.box.aabb.unclipped; outward rounded and canvas clipped",
    string Space = "scene-px",
    string Contribution = "full"
);

public sealed record RasterDifference(
    string Id,
    string Status,
    RasterObservation? Expected,
    RasterObservation? Actual,
    PixelMetrics? Metrics = null,
    RasterRegion[]? Regions = null,
    string? DifferenceImage = null,
    string? Reason = null,
    ComparisonIssue? Issue = null
);

public sealed record SampleDifference(
    string Id,
    long? ExpectedTimeUs,
    long? ActualTimeUs,
    string? Stage,
    string Status,
    PointDifference[] Points,
    RasterDifference[] Rasters,
    string? Reason = null,
    string? ExpectedStage = null,
    string? ActualStage = null,
    long? ExpectedFrameTimeUs = null,
    long? ActualFrameTimeUs = null,
    ComparisonIssue? Issue = null
);

public sealed record Coverage(int Equal, int Different, int NotCompared);

public sealed record Comparison(
    int SchemaVersion,
    string CaseId,
    string Status,
    Coverage Coverage,
    SampleDifference[] Samples,
    string[] Errors,
    FieldDifference[] Environment,
    string Kind = "parity"
);

