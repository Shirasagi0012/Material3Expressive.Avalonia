using System;
using Avalonia;
using Avalonia.Data;

namespace Material3Expressive.Foundation;

/// <summary>
/// Bindings for the button markup extension.
/// </summary>
/// <remarks>
/// There is one definition of every default now — the token bag's initializer. This resolver only
/// selects, it never supplies values of its own, which is what removes the old triplication
/// between constructor, resolver and markup fallback.
/// </remarks>
internal static class ButtonTokenResolver
{
    public static BindingBase Resolve(ButtonToken token)
    {
        return token switch
        {
            ButtonToken.ContainerColor => Color(static colors => colors.Container),
            ButtonToken.LabelTextColor => Color(static colors => colors.LabelText),
            ButtonToken.DisabledContainerColor => Color(static colors => colors.DisabledContainer),
            ButtonToken.DisabledLabelTextColor => Color(static colors => colors.DisabledLabelText),
            ButtonToken.StateLayerColor => Color(static colors => colors.StateLayer),
            ButtonToken.ContainerCornerRadius => Metric(static theme => ContainerCornerRadius(theme)),
            ButtonToken.ContentPadding => Metric(static theme => theme.ButtonTokens.ContentPadding),
            ButtonToken.ContainerHeight => Metric(static theme => theme.ButtonTokens.ContainerHeight),
            ButtonToken.HoverStateLayerOpacity => Metric(static theme => theme.ButtonTokens.HoverStateLayerOpacity),
            ButtonToken.FocusStateLayerOpacity => Metric(static theme => theme.ButtonTokens.FocusStateLayerOpacity),
            ButtonToken.PressedStateLayerOpacity => Metric(static theme => theme.ButtonTokens.PressedStateLayerOpacity),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    /// <summary>
    /// On this binding-driven path the shape resolves against the button's own token height, which
    /// is what a percentage corner means for a pill-shaped button. A control that reads its tokens
    /// in code resolves against its actual bounds instead.
    /// </summary>
    private static CornerRadius ContainerCornerRadius(ResolvedTheme theme)
    {
        var height = theme.ButtonTokens.ContainerHeight;
        return theme.Shapes.GetCornerRadius(theme.ButtonTokens.ContainerShape, new Size(height, height));
    }

    private static BindingBase Color(Func<ButtonColors, object?> select)
    {
        return ThemeBinding.FromThemeVariant(
            (theme, isDark) => select(theme.GetButtonColors(isDark)),
            AvaloniaProperty.UnsetValue);
    }

    private static BindingBase Metric(Func<ResolvedTheme, object?> select)
    {
        return ThemeBinding.FromTheme(select, AvaloniaProperty.UnsetValue);
    }
}
