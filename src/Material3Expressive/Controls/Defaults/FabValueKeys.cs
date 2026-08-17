using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by <see cref="MaterialFab" />.
/// </summary>
/// <remarks>
/// Variant and size selection belongs to the FAB control theme. The plans below only evaluate
/// their declared Raw dependencies; interaction state, bounds, shape geometry and motion remain
/// late control policies. In particular, this catalog does not inspect a requesting control.
/// </remarks>
public static class FabValueKeys
{
    /// <summary>Gets the primary-container FAB visual profile.</summary>
    public static MaterialValueKey<FabVisualColors> PrimaryVisualColors { get; } =
        VisualColors(
            "Value.Fab.Primary.VisualColors",
            FabPrimaryContainerTokensKeys.ContainerColor,
            FabPrimaryContainerTokensKeys.IconColor);

    /// <summary>Gets the secondary-container FAB visual profile.</summary>
    public static MaterialValueKey<FabVisualColors> SecondaryVisualColors { get; } =
        VisualColors(
            "Value.Fab.Secondary.VisualColors",
            FabSecondaryContainerTokensKeys.ContainerColor,
            FabSecondaryContainerTokensKeys.IconColor);

    /// <summary>Gets the tertiary-container FAB visual profile.</summary>
    public static MaterialValueKey<FabVisualColors> TertiaryVisualColors { get; } =
        VisualColors(
            "Value.Fab.Tertiary.VisualColors",
            SystemColorTokens.TertiaryContainer,
            SystemColorTokens.OnTertiaryContainer);

    /// <summary>Gets the surface FAB visual profile.</summary>
    public static MaterialValueKey<FabVisualColors> SurfaceVisualColors { get; } =
        VisualColors(
            "Value.Fab.Surface.VisualColors",
            SystemColorTokens.SurfaceContainerHigh,
            SystemColorTokens.Primary);

    /// <summary>Gets the primary FAB interaction-elevation profile.</summary>
    public static MaterialValueKey<FabElevationProfile> PrimaryElevation { get; } =
        Elevation(
            "Value.Fab.Primary.Elevation",
            FabPrimaryContainerTokensKeys.ContainerElevation,
            FabPrimaryContainerTokensKeys.HoveredContainerElevation,
            FabPrimaryContainerTokensKeys.FocusedContainerElevation,
            FabPrimaryContainerTokensKeys.PressedContainerElevation,
            FabPrimaryContainerTokensKeys.LoweredContainerElevation);

    /// <summary>Gets the secondary FAB interaction-elevation profile.</summary>
    public static MaterialValueKey<FabElevationProfile> SecondaryElevation { get; } =
        Elevation(
            "Value.Fab.Secondary.Elevation",
            FabSecondaryContainerTokensKeys.ContainerElevation,
            FabSecondaryContainerTokensKeys.HoveredContainerElevation,
            FabSecondaryContainerTokensKeys.FocusedContainerElevation,
            FabSecondaryContainerTokensKeys.PressedContainerElevation,
            FabSecondaryContainerTokensKeys.LoweredContainerElevation);

    /// <summary>Gets the tertiary FAB interaction-elevation profile.</summary>
    /// <remarks>The current Material policy reuses the primary elevation Raw table.</remarks>
    public static MaterialValueKey<FabElevationProfile> TertiaryElevation { get; } =
        Elevation(
            "Value.Fab.Tertiary.Elevation",
            FabPrimaryContainerTokensKeys.ContainerElevation,
            FabPrimaryContainerTokensKeys.HoveredContainerElevation,
            FabPrimaryContainerTokensKeys.FocusedContainerElevation,
            FabPrimaryContainerTokensKeys.PressedContainerElevation,
            FabPrimaryContainerTokensKeys.LoweredContainerElevation);

