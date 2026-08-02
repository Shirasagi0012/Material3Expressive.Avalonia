using System;
using Avalonia;
using Avalonia.Data;

namespace Material3Expressive.Foundation;

internal static class IconButtonTokenResolver
{
    public static BindingBase Resolve(IconButtonToken token)
    {
        return token switch
        {
            IconButtonToken.ContainerColor => Color(static colors => colors.ContainerColor),
            IconButtonToken.IconColor => Color(static colors => colors.IconColor),
            IconButtonToken.OutlineColor => Color(static colors => colors.OutlineColor),
            IconButtonToken.StateLayerColor => Color(static colors => colors.StateLayerColor),
            IconButtonToken.DisabledContainerColor => Color(static colors => colors.DisabledContainerColor),
            IconButtonToken.DisabledIconColor => Color(static colors => colors.DisabledIconColor),
            IconButtonToken.DisabledOutlineColor => Color(static colors => colors.DisabledOutlineColor),
            IconButtonToken.UnselectedContainerColor => Color(static colors => colors.UnselectedContainerColor),
            IconButtonToken.SelectedContainerColor => Color(static colors => colors.SelectedContainerColor),
            IconButtonToken.UnselectedIconColor => Color(static colors => colors.UnselectedIconColor),
            IconButtonToken.SelectedIconColor => Color(static colors => colors.SelectedIconColor),
            IconButtonToken.UnselectedOutlineColor => Color(static colors => colors.UnselectedOutlineColor),
            IconButtonToken.ContentPadding => WidthMetric(
                static (metrics, width) => new Thickness(Space(metrics, width), 0)),
            IconButtonToken.ContainerHeight => Metric(static metrics => metrics.ContainerHeight),
            IconButtonToken.ContainerWidth => WidthMetric(
                static (metrics, width) => metrics.IconSize + Space(metrics, width) * 2),
            IconButtonToken.IconSize => Metric(static metrics => metrics.IconSize),
            IconButtonToken.ContainerOutlineThickness => OutlineThickness(),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    private static BindingBase Color(Func<IconButtonColors, object?> select) =>
        ThemeBinding.FromThemeVariantProperty<IconButtonVariant, object?>(
            IconButtonAssist.VariantProperty,
            (theme, isDark, variant) => select(theme.GetIconButtonColors(variant, isDark)),
            IconButtonVariant.Standard,
            AvaloniaProperty.UnsetValue);

    private static BindingBase Metric(Func<IconButtonSizeTokens, object?> select) =>
        ThemeBinding.FromThemeProperty<IconButtonSize, object?>(
            IconButtonAssist.SizeProperty,
            (theme, size) => select(theme.GetIconButtonSizeTokens(size)),
            IconButtonSize.Small,
            AvaloniaProperty.UnsetValue);

    private static BindingBase WidthMetric(Func<IconButtonSizeTokens, IconButtonWidth, object?> select) =>
        ThemeBinding.FromThemeProperties<IconButtonSize, IconButtonWidth, object?>(
            IconButtonAssist.SizeProperty,
            IconButtonAssist.ButtonWidthProperty,
            (theme, size, width) => select(theme.GetIconButtonSizeTokens(size), width),
            IconButtonSize.Small,
            IconButtonWidth.Default,
            AvaloniaProperty.UnsetValue);

    private static BindingBase OutlineThickness() =>
        ThemeBinding.FromThemeProperties<IconButtonVariant, IconButtonSize, object?>(
            IconButtonAssist.VariantProperty,
            IconButtonAssist.SizeProperty,
            (theme, variant, size) => new Thickness(
                theme.GetIconButtonColorTokens(variant).HasOutline
                    ? theme.GetIconButtonSizeTokens(size).OutlineWidth
                    : 0),
            IconButtonVariant.Standard,
            IconButtonSize.Small,
            AvaloniaProperty.UnsetValue);

    private static double Space(IconButtonSizeTokens metrics, IconButtonWidth width) => width switch
    {
        IconButtonWidth.Narrow => metrics.NarrowSpace,
        IconButtonWidth.Wide => metrics.WideSpace,
        _ => metrics.DefaultSpace
    };
}
