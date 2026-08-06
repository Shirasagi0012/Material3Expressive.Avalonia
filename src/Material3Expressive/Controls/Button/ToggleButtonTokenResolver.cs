using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls.ButtonRuntime;

/// <summary>Bindings for the ToggleButton-owned token surface.</summary>
internal static class ToggleButtonTokenResolver
{
    public static BindingBase Resolve(ToggleButtonToken token)
    {
        return token switch
        {
            ToggleButtonToken.UncheckedContainerColor => Color(static (colors, _) => colors.UncheckedContainerColor),
            ToggleButtonToken.UncheckedContentColor => Color(static (colors, _) => colors.UncheckedContentColor),
            ToggleButtonToken.CheckedContainerColor => Color(static (colors, _) => colors.CheckedContainerColor),
            ToggleButtonToken.CheckedContentColor => Color(static (colors, _) => colors.CheckedContentColor),
            ToggleButtonToken.DisabledContainerColor => Color(static (colors, _) => colors.DisabledContainerColor),
            ToggleButtonToken.DisabledContentColor => Color(static (colors, _) => colors.DisabledContentColor),
            ToggleButtonToken.StateLayerColor => Color(static (colors, checkedState) => colors.StateLayerColor(checkedState)),
            ToggleButtonToken.OutlineColor => Color(static (colors, _) => colors.OutlineColor),
            ToggleButtonToken.DisabledOutlineColor => Color(static (colors, _) => colors.DisabledOutlineColor),
            ToggleButtonToken.ContainerOutlineThickness => MetricWithState(
                static (theme, variant, size, checkedState) =>
                    new Thickness(
                        ToggleButtonDefaults.ColorTokens(theme, variant).HasOutline && checkedState != true
                            ? theme.GetButtonSizeTokens(size).OutlineWidth
                            : 0)),
            ToggleButtonToken.ContentPadding => Metric(static (theme, size) =>
                new Thickness(
                    theme.GetButtonSizeTokens(size).LeadingSpace,
                    theme.GetButtonSizeTokens(size).VerticalSpace,
                    theme.GetButtonSizeTokens(size).TrailingSpace,
                    theme.GetButtonSizeTokens(size).VerticalSpace)),
            ToggleButtonToken.ContainerHeight => Metric(static (theme, size) =>
                theme.GetButtonSizeTokens(size).ContainerHeight),
            ToggleButtonToken.ToggleContainerHeight => Metric(static (theme, size) =>
                Math.Max(48, theme.GetButtonSizeTokens(size).ContainerHeight)),
            ToggleButtonToken.LabelTextStyle => Metric(static (theme, size) =>
                theme.GetButtonSizeTokens(size).LabelTextStyle),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    private static BindingBase Color(
        Func<ToggleButtonColors, bool, object?> select)
    {
        return ThemeBinding.FromThemeVariantProperties<ButtonVariant, bool?, object?>(
            ButtonAssist.VariantProperty,
            ToggleButton.IsCheckedProperty,
            (theme, isDark, variant, checkedValue) =>
                select(
                    ToggleButtonDefaults.Colors(theme, variant, isDark),
                    checkedValue == true),
            ButtonVariant.Filled,
            false,
            AvaloniaProperty.UnsetValue);
    }

    private static BindingBase Metric(
        Func<ResolvedTheme, ButtonSize, object?> select)
    {
        return ThemeBinding.FromThemeProperty<ButtonSize, object?>(
            ButtonAssist.SizeProperty,
            select,
            ButtonSize.Small,
            AvaloniaProperty.UnsetValue);
    }

    private static BindingBase MetricWithState(
        Func<ResolvedTheme, ButtonVariant, ButtonSize, bool?, object?> select)
    {
        return ThemeBinding.FromThemeProperties<ButtonVariant, ButtonSize, bool?, object?>(
            ButtonAssist.VariantProperty,
            ButtonAssist.SizeProperty,
            ToggleButton.IsCheckedProperty,
            (theme, variant, size, checkedValue) =>
                select(
                    theme,
                    variant,
                    size,
                    checkedValue),
            ButtonVariant.Filled,
            ButtonSize.Small,
            false,
            AvaloniaProperty.UnsetValue);
    }
}