    /// <summary>Gets the surface FAB interaction-elevation profile.</summary>
    /// <remarks>The current Material policy reuses the primary elevation Raw table.</remarks>
    public static MaterialValueKey<FabElevationProfile> SurfaceElevation { get; } =
        Elevation(
            "Value.Fab.Surface.Elevation",
            FabPrimaryContainerTokensKeys.ContainerElevation,
            FabPrimaryContainerTokensKeys.HoveredContainerElevation,
            FabPrimaryContainerTokensKeys.FocusedContainerElevation,
            FabPrimaryContainerTokensKeys.PressedContainerElevation,
            FabPrimaryContainerTokensKeys.LoweredContainerElevation);

    /// <summary>Gets the small (56dp) FAB geometry profile.</summary>
    public static MaterialValueKey<FabGeometryProfile> SmallGeometry { get; } =
        Geometry(
            "Value.Fab.Small.Geometry",
            FabBaselineTokensKeys.ContainerHeight,
            FabBaselineTokensKeys.ContainerWidth,
            FabBaselineTokensKeys.ContainerShape,
            ShapeRole.CornerMedium,
            FabBaselineTokensKeys.IconSize,
            labelSpace: 16,
            iconLabelSpace: 12,
            TypographyRole.LabelLarge);

    /// <summary>Gets the medium (80dp) FAB geometry profile.</summary>
    public static MaterialValueKey<FabGeometryProfile> MediumGeometry { get; } =
        Geometry(
            "Value.Fab.Medium.Geometry",
            FabMediumTokensKeys.ContainerHeight,
            FabMediumTokensKeys.ContainerWidth,
            FabMediumTokensKeys.ContainerShape,
            ShapeRole.CornerLarge,
            FabMediumTokensKeys.IconSize,
            labelSpace: 24,
            iconLabelSpace: 16,
            TypographyRole.TitleMedium);

    /// <summary>Gets the large (96dp) FAB geometry profile.</summary>
    public static MaterialValueKey<FabGeometryProfile> LargeGeometry { get; } =
        Geometry(
            "Value.Fab.Large.Geometry",
            FabLargeTokensKeys.ContainerHeight,
            FabLargeTokensKeys.ContainerWidth,
            FabLargeTokensKeys.ContainerShape,
            ShapeRole.CornerLarge,
            FabLargeTokensKeys.IconSize,
            labelSpace: 28,
            iconLabelSpace: 20,
            TypographyRole.HeadlineSmall);

    private static MaterialValueKey<FabVisualColors> VisualColors(
        string path,
        MaterialToken<Color> containerColor,
        MaterialToken<Color> contentColor) =>
        new(
            path,
            new RawRecipePlan<FabVisualColors>(
                new FabVisualColorsRecipe(
                    containerColor,
                    contentColor,
                    SystemColorTokens.Shadow)));

    private static MaterialValueKey<FabElevationProfile> Elevation(
        string path,
        MaterialToken<double> resting,
        MaterialToken<double> hovered,
        MaterialToken<double> focused,
        MaterialToken<double> pressed,
        MaterialToken<double> lowered) =>
        new(
            path,
            new RawRecipePlan<FabElevationProfile>(
                new FabElevationProfileRecipe(resting, hovered, focused, pressed, lowered)));

    private static MaterialValueKey<FabGeometryProfile> Geometry(
        string path,
        MaterialToken<double> containerHeight,
        MaterialToken<double> containerWidth,
        MaterialToken<ShapeRole> containerShape,
        ShapeRole pressedContainerShape,
        MaterialToken<double> iconSize,
        double labelSpace,
        double iconLabelSpace,
        TypographyRole labelTextStyle) =>
        new(
            path,
            new RawRecipePlan<FabGeometryProfile>(
                new FabGeometryProfileRecipe(
                    containerHeight,
                    containerWidth,
                    containerShape,
                    pressedContainerShape,
                    iconSize,
                    labelSpace,
                    iconLabelSpace,
                    labelTextStyle)));
}

/// <summary>Immutable visual inputs consumed by the FAB renderer and template.</summary>
public sealed record FabVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ContentColor,
    IImmutableBrush StateLayerColor,
    Color ShadowColor);

