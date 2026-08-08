namespace Material3Expressive.Controls;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

/// <summary>
/// The component-owned runtime adapter for an ordinary Material button.
/// </summary>
/// <remarks>
/// <para>
/// The generated raw tables are immutable upstream facts. This adapter is the boundary where the
/// Button maps its axes to those facts, resolves system roles, and applies Compose/Avalonia policy.
/// </para>
/// <para>
/// Interaction selection and Avalonia-specific value shaping belong here rather than in a token
/// table. In particular, the JSON table must not become a behaviour language for pointer state,
/// bindings, or control templates.
/// </para>
/// </remarks>
internal static class ButtonDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    /// <summary>Resolves the visual colour profile for a variant against system theme semantics.</summary>
    public static ButtonColors Colors(ResolvedTheme theme, ButtonVariant variant, bool isDark)
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

    /// <summary>
    /// Resolves the size axis into the values actually consumed by the control theme and presenter.
    /// Corrections from Compose <c>ButtonDefaults</c> intentionally live here rather than in JSON.
    /// </summary>
    public static ButtonMetrics Metrics(ButtonSize size)
    {
        return size switch
        {
            ButtonSize.ExtraSmall => Metrics(
                ButtonXSmallTokens.ContainerHeight,
                ButtonXSmallTokens.ContainerShapeRound,
                ButtonXSmallTokens.PressedContainerShape,
                ButtonXSmallTokens.ContainerShapeSquare,
                leading: 12,
                vertical: 6,
                trailing: 12,
                ButtonXSmallTokens.IconSize,
                ButtonXSmallTokens.IconLabelSpace,
                ButtonXSmallTokens.OutlinedOutlineWidth,
                TypographyRole.LabelLarge),
            ButtonSize.Small => Metrics(
                ButtonSmallTokens.ContainerHeight,
                ButtonSmallTokens.ContainerShapeRound,
                ButtonSmallTokens.PressedContainerShape,
                ButtonSmallTokens.ContainerShapeSquare,
                ButtonSmallTokens.LeadingSpace,
                vertical: 10,
                ButtonSmallTokens.TrailingSpace,
                ButtonSmallTokens.IconSize,
                ButtonSmallTokens.IconLabelSpace,
                ButtonSmallTokens.OutlinedOutlineWidth,
                TypographyRole.LabelLarge),
            ButtonSize.Medium => Metrics(
                ButtonMediumTokens.ContainerHeight,
                ButtonMediumTokens.ContainerShapeRound,
                ButtonMediumTokens.PressedContainerShape,
                ButtonMediumTokens.ContainerShapeSquare,
                ButtonMediumTokens.LeadingSpace,
                vertical: 16,
                ButtonMediumTokens.TrailingSpace,
                ButtonMediumTokens.IconSize,
                ButtonMediumTokens.IconLabelSpace,
                ButtonMediumTokens.OutlinedOutlineWidth,
                TypographyRole.TitleMedium),
            ButtonSize.Large => Metrics(
                ButtonLargeTokens.ContainerHeight,
                ButtonLargeTokens.ContainerShapeRound,
                ButtonLargeTokens.PressedContainerShape,
                ButtonLargeTokens.ContainerShapeSquare,
                ButtonLargeTokens.LeadingSpace,
                vertical: 32,
                ButtonLargeTokens.TrailingSpace,
                ButtonLargeTokens.IconSize,
                ButtonLargeTokens.IconLabelSpace,
                ButtonLargeTokens.OutlinedOutlineWidth,
                TypographyRole.HeadlineSmall),
            ButtonSize.ExtraLarge => Metrics(
                ButtonXLargeTokens.ContainerHeight,
                ButtonXLargeTokens.ContainerShapeRound,
                ButtonXLargeTokens.PressedContainerShape,
                ButtonXLargeTokens.ContainerShapeSquare,
                ButtonXLargeTokens.LeadingSpace,
                vertical: 48,
                ButtonXLargeTokens.TrailingSpace,
                ButtonXLargeTokens.IconSize,
                ButtonXLargeTokens.IconLabelSpace,
                ButtonXLargeTokens.OutlinedOutlineWidth,
                TypographyRole.HeadlineLarge),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>Returns the geometry roles needed by the runtime presenter.</summary>
    public static ButtonShapes Shapes(ButtonSize size)
    {
        var metrics = Metrics(size);
        return new ButtonShapes(
            metrics.ContainerShape,
            metrics.PressedContainerShape,
            metrics.SquareContainerShape);
    }

    /// <summary>Returns the interaction elevation profile for a variant.</summary>
    public static ButtonElevation Elevation(ButtonVariant variant)
    {
        return variant switch
        {
            ButtonVariant.Filled => Elevation(
                FilledButtonTokens.ContainerElevation,
                FilledButtonTokens.HoveredContainerElevation,
                FilledButtonTokens.FocusedContainerElevation,
                FilledButtonTokens.PressedContainerElevation,
                FilledButtonTokens.DisabledContainerElevation),
            ButtonVariant.Tonal => Elevation(
                TonalButtonTokens.ContainerElevation,
                TonalButtonTokens.HoveredContainerElevation,
                TonalButtonTokens.FocusedContainerElevation,
                TonalButtonTokens.PressedContainerElevation,
                TonalButtonTokens.DisabledContainerElevation),
            ButtonVariant.Elevated => Elevation(
                ElevatedButtonTokens.ContainerElevation,
                ElevatedButtonTokens.HoveredContainerElevation,
                ElevatedButtonTokens.FocusedContainerElevation,
                ElevatedButtonTokens.PressedContainerElevation,
                ElevatedButtonTokens.DisabledContainerElevation),
            ButtonVariant.Outlined or ButtonVariant.Text => new ButtonElevation(0, 0, 0, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>The effects spring used for the Button's pressed-shape morph.</summary>
    public static SpringSpec ShapeMotion(ResolvedTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return theme.Motion.Get(MotionRole.DefaultEffects);
    }

    /// <summary>Returns the outline thickness consumed by the ordinary Button ControlTheme.</summary>
    public static Thickness OutlineThickness(ButtonVariant variant, ButtonSize size)
    {
        var width = variant == ButtonVariant.Outlined ? Metrics(size).OutlineWidth : 0;
        return new Thickness(width);
    }

    private static ButtonMetrics Metrics(
        double height,
        ShapeRole container,
        ShapeRole pressed,
        ShapeRole square,
        double leading,
        double vertical,
        double trailing,
        double iconSize,
        double iconLabelSpace,
        double outlineWidth,
        TypographyRole textStyle)
    {
        return new ButtonMetrics(
            height,
            container,
            pressed,
            square,
            new Thickness(leading, vertical, trailing, vertical),
            iconSize,
            iconLabelSpace,
            outlineWidth,
            textStyle);
    }

    private static ButtonElevation Elevation(
        double resting,
        double hovered,
        double focused,
        double pressed,
        double disabled) => new(resting, hovered, focused, pressed, disabled);

    private static ButtonColors ResolveFilled(ResolvedTheme theme, bool isDark) => new(
        Brush(FilledButtonTokens.ContainerColor, theme, isDark),
        Brush(FilledButtonTokens.LabelTextColor, theme, isDark),
        Brush(FilledButtonTokens.IconColor, theme, isDark),
        Transparent,
        Brush(FilledButtonTokens.LabelTextColor, theme, isDark),
        Brush(FilledButtonTokens.DisabledContainerColor, theme, isDark, FilledButtonTokens.DisabledContainerOpacity),
        Brush(FilledButtonTokens.DisabledLabelTextColor, theme, isDark, FilledButtonTokens.DisabledLabelTextOpacity),
        Brush(FilledButtonTokens.DisabledIconColor, theme, isDark, FilledButtonTokens.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveTonal(ResolvedTheme theme, bool isDark) => new(
        Brush(TonalButtonTokens.ContainerColor, theme, isDark),
        Brush(TonalButtonTokens.LabelTextColor, theme, isDark),
        Brush(TonalButtonTokens.IconColor, theme, isDark),
        Transparent,
        Brush(TonalButtonTokens.LabelTextColor, theme, isDark),
        Brush(TonalButtonTokens.DisabledContainerColor, theme, isDark, TonalButtonTokens.DisabledContainerOpacity),
        Brush(TonalButtonTokens.DisabledLabelTextColor, theme, isDark, TonalButtonTokens.DisabledLabelTextOpacity),
        Brush(TonalButtonTokens.DisabledIconColor, theme, isDark, TonalButtonTokens.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveElevated(ResolvedTheme theme, bool isDark) => new(
        Brush(ElevatedButtonTokens.ContainerColor, theme, isDark),
        Brush(ElevatedButtonTokens.LabelTextColor, theme, isDark),
        Brush(ElevatedButtonTokens.IconColor, theme, isDark),
        Transparent,
        Brush(ElevatedButtonTokens.LabelTextColor, theme, isDark),
        Brush(ElevatedButtonTokens.DisabledContainerColor, theme, isDark, ElevatedButtonTokens.DisabledContainerOpacity),
        Brush(ElevatedButtonTokens.DisabledLabelTextColor, theme, isDark, ElevatedButtonTokens.DisabledLabelTextOpacity),
        Brush(ElevatedButtonTokens.DisabledIconColor, theme, isDark, ElevatedButtonTokens.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveOutlined(ResolvedTheme theme, bool isDark) => new(
        Transparent,
        Brush(OutlinedButtonTokens.LabelTextColor, theme, isDark),
        Brush(OutlinedButtonTokens.IconColor, theme, isDark),
        Brush(OutlinedButtonTokens.OutlineColor, theme, isDark),
        Brush(OutlinedButtonTokens.LabelTextColor, theme, isDark),
        Transparent,
        Brush(OutlinedButtonTokens.DisabledLabelTextColor, theme, isDark, OutlinedButtonTokens.DisabledLabelTextOpacity),
        Brush(OutlinedButtonTokens.DisabledIconColor, theme, isDark, OutlinedButtonTokens.DisabledIconOpacity),
        Brush(OutlinedButtonTokens.DisabledOutlineColor, theme, isDark));

    private static ButtonColors ResolveText(ResolvedTheme theme, bool isDark) => new(
        Transparent,
        Brush(SysColorToken.Primary, theme, isDark),
        Brush(SysColorToken.Primary, theme, isDark),
        Transparent,
        Brush(SysColorToken.Primary, theme, isDark),
        Transparent,
        Brush(TextButtonTokens.DisabledLabelColor, theme, isDark, TextButtonTokens.DisabledLabelOpacity),
        Brush(TextButtonTokens.DisabledIconColor, theme, isDark, TextButtonTokens.DisabledIconOpacity),
        Transparent);

    private static IImmutableBrush Brush(
        ColorTokenValue token,
        ResolvedTheme theme,
        bool isDark,
        double opacity = 1) => token.ResolveBrush(theme.ColorScheme, isDark, opacity);
}
