using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record BadgeMetrics(
    double Size,
    ShapeRole ContainerShape,
    double HorizontalSpace,
    TypographyRole LabelTextStyle,
    bool HasLabel);

internal sealed record BadgeVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush LabelTextColor);

/// <summary>Component-owned policy for badge geometry and colors.</summary>
internal static class BadgeDefaults
{
    public static BadgeMetrics Metrics(BadgeVariant variant) => variant switch
    {
        BadgeVariant.Small => new(
            BadgeTokens.Size,
            BadgeTokens.Shape,
            0,
            BadgeTokens.LargeLabelTextFont,
            false),
        BadgeVariant.Large => new(
            BadgeTokens.LargeSize,
            BadgeTokens.LargeShape,
            4,
            BadgeTokens.LargeLabelTextFont,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static BadgeVisualColors Colors(ResolvedTheme theme, BadgeVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var container = (variant == BadgeVariant.Large ? BadgeTokens.LargeColor : BadgeTokens.Color)
            .ResolveBrush(theme.ColorScheme, isDark);
        return new(container, BadgeTokens.LargeLabelTextColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
