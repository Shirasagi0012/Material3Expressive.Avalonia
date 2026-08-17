using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the ordinary Button control theme.
/// </summary>
/// <remarks>
/// Variant, size and interaction selection remain in the Button theme and presenter. These keys
/// contain only bounded projections from the declared Raw token graph.
/// </remarks>
public static class ButtonValueKeys
{
    public static MaterialValueKey<ButtonVisualColors> FilledVisualColors { get; } =
        VisualColors(
            "Value.Button.Filled.VisualColors",
            FilledButtonTokensKeys.LabelTextColor,
            outline: null,
            FilledButtonTokensKeys.DisabledLabelTextColor,
            FilledButtonTokensKeys.DisabledLabelTextOpacity,
            disabledOutline: null);

    public static MaterialValueKey<ButtonVisualColors> TonalVisualColors { get; } =
        VisualColors(
            "Value.Button.Tonal.VisualColors",
            TonalButtonTokensKeys.LabelTextColor,
            outline: null,
            TonalButtonTokensKeys.DisabledLabelTextColor,
            TonalButtonTokensKeys.DisabledLabelTextOpacity,
            disabledOutline: null);

    public static MaterialValueKey<ButtonVisualColors> ElevatedVisualColors { get; } =
        VisualColors(
            "Value.Button.Elevated.VisualColors",
            ElevatedButtonTokensKeys.LabelTextColor,
            outline: null,
            ElevatedButtonTokensKeys.DisabledLabelTextColor,
            ElevatedButtonTokensKeys.DisabledLabelTextOpacity,
            disabledOutline: null);

    public static MaterialValueKey<ButtonVisualColors> OutlinedVisualColors { get; } =
        VisualColors(
            "Value.Button.Outlined.VisualColors",
            OutlinedButtonTokensKeys.LabelTextColor,
            OutlinedButtonTokensKeys.OutlineColor,
            OutlinedButtonTokensKeys.DisabledLabelTextColor,
            OutlinedButtonTokensKeys.DisabledLabelTextOpacity,
            OutlinedButtonTokensKeys.DisabledOutlineColor);

    /// <summary>
    /// Gets the text-button visual profile, including Compose's public ButtonDefaults correction
    /// from the generated OnSurfaceVariant value to Primary.
    /// </summary>
    public static MaterialValueKey<ButtonVisualColors> TextVisualColors { get; } =
        VisualColors(
            "Value.Button.Text.VisualColors",
            SystemColorTokens.Primary,
            outline: null,
            TextButtonTokensKeys.DisabledLabelColor,
            TextButtonTokensKeys.DisabledLabelOpacity,
            disabledOutline: null);

    public static MaterialValueKey<ButtonElevationProfile> FilledElevation { get; } =
        Elevation(
            "Value.Button.Filled.Elevation",
            FilledButtonTokensKeys.ContainerElevation,
            FilledButtonTokensKeys.HoveredContainerElevation,
            FilledButtonTokensKeys.FocusedContainerElevation,
            FilledButtonTokensKeys.PressedContainerElevation,
            FilledButtonTokensKeys.DisabledContainerElevation);

    public static MaterialValueKey<ButtonElevationProfile> TonalElevation { get; } =
        Elevation(
            "Value.Button.Tonal.Elevation",
            TonalButtonTokensKeys.ContainerElevation,
            TonalButtonTokensKeys.HoveredContainerElevation,
            TonalButtonTokensKeys.FocusedContainerElevation,
            TonalButtonTokensKeys.PressedContainerElevation,
            TonalButtonTokensKeys.DisabledContainerElevation);

    public static MaterialValueKey<ButtonElevationProfile> ElevatedElevation { get; } =
        Elevation(
            "Value.Button.Elevated.Elevation",
            ElevatedButtonTokensKeys.ContainerElevation,
            ElevatedButtonTokensKeys.HoveredContainerElevation,
            ElevatedButtonTokensKeys.FocusedContainerElevation,
            ElevatedButtonTokensKeys.PressedContainerElevation,
            ElevatedButtonTokensKeys.DisabledContainerElevation);

