using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Compatibility-only geometry snapshot for unmigrated shared tests.</summary>
[Obsolete(
    "Legacy radio-button geometry snapshot. Use RadioButtonValueKeys.GeometryProfile instead.",
    DiagnosticId = "M3E1005")]
internal sealed record RadioButtonMetrics(
    double IconSize,
    double StateLayerSize,
    double RingWidth,
    double DotSize);

/// <summary>
/// Compatibility-only bridge retained until the old shared tests are migrated.
/// </summary>
[Obsolete(
    "Legacy radio-button defaults. Use RadioButtonValueKeys profiles instead.",
    DiagnosticId = "M3E1005")]
internal static class RadioButtonDefaults
{
    /// <summary>Gets the old geometry snapshot until the remaining tests migrate.</summary>
    [Obsolete(
        "Legacy radio-button geometry snapshot. Use RadioButtonValueKeys.GeometryProfile instead.",
        DiagnosticId = "M3E1005")]
    public static RadioButtonMetrics Metrics() => DefaultMetrics;

    private static readonly RadioButtonMetrics DefaultMetrics = new(
        RadioButtonTokens.IconSize,
        RadioButtonTokens.StateLayerSize,
        RingWidth: 2,
        DotSize: 10);
}
