using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Geometry shared by the two halves of a split button.</summary>
[Obsolete(
    "Legacy split-button defaults transport. Use typed MaterialValue endpoints.",
    DiagnosticId = "M3E1005")]
internal sealed record SplitButtonMetrics(
    double BetweenSpace,
    double ContainerHeight,
    ShapeRole ContainerShape,
    double InnerCornerSize,
    double InnerHoveredCornerSize,
    double InnerPressedCornerSize,
    double LeadingButtonLeadingSpace,
    double LeadingButtonTrailingSpace,
    double TrailingButtonLeadingSpace,
    double TrailingButtonTrailingSpace,
    double TrailingIconSize,
    double TrailingInnerSelectedCornerPercent);

/// <summary>Component-owned policy for split-button geometry.</summary>
[Obsolete(
    "Legacy split-button defaults adapter. Use typed MaterialValue endpoints.",
    DiagnosticId = "M3E1005")]
internal static class SplitButtonDefaults
{
    public static SplitButtonMetrics Metrics() => new(
        SplitButtonSmallTokens.BetweenSpace,
        SplitButtonSmallTokens.ContainerHeight,
        SplitButtonSmallTokens.ContainerShape,
        SplitButtonSmallTokens.InnerCornerSize,
        SplitButtonSmallTokens.InnerHoveredCornerSize,
        SplitButtonSmallTokens.InnerPressedCornerSize,
        SplitButtonSmallTokens.LeadingButtonLeadingSpace,
        SplitButtonSmallTokens.LeadingButtonTrailingSpace,
        SplitButtonSmallTokens.TrailingButtonLeadingSpace,
        SplitButtonSmallTokens.TrailingButtonTrailingSpace,
        SplitButtonSmallTokens.TrailingIconSize,
        SplitButtonSmallTokens.TrailingInnerSelectedCornerCornerSizePercent);
}
