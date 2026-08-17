using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

[Obsolete(
    "Legacy loading-indicator metrics snapshot. Use LoadingIndicatorValueKeys endpoints instead.",
    DiagnosticId = "M3E1005")]
internal sealed record LoadingIndicatorMetrics(
    double ActiveSize,
    double ContainerSize,
    ShapeRole ContainerShape,
    bool HasContainer);

/// <summary>Component-owned policy for loading-indicator geometry and colors.</summary>
[Obsolete(
    "Legacy loading-indicator defaults transport. Use LoadingIndicatorValueKeys endpoints instead.",
    DiagnosticId = "M3E1005")]
internal static class LoadingIndicatorDefaults
{
    [Obsolete(
        "Legacy loading-indicator metrics snapshot. Use LoadingIndicatorValueKeys endpoints instead.",
        DiagnosticId = "M3E1005")]
    public static LoadingIndicatorMetrics Metrics(LoadingIndicatorVariant variant) => new(
        LoadingIndicatorTokens.ActiveSize,
        LoadingIndicatorTokens.ContainerWidth,
        LoadingIndicatorTokens.ContainerShape,
        variant == LoadingIndicatorVariant.Contained);

    [Obsolete(
        "Legacy loading-indicator colour projection. Use LoadingIndicatorValueKeys endpoints instead.",
        DiagnosticId = "M3E1005")]
    public static LoadingIndicatorVisualColors Colors(ResolvedTheme theme, LoadingIndicatorVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var active = (variant == LoadingIndicatorVariant.Contained
                ? LoadingIndicatorTokens.ContainedActiveColor
                : LoadingIndicatorTokens.ActiveIndicatorColor)
            .ResolveBrush(theme.ColorScheme, isDark);
        var container = (variant == LoadingIndicatorVariant.Contained
                ? LoadingIndicatorTokens.ContainedContainerColor
                : ColorTokenValue.Literal(Avalonia.Media.Colors.Transparent))
            .ResolveBrush(theme.ColorScheme, isDark);
        return new(active, container);
    }
}
