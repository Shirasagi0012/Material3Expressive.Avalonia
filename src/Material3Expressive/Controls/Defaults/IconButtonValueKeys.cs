using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints for the native icon-button family.
/// </summary>
/// <remarks>
/// Variant, size and width are selected by the ControlTheme. Checked/enabled state is selected by
/// the toggle ControlTheme. These plans contain only their declared Raw token dependencies and do
/// not inspect the requesting control, bounds, flow direction or motion state.
/// </remarks>
public static class IconButtonValueKeys
{
    public static MaterialValueKey<IconButtonGeometryProfile> ExtraSmallNarrowGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraSmall.Narrow.Geometry",
            XSmallIconButtonTokensKeys.ContainerHeight,
            XSmallIconButtonTokensKeys.IconSize,
            XSmallIconButtonTokensKeys.NarrowLeadingSpace,
            XSmallIconButtonTokensKeys.OutlinedOutlineWidth,
            XSmallIconButtonTokensKeys.ContainerShapeRound,
            XSmallIconButtonTokensKeys.PressedContainerShape,
            XSmallIconButtonTokensKeys.ContainerShapeSquare,
            XSmallIconButtonTokensKeys.SelectedContainerShapeRound,
            XSmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> ExtraSmallDefaultGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraSmall.Default.Geometry",
            XSmallIconButtonTokensKeys.ContainerHeight,
            XSmallIconButtonTokensKeys.IconSize,
            XSmallIconButtonTokensKeys.DefaultLeadingSpace,
            XSmallIconButtonTokensKeys.OutlinedOutlineWidth,
            XSmallIconButtonTokensKeys.ContainerShapeRound,
            XSmallIconButtonTokensKeys.PressedContainerShape,
            XSmallIconButtonTokensKeys.ContainerShapeSquare,
            XSmallIconButtonTokensKeys.SelectedContainerShapeRound,
            XSmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> ExtraSmallWideGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraSmall.Wide.Geometry",
            XSmallIconButtonTokensKeys.ContainerHeight,
            XSmallIconButtonTokensKeys.IconSize,
            XSmallIconButtonTokensKeys.WideLeadingSpace,
            XSmallIconButtonTokensKeys.OutlinedOutlineWidth,
            XSmallIconButtonTokensKeys.ContainerShapeRound,
            XSmallIconButtonTokensKeys.PressedContainerShape,
            XSmallIconButtonTokensKeys.ContainerShapeSquare,
            XSmallIconButtonTokensKeys.SelectedContainerShapeRound,
            XSmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> SmallNarrowGeometry { get; } =
        Geometry(
            "Value.IconButton.Small.Narrow.Geometry",
            SmallIconButtonTokensKeys.ContainerHeight,
            SmallIconButtonTokensKeys.IconSize,
            SmallIconButtonTokensKeys.NarrowLeadingSpace,
            SmallIconButtonTokensKeys.OutlinedOutlineWidth,
            SmallIconButtonTokensKeys.ContainerShapeRound,
            SmallIconButtonTokensKeys.PressedContainerShape,
            SmallIconButtonTokensKeys.ContainerShapeSquare,
            SmallIconButtonTokensKeys.SelectedContainerShapeRound,
            SmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> SmallDefaultGeometry { get; } =
        Geometry(
            "Value.IconButton.Small.Default.Geometry",
            SmallIconButtonTokensKeys.ContainerHeight,
            SmallIconButtonTokensKeys.IconSize,
            SmallIconButtonTokensKeys.DefaultLeadingSpace,
            SmallIconButtonTokensKeys.OutlinedOutlineWidth,
            SmallIconButtonTokensKeys.ContainerShapeRound,
            SmallIconButtonTokensKeys.PressedContainerShape,
            SmallIconButtonTokensKeys.ContainerShapeSquare,
            SmallIconButtonTokensKeys.SelectedContainerShapeRound,
            SmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> SmallWideGeometry { get; } =
        Geometry(
            "Value.IconButton.Small.Wide.Geometry",
            SmallIconButtonTokensKeys.ContainerHeight,
            SmallIconButtonTokensKeys.IconSize,
            SmallIconButtonTokensKeys.WideLeadingSpace,
            SmallIconButtonTokensKeys.OutlinedOutlineWidth,
            SmallIconButtonTokensKeys.ContainerShapeRound,
            SmallIconButtonTokensKeys.PressedContainerShape,
            SmallIconButtonTokensKeys.ContainerShapeSquare,
            SmallIconButtonTokensKeys.SelectedContainerShapeRound,
            SmallIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> MediumNarrowGeometry { get; } =
        Geometry(
            "Value.IconButton.Medium.Narrow.Geometry",
            MediumIconButtonTokensKeys.ContainerHeight,
            MediumIconButtonTokensKeys.IconSize,
            MediumIconButtonTokensKeys.NarrowLeadingSpace,
            MediumIconButtonTokensKeys.OutlinedOutlineWidth,
            MediumIconButtonTokensKeys.ContainerShapeRound,
            MediumIconButtonTokensKeys.PressedContainerShape,
            MediumIconButtonTokensKeys.ContainerShapeSquare,
            MediumIconButtonTokensKeys.SelectedContainerShapeRound,
            MediumIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> MediumDefaultGeometry { get; } =
        Geometry(
            "Value.IconButton.Medium.Default.Geometry",
            MediumIconButtonTokensKeys.ContainerHeight,
            MediumIconButtonTokensKeys.IconSize,
            MediumIconButtonTokensKeys.DefaultLeadingSpace,
            MediumIconButtonTokensKeys.OutlinedOutlineWidth,
            MediumIconButtonTokensKeys.ContainerShapeRound,
            MediumIconButtonTokensKeys.PressedContainerShape,
            MediumIconButtonTokensKeys.ContainerShapeSquare,
            MediumIconButtonTokensKeys.SelectedContainerShapeRound,
            MediumIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> MediumWideGeometry { get; } =
        Geometry(
            "Value.IconButton.Medium.Wide.Geometry",
            MediumIconButtonTokensKeys.ContainerHeight,
            MediumIconButtonTokensKeys.IconSize,
            MediumIconButtonTokensKeys.WideLeadingSpace,
            MediumIconButtonTokensKeys.OutlinedOutlineWidth,
            MediumIconButtonTokensKeys.ContainerShapeRound,
            MediumIconButtonTokensKeys.PressedContainerShape,
            MediumIconButtonTokensKeys.ContainerShapeSquare,
            MediumIconButtonTokensKeys.SelectedContainerShapeRound,
            MediumIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> LargeNarrowGeometry { get; } =
        Geometry(
            "Value.IconButton.Large.Narrow.Geometry",
            LargeIconButtonTokensKeys.ContainerHeight,
            LargeIconButtonTokensKeys.IconSize,
            LargeIconButtonTokensKeys.NarrowLeadingSpace,
            LargeIconButtonTokensKeys.OutlinedOutlineWidth,
            LargeIconButtonTokensKeys.ContainerShapeRound,
            LargeIconButtonTokensKeys.PressedContainerShape,
            LargeIconButtonTokensKeys.ContainerShapeSquare,
            LargeIconButtonTokensKeys.SelectedContainerShapeRound,
            LargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> LargeDefaultGeometry { get; } =
        Geometry(
            "Value.IconButton.Large.Default.Geometry",
            LargeIconButtonTokensKeys.ContainerHeight,
            LargeIconButtonTokensKeys.IconSize,
            LargeIconButtonTokensKeys.DefaultLeadingSpace,
            LargeIconButtonTokensKeys.OutlinedOutlineWidth,
            LargeIconButtonTokensKeys.ContainerShapeRound,
            LargeIconButtonTokensKeys.PressedContainerShape,
            LargeIconButtonTokensKeys.ContainerShapeSquare,
            LargeIconButtonTokensKeys.SelectedContainerShapeRound,
            LargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> LargeWideGeometry { get; } =
        Geometry(
            "Value.IconButton.Large.Wide.Geometry",
            LargeIconButtonTokensKeys.ContainerHeight,
            LargeIconButtonTokensKeys.IconSize,
            LargeIconButtonTokensKeys.WideLeadingSpace,
            LargeIconButtonTokensKeys.OutlinedOutlineWidth,
            LargeIconButtonTokensKeys.ContainerShapeRound,
            LargeIconButtonTokensKeys.PressedContainerShape,
            LargeIconButtonTokensKeys.ContainerShapeSquare,
            LargeIconButtonTokensKeys.SelectedContainerShapeRound,
            LargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> ExtraLargeNarrowGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraLarge.Narrow.Geometry",
            XLargeIconButtonTokensKeys.ContainerHeight,
            XLargeIconButtonTokensKeys.IconSize,
            XLargeIconButtonTokensKeys.NarrowLeadingSpace,
            XLargeIconButtonTokensKeys.OutlinedOutlineWidth,
            XLargeIconButtonTokensKeys.ContainerShapeRound,
            XLargeIconButtonTokensKeys.PressedContainerShape,
            XLargeIconButtonTokensKeys.ContainerShapeSquare,
            XLargeIconButtonTokensKeys.SelectedContainerShapeRound,
            XLargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> ExtraLargeDefaultGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraLarge.Default.Geometry",
            XLargeIconButtonTokensKeys.ContainerHeight,
            XLargeIconButtonTokensKeys.IconSize,
            XLargeIconButtonTokensKeys.DefaultLeadingSpace,
            XLargeIconButtonTokensKeys.OutlinedOutlineWidth,
            XLargeIconButtonTokensKeys.ContainerShapeRound,
            XLargeIconButtonTokensKeys.PressedContainerShape,
            XLargeIconButtonTokensKeys.ContainerShapeSquare,
            XLargeIconButtonTokensKeys.SelectedContainerShapeRound,
            XLargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonGeometryProfile> ExtraLargeWideGeometry { get; } =
        Geometry(
            "Value.IconButton.ExtraLarge.Wide.Geometry",
            XLargeIconButtonTokensKeys.ContainerHeight,
            XLargeIconButtonTokensKeys.IconSize,
            XLargeIconButtonTokensKeys.WideLeadingSpace,
            XLargeIconButtonTokensKeys.OutlinedOutlineWidth,
            XLargeIconButtonTokensKeys.ContainerShapeRound,
            XLargeIconButtonTokensKeys.PressedContainerShape,
            XLargeIconButtonTokensKeys.ContainerShapeSquare,
            XLargeIconButtonTokensKeys.SelectedContainerShapeRound,
            XLargeIconButtonTokensKeys.SelectedContainerShapeSquare);

    public static MaterialValueKey<IconButtonVisualColors> StandardVisualColors { get; } =
        new(
            "Value.IconButton.Standard.VisualColors",
            new RawRecipePlan<IconButtonVisualColors>(
                new IconButtonVisualColorsRecipe(
                    icon: IconButtonTokensKeys.Color,
                    disabledIcon: IconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: IconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<IconButtonVisualColors> FilledVisualColors { get; } =
        new(
            "Value.IconButton.Filled.VisualColors",
            new RawRecipePlan<IconButtonVisualColors>(
                new IconButtonVisualColorsRecipe(
                    container: FilledIconButtonTokensKeys.ContainerColor,
                    icon: FilledIconButtonTokensKeys.Color,
                    disabledContainer: FilledIconButtonTokensKeys.DisabledContainerColor,
                    disabledContainerOpacity: FilledIconButtonTokensKeys.DisabledContainerOpacity,
                    disabledIcon: FilledIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: FilledIconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<IconButtonVisualColors> TonalVisualColors { get; } =
        new(
            "Value.IconButton.Tonal.VisualColors",
            new RawRecipePlan<IconButtonVisualColors>(
                new IconButtonVisualColorsRecipe(
                    container: FilledTonalIconButtonTokensKeys.ContainerColor,
                    icon: FilledTonalIconButtonTokensKeys.Color,
                    disabledContainer: FilledTonalIconButtonTokensKeys.DisabledContainerColor,
                    disabledContainerOpacity: FilledTonalIconButtonTokensKeys.DisabledContainerOpacity,
                    disabledIcon: FilledTonalIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: FilledTonalIconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<IconButtonVisualColors> OutlinedVisualColors { get; } =
        new(
            "Value.IconButton.Outlined.VisualColors",
            new RawRecipePlan<IconButtonVisualColors>(
                new IconButtonVisualColorsRecipe(
                    icon: OutlinedIconButtonTokensKeys.Color,
                    outline: OutlinedIconButtonTokensKeys.OutlineColor,
                    disabledIcon: OutlinedIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: OutlinedIconButtonTokensKeys.DisabledOpacity,
                    disabledOutline: OutlinedIconButtonTokensKeys.DisabledOutlineColor)));

    public static MaterialValueKey<ToggleIconButtonVisualColors> StandardToggleVisualColors { get; } =
        new(
            "Value.IconButton.StandardToggle.VisualColors",
            new RawRecipePlan<ToggleIconButtonVisualColors>(
                new ToggleIconButtonVisualColorsRecipe(
                    uncheckedIcon: IconButtonTokensKeys.UnselectedColor,
                    checkedIcon: IconButtonTokensKeys.SelectedColor,
                    disabledIcon: IconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: IconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<ToggleIconButtonVisualColors> FilledToggleVisualColors { get; } =
        new(
            "Value.IconButton.FilledToggle.VisualColors",
            new RawRecipePlan<ToggleIconButtonVisualColors>(
                new ToggleIconButtonVisualColorsRecipe(
                    uncheckedContainer: FilledIconButtonTokensKeys.UnselectedContainerColor,
                    uncheckedIcon: FilledIconButtonTokensKeys.UnselectedColor,
                    checkedContainer: FilledIconButtonTokensKeys.SelectedContainerColor,
                    checkedIcon: FilledIconButtonTokensKeys.SelectedColor,
                    disabledContainer: FilledIconButtonTokensKeys.DisabledContainerColor,
                    disabledContainerOpacity: FilledIconButtonTokensKeys.DisabledContainerOpacity,
                    disabledIcon: FilledIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: FilledIconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<ToggleIconButtonVisualColors> TonalToggleVisualColors { get; } =
        new(
            "Value.IconButton.TonalToggle.VisualColors",
            new RawRecipePlan<ToggleIconButtonVisualColors>(
                new ToggleIconButtonVisualColorsRecipe(
                    uncheckedContainer: FilledTonalIconButtonTokensKeys.UnselectedContainerColor,
                    uncheckedIcon: FilledTonalIconButtonTokensKeys.UnselectedColor,
                    checkedContainer: FilledTonalIconButtonTokensKeys.SelectedContainerColor,
                    checkedIcon: FilledTonalIconButtonTokensKeys.SelectedColor,
                    disabledContainer: FilledTonalIconButtonTokensKeys.DisabledContainerColor,
                    disabledContainerOpacity: FilledTonalIconButtonTokensKeys.DisabledContainerOpacity,
                    disabledIcon: FilledTonalIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: FilledTonalIconButtonTokensKeys.DisabledOpacity)));

    public static MaterialValueKey<ToggleIconButtonVisualColors> OutlinedToggleVisualColors { get; } =
        new(
            "Value.IconButton.OutlinedToggle.VisualColors",
            new RawRecipePlan<ToggleIconButtonVisualColors>(
                new ToggleIconButtonVisualColorsRecipe(
                    uncheckedIcon: OutlinedIconButtonTokensKeys.UnselectedColor,
                    uncheckedOutline: OutlinedIconButtonTokensKeys.UnselectedOutlineColor,
                    checkedContainer: OutlinedIconButtonTokensKeys.SelectedContainerColor,
                    checkedIcon: OutlinedIconButtonTokensKeys.SelectedColor,
                    disabledIcon: OutlinedIconButtonTokensKeys.DisabledColor,
                    disabledIconOpacity: OutlinedIconButtonTokensKeys.DisabledOpacity,
                    disabledOutline: OutlinedIconButtonTokensKeys.DisabledOutlineColor)));

    private static MaterialValueKey<IconButtonGeometryProfile> Geometry(
        string path,
        MaterialToken<double> containerHeight,
        MaterialToken<double> iconSize,
        MaterialToken<double> leadingSpace,
        MaterialToken<double> outlineWidth,
        MaterialToken<ShapeRole> containerShape,
        MaterialToken<ShapeRole> pressedShape,
        MaterialToken<ShapeRole> squareShape,
        MaterialToken<ShapeRole> selectedShape,
        MaterialToken<ShapeRole> selectedSquareShape) =>
        new(
            path,
            new RawRecipePlan<IconButtonGeometryProfile>(
                new IconButtonGeometryProfileRecipe(
                    containerHeight,
                    iconSize,
                    leadingSpace,
                    outlineWidth,
                    containerShape,
                    pressedShape,
                    squareShape,
                    selectedShape,
                    selectedSquareShape)));
}

/// <summary>Immutable size, width and late-shape inputs consumed by the icon-button family.</summary>
public sealed record IconButtonGeometryProfile(
    double ContainerHeight,
    double IconSize,
    Thickness ContentPadding,
    double ContainerWidth,
    double OutlineWidth,
    ShapeRole ContainerShape,
    ShapeRole PressedShape,
    ShapeRole SquareShape,
    ShapeRole SelectedShape,
    ShapeRole SelectedSquareShape);

/// <summary>Immutable enabled/disabled alternatives consumed by an ordinary icon button.</summary>
public sealed record IconButtonVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush IconColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush StateLayerColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor);

/// <summary>Immutable checked, unchecked and disabled alternatives consumed by a toggle icon button.</summary>
public sealed record ToggleIconButtonVisualColors(
    IImmutableBrush UncheckedContainerColor,
    IImmutableBrush UncheckedIconColor,
    IImmutableBrush UncheckedOutlineColor,
    IImmutableBrush CheckedContainerColor,
    IImmutableBrush CheckedIconColor,
    IImmutableBrush CheckedOutlineColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor,
    IImmutableBrush UncheckedStateLayerColor,
    IImmutableBrush CheckedStateLayerColor);

internal sealed class IconButtonGeometryProfileRecipe : IRawValueRecipe<IconButtonGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<double> _leadingSpace;
    private readonly MaterialToken<double> _outlineWidth;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly MaterialToken<ShapeRole> _pressedShape;
    private readonly MaterialToken<ShapeRole> _squareShape;
    private readonly MaterialToken<ShapeRole> _selectedShape;
    private readonly MaterialToken<ShapeRole> _selectedSquareShape;

    internal IconButtonGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<double> iconSize,
        MaterialToken<double> leadingSpace,
        MaterialToken<double> outlineWidth,
        MaterialToken<ShapeRole> containerShape,
        MaterialToken<ShapeRole> pressedShape,
        MaterialToken<ShapeRole> squareShape,
        MaterialToken<ShapeRole> selectedShape,
        MaterialToken<ShapeRole> selectedSquareShape)
    {
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        _leadingSpace = leadingSpace ?? throw new ArgumentNullException(nameof(leadingSpace));
        _outlineWidth = outlineWidth ?? throw new ArgumentNullException(nameof(outlineWidth));
        _containerShape = containerShape ?? throw new ArgumentNullException(nameof(containerShape));
        _pressedShape = pressedShape ?? throw new ArgumentNullException(nameof(pressedShape));
        _squareShape = squareShape ?? throw new ArgumentNullException(nameof(squareShape));
        _selectedShape = selectedShape ?? throw new ArgumentNullException(nameof(selectedShape));
        _selectedSquareShape = selectedSquareShape ??
            throw new ArgumentNullException(nameof(selectedSquareShape));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            containerHeight,
            iconSize,
            leadingSpace,
            outlineWidth,
            containerShape,
            pressedShape,
            squareShape,
            selectedShape,
            selectedSquareShape,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public IconButtonGeometryProfile Evaluate(MaterialValueContext context)
    {
        var iconSize = context.Get(_iconSize);
        var leadingSpace = context.Get(_leadingSpace);
        return new IconButtonGeometryProfile(
            context.Get(_containerHeight),
            iconSize,
            new Thickness(leadingSpace, 0),
            iconSize + leadingSpace * 2,
            context.Get(_outlineWidth),
            context.Get(_containerShape),
            context.Get(_pressedShape),
            context.Get(_squareShape),
            context.Get(_selectedShape),
            context.Get(_selectedSquareShape));
    }
}

internal sealed class IconButtonVisualColorsRecipe : IRawValueRecipe<IconButtonVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color>? _container;
    private readonly MaterialToken<Color>? _icon;
    private readonly MaterialToken<Color>? _outline;
    private readonly MaterialToken<Color>? _disabledContainer;
    private readonly MaterialToken<double>? _disabledContainerOpacity;
    private readonly MaterialToken<Color>? _disabledIcon;
    private readonly MaterialToken<double>? _disabledIconOpacity;
    private readonly MaterialToken<Color>? _disabledOutline;

    internal IconButtonVisualColorsRecipe(
        MaterialToken<Color>? container = null,
        MaterialToken<Color>? icon = null,
        MaterialToken<Color>? outline = null,
        MaterialToken<Color>? disabledContainer = null,
        MaterialToken<double>? disabledContainerOpacity = null,
        MaterialToken<Color>? disabledIcon = null,
        MaterialToken<double>? disabledIconOpacity = null,
        MaterialToken<Color>? disabledOutline = null)
    {
        _container = container;
        _icon = icon;
        _outline = outline;
        _disabledContainer = disabledContainer;
        _disabledContainerOpacity = disabledContainerOpacity;
        _disabledIcon = disabledIcon;
        _disabledIconOpacity = disabledIconOpacity;
        _disabledOutline = disabledOutline;
        var dependencies = new List<MaterialToken>();
        Add(dependencies, container);
        Add(dependencies, icon);
        Add(dependencies, outline);
        Add(dependencies, disabledContainer);
        Add(dependencies, disabledContainerOpacity);
        Add(dependencies, disabledIcon);
        Add(dependencies, disabledIconOpacity);
        Add(dependencies, disabledOutline);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public IconButtonVisualColors Evaluate(MaterialValueContext context)
    {
        var icon = Brush(context, _icon);
        return new(
            Brush(context, _container),
            icon,
            Brush(context, _outline),
            icon,
            Brush(context, _disabledContainer, _disabledContainerOpacity),
            Brush(context, _disabledIcon, _disabledIconOpacity),
            Brush(context, _disabledOutline));
    }

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

internal sealed class ToggleIconButtonVisualColorsRecipe
    : IRawValueRecipe<ToggleIconButtonVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color>? _uncheckedContainer;
    private readonly MaterialToken<Color>? _uncheckedIcon;
    private readonly MaterialToken<Color>? _uncheckedOutline;
    private readonly MaterialToken<Color>? _checkedContainer;
    private readonly MaterialToken<Color>? _checkedIcon;
    private readonly MaterialToken<Color>? _checkedOutline;
    private readonly MaterialToken<Color>? _disabledContainer;
    private readonly MaterialToken<double>? _disabledContainerOpacity;
    private readonly MaterialToken<Color>? _disabledIcon;
    private readonly MaterialToken<double>? _disabledIconOpacity;
    private readonly MaterialToken<Color>? _disabledOutline;

    internal ToggleIconButtonVisualColorsRecipe(
        MaterialToken<Color>? uncheckedContainer = null,
        MaterialToken<Color>? uncheckedIcon = null,
        MaterialToken<Color>? uncheckedOutline = null,
        MaterialToken<Color>? checkedContainer = null,
        MaterialToken<Color>? checkedIcon = null,
        MaterialToken<Color>? checkedOutline = null,
        MaterialToken<Color>? disabledContainer = null,
        MaterialToken<double>? disabledContainerOpacity = null,
        MaterialToken<Color>? disabledIcon = null,
        MaterialToken<double>? disabledIconOpacity = null,
        MaterialToken<Color>? disabledOutline = null)
    {
        _uncheckedContainer = uncheckedContainer;
        _uncheckedIcon = uncheckedIcon;
        _uncheckedOutline = uncheckedOutline;
        _checkedContainer = checkedContainer;
        _checkedIcon = checkedIcon;
        _checkedOutline = checkedOutline;
        _disabledContainer = disabledContainer;
        _disabledContainerOpacity = disabledContainerOpacity;
        _disabledIcon = disabledIcon;
        _disabledIconOpacity = disabledIconOpacity;
        _disabledOutline = disabledOutline;
        var dependencies = new List<MaterialToken>();
        Add(dependencies, uncheckedContainer);
        Add(dependencies, uncheckedIcon);
        Add(dependencies, uncheckedOutline);
        Add(dependencies, checkedContainer);
        Add(dependencies, checkedIcon);
        Add(dependencies, checkedOutline);
        Add(dependencies, disabledContainer);
        Add(dependencies, disabledContainerOpacity);
        Add(dependencies, disabledIcon);
        Add(dependencies, disabledIconOpacity);
        Add(dependencies, disabledOutline);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ToggleIconButtonVisualColors Evaluate(MaterialValueContext context)
    {
        var uncheckedIcon = Brush(context, _uncheckedIcon);
        var checkedIcon = Brush(context, _checkedIcon);
        return new(
            Brush(context, _uncheckedContainer),
            uncheckedIcon,
            Brush(context, _uncheckedOutline),
            Brush(context, _checkedContainer),
            checkedIcon,
            Brush(context, _checkedOutline),
            Brush(context, _disabledContainer, _disabledContainerOpacity),
            Brush(context, _disabledIcon, _disabledIconOpacity),
            Brush(context, _disabledOutline),
            uncheckedIcon,
            checkedIcon);
    }

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
