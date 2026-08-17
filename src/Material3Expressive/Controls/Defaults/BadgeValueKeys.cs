using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material values used by <see cref="MaterialBadge" />.
/// </summary>
/// <remarks>
/// Badge variant selection belongs to the control theme.  The endpoints below only project the
/// Raw facts needed by the badge layout, typography and renderer; <see cref="MaterialBadge.Count" />
/// and the late shape calculation remain control policy.
/// </remarks>
public static class BadgeValueKeys
{
    /// <summary>Gets the geometry profile for a dot-only badge.</summary>
    public static MaterialValueKey<BadgeGeometryProfile> SmallGeometry { get; } =
        Geometry(
            "Value.Badge.Small.Geometry",
            BadgeTokensKeys.Size,
            BadgeTokensKeys.Shape,
            BadgeTokensKeys.LargeLabelTextFont,
            horizontalSpace: 0);

    /// <summary>Gets the geometry profile for a badge which can display a count.</summary>
    public static MaterialValueKey<BadgeGeometryProfile> LargeGeometry { get; } =
        Geometry(
            "Value.Badge.Large.Geometry",
            BadgeTokensKeys.LargeSize,
            BadgeTokensKeys.LargeShape,
            BadgeTokensKeys.LargeLabelTextFont,
            horizontalSpace: 4);

    /// <summary>Gets the small badge's container and label brushes.</summary>
    public static MaterialValueKey<BadgeVisualColors> SmallVisualColors { get; } =
        VisualColors(
            "Value.Badge.Small.VisualColors",
            BadgeTokensKeys.Color,
            BadgeTokensKeys.LargeLabelTextColor);

    /// <summary>Gets the large badge's container and label brushes.</summary>
    public static MaterialValueKey<BadgeVisualColors> LargeVisualColors { get; } =
        VisualColors(
            "Value.Badge.Large.VisualColors",
            BadgeTokensKeys.LargeColor,
            BadgeTokensKeys.LargeLabelTextColor);

    private static MaterialValueKey<BadgeGeometryProfile> Geometry(
        string path,
        MaterialToken<double> size,
        MaterialToken<ShapeRole> shape,
        MaterialToken<TypographyRole> labelTextStyle,
        double horizontalSpace) =>
        new(
            path,
            new RawRecipePlan<BadgeGeometryProfile>(
                new BadgeGeometryProfileRecipe(
                    size,
                    shape,
                    labelTextStyle,
                    horizontalSpace)));

    private static MaterialValueKey<BadgeVisualColors> VisualColors(
        string path,
        MaterialToken<Color> containerColor,
        MaterialToken<Color> labelTextColor) =>
        new(
            path,
            new RawRecipePlan<BadgeVisualColors>(
                new BadgeVisualColorsRecipe(containerColor, labelTextColor)));
}

/// <summary>Layout and late-shape inputs consumed by a <see cref="MaterialBadge" />.</summary>
public sealed record BadgeGeometryProfile(
    double Size,
    ShapeRole ContainerShape,
    Thickness ContentPadding,
    TypographyRole LabelTextStyle);

/// <summary>State-independent brushes consumed by a <see cref="MaterialBadge" />.</summary>
public sealed record BadgeVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush LabelTextColor);

internal sealed class BadgeGeometryProfileRecipe : IRawValueRecipe<BadgeGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _size;
    private readonly MaterialToken<ShapeRole> _shape;
    private readonly MaterialToken<TypographyRole> _labelTextStyle;
    private readonly double _horizontalSpace;

    internal BadgeGeometryProfileRecipe(
        MaterialToken<double> size,
        MaterialToken<ShapeRole> shape,
        MaterialToken<TypographyRole> labelTextStyle,
        double horizontalSpace)
    {
        _size = size ?? throw new ArgumentNullException(nameof(size));
        _shape = shape ?? throw new ArgumentNullException(nameof(shape));
        _labelTextStyle = labelTextStyle ?? throw new ArgumentNullException(nameof(labelTextStyle));
        if (!double.IsFinite(horizontalSpace) || horizontalSpace < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizontalSpace),
                horizontalSpace,
                "Badge horizontal space must be finite and non-negative.");
        }

        _horizontalSpace = horizontalSpace;
        _dependencies = Array.AsReadOnly<MaterialToken>([size, shape, labelTextStyle]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public BadgeGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_size),
        context.Get(_shape),
        new Thickness(_horizontalSpace, 0),
        context.Get(_labelTextStyle));
}

internal sealed class BadgeVisualColorsRecipe : IRawValueRecipe<BadgeVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _containerColor;
    private readonly MaterialToken<Color> _labelTextColor;

    internal BadgeVisualColorsRecipe(
        MaterialToken<Color> containerColor,
        MaterialToken<Color> labelTextColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _labelTextColor = labelTextColor ?? throw new ArgumentNullException(nameof(labelTextColor));
        _dependencies = Array.AsReadOnly<MaterialToken>([containerColor, labelTextColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public BadgeVisualColors Evaluate(MaterialValueContext context) => new(
        new ImmutableSolidColorBrush(context.Get(_containerColor)),
        new ImmutableSolidColorBrush(context.Get(_labelTextColor)));
}