/// <summary>Interaction-independent elevation inputs consumed by the FAB renderer.</summary>
public sealed record FabElevationProfile(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Lowered)
{
    /// <summary>Returns the elevation selected by the late control interaction policy.</summary>
    public double For(bool isLowered, InteractionState interaction)
    {
        if (isLowered)
            return Lowered;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}

/// <summary>Size-dependent semantic geometry and content inputs consumed by a FAB.</summary>
public sealed record FabGeometryProfile(
    double ContainerHeight,
    double ContainerWidth,
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    double IconSize,
    double LabelSpace,
    double IconLabelSpace,
    TypographyRole LabelTextStyle);

internal sealed class FabVisualColorsRecipe : IRawValueRecipe<FabVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _containerColor;
    private readonly MaterialToken<Color> _contentColor;
    private readonly MaterialToken<Color> _shadowColor;

    internal FabVisualColorsRecipe(
        MaterialToken<Color> containerColor,
        MaterialToken<Color> contentColor,
        MaterialToken<Color> shadowColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _contentColor = contentColor ?? throw new ArgumentNullException(nameof(contentColor));
        _shadowColor = shadowColor ?? throw new ArgumentNullException(nameof(shadowColor));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerColor, contentColor, shadowColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public FabVisualColors Evaluate(MaterialValueContext context)
    {
        var container = Brush(context.Get(_containerColor));
        var content = Brush(context.Get(_contentColor));
        return new(container, content, content, context.Get(_shadowColor));
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}

internal sealed class FabElevationProfileRecipe : IRawValueRecipe<FabElevationProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _resting;
    private readonly MaterialToken<double> _hovered;
    private readonly MaterialToken<double> _focused;
    private readonly MaterialToken<double> _pressed;
    private readonly MaterialToken<double> _lowered;

    internal FabElevationProfileRecipe(
        MaterialToken<double> resting,
        MaterialToken<double> hovered,
        MaterialToken<double> focused,
        MaterialToken<double> pressed,
        MaterialToken<double> lowered)
    {
        _resting = resting ?? throw new ArgumentNullException(nameof(resting));
        _hovered = hovered ?? throw new ArgumentNullException(nameof(hovered));
        _focused = focused ?? throw new ArgumentNullException(nameof(focused));
        _pressed = pressed ?? throw new ArgumentNullException(nameof(pressed));
        _lowered = lowered ?? throw new ArgumentNullException(nameof(lowered));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [resting, hovered, focused, pressed, lowered]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public FabElevationProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_resting),
        context.Get(_hovered),
        context.Get(_focused),
        context.Get(_pressed),
        context.Get(_lowered));
}

internal sealed class FabGeometryProfileRecipe : IRawValueRecipe<FabGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double> _containerWidth;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly ShapeRole _pressedContainerShape;
    private readonly MaterialToken<double> _iconSize;
    private readonly double _labelSpace;
    private readonly double _iconLabelSpace;
    private readonly TypographyRole _labelTextStyle;

    internal FabGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<double> containerWidth,
        MaterialToken<ShapeRole> containerShape,
        ShapeRole pressedContainerShape,
        MaterialToken<double> iconSize,
        double labelSpace,
        double iconLabelSpace,
        TypographyRole labelTextStyle)
    {
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _containerWidth = containerWidth ?? throw new ArgumentNullException(nameof(containerWidth));
        _containerShape = containerShape ?? throw new ArgumentNullException(nameof(containerShape));
        _pressedContainerShape = pressedContainerShape;
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        if (!double.IsFinite(labelSpace))
            throw new ArgumentOutOfRangeException(nameof(labelSpace), "The label space must be finite.");
        if (!double.IsFinite(iconLabelSpace))
            throw new ArgumentOutOfRangeException(
                nameof(iconLabelSpace),
                "The icon-label space must be finite.");
        _labelSpace = labelSpace;
        _iconLabelSpace = iconLabelSpace;
        _labelTextStyle = labelTextStyle;
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerHeight, containerWidth, containerShape, iconSize]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public FabGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_containerHeight),
        context.Get(_containerWidth),
        context.Get(_containerShape),
        _pressedContainerShape,
        context.Get(_iconSize),
        _labelSpace,
        _iconLabelSpace,
        _labelTextStyle);
}