    public static MaterialValueKey<ButtonElevationProfile> OutlinedElevation { get; } =
        ConstantElevation("Value.Button.Outlined.Elevation");

    public static MaterialValueKey<ButtonElevationProfile> TextElevation { get; } =
        ConstantElevation("Value.Button.Text.Elevation");

    public static MaterialValueKey<SpringSpec> ShapeMotion { get; } =
        MaterialValue.FromRaw(
            "Value.Button.ShapeMotion",
            FoundationThemeRawTokens.Motion.DefaultEffects);

    public static MaterialValueKey<Thickness> NoOutlineThickness { get; } =
        MaterialValue.Thickness("Value.Button.NoOutlineThickness", 0, 0, 0, 0);

    public static MaterialValueKey<Thickness> ExtraSmallOutlineThickness { get; } =
        MaterialValue.Thickness(
            "Value.Button.ExtraSmall.OutlineThickness",
            ButtonXSmallTokensKeys.OutlinedOutlineWidth);

    public static MaterialValueKey<Thickness> SmallOutlineThickness { get; } =
        MaterialValue.Thickness(
            "Value.Button.Small.OutlineThickness",
            ButtonSmallTokensKeys.OutlinedOutlineWidth);

    public static MaterialValueKey<Thickness> MediumOutlineThickness { get; } =
        MaterialValue.Thickness(
            "Value.Button.Medium.OutlineThickness",
            ButtonMediumTokensKeys.OutlinedOutlineWidth);

    public static MaterialValueKey<Thickness> LargeOutlineThickness { get; } =
        MaterialValue.Thickness(
            "Value.Button.Large.OutlineThickness",
            ButtonLargeTokensKeys.OutlinedOutlineWidth);

    public static MaterialValueKey<Thickness> ExtraLargeOutlineThickness { get; } =
        MaterialValue.Thickness(
            "Value.Button.ExtraLarge.OutlineThickness",
            ButtonXLargeTokensKeys.OutlinedOutlineWidth);

