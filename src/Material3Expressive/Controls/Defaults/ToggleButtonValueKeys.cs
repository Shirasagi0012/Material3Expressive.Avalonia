using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Consumer-ready Material value endpoints for native ToggleButton themes.</summary>
/// <remarks>
/// Variant and size selection remain ControlTheme policy. Every plan is a bounded projection of
/// Raw tokens and does not inspect the requesting control or resolve another Material value.
/// </remarks>
public static class ToggleButtonValueKeys
{
    public static MaterialValueKey<ToggleButtonVisualColors> FilledVisualColors { get; } =
        VisualColors(
            "Value.ToggleButton.Filled.VisualColors",
            FilledButtonTokensKeys.UnselectedContainerColor,
            FilledButtonTokensKeys.LabelTextUnselectedColor,
            FilledButtonTokensKeys.SelectedContainerColor,
            FilledButtonTokensKeys.LabelTextSelectedColor,
            FilledButtonTokensKeys.DisabledContainerColor,
            FilledButtonTokensKeys.DisabledContainerOpacity,
            FilledButtonTokensKeys.DisabledLabelTextColor,
            FilledButtonTokensKeys.DisabledLabelTextOpacity);

    public static MaterialValueKey<ToggleButtonVisualColors> TonalVisualColors { get; } =
        VisualColors(
            "Value.ToggleButton.Tonal.VisualColors",
            TonalButtonTokensKeys.UnselectedContainerColor,
            TonalButtonTokensKeys.UnselectedLabelTextColor,
            TonalButtonTokensKeys.SelectedContainerColor,
            TonalButtonTokensKeys.SelectedLabelTextColor,
            TonalButtonTokensKeys.DisabledContainerColor,
            TonalButtonTokensKeys.DisabledContainerOpacity,
            TonalButtonTokensKeys.DisabledLabelTextColor,
            TonalButtonTokensKeys.DisabledLabelTextOpacity);

    public static MaterialValueKey<ToggleButtonVisualColors> ElevatedVisualColors { get; } =
        VisualColors(
            "Value.ToggleButton.Elevated.VisualColors",
            ElevatedButtonTokensKeys.UnselectedContainerColor,
            ElevatedButtonTokensKeys.LabelTextUnselectedColor,
            ElevatedButtonTokensKeys.SelectedContainerColor,
            ElevatedButtonTokensKeys.LabelTextSelectedColor,
            ElevatedButtonTokensKeys.DisabledContainerColor,
            ElevatedButtonTokensKeys.DisabledContainerOpacity,
            ElevatedButtonTokensKeys.DisabledLabelTextColor,
            ElevatedButtonTokensKeys.DisabledLabelTextOpacity);

    public static MaterialValueKey<ToggleButtonVisualColors> OutlinedVisualColors { get; } =
        VisualColors(
            "Value.ToggleButton.Outlined.VisualColors",
            uncheckedContainer: null,
            OutlinedButtonTokensKeys.UnselectedLabelTextColor,
            OutlinedButtonTokensKeys.SelectedContainerColor,
            OutlinedButtonTokensKeys.SelectedLabelTextColor,
            disabledContainer: null,
            disabledContainerOpacity: null,
            OutlinedButtonTokensKeys.DisabledLabelTextColor,
            OutlinedButtonTokensKeys.DisabledLabelTextOpacity,
            OutlinedButtonTokensKeys.OutlineColor,
            OutlinedButtonTokensKeys.DisabledOutlineColor);

    public static MaterialValueKey<ToggleButtonVisualColors> TextVisualColors { get; } =
        VisualColors(
            "Value.ToggleButton.Text.VisualColors",
            uncheckedContainer: null,
            TextButtonTokensKeys.LabelColor,
            checkedContainer: null,
            TextButtonTokensKeys.LabelColor,
            disabledContainer: null,
            disabledContainerOpacity: null,
            TextButtonTokensKeys.DisabledLabelColor,
            TextButtonTokensKeys.DisabledLabelOpacity);

