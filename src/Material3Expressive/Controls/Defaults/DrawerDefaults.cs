using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

internal sealed record DrawerMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ScrimColor,
    ColorTokenValue HeadlineColor,
    ColorTokenValue ActiveIndicatorColor,
    ColorTokenValue ActiveIconColor,
    ColorTokenValue ActiveLabelTextColor,
    ColorTokenValue InactiveIconColor,
    ColorTokenValue InactiveLabelTextColor,
    ColorTokenValue StateLayerColor,
    double ContainerElevation,
    double ContainerWidth,
    ShapeRole ContainerShape,
    double ItemHeight,
    ShapeRole ItemShape,
    double IconSize,
    double ScrimOpacity,
    TypographyRole HeadlineTextStyle,
    TypographyRole LabelTextStyle,
    bool IsModal);

internal sealed record DrawerVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrimColor,
    IImmutableBrush HeadlineColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush StateLayerColor);

/// <summary>Component-owned policy for modal and standard navigation drawers.</summary>
internal static class DrawerDefaults
{
    public static DrawerMetrics Metrics(DrawerVariant variant) => variant switch
    {
        DrawerVariant.Modal => new(
            NavigationDrawerTokens.ModalContainerColor,
            SysColorToken.Scrim,
            NavigationDrawerTokens.HeadlineColor,
            NavigationDrawerTokens.ActiveIndicatorColor,
            NavigationDrawerTokens.ActiveIconColor,
            NavigationDrawerTokens.ActiveLabelTextColor,
            NavigationDrawerTokens.InactiveIconColor,
            NavigationDrawerTokens.InactiveLabelTextColor,
            SysColorToken.OnSurface,
            NavigationDrawerTokens.ModalContainerElevation,
            NavigationDrawerTokens.ContainerWidth,
            NavigationDrawerTokens.ContainerShape,
            NavigationDrawerTokens.ActiveIndicatorHeight,
            NavigationDrawerTokens.ActiveIndicatorShape,
            NavigationDrawerTokens.IconSize,
            ScrimTokens.ContainerOpacity,
            NavigationDrawerTokens.HeadlineFont,
            NavigationDrawerTokens.LabelTextFont,
            true),
        DrawerVariant.Standard => new(
            NavigationDrawerTokens.StandardContainerColor,
            SysColorToken.Scrim,
            NavigationDrawerTokens.HeadlineColor,
            NavigationDrawerTokens.ActiveIndicatorColor,
            NavigationDrawerTokens.ActiveIconColor,
            NavigationDrawerTokens.ActiveLabelTextColor,
            NavigationDrawerTokens.InactiveIconColor,
            NavigationDrawerTokens.InactiveLabelTextColor,
            SysColorToken.OnSurface,
            NavigationDrawerTokens.StandardContainerElevation,
            NavigationDrawerTokens.ContainerWidth,
            NavigationDrawerTokens.ContainerShape,
            NavigationDrawerTokens.ActiveIndicatorHeight,
            NavigationDrawerTokens.ActiveIndicatorShape,
            NavigationDrawerTokens.IconSize,
            0,
            NavigationDrawerTokens.HeadlineFont,
            NavigationDrawerTokens.LabelTextFont,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static DrawerVisualColors Colors(ResolvedTheme theme, DrawerVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ScrimColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.HeadlineColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveIndicatorColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveIconColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActiveLabelTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.InactiveIconColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.InactiveLabelTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.StateLayerColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
