using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

internal sealed record TabMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ActiveIndicatorColor,
    ColorTokenValue ActiveIconColor,
    ColorTokenValue ActiveLabelTextColor,
    ColorTokenValue InactiveIconColor,
    ColorTokenValue InactiveLabelTextColor,
    ColorTokenValue DividerColor,
    ColorTokenValue StateLayerColor,
    double ContainerHeight,
    double IconAndLabelHeight,
    double ActiveIndicatorHeight,
    double ActiveIndicatorCornerRadius,
    double DividerHeight,
    double IconSize,
    TypographyRole LabelTextStyle,
    bool IndicatorSpansLabel);

internal sealed record TabVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush DividerColor,
    IImmutableBrush StateLayerColor);

/// <summary>Component-owned policy for primary and secondary tabs.</summary>
internal static class TabDefaults
{
    public static TabMetrics Metrics(TabVariant variant) => variant switch
    {
        TabVariant.Primary => new(
            SysColorToken.Surface,
            SysColorToken.Primary,
            SysColorToken.Primary,
            SysColorToken.Primary,
            SysColorToken.Primary,
            SysColorToken.Primary,
            SysColorToken.OutlineVariant,
            SysColorToken.Primary,
            48,
            64,
            3,
            3,
            1,
            24,
            TypographyRole.TitleSmall,
            true),
        TabVariant.Secondary => new(
            SecondaryNavigationTabTokens.ContainerColor,
            SysColorToken.Primary,
            SecondaryNavigationTabTokens.ActiveIconColor,
            SecondaryNavigationTabTokens.ActiveLabelTextColor,
            SecondaryNavigationTabTokens.InactiveIconColor,
            SecondaryNavigationTabTokens.InactiveLabelTextColor,
            SecondaryNavigationTabTokens.DividerColor,
            SysColorToken.Primary,
            SecondaryNavigationTabTokens.ContainerHeight,
            64,
            3,
            0,
            1,
            SecondaryNavigationTabTokens.IconSize,
            SecondaryNavigationTabTokens.LabelTextFont,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static TabVisualColors Colors(ResolvedTheme theme, TabVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveIndicatorColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveIconColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveLabelTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.InactiveIconColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.InactiveLabelTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.DividerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.StateLayerColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