    public static MaterialValueKey<ToggleButtonGeometryProfile> ExtraSmallGeometry { get; } =
        Geometry(
            "Value.ToggleButton.ExtraSmall.Geometry",
            ButtonXSmallTokensKeys.ContainerHeight,
            leading: null,
            leadingValue: 12,
            vertical: 6,
            trailing: null,
            trailingValue: 12,
            ButtonXSmallTokensKeys.OutlinedOutlineWidth,
            TypographyRole.LabelLarge,
            ButtonXSmallTokensKeys.ContainerShapeRound,
            ButtonXSmallTokensKeys.PressedContainerShape,
            ButtonXSmallTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ToggleButtonGeometryProfile> SmallGeometry { get; } =
        Geometry(
            "Value.ToggleButton.Small.Geometry",
            ButtonSmallTokensKeys.ContainerHeight,
            ButtonSmallTokensKeys.LeadingSpace,
            leadingValue: 0,
            vertical: 10,
            ButtonSmallTokensKeys.TrailingSpace,
            trailingValue: 0,
            ButtonSmallTokensKeys.OutlinedOutlineWidth,
            TypographyRole.LabelLarge,
            ButtonSmallTokensKeys.ContainerShapeRound,
            ButtonSmallTokensKeys.PressedContainerShape,
            ButtonSmallTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ToggleButtonGeometryProfile> MediumGeometry { get; } =
        Geometry(
            "Value.ToggleButton.Medium.Geometry",
            ButtonMediumTokensKeys.ContainerHeight,
            ButtonMediumTokensKeys.LeadingSpace,
            leadingValue: 0,
            vertical: 16,
            ButtonMediumTokensKeys.TrailingSpace,
            trailingValue: 0,
            ButtonMediumTokensKeys.OutlinedOutlineWidth,
            TypographyRole.TitleMedium,
            ButtonMediumTokensKeys.ContainerShapeRound,
            ButtonMediumTokensKeys.PressedContainerShape,
            ButtonMediumTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ToggleButtonGeometryProfile> LargeGeometry { get; } =
        Geometry(
            "Value.ToggleButton.Large.Geometry",
            ButtonLargeTokensKeys.ContainerHeight,
            ButtonLargeTokensKeys.LeadingSpace,
            leadingValue: 0,
            vertical: 32,
            ButtonLargeTokensKeys.TrailingSpace,
            trailingValue: 0,
            ButtonLargeTokensKeys.OutlinedOutlineWidth,
            TypographyRole.HeadlineSmall,
            ButtonLargeTokensKeys.ContainerShapeRound,
            ButtonLargeTokensKeys.PressedContainerShape,
            ButtonLargeTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<ToggleButtonGeometryProfile> ExtraLargeGeometry { get; } =
        Geometry(
            "Value.ToggleButton.ExtraLarge.Geometry",
            ButtonXLargeTokensKeys.ContainerHeight,
            ButtonXLargeTokensKeys.LeadingSpace,
            leadingValue: 0,
            vertical: 48,
            ButtonXLargeTokensKeys.TrailingSpace,
            trailingValue: 0,
            ButtonXLargeTokensKeys.OutlinedOutlineWidth,
            TypographyRole.HeadlineLarge,
            ButtonXLargeTokensKeys.ContainerShapeRound,
            ButtonXLargeTokensKeys.PressedContainerShape,
            ButtonXLargeTokensKeys.ContainerShapeSquare);

    public static MaterialValueKey<SpringSpec> ShapeMotion { get; } =
        MaterialValue.FromRaw(
            "Value.ToggleButton.ShapeMotion",
            FoundationThemeRawTokens.Motion.FastSpatial);

    private static MaterialValueKey<ToggleButtonVisualColors> VisualColors(
        string path,
        MaterialToken<Color>? uncheckedContainer,
        MaterialToken<Color> uncheckedContent,
        MaterialToken<Color>? checkedContainer,
        MaterialToken<Color> checkedContent,
        MaterialToken<Color>? disabledContainer,
        MaterialToken<double>? disabledContainerOpacity,
        MaterialToken<Color> disabledContent,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<Color>? outline = null,
        MaterialToken<Color>? disabledOutline = null) =>
        new(
            path,
            new RawRecipePlan<ToggleButtonVisualColors>(
                new ToggleButtonVisualColorsRecipe(
                    uncheckedContainer,
                    uncheckedContent,
                    checkedContainer,
                    checkedContent,
                    disabledContainer,
                    disabledContainerOpacity,
                    disabledContent,
                    disabledContentOpacity,
                    outline,
                    disabledOutline)));

    private static MaterialValueKey<ToggleButtonGeometryProfile> Geometry(
        string path,
        MaterialToken<double> containerHeight,
        MaterialToken<double>? leading,
        double leadingValue,
        double vertical,
        MaterialToken<double>? trailing,
        double trailingValue,
        MaterialToken<double> outlineWidth,
        TypographyRole labelTextStyle,
        MaterialToken<ShapeRole> uncheckedShape,
        MaterialToken<ShapeRole> pressedShape,
        MaterialToken<ShapeRole> checkedShape) =>
        new(
            path,
            new RawRecipePlan<ToggleButtonGeometryProfile>(
                new ToggleButtonGeometryProfileRecipe(
                    containerHeight,
                    leading,
                    leadingValue,
                    vertical,
                    trailing,
                    trailingValue,
                    outlineWidth,
                    labelTextStyle,
                    uncheckedShape,
                    pressedShape,
                    checkedShape)));
}

/// <summary>Immutable state colours selected late by the ToggleButton ControlTheme.</summary>
public sealed record ToggleButtonVisualColors(
    IImmutableBrush UncheckedContainer,
    IImmutableBrush UncheckedContent,
    IImmutableBrush CheckedContainer,
    IImmutableBrush CheckedContent,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledContent,
    IImmutableBrush Outline,
    IImmutableBrush DisabledOutline);

/// <summary>Immutable size and resolved-shape inputs consumed by a ToggleButton theme.</summary>
public sealed record ToggleButtonGeometryProfile(
    double ContainerHeight,
    double ToggleContainerHeight,
    Thickness ContentPadding,
    Thickness OutlineThickness,
    TypographyRole LabelTextStyle,
    RoundedCornerShape UncheckedShape,
    RoundedCornerShape PressedShape,
    RoundedCornerShape CheckedShape);

internal sealed class ToggleButtonVisualColorsRecipe : IRawValueRecipe<ToggleButtonVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color>? _uncheckedContainer;
    private readonly MaterialToken<Color> _uncheckedContent;
    private readonly MaterialToken<Color>? _checkedContainer;
    private readonly MaterialToken<Color> _checkedContent;
    private readonly MaterialToken<Color>? _disabledContainer;
    private readonly MaterialToken<double>? _disabledContainerOpacity;
    private readonly MaterialToken<Color> _disabledContent;
    private readonly MaterialToken<double> _disabledContentOpacity;
    private readonly MaterialToken<Color>? _outline;
    private readonly MaterialToken<Color>? _disabledOutline;

    internal ToggleButtonVisualColorsRecipe(
        MaterialToken<Color>? uncheckedContainer,
        MaterialToken<Color> uncheckedContent,
        MaterialToken<Color>? checkedContainer,
        MaterialToken<Color> checkedContent,
        MaterialToken<Color>? disabledContainer,
        MaterialToken<double>? disabledContainerOpacity,
        MaterialToken<Color> disabledContent,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<Color>? outline,
        MaterialToken<Color>? disabledOutline)
    {
        _uncheckedContainer = uncheckedContainer;
        _uncheckedContent = uncheckedContent ??
            throw new ArgumentNullException(nameof(uncheckedContent));
        _checkedContainer = checkedContainer;
        _checkedContent = checkedContent ?? throw new ArgumentNullException(nameof(checkedContent));
        _disabledContainer = disabledContainer;
        _disabledContainerOpacity = disabledContainerOpacity;
        if ((_disabledContainer is null) != (_disabledContainerOpacity is null))
            throw new ArgumentException(
                "Disabled container colour and opacity must be supplied together.",
                nameof(disabledContainer));
        _disabledContent = disabledContent ??
            throw new ArgumentNullException(nameof(disabledContent));
        _disabledContentOpacity = disabledContentOpacity ??
            throw new ArgumentNullException(nameof(disabledContentOpacity));
        _outline = outline;
        _disabledOutline = disabledOutline;

        var dependencies = new List<MaterialToken>();
        Add(dependencies, uncheckedContainer);
        Add(dependencies, uncheckedContent);
        Add(dependencies, checkedContainer);
        Add(dependencies, checkedContent);
        Add(dependencies, disabledContainer);
        Add(dependencies, disabledContainerOpacity);
        Add(dependencies, disabledContent);
        Add(dependencies, disabledContentOpacity);
        Add(dependencies, outline);
        Add(dependencies, disabledOutline);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ToggleButtonVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _uncheckedContainer),
        Brush(context, _uncheckedContent),
        Brush(context, _checkedContainer),
        Brush(context, _checkedContent),
        Brush(context, _disabledContainer, _disabledContainerOpacity),
        Brush(context, _disabledContent, _disabledContentOpacity),
        Brush(context, _outline),
        Brush(context, _disabledOutline));

    private static void Add(List<MaterialToken> dependencies, MaterialToken? token)
    {
        if (token is not null)
            dependencies.Add(token);
    }

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color>? colorToken,
        MaterialToken<double>? opacityToken = null)
    {
        if (colorToken is null)
            return Transparent;

        var color = context.Get(colorToken);
        if (opacityToken is not null)
        {
            var opacity = Math.Clamp(context.Get(opacityToken), 0d, 1d);
            color = new Color(
                (byte)Math.Round(color.A * opacity),
                color.R,
                color.G,
                color.B);
        }

        return new ImmutableSolidColorBrush(color);
    }
}

internal sealed class ToggleButtonGeometryProfileRecipe
    : IRawValueRecipe<ToggleButtonGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double>? _leading;
    private readonly double _leadingValue;
    private readonly double _vertical;
    private readonly MaterialToken<double>? _trailing;
    private readonly double _trailingValue;
    private readonly MaterialToken<double> _outlineWidth;
    private readonly TypographyRole _labelTextStyle;
    private readonly MaterialToken<ShapeRole> _uncheckedShape;
    private readonly MaterialToken<ShapeRole> _pressedShape;
    private readonly MaterialToken<ShapeRole> _checkedShape;

