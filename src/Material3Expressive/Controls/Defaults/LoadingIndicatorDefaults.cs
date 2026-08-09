using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record LoadingIndicatorMetrics(
    double ActiveSize,
    double ContainerSize,
    ShapeRole ContainerShape,
    bool HasContainer);

internal sealed record LoadingIndicatorVisualColors(
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ContainerColor);

/// <summary>Component-owned policy for loading-indicator geometry and colors.</summary>
internal static class LoadingIndicatorDefaults
{
    public static LoadingIndicatorMetrics Metrics(LoadingIndicatorVariant variant) => new(
        LoadingIndicatorTokens.ActiveSize,
        LoadingIndicatorTokens.ContainerWidth,
        LoadingIndicatorTokens.ContainerShape,
        variant == LoadingIndicatorVariant.Contained);

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
