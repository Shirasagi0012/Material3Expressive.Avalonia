using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record NavigationMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ActiveIndicatorColor,
    ColorTokenValue ActiveIconColor,
    ColorTokenValue ActiveLabelTextColor,
    ColorTokenValue InactiveIconColor,
    ColorTokenValue InactiveLabelTextColor,
    ColorTokenValue StateLayerColor,
    double ContainerElevation,
    double ContainerSize,
    double ActiveIndicatorWidth,
    double ActiveIndicatorHeight,
    ShapeRole ActiveIndicatorShape,
    double IconSize,
    double IconLabelSpace,
    double StartActiveIndicatorHeight,
    double StartIndicatorHorizontalPadding,
    double StartIndicatorVerticalPadding,
    double TopItemVerticalPadding,
    double ItemBetweenSpace,
    TypographyRole LabelTextStyle,
    bool IsVertical);

internal sealed record NavigationVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush StateLayerColor);

/// <summary>Component-owned policy shared by navigation bars, rails and their items.</summary>
internal static class NavigationDefaults
{
    public static NavigationMetrics Metrics(NavigationVariant variant) => variant switch
    {
        NavigationVariant.Bar => new(
            NavigationBarTokens.ContainerColor,
            NavigationBarTokens.ItemActiveIndicatorColor,
            NavigationBarTokens.ItemActiveIconColor,
            NavigationBarTokens.ItemActiveLabelTextColor,
            NavigationBarTokens.ItemInactiveIconColor,
            NavigationBarTokens.ItemInactiveLabelTextColor,
            MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurface,
            NavigationBarTokens.ContainerElevation,
            NavigationBarTokens.TallContainerHeight,
            NavigationBarVerticalItemTokens.ActiveIndicatorWidth,
            NavigationBarVerticalItemTokens.ActiveIndicatorHeight,
            NavigationBarTokens.ItemActiveIndicatorShape,
            NavigationBarVerticalItemTokens.IconSize,
            8,
            NavigationBarHorizontalItemTokens.ActiveIndicatorHeight,
            NavigationBarHorizontalItemTokens.ActiveIndicatorLeadingSpace,
            8,
            6,
            8,
            NavigationBarTokens.LabelTextFont,
            false),
        NavigationVariant.Rail => new(
            NavigationRailCollapsedTokens.ContainerColor,
            NavigationRailColorTokens.ItemActiveIndicator,
            NavigationRailColorTokens.ItemActiveIcon,
            NavigationRailColorTokens.ItemActiveLabelText,
            NavigationRailColorTokens.ItemInactiveIcon,
            NavigationRailColorTokens.ItemInactiveLabelText,
            MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurface,
            NavigationRailCollapsedTokens.ContainerElevation,
            NavigationRailCollapsedTokens.ContainerWidth,
            NavigationRailVerticalItemTokens.ActiveIndicatorWidth,
            NavigationRailVerticalItemTokens.ActiveIndicatorHeight,
            NavigationRailBaselineItemTokens.ActiveIndicatorShape,
            NavigationRailBaselineItemTokens.IconSize,
            NavigationRailVerticalItemTokens.IconLabelSpace,
            NavigationBarHorizontalItemTokens.ActiveIndicatorHeight,
            NavigationRailVerticalItemTokens.LeadingSpace,
            8,
            6,
            NavigationRailCollapsedTokens.ItemVerticalSpace,
            NavigationRailVerticalItemTokens.LabelTextFont,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static NavigationVisualColors Colors(ResolvedTheme theme, NavigationVariant variant, bool isDark)
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
            metrics.StateLayerColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
