namespace Material3Expressive.Controls;

using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using MaterialColorUtilities.Avalonia.Tokens;

/// <summary>Theme-resolved colours published by the Button defaults adapter.</summary>
/// <remarks>
/// This is a library-owned fallback profile, not a public token override bag. It is public only
/// because compiled library AXAML must resolve the type of its attached-property contract.
/// Applications customise ordinary visual properties through Avalonia styles and local values.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonColors(
    IImmutableBrush Container,
    IImmutableBrush Content,
    IImmutableBrush Icon,
    IImmutableBrush Outline,
    IImmutableBrush StateLayer,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledContent,
    IImmutableBrush DisabledIcon,
    IImmutableBrush DisabledOutline);

/// <summary>The size-dependent semantic metrics published by the Button defaults adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonMetrics(
    double ContainerHeight,
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    ShapeRole SquareContainerShape,
    Thickness ContentPadding,
    double IconSize,
    double IconLabelSpace,
    double OutlineWidth,
    TypographyRole LabelTextStyle);

/// <summary>The geometry roles a Button presenter needs for its runtime shape selection.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonShapes(
    ShapeRole Container,
    ShapeRole Pressed,
    ShapeRole Square);

/// <summary>The elevation profile selected by a Button presenter from interaction state.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonElevation(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Disabled)
{
    public double For(bool enabled, InteractionState interaction)
    {
        if (!enabled)
            return Disabled;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}

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
    /// Resolves the visual colour profile from the resource scope of the eventual consumer.
    /// Component aliases are deliberately evaluated by <paramref name="tokens" /> rather than
    /// pre-resolved in a theme provider, so a local system-token override is visible here.
    /// </summary>
    public static ButtonColors Colors(TokenReader tokens, ButtonVariant variant)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        return variant switch
        {
            ButtonVariant.Filled => ResolveFilled(tokens),
            ButtonVariant.Tonal => ResolveTonal(tokens),
            ButtonVariant.Elevated => ResolveElevated(tokens),
            ButtonVariant.Outlined => ResolveOutlined(tokens),
            ButtonVariant.Text => ResolveText(tokens),
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

    /// <summary>Resolves size tokens from the resource scope of the eventual consumer.</summary>
    public static ButtonMetrics Metrics(TokenReader tokens, ButtonSize size)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        return size switch
        {
            ButtonSize.ExtraSmall => Metrics(
                tokens.Get(ButtonXSmallTokensKeys.ContainerHeight),
                tokens.Get(ButtonXSmallTokensKeys.ContainerShapeRound),
                tokens.Get(ButtonXSmallTokensKeys.PressedContainerShape),
                tokens.Get(ButtonXSmallTokensKeys.ContainerShapeSquare),
                // Compose ships 12dp here even though the raw upstream table still says 16dp.
                leading: 12,
                vertical: 6,
                trailing: 12,
                tokens.Get(ButtonXSmallTokensKeys.IconSize),
                tokens.Get(ButtonXSmallTokensKeys.IconLabelSpace),
                tokens.Get(ButtonXSmallTokensKeys.OutlinedOutlineWidth),
                TypographyRole.LabelLarge),
            ButtonSize.Small => Metrics(
                tokens.Get(ButtonSmallTokensKeys.ContainerHeight),
                tokens.Get(ButtonSmallTokensKeys.ContainerShapeRound),
                tokens.Get(ButtonSmallTokensKeys.PressedContainerShape),
                tokens.Get(ButtonSmallTokensKeys.ContainerShapeSquare),
                tokens.Get(ButtonSmallTokensKeys.LeadingSpace),
                vertical: 10,
                tokens.Get(ButtonSmallTokensKeys.TrailingSpace),
                tokens.Get(ButtonSmallTokensKeys.IconSize),
                tokens.Get(ButtonSmallTokensKeys.IconLabelSpace),
                tokens.Get(ButtonSmallTokensKeys.OutlinedOutlineWidth),
                TypographyRole.LabelLarge),
            ButtonSize.Medium => Metrics(
                tokens.Get(ButtonMediumTokensKeys.ContainerHeight),
                tokens.Get(ButtonMediumTokensKeys.ContainerShapeRound),
                tokens.Get(ButtonMediumTokensKeys.PressedContainerShape),
                tokens.Get(ButtonMediumTokensKeys.ContainerShapeSquare),
                tokens.Get(ButtonMediumTokensKeys.LeadingSpace),
                vertical: 16,
                tokens.Get(ButtonMediumTokensKeys.TrailingSpace),
                tokens.Get(ButtonMediumTokensKeys.IconSize),
                tokens.Get(ButtonMediumTokensKeys.IconLabelSpace),
                tokens.Get(ButtonMediumTokensKeys.OutlinedOutlineWidth),
                TypographyRole.TitleMedium),
            ButtonSize.Large => Metrics(
                tokens.Get(ButtonLargeTokensKeys.ContainerHeight),
                tokens.Get(ButtonLargeTokensKeys.ContainerShapeRound),
                tokens.Get(ButtonLargeTokensKeys.PressedContainerShape),
                tokens.Get(ButtonLargeTokensKeys.ContainerShapeSquare),
                tokens.Get(ButtonLargeTokensKeys.LeadingSpace),
                vertical: 32,
                tokens.Get(ButtonLargeTokensKeys.TrailingSpace),
                tokens.Get(ButtonLargeTokensKeys.IconSize),
                tokens.Get(ButtonLargeTokensKeys.IconLabelSpace),
                tokens.Get(ButtonLargeTokensKeys.OutlinedOutlineWidth),
                TypographyRole.HeadlineSmall),
            ButtonSize.ExtraLarge => Metrics(
                tokens.Get(ButtonXLargeTokensKeys.ContainerHeight),
                tokens.Get(ButtonXLargeTokensKeys.ContainerShapeRound),
                tokens.Get(ButtonXLargeTokensKeys.PressedContainerShape),
                tokens.Get(ButtonXLargeTokensKeys.ContainerShapeSquare),
                tokens.Get(ButtonXLargeTokensKeys.LeadingSpace),
                vertical: 48,
                tokens.Get(ButtonXLargeTokensKeys.TrailingSpace),
                tokens.Get(ButtonXLargeTokensKeys.IconSize),
                tokens.Get(ButtonXLargeTokensKeys.IconLabelSpace),
                tokens.Get(ButtonXLargeTokensKeys.OutlinedOutlineWidth),
                TypographyRole.HeadlineLarge),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>Returns the geometry roles needed by the runtime presenter.</summary>
    public static ButtonShapes Shapes(ButtonSize size)
    {
        return Shapes(Metrics(size));
    }

    /// <summary>Returns requester-local geometry roles for the runtime presenter.</summary>
    public static ButtonShapes Shapes(TokenReader tokens, ButtonSize size)
    {
        return Shapes(Metrics(tokens, size));
    }

    /// <summary>Projects an already-resolved metrics profile into presenter geometry.</summary>
    public static ButtonShapes Shapes(ButtonMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
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

    /// <summary>Returns the requester-local interaction elevation profile for a variant.</summary>
    public static ButtonElevation Elevation(TokenReader tokens, ButtonVariant variant)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        return variant switch
        {
            ButtonVariant.Filled => Elevation(
                tokens.Get(FilledButtonTokensKeys.ContainerElevation),
                tokens.Get(FilledButtonTokensKeys.HoveredContainerElevation),
                tokens.Get(FilledButtonTokensKeys.FocusedContainerElevation),
                tokens.Get(FilledButtonTokensKeys.PressedContainerElevation),
                tokens.Get(FilledButtonTokensKeys.DisabledContainerElevation)),
            ButtonVariant.Tonal => Elevation(
                tokens.Get(TonalButtonTokensKeys.ContainerElevation),
                tokens.Get(TonalButtonTokensKeys.HoveredContainerElevation),
                tokens.Get(TonalButtonTokensKeys.FocusedContainerElevation),
                tokens.Get(TonalButtonTokensKeys.PressedContainerElevation),
                tokens.Get(TonalButtonTokensKeys.DisabledContainerElevation)),
            ButtonVariant.Elevated => Elevation(
                tokens.Get(ElevatedButtonTokensKeys.ContainerElevation),
                tokens.Get(ElevatedButtonTokensKeys.HoveredContainerElevation),
                tokens.Get(ElevatedButtonTokensKeys.FocusedContainerElevation),
                tokens.Get(ElevatedButtonTokensKeys.PressedContainerElevation),
                tokens.Get(ElevatedButtonTokensKeys.DisabledContainerElevation)),
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
        return OutlineThickness(variant, Metrics(size));
    }

    /// <summary>Returns the requester-local outline thickness consumed by the ControlTheme.</summary>
    public static Thickness OutlineThickness(TokenReader tokens, ButtonVariant variant, ButtonSize size)
    {
        return OutlineThickness(variant, Metrics(tokens, size));
    }

    /// <summary>Returns outline thickness from an already-resolved metrics profile.</summary>
    public static Thickness OutlineThickness(ButtonVariant variant, ButtonMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        var width = variant == ButtonVariant.Outlined ? metrics.OutlineWidth : 0;
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

    private static ButtonColors ResolveFilled(TokenReader tokens) => new(
        Brush(tokens, FilledButtonTokensKeys.ContainerColor),
        Brush(tokens, FilledButtonTokensKeys.LabelTextColor),
        Brush(tokens, FilledButtonTokensKeys.IconColor),
        Transparent,
        Brush(tokens, FilledButtonTokensKeys.LabelTextColor),
        Brush(tokens, FilledButtonTokensKeys.DisabledContainerColor, FilledButtonTokensKeys.DisabledContainerOpacity),
        Brush(tokens, FilledButtonTokensKeys.DisabledLabelTextColor, FilledButtonTokensKeys.DisabledLabelTextOpacity),
        Brush(tokens, FilledButtonTokensKeys.DisabledIconColor, FilledButtonTokensKeys.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveTonal(TokenReader tokens) => new(
        Brush(tokens, TonalButtonTokensKeys.ContainerColor),
        Brush(tokens, TonalButtonTokensKeys.LabelTextColor),
        Brush(tokens, TonalButtonTokensKeys.IconColor),
        Transparent,
        Brush(tokens, TonalButtonTokensKeys.LabelTextColor),
        Brush(tokens, TonalButtonTokensKeys.DisabledContainerColor, TonalButtonTokensKeys.DisabledContainerOpacity),
        Brush(tokens, TonalButtonTokensKeys.DisabledLabelTextColor, TonalButtonTokensKeys.DisabledLabelTextOpacity),
        Brush(tokens, TonalButtonTokensKeys.DisabledIconColor, TonalButtonTokensKeys.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveElevated(TokenReader tokens) => new(
        Brush(tokens, ElevatedButtonTokensKeys.ContainerColor),
        Brush(tokens, ElevatedButtonTokensKeys.LabelTextColor),
        Brush(tokens, ElevatedButtonTokensKeys.IconColor),
        Transparent,
        Brush(tokens, ElevatedButtonTokensKeys.LabelTextColor),
        Brush(tokens, ElevatedButtonTokensKeys.DisabledContainerColor, ElevatedButtonTokensKeys.DisabledContainerOpacity),
        Brush(tokens, ElevatedButtonTokensKeys.DisabledLabelTextColor, ElevatedButtonTokensKeys.DisabledLabelTextOpacity),
        Brush(tokens, ElevatedButtonTokensKeys.DisabledIconColor, ElevatedButtonTokensKeys.DisabledIconOpacity),
        Transparent);

    private static ButtonColors ResolveOutlined(TokenReader tokens) => new(
        Transparent,
        Brush(tokens, OutlinedButtonTokensKeys.LabelTextColor),
        Brush(tokens, OutlinedButtonTokensKeys.IconColor),
        Brush(tokens, OutlinedButtonTokensKeys.OutlineColor),
        Brush(tokens, OutlinedButtonTokensKeys.LabelTextColor),
        Transparent,
        Brush(tokens, OutlinedButtonTokensKeys.DisabledLabelTextColor, OutlinedButtonTokensKeys.DisabledLabelTextOpacity),
        Brush(tokens, OutlinedButtonTokensKeys.DisabledIconColor, OutlinedButtonTokensKeys.DisabledIconOpacity),
        Brush(tokens, OutlinedButtonTokensKeys.DisabledOutlineColor));

    private static ButtonColors ResolveText(TokenReader tokens)
    {
        // Compose's public ButtonDefaults correct these two raw generated defaults to Primary.
        // An explicit component token still takes precedence, including requester-local aliases.
        var label = Brush(tokens.GetDefinedOr(TextButtonTokensKeys.LabelColor, SystemColorTokens.Primary));
        var icon = Brush(tokens.GetDefinedOr(TextButtonTokensKeys.IconColor, SystemColorTokens.Primary));
        return new ButtonColors(
            Transparent,
            label,
            icon,
            Transparent,
            label,
            Transparent,
            Brush(tokens, TextButtonTokensKeys.DisabledLabelColor, TextButtonTokensKeys.DisabledLabelOpacity),
            Brush(tokens, TextButtonTokensKeys.DisabledIconColor, TextButtonTokensKeys.DisabledIconOpacity),
            Transparent);
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static IImmutableBrush Brush(
        TokenReader tokens,
        MaterialToken<Color> colorToken,
        MaterialToken<double>? opacityToken = null)
    {
        var color = tokens.Get(colorToken);
        var opacity = opacityToken is null ? 1 : tokens.Get(opacityToken);
        if (opacity < 1)
        {
            color = new Color(
                (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
                color.R,
                color.G,
                color.B);
        }

        return new ImmutableSolidColorBrush(color);
    }

    private static IImmutableBrush Brush(
        ColorTokenValue token,
        ResolvedTheme theme,
        bool isDark,
        double opacity = 1) => token.ResolveBrush(theme.ColorScheme, isDark, opacity);
}