    public static MaterialValueKey<IImmutableBrush> FilledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Filled.ContainerBrush",
            FilledButtonTokensKeys.ContainerColor);

    public static MaterialValueKey<IImmutableBrush> FilledDisabledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Filled.DisabledContainerBrush",
            FilledButtonTokensKeys.DisabledContainerColor,
            FilledButtonTokensKeys.DisabledContainerOpacity);

    public static MaterialValueKey<IImmutableBrush> TonalContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Tonal.ContainerBrush",
            TonalButtonTokensKeys.ContainerColor);

    public static MaterialValueKey<IImmutableBrush> TonalDisabledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Tonal.DisabledContainerBrush",
            TonalButtonTokensKeys.DisabledContainerColor,
            TonalButtonTokensKeys.DisabledContainerOpacity);

    public static MaterialValueKey<IImmutableBrush> ElevatedContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Elevated.ContainerBrush",
            ElevatedButtonTokensKeys.ContainerColor);

    public static MaterialValueKey<IImmutableBrush> ElevatedDisabledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Elevated.DisabledContainerBrush",
            ElevatedButtonTokensKeys.DisabledContainerColor,
            ElevatedButtonTokensKeys.DisabledContainerOpacity);

    public static MaterialValueKey<IImmutableBrush> OutlinedContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Outlined.ContainerBrush",
            Colors.Transparent);

    public static MaterialValueKey<IImmutableBrush> OutlinedDisabledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Outlined.DisabledContainerBrush",
            Colors.Transparent);

    public static MaterialValueKey<IImmutableBrush> TextContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Text.ContainerBrush",
            Colors.Transparent);

    public static MaterialValueKey<IImmutableBrush> TextDisabledContainerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Button.Text.DisabledContainerBrush",
            Colors.Transparent);

    public static MaterialValueKey<double> ExtraSmallContainerHeight { get; } =
        MaterialValue.FromRaw(
            "Value.Button.ExtraSmall.ContainerHeight",
            ButtonXSmallTokensKeys.ContainerHeight);

    public static MaterialValueKey<double> SmallContainerHeight { get; } =
        MaterialValue.FromRaw(
            "Value.Button.Small.ContainerHeight",
            ButtonSmallTokensKeys.ContainerHeight);

    public static MaterialValueKey<double> MediumContainerHeight { get; } =
        MaterialValue.FromRaw(
            "Value.Button.Medium.ContainerHeight",
            ButtonMediumTokensKeys.ContainerHeight);

    public static MaterialValueKey<double> LargeContainerHeight { get; } =
        MaterialValue.FromRaw(
            "Value.Button.Large.ContainerHeight",
            ButtonLargeTokensKeys.ContainerHeight);

    public static MaterialValueKey<double> ExtraLargeContainerHeight { get; } =
        MaterialValue.FromRaw(
            "Value.Button.ExtraLarge.ContainerHeight",
            ButtonXLargeTokensKeys.ContainerHeight);

    public static MaterialValueKey<Thickness> ExtraSmallContentPadding { get; } =
        MaterialValue.Thickness(
            "Value.Button.ExtraSmall.ContentPadding",
            left: 12,
            top: 6,
            right: 12,
            bottom: 6);

    public static MaterialValueKey<Thickness> SmallContentPadding { get; } =
        MaterialValue.Thickness(
            "Value.Button.Small.ContentPadding",
            ButtonSmallTokensKeys.LeadingSpace,
            vertical: 10,
            ButtonSmallTokensKeys.TrailingSpace);

    public static MaterialValueKey<Thickness> MediumContentPadding { get; } =
        MaterialValue.Thickness(
            "Value.Button.Medium.ContentPadding",
            ButtonMediumTokensKeys.LeadingSpace,
            vertical: 16,
            ButtonMediumTokensKeys.TrailingSpace);

    public static MaterialValueKey<Thickness> LargeContentPadding { get; } =
        MaterialValue.Thickness(
            "Value.Button.Large.ContentPadding",
            ButtonLargeTokensKeys.LeadingSpace,
            vertical: 32,
            ButtonLargeTokensKeys.TrailingSpace);

    public static MaterialValueKey<Thickness> ExtraLargeContentPadding { get; } =
        MaterialValue.Thickness(
            "Value.Button.ExtraLarge.ContentPadding",
            ButtonXLargeTokensKeys.LeadingSpace,
            vertical: 48,
            ButtonXLargeTokensKeys.TrailingSpace);

    public static MaterialValueKey<TypographyRole> ExtraSmallTextStyle { get; } =
        TextStyle("Value.Button.ExtraSmall.TextStyle", TypographyRole.LabelLarge);

    public static MaterialValueKey<TypographyRole> SmallTextStyle { get; } =
        TextStyle("Value.Button.Small.TextStyle", TypographyRole.LabelLarge);

    public static MaterialValueKey<TypographyRole> MediumTextStyle { get; } =
        TextStyle("Value.Button.Medium.TextStyle", TypographyRole.TitleMedium);

    public static MaterialValueKey<TypographyRole> LargeTextStyle { get; } =
        TextStyle("Value.Button.Large.TextStyle", TypographyRole.HeadlineSmall);

    public static MaterialValueKey<TypographyRole> ExtraLargeTextStyle { get; } =
        TextStyle("Value.Button.ExtraLarge.TextStyle", TypographyRole.HeadlineLarge);

    public static MaterialValueKey<ButtonShapeProfile> ExtraSmallShapeProfile { get; } =
        ShapeProfile(
            "Value.Button.ExtraSmall.ShapeProfile",
            ButtonXSmallTokensKeys.ContainerShapeRound,
            ButtonXSmallTokensKeys.PressedContainerShape,
            ButtonXSmallTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ButtonShapeProfile> SmallShapeProfile { get; } =
        ShapeProfile(
            "Value.Button.Small.ShapeProfile",
            ButtonSmallTokensKeys.ContainerShapeRound,
            ButtonSmallTokensKeys.PressedContainerShape,
            ButtonSmallTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ButtonShapeProfile> MediumShapeProfile { get; } =
        ShapeProfile(
            "Value.Button.Medium.ShapeProfile",
            ButtonMediumTokensKeys.ContainerShapeRound,
            ButtonMediumTokensKeys.PressedContainerShape,
            ButtonMediumTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ButtonShapeProfile> LargeShapeProfile { get; } =
        ShapeProfile(
            "Value.Button.Large.ShapeProfile",
            ButtonLargeTokensKeys.ContainerShapeRound,
            ButtonLargeTokensKeys.PressedContainerShape,
            ButtonLargeTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ButtonShapeProfile> ExtraLargeShapeProfile { get; } =
        ShapeProfile(
            "Value.Button.ExtraLarge.ShapeProfile",
            ButtonXLargeTokensKeys.ContainerShapeRound,
            ButtonXLargeTokensKeys.PressedContainerShape,
            ButtonXLargeTokensKeys.ContainerShapeSquare);

    private static MaterialValueKey<ButtonShapeProfile> ShapeProfile(
        string path,
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> container,
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> pressed,
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> square) =>
        new(path, new RawRecipePlan<ButtonShapeProfile>(
            new ButtonShapeProfileRecipe(container, pressed, square)));

    private static MaterialValueKey<ButtonVisualColors> VisualColors(
        string path,
        MaterialToken<Color> content,
        MaterialToken<Color>? outline,
        MaterialToken<Color> disabledContent,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<Color>? disabledOutline) =>
        new(path, new RawRecipePlan<ButtonVisualColors>(
            new ButtonVisualColorsRecipe(
                content,
                outline,
                disabledContent,
                disabledContentOpacity,
                disabledOutline)));

    private static MaterialValueKey<ButtonElevationProfile> Elevation(
        string path,
        MaterialToken<double> resting,
        MaterialToken<double> hovered,
        MaterialToken<double> focused,
        MaterialToken<double> pressed,
        MaterialToken<double> disabled) =>
        new(path, new RawRecipePlan<ButtonElevationProfile>(
            new ButtonElevationProfileRecipe(resting, hovered, focused, pressed, disabled)));

    private static MaterialValueKey<ButtonElevationProfile> ConstantElevation(string path) =>
        new(path, new RawRecipePlan<ButtonElevationProfile>(
            new ConstantButtonElevationProfileRecipe()));

    private static MaterialValueKey<TypographyRole> TextStyle(
        string path,
        TypographyRole role) =>
        new(path, new RawRecipePlan<TypographyRole>(
            new ButtonTypographyRoleRecipe(role)));
}

/// <summary>
/// Variant-dependent colour inputs that remain after the container brush is selected separately.
/// </summary>
public sealed record ButtonVisualColors(
    IImmutableBrush Content,
    IImmutableBrush Outline,
    IImmutableBrush StateLayer,
    IImmutableBrush DisabledContent,
    IImmutableBrush DisabledOutline);

/// <summary>Interaction-independent elevation inputs consumed by the Button presenter.</summary>
public sealed record ButtonElevationProfile(
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

internal sealed class ButtonVisualColorsRecipe : IRawValueRecipe<ButtonVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _content;
    private readonly MaterialToken<Color>? _outline;
    private readonly MaterialToken<Color> _disabledContent;
    private readonly MaterialToken<double> _disabledContentOpacity;
    private readonly MaterialToken<Color>? _disabledOutline;

    internal ButtonVisualColorsRecipe(
        MaterialToken<Color> content,
        MaterialToken<Color>? outline,
        MaterialToken<Color> disabledContent,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<Color>? disabledOutline)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _outline = outline;
        _disabledContent = disabledContent ??
            throw new ArgumentNullException(nameof(disabledContent));
        _disabledContentOpacity = disabledContentOpacity ??
            throw new ArgumentNullException(nameof(disabledContentOpacity));
        _disabledOutline = disabledOutline;

        var dependencies = new List<MaterialToken>
        {
            content,
            disabledContent,
            disabledContentOpacity,
        };
        if (outline is not null)
            dependencies.Add(outline);
        if (disabledOutline is not null)
            dependencies.Add(disabledOutline);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ButtonVisualColors Evaluate(MaterialValueContext context)
    {
        var content = Brush(context.Get(_content));
        return new ButtonVisualColors(
            content,
            _outline is null ? Transparent : Brush(context.Get(_outline)),
            content,
            Brush(context.Get(_disabledContent), context.Get(_disabledContentOpacity)),
            _disabledOutline is null ? Transparent : Brush(context.Get(_disabledOutline)));
    }

    private static IImmutableBrush Brush(Color color, double opacity = 1)
    {
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
}

internal sealed class ButtonElevationProfileRecipe : IRawValueRecipe<ButtonElevationProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _resting;
    private readonly MaterialToken<double> _hovered;
    private readonly MaterialToken<double> _focused;
    private readonly MaterialToken<double> _pressed;
    private readonly MaterialToken<double> _disabled;

    internal ButtonElevationProfileRecipe(
        MaterialToken<double> resting,
        MaterialToken<double> hovered,
        MaterialToken<double> focused,
        MaterialToken<double> pressed,
        MaterialToken<double> disabled)
    {
        _resting = resting ?? throw new ArgumentNullException(nameof(resting));
        _hovered = hovered ?? throw new ArgumentNullException(nameof(hovered));
        _focused = focused ?? throw new ArgumentNullException(nameof(focused));
        _pressed = pressed ?? throw new ArgumentNullException(nameof(pressed));
        _disabled = disabled ?? throw new ArgumentNullException(nameof(disabled));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [resting, hovered, focused, pressed, disabled]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ButtonElevationProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_resting),
        context.Get(_hovered),
        context.Get(_focused),
        context.Get(_pressed),
        context.Get(_disabled));
}

internal sealed class ConstantButtonElevationProfileRecipe : IRawValueRecipe<ButtonElevationProfile>
{
    private static readonly IReadOnlyList<MaterialToken> NoDependencies =
        Array.Empty<MaterialToken>();
    private static readonly ButtonElevationProfile Value = new(0, 0, 0, 0, 0);

    public IReadOnlyList<MaterialToken> Dependencies => NoDependencies;

    public ButtonElevationProfile Evaluate(MaterialValueContext context) => Value;
}

internal sealed class ButtonTypographyRoleRecipe : IRawValueRecipe<TypographyRole>
{
    private static readonly IReadOnlyList<MaterialDesignTokens.Avalonia.MaterialToken>
        NoDependencies = Array.Empty<MaterialDesignTokens.Avalonia.MaterialToken>();
    private readonly TypographyRole _role;

    internal ButtonTypographyRoleRecipe(TypographyRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, null);
        _role = role;
    }

    public IReadOnlyList<MaterialDesignTokens.Avalonia.MaterialToken> Dependencies =>
        NoDependencies;

    public TypographyRole Evaluate(MaterialValueContext context) => _role;
}

/// <summary>The three semantic shapes atomically consumed by a Button presenter.</summary>
public sealed record ButtonShapeProfile(
    ShapeRole Container,
    ShapeRole Pressed,
    ShapeRole Square);

internal sealed class ButtonShapeProfileRecipe : IRawValueRecipe<ButtonShapeProfile>
{
    private readonly IReadOnlyList<MaterialDesignTokens.Avalonia.MaterialToken> _dependencies;
    private readonly MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> _container;
    private readonly MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> _pressed;
    private readonly MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> _square;

    internal ButtonShapeProfileRecipe(
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> container,
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> pressed,
        MaterialDesignTokens.Avalonia.MaterialToken<ShapeRole> square)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _pressed = pressed ?? throw new ArgumentNullException(nameof(pressed));
        _square = square ?? throw new ArgumentNullException(nameof(square));
        _dependencies = Array.AsReadOnly<MaterialDesignTokens.Avalonia.MaterialToken>(
            [container, pressed, square]);
    }

    public IReadOnlyList<MaterialDesignTokens.Avalonia.MaterialToken> Dependencies => _dependencies;

    public ButtonShapeProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_container),
        context.Get(_pressed),
        context.Get(_square));
}
