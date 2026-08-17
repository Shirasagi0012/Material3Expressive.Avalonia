using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Consumer-ready geometry and colour endpoints for navigation bars, rails and items.</summary>
public static class NavigationValueKeys
{
    public static MaterialValueKey<NavigationGeometryProfile> BarGeometry { get; } =
        new(
            "Value.Navigation.Bar.Geometry",
            new RawRecipePlan<NavigationGeometryProfile>(
                new NavigationGeometryRecipe(
                    NavigationBarTokensKeys.ContainerElevation,
                    NavigationBarTokensKeys.TallContainerHeight,
                    NavigationBarVerticalItemTokensKeys.ActiveIndicatorWidth,
                    NavigationBarVerticalItemTokensKeys.ActiveIndicatorHeight,
                    NavigationBarTokensKeys.ItemActiveIndicatorShape,
                    NavigationBarVerticalItemTokensKeys.IconSize,
                    NavigationBarHorizontalItemTokensKeys.ActiveIndicatorHeight,
                    NavigationBarHorizontalItemTokensKeys.ActiveIndicatorLeadingSpace,
                    NavigationBarTokensKeys.ItemBetweenSpace,
                    NavigationBarTokensKeys.LabelTextFont,
                    iconLabelSpace: 8,
                    startIndicatorVerticalPadding: 8,
                    topItemVerticalPadding: 6,
                    isVertical: false)));

    public static MaterialValueKey<NavigationGeometryProfile> RailGeometry { get; } =
        new(
            "Value.Navigation.Rail.Geometry",
            new RawRecipePlan<NavigationGeometryProfile>(
                new NavigationGeometryRecipe(
                    NavigationRailCollapsedTokensKeys.ContainerElevation,
                    NavigationRailCollapsedTokensKeys.ContainerWidth,
                    NavigationRailVerticalItemTokensKeys.ActiveIndicatorWidth,
                    NavigationRailVerticalItemTokensKeys.ActiveIndicatorHeight,
                    NavigationRailBaselineItemTokensKeys.ActiveIndicatorShape,
                    NavigationRailBaselineItemTokensKeys.IconSize,
                    NavigationBarHorizontalItemTokensKeys.ActiveIndicatorHeight,
                    NavigationRailVerticalItemTokensKeys.LeadingSpace,
                    NavigationRailCollapsedTokensKeys.ItemVerticalSpace,
                    NavigationRailVerticalItemTokensKeys.LabelTextFont,
                    iconLabelSpace: 4,
                    startIndicatorVerticalPadding: 8,
                    topItemVerticalPadding: 6,
                    isVertical: true)));

    public static MaterialValueKey<NavigationVisualColors> BarVisualColors { get; } =
        Colors(
            "Value.Navigation.Bar.VisualColors",
            NavigationBarTokensKeys.ContainerColor,
            NavigationBarTokensKeys.ItemActiveIndicatorColor,
            NavigationBarTokensKeys.ItemActiveIconColor,
            NavigationBarTokensKeys.ItemActiveLabelTextColor,
            NavigationBarTokensKeys.ItemInactiveIconColor,
            NavigationBarTokensKeys.ItemInactiveLabelTextColor);

    public static MaterialValueKey<NavigationVisualColors> RailVisualColors { get; } =
        Colors(
            "Value.Navigation.Rail.VisualColors",
            NavigationRailCollapsedTokensKeys.ContainerColor,
            NavigationRailColorTokensKeys.ItemActiveIndicator,
            NavigationRailColorTokensKeys.ItemActiveIcon,
            NavigationRailColorTokensKeys.ItemActiveLabelText,
            NavigationRailColorTokensKeys.ItemInactiveIcon,
            NavigationRailColorTokensKeys.ItemInactiveLabelText);

    public static MaterialValueKey<IImmutableBrush> SuiteBackground { get; } =
        MaterialValue.SolidBrush("Value.Navigation.Suite.Background", SystemColorTokens.Background);

    public static MaterialValueKey<IImmutableBrush> SuiteForeground { get; } =
        MaterialValue.SolidBrush("Value.Navigation.Suite.Foreground", SystemColorTokens.OnBackground);

    private static MaterialValueKey<NavigationVisualColors> Colors(
        string path,
        MaterialToken<Color> container,
        MaterialToken<Color> activeIndicator,
        MaterialToken<Color> activeIcon,
        MaterialToken<Color> activeLabel,
        MaterialToken<Color> inactiveIcon,
        MaterialToken<Color> inactiveLabel) =>
        new(
            path,
            new RawRecipePlan<NavigationVisualColors>(
                new NavigationVisualColorsRecipe(
                    container,
                    activeIndicator,
                    activeIcon,
                    activeLabel,
                    inactiveIcon,
                    inactiveLabel,
                    SystemColorTokens.OnSurface)));
}

