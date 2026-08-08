namespace Material3Expressive.Controls;

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

/// <summary>
/// The component-owned runtime adapter for a Material ToggleButton.
/// </summary>
internal static class ToggleButtonDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    public static ToggleButtonColors Colors(ResolvedTheme theme, ButtonVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return variant switch
        {
            ButtonVariant.Filled => ResolveFilled(theme, isDark),
            ButtonVariant.Tonal => ResolveTonal(theme, isDark),
            ButtonVariant.Elevated => ResolveElevated(theme, isDark),
            ButtonVariant.Outlined => ResolveOutlined(theme, isDark),
            ButtonVariant.Text => ResolveText(theme, isDark),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>Projects the shared Button size axis into ToggleButton-specific template metrics.</summary>
    public static ToggleButtonMetrics Metrics(ButtonSize size)
    {
        var metrics = ButtonDefaults.Metrics(size);
        return new ToggleButtonMetrics(
            metrics.ContainerHeight,
            Math.Max(48, metrics.ContainerHeight),
            metrics.ContentPadding,
            metrics.OutlineWidth,
            metrics.LabelTextStyle);
    }

    public static ToggleButtonShapes Shapes(ButtonSize size)
    {
        var metrics = ButtonDefaults.Metrics(size);
        return new ToggleButtonShapes(
            metrics.ContainerShape,
            metrics.PressedContainerShape,
            metrics.SquareContainerShape);
    }

    public static SpringSpec ShapeMotion(ResolvedTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        // ToggleButton's checked/pressed shape morph is spatial motion in Compose. FastSpatial is
        // intentionally bouncy; ordinary Button uses DefaultEffects to avoid a bounce.
        return theme.Motion.Get(MotionRole.FastSpatial);
    }

    public static Thickness OutlineThickness(
        ToggleButtonColors colors,
        ButtonSize size,
        bool isChecked) => new(colors.HasOutline && !isChecked ? Metrics(size).OutlineWidth : 0);

    private static ToggleButtonColors ResolveFilled(ResolvedTheme theme, bool isDark) => new(
        Brush(FilledButtonTokens.UnselectedContainerColor, theme, isDark),
        Brush(FilledButtonTokens.LabelTextUnselectedColor, theme, isDark),
        Brush(FilledButtonTokens.SelectedContainerColor, theme, isDark),
        Brush(FilledButtonTokens.LabelTextSelectedColor, theme, isDark),
        Brush(FilledButtonTokens.DisabledContainerColor, theme, isDark, FilledButtonTokens.DisabledContainerOpacity),
        Brush(FilledButtonTokens.DisabledLabelTextColor, theme, isDark, FilledButtonTokens.DisabledLabelTextOpacity),
        Transparent,
        Transparent,
        HasOutline: false);

    private static ToggleButtonColors ResolveTonal(ResolvedTheme theme, bool isDark) => new(
        Brush(TonalButtonTokens.UnselectedContainerColor, theme, isDark),
        Brush(TonalButtonTokens.UnselectedLabelTextColor, theme, isDark),
        Brush(TonalButtonTokens.SelectedContainerColor, theme, isDark),
        Brush(TonalButtonTokens.SelectedLabelTextColor, theme, isDark),
        Brush(TonalButtonTokens.DisabledContainerColor, theme, isDark, TonalButtonTokens.DisabledContainerOpacity),
        Brush(TonalButtonTokens.DisabledLabelTextColor, theme, isDark, TonalButtonTokens.DisabledLabelTextOpacity),
        Transparent,
        Transparent,
        HasOutline: false);

    private static ToggleButtonColors ResolveElevated(ResolvedTheme theme, bool isDark) => new(
        Brush(ElevatedButtonTokens.UnselectedContainerColor, theme, isDark),
        Brush(ElevatedButtonTokens.LabelTextUnselectedColor, theme, isDark),
        Brush(ElevatedButtonTokens.SelectedContainerColor, theme, isDark),
        Brush(ElevatedButtonTokens.LabelTextSelectedColor, theme, isDark),
        Brush(ElevatedButtonTokens.DisabledContainerColor, theme, isDark, ElevatedButtonTokens.DisabledContainerOpacity),
        Brush(ElevatedButtonTokens.DisabledLabelTextColor, theme, isDark, ElevatedButtonTokens.DisabledLabelTextOpacity),
        Transparent,
        Transparent,
        HasOutline: false);

    private static ToggleButtonColors ResolveOutlined(ResolvedTheme theme, bool isDark) => new(
        Transparent,
        Brush(OutlinedButtonTokens.UnselectedLabelTextColor, theme, isDark),
        Brush(OutlinedButtonTokens.SelectedContainerColor, theme, isDark),
        Brush(OutlinedButtonTokens.SelectedLabelTextColor, theme, isDark),
        Transparent,
        Brush(OutlinedButtonTokens.DisabledLabelTextColor, theme, isDark, OutlinedButtonTokens.DisabledLabelTextOpacity),
        Brush(OutlinedButtonTokens.OutlineColor, theme, isDark),
        Brush(OutlinedButtonTokens.DisabledOutlineColor, theme, isDark),
        HasOutline: true);

    private static ToggleButtonColors ResolveText(ResolvedTheme theme, bool isDark) => new(
        Transparent,
        Brush(SysColorToken.Primary, theme, isDark),
        Transparent,
        Brush(SysColorToken.Primary, theme, isDark),
        Transparent,
        Brush(TextButtonTokens.DisabledLabelColor, theme, isDark, TextButtonTokens.DisabledLabelOpacity),
        Transparent,
        Transparent,
        HasOutline: false);

    private static IImmutableBrush Brush(
        ColorTokenValue token,
        ResolvedTheme theme,
        bool isDark,
        double opacity = 1) => token.ResolveBrush(theme.ColorScheme, isDark, opacity);
}