    internal ToggleButtonGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<double>? leading,
        double leadingValue,
        double vertical,
        MaterialToken<double>? trailing,
        double trailingValue,
        MaterialToken<double> outlineWidth,
        TypographyRole labelTextStyle,
        MaterialToken<ShapeRole> uncheckedShape,
        MaterialToken<ShapeRole> pressedShape,
        MaterialToken<ShapeRole> checkedShape)
    {
        _containerHeight = containerHeight ??
            throw new ArgumentNullException(nameof(containerHeight));
        _leading = leading;
        _leadingValue = leadingValue;
        _vertical = vertical;
        _trailing = trailing;
        _trailingValue = trailingValue;
        _outlineWidth = outlineWidth ?? throw new ArgumentNullException(nameof(outlineWidth));
        _labelTextStyle = labelTextStyle;
        _uncheckedShape = uncheckedShape ?? throw new ArgumentNullException(nameof(uncheckedShape));
        _pressedShape = pressedShape ?? throw new ArgumentNullException(nameof(pressedShape));
        _checkedShape = checkedShape ?? throw new ArgumentNullException(nameof(checkedShape));

        var dependencies = new List<MaterialToken>
        {
            containerHeight,
            outlineWidth,
            uncheckedShape,
            pressedShape,
            checkedShape,
        };
        Add(dependencies, leading);
        Add(dependencies, trailing);
        dependencies.AddRange(FoundationThemeRawTokens.Shape.All);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ToggleButtonGeometryProfile Evaluate(MaterialValueContext context)
    {
        var height = context.Get(_containerHeight);
        var leading = _leading is null ? _leadingValue : context.Get(_leading);
        var trailing = _trailing is null ? _trailingValue : context.Get(_trailing);
        return new(
            height,
            Math.Max(48, height),
            new Thickness(leading, _vertical, trailing, _vertical),
            new Thickness(context.Get(_outlineWidth)),
            _labelTextStyle,
            ShapeRawProjection.Resolve(context.Get(_uncheckedShape), context),
            ShapeRawProjection.Resolve(context.Get(_pressedShape), context),
            ShapeRawProjection.Resolve(context.Get(_checkedShape), context));
    }

    private static void Add(List<MaterialToken> dependencies, MaterialToken? token)
    {
        if (token is not null)
            dependencies.Add(token);
    }
}
