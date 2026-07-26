using System;
using Avalonia;
using Avalonia.Data;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// Bindings for the button markup extension.
/// </summary>
/// <remarks>
/// This resolver only selects; every value it hands back comes from the generated token tables, so
/// there is exactly one definition of each default in the repository. Colours additionally depend
/// on the target's theme variant, which is why they take the two-input binding.
/// </remarks>
internal static class ButtonTokenResolver
{
    public static BindingBase Resolve(ButtonToken token, ButtonVariant variant, ButtonSize size)
    {
        return token switch
        {
            ButtonToken.ContainerColor => Color(variant, static colors => colors.ContainerColor),
            ButtonToken.LabelTextColor => Color(variant, static colors => colors.LabelTextColor),
            ButtonToken.IconColor => Color(variant, static colors => colors.IconColor),
            ButtonToken.OutlineColor => Color(variant, static colors => colors.OutlineColor),
            ButtonToken.StateLayerColor => Color(variant, static colors => colors.StateLayerColor),
            ButtonToken.DisabledContainerColor => Color(variant, static colors => colors.DisabledContainerColor),
            ButtonToken.DisabledLabelTextColor => Color(variant, static colors => colors.DisabledLabelTextColor),
            ButtonToken.DisabledIconColor => Color(variant, static colors => colors.DisabledIconColor),
            ButtonToken.DisabledOutlineColor => Color(variant, static colors => colors.DisabledOutlineColor),

            ButtonToken.ContainerCornerRadius =>
                Metric(theme => CornerRadius(theme, size, Metrics(theme, size).ContainerShape)),
            ButtonToken.PressedContainerCornerRadius =>
                Metric(theme => CornerRadius(theme, size, Metrics(theme, size).PressedContainerShape)),
            ButtonToken.ContentPadding => Metric(theme => ContentPadding(Metrics(theme, size))),
            ButtonToken.ContainerHeight => Metric(theme => Metrics(theme, size).ContainerHeight),
            ButtonToken.IconSize => Metric(theme => Metrics(theme, size).IconSize),
            ButtonToken.IconLabelSpace => Metric(theme => Metrics(theme, size).IconLabelSpace),
            ButtonToken.OutlineWidth => Metric(theme => Metrics(theme, size).OutlineWidth),
            ButtonToken.LabelTextStyle => Metric(theme => Metrics(theme, size).LabelTextStyle),

            ButtonToken.HoverStateLayerOpacity => Constant(StateTokens.HoverStateLayerOpacity),
            ButtonToken.FocusStateLayerOpacity => Constant(StateTokens.FocusStateLayerOpacity),
            ButtonToken.PressedStateLayerOpacity => Constant(StateTokens.PressedStateLayerOpacity),

            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    /// <summary>The content padding implied by a size's three spacing tokens.</summary>
    public static Thickness ContentPadding(ButtonSizeTokens size)
    {
        return new Thickness(size.LeadingSpace, size.VerticalSpace, size.TrailingSpace, size.VerticalSpace);
    }

    private static ButtonSizeTokens Metrics(ResolvedTheme theme, ButtonSize size)
    {
        return theme.GetButtonSizeTokens(size);
    }

    /// <summary>
    /// On this binding-driven path the shape resolves against the size's own container height,
    /// which is what a percentage corner means for a pill-shaped button. A control that reads its
    /// tokens in code resolves against its actual bounds instead.
    /// </summary>
    private static CornerRadius CornerRadius(ResolvedTheme theme, ButtonSize size, ShapeRole role)
    {
        var height = Metrics(theme, size).ContainerHeight;
        return theme.Shapes.GetCornerRadius(role, new Size(height, height));
    }

    private static BindingBase Color(ButtonVariant variant, Func<ButtonColors, object?> select)
    {
        return ThemeBinding.FromThemeVariant(
            (theme, isDark) => select(theme.GetButtonColors(variant, isDark)),
            AvaloniaProperty.UnsetValue);
    }

    private static BindingBase Metric(Func<ResolvedTheme, object?> select)
    {
        return ThemeBinding.FromTheme(select, AvaloniaProperty.UnsetValue);
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