public sealed record NavigationGeometryProfile(
    double ContainerElevation,
    double ContainerSize,
    double ActiveIndicatorWidth,
    double ActiveIndicatorHeight,
    ShapeRole ActiveIndicatorShape,
    double IconSize,
    double IconLabelSpace,
    double StartActiveIndicatorHeight,
    double StartIndicatorHorizontalPadding,
    double StartIndicatorVerticalPadding,
    double TopItemVerticalPadding,
    double ItemBetweenSpace,
    TypographyRole LabelTextStyle,
    bool IsVertical);

public sealed record NavigationVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush StateLayerColor);

internal sealed class NavigationGeometryRecipe : IRawValueRecipe<NavigationGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerElevation;
    private readonly MaterialToken<double> _containerSize;
    private readonly MaterialToken<double> _activeIndicatorWidth;
    private readonly MaterialToken<double> _activeIndicatorHeight;
    private readonly MaterialToken<ShapeRole> _activeIndicatorShape;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<double> _startActiveIndicatorHeight;
    private readonly MaterialToken<double> _startIndicatorHorizontalPadding;
    private readonly MaterialToken<double> _itemBetweenSpace;
    private readonly MaterialToken<TypographyRole> _labelTextStyle;
    private readonly double _iconLabelSpace;
    private readonly double _startIndicatorVerticalPadding;
    private readonly double _topItemVerticalPadding;
    private readonly bool _isVertical;

    internal NavigationGeometryRecipe(
        MaterialToken<double> containerElevation,
        MaterialToken<double> containerSize,
        MaterialToken<double> activeIndicatorWidth,
        MaterialToken<double> activeIndicatorHeight,
        MaterialToken<ShapeRole> activeIndicatorShape,
        MaterialToken<double> iconSize,
        MaterialToken<double> startActiveIndicatorHeight,
        MaterialToken<double> startIndicatorHorizontalPadding,
        MaterialToken<double> itemBetweenSpace,
        MaterialToken<TypographyRole> labelTextStyle,
        double iconLabelSpace,
        double startIndicatorVerticalPadding,
        double topItemVerticalPadding,
        bool isVertical)
    {
        _containerElevation = containerElevation;
        _containerSize = containerSize;
        _activeIndicatorWidth = activeIndicatorWidth;
        _activeIndicatorHeight = activeIndicatorHeight;
        _activeIndicatorShape = activeIndicatorShape;
        _iconSize = iconSize;
        _startActiveIndicatorHeight = startActiveIndicatorHeight;
        _startIndicatorHorizontalPadding = startIndicatorHorizontalPadding;
        _itemBetweenSpace = itemBetweenSpace;
        _labelTextStyle = labelTextStyle;
        _iconLabelSpace = iconLabelSpace;
        _startIndicatorVerticalPadding = startIndicatorVerticalPadding;
        _topItemVerticalPadding = topItemVerticalPadding;
        _isVertical = isVertical;
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            containerElevation,
            containerSize,
            activeIndicatorWidth,
            activeIndicatorHeight,
            activeIndicatorShape,
            iconSize,
            startActiveIndicatorHeight,
            startIndicatorHorizontalPadding,
            itemBetweenSpace,
            labelTextStyle,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public NavigationGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_containerElevation),
        context.Get(_containerSize),
        context.Get(_activeIndicatorWidth),
        context.Get(_activeIndicatorHeight),
        context.Get(_activeIndicatorShape),
        context.Get(_iconSize),
        _iconLabelSpace,
        context.Get(_startActiveIndicatorHeight),
        context.Get(_startIndicatorHorizontalPadding),
        _startIndicatorVerticalPadding,
        _topItemVerticalPadding,
        context.Get(_itemBetweenSpace),
        context.Get(_labelTextStyle),
        _isVertical);
}

internal sealed class NavigationVisualColorsRecipe : IRawValueRecipe<NavigationVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color>[] _tokens;

    internal NavigationVisualColorsRecipe(params MaterialToken<Color>[] tokens)
    {
        if (tokens.Length != 7)
            throw new ArgumentException("A navigation colour profile requires seven tokens.", nameof(tokens));
        _tokens = tokens;
        _dependencies = Array.AsReadOnly<MaterialToken>(tokens);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public NavigationVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _tokens[0]),
        Brush(context, _tokens[1]),
        Brush(context, _tokens[2]),
        Brush(context, _tokens[3]),
        Brush(context, _tokens[4]),
        Brush(context, _tokens[5]),
        Brush(context, _tokens[6]));

    private static IImmutableBrush Brush(MaterialValueContext context, MaterialToken<Color> token) =>
        new ImmutableSolidColorBrush(context.Get(token));
}
