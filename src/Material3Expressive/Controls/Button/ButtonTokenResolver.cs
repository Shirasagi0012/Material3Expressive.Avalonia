using System;
using Avalonia;
using Avalonia.Data;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls.ButtonRuntime;

/// <summary>
/// Bindings for the ordinary Button component's markup token surface.
/// </summary>
/// <remarks>
/// The enum remains in <c>Foundation</c> as the stable markup-facing compatibility name. The
/// binding policy itself is component-owned and therefore lives beside <see cref="ButtonDefaults"/>
/// rather than in the cross-component foundation layer.
/// </remarks>
internal static class ButtonTokenResolver
{
    public static BindingBase Resolve(ButtonToken token)
    {
        return token switch
        {
            ButtonToken.ContainerColor => Color(static colors => colors.ContainerColor),
            ButtonToken.LabelTextColor => Color(static colors => colors.LabelTextColor),
            ButtonToken.IconColor => Color(static colors => colors.IconColor),
            ButtonToken.OutlineColor => Color(static colors => colors.OutlineColor),
            ButtonToken.StateLayerColor => Color(static colors => colors.StateLayerColor),
            ButtonToken.DisabledContainerColor => Color(static colors => colors.DisabledContainerColor),
            ButtonToken.DisabledLabelTextColor => Color(static colors => colors.DisabledLabelTextColor),
            ButtonToken.DisabledIconColor => Color(static colors => colors.DisabledIconColor),
            ButtonToken.DisabledOutlineColor => Color(static colors => colors.DisabledOutlineColor),
            ButtonToken.UnselectedContainerColor => Color(static colors => colors.UnselectedContainerColor),
            ButtonToken.SelectedContainerColor => Color(static colors => colors.SelectedContainerColor),
            ButtonToken.UnselectedLabelTextColor => Color(static colors => colors.UnselectedLabelTextColor),
            ButtonToken.SelectedLabelTextColor => Color(static colors => colors.SelectedLabelTextColor),

            ButtonToken.ContainerCornerRadius =>
                Metric((theme, size) => CornerRadius(
                    theme,
                    size,
                    ButtonDefaults.Metrics(theme, size).ContainerShape)),
            ButtonToken.PressedContainerCornerRadius =>
                Metric((theme, size) => CornerRadius(
                    theme,
                    size,
                    ButtonDefaults.Metrics(theme, size).PressedContainerShape)),
            ButtonToken.ContentPadding => Metric((theme, size) =>
                ButtonDefaults.ContentPadding(theme, size)),
            ButtonToken.ContainerHeight => Metric((theme, size) =>
                ButtonDefaults.Metrics(theme, size).ContainerHeight),
            ButtonToken.ToggleContainerHeight => Metric(
                (theme, size) => Math.Max(48, ButtonDefaults.Metrics(theme, size).ContainerHeight)),
            ButtonToken.IconSize => Metric((theme, size) => ButtonDefaults.Metrics(theme, size).IconSize),
            ButtonToken.IconLabelSpace => Metric((theme, size) =>
                ButtonDefaults.Metrics(theme, size).IconLabelSpace),
            ButtonToken.OutlineWidth => Metric((theme, size) =>
                ButtonDefaults.Metrics(theme, size).OutlineWidth),
            ButtonToken.ContainerOutlineThickness => ThemeBinding.FromThemeProperties<
                ButtonVariant,
                ButtonSize,
                object?>(
                ButtonAssist.VariantProperty,
                ButtonAssist.SizeProperty,
                (theme, variant, size) => new Thickness(
                    ButtonDefaults.ColorTokens(theme, variant).HasOutline
                        ? ButtonDefaults.Metrics(theme, size).OutlineWidth
                        : 0),
                ButtonVariant.Filled,
                ButtonSize.Small,
                AvaloniaProperty.UnsetValue),
            ButtonToken.LabelTextStyle => Metric((theme, size) =>
                ButtonDefaults.Metrics(theme, size).LabelTextStyle),

            ButtonToken.HoverStateLayerOpacity => Constant(StateTokens.HoverStateLayerOpacity),
            ButtonToken.FocusStateLayerOpacity => Constant(StateTokens.FocusStateLayerOpacity),
            ButtonToken.PressedStateLayerOpacity => Constant(StateTokens.PressedStateLayerOpacity),

            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    /// <summary>
    /// On this binding-driven path the shape resolves against the size's own container height,
    /// which is what a percentage corner means for a pill-shaped button. A control that reads its
    /// tokens in code resolves against its actual bounds instead.
    /// </summary>
    private static CornerRadius CornerRadius(ResolvedTheme theme, ButtonSize size, ShapeRole role)
    {
        var height = ButtonDefaults.Metrics(theme, size).ContainerHeight;
        return theme.Shapes.GetCornerRadius(role, new Size(height, height));
    }

    private static BindingBase Color(Func<ButtonColors, object?> select)
    {
        return ThemeBinding.FromThemeVariantProperty<ButtonVariant, object?>(
            ButtonAssist.VariantProperty,
            (theme, isDark, variant) => select(ButtonDefaults.Colors(theme, variant, isDark)),
            ButtonVariant.Filled,
            AvaloniaProperty.UnsetValue);
    }

    private static BindingBase Metric(Func<ResolvedTheme, ButtonSize, object?> select)
    {
        return ThemeBinding.FromThemeProperty<ButtonSize, object?>(
            ButtonAssist.SizeProperty,
            select,
            ButtonSize.Small,
            AvaloniaProperty.UnsetValue);
    }

    /// <summary>
    /// A token that does not depend on the theme at all. It still travels as a binding so that the
    /// markup extension has one return type.
    /// </summary>
    private static BindingBase Constant(object value)
    {
        return ThemeBinding.FromTheme(_ => value, AvaloniaProperty.UnsetValue);
    }
}
