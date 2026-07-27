namespace Material3Expressive.VisualTesting.Model;

/// <summary>Pixel-difference limits for one family of rendered output.</summary>
public sealed record ComparisonProfile(
    string Name,
    byte IgnoredChannelDelta,
    double MaximumChangedPixelRatio,
    double MaximumRootMeanSquareError,
    int MaximumChangedComponentSize)
{
    public static ComparisonProfile Exact { get; } =
        new("Exact", 0, 0, 0, 0);

    // These are deliberately strict provisional values. Stage 0's repeatability probe decides
    // whether any relaxation is warranted on the canonical renderer.
    public static ComparisonProfile AntiAlias { get; } =
        new("AntiAlias", 1, 0.0002, 0.05, 16);

    public static ComparisonProfile Shadow { get; } =
        new("Shadow", 2, 0.0005, 0.1, 32);
}
