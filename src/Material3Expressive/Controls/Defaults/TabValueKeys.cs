using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the tab item and tab row.
/// </summary>
/// <remarks>
/// The tab variant is selected by the two ControlThemes in <c>Tabs.axaml</c>.  Selection,
/// enabled state, scrolling, indicator bounds, RTL and motion remain control policy; the
/// endpoints below only carry the geometry, typography role and brushes which the controls
/// actually consume.
/// </remarks>
public static class TabValueKeys
{
    /// <summary>Gets the primary tab item's geometry and typography profile.</summary>
    public static MaterialValueKey<TabItemGeometryProfile> PrimaryItemGeometry { get; } =
        new(
            "Value.Tab.Primary.ItemGeometry",
            new RawRecipePlan<TabItemGeometryProfile>(
                new ConstantTabItemGeometryRecipe(48, 64, 24, TypographyRole.TitleSmall)));

    /// <summary>Gets the secondary tab item's geometry and typography profile.</summary>
    public static MaterialValueKey<TabItemGeometryProfile> SecondaryItemGeometry { get; } =
        new(
            "Value.Tab.Secondary.ItemGeometry",
            new RawRecipePlan<TabItemGeometryProfile>(
                new TabItemGeometryRecipe(
                    SecondaryNavigationTabTokensKeys.ContainerHeight,
                    SecondaryNavigationTabTokensKeys.IconSize,
                    SecondaryNavigationTabTokensKeys.LabelTextFont,
                    iconAndLabelHeight: 64)));

    /// <summary>Gets the primary row's indicator and divider geometry.</summary>
    public static MaterialValueKey<TabRowGeometryProfile> PrimaryRowGeometry { get; } =
        new(
            "Value.Tab.Primary.RowGeometry",
            new RawRecipePlan<TabRowGeometryProfile>(
                new ConstantTabRowGeometryRecipe(3, 3, 1, indicatorSpansLabel: true)));

    /// <summary>Gets the secondary row's indicator and divider geometry.</summary>
    public static MaterialValueKey<TabRowGeometryProfile> SecondaryRowGeometry { get; } =
        new(
            "Value.Tab.Secondary.RowGeometry",
            new RawRecipePlan<TabRowGeometryProfile>(
                new TabRowGeometryRecipe(
                    SecondaryNavigationTabTokensKeys.DividerHeight,
                    activeIndicatorHeight: 3,
                    activeIndicatorCornerRadius: 0,
                    indicatorSpansLabel: false)));

    /// <summary>Gets primary tab item brushes.</summary>
    public static MaterialValueKey<TabItemVisualColors> PrimaryItemVisualColors { get; } =
        new(
            "Value.Tab.Primary.ItemVisualColors",
            new RawRecipePlan<TabItemVisualColors>(
                new TabItemVisualColorsRecipe(
                    SystemColorTokens.Primary,
                    SystemColorTokens.Primary,
                    SystemColorTokens.Primary,
                    SystemColorTokens.Primary,
                    SystemColorTokens.Primary)));

    /// <summary>Gets secondary tab item brushes.</summary>
    public static MaterialValueKey<TabItemVisualColors> SecondaryItemVisualColors { get; } =
        new(
            "Value.Tab.Secondary.ItemVisualColors",
            new RawRecipePlan<TabItemVisualColors>(
                new TabItemVisualColorsRecipe(
                    SecondaryNavigationTabTokensKeys.ActiveIconColor,
                    SecondaryNavigationTabTokensKeys.ActiveLabelTextColor,
                    // The current Compose parity baseline supplies the active content colour for
                    // every enabled secondary tab. Keep that deliberate policy here; the raw
                    // inactive roles remain available for a future component policy change.
                    SecondaryNavigationTabTokensKeys.ActiveIconColor,
                    SecondaryNavigationTabTokensKeys.ActiveLabelTextColor,
                    SystemColorTokens.Primary)));

    /// <summary>Gets primary tab-row brushes.</summary>
    public static MaterialValueKey<TabRowVisualColors> PrimaryRowVisualColors { get; } =
        new(
            "Value.Tab.Primary.RowVisualColors",
            new RawRecipePlan<TabRowVisualColors>(
                new TabRowVisualColorsRecipe(
                    SystemColorTokens.Surface,
                    SystemColorTokens.Primary,
                    SystemColorTokens.OutlineVariant)));

    /// <summary>Gets secondary tab-row brushes.</summary>
    public static MaterialValueKey<TabRowVisualColors> SecondaryRowVisualColors { get; } =
        new(
            "Value.Tab.Secondary.RowVisualColors",
            new RawRecipePlan<TabRowVisualColors>(
                new TabRowVisualColorsRecipe(
                    SecondaryNavigationTabTokensKeys.ContainerColor,
                    SystemColorTokens.Primary,
                    SystemColorTokens.OutlineVariant)));
}

/// <summary>Geometry and typography role consumed by one <see cref="MaterialTab" />.</summary>
public sealed record TabItemGeometryProfile(
    double ContainerHeight,
    double IconAndLabelHeight,
    double IconSize,
    TypographyRole LabelTextStyle);

/// <summary>Indicator and divider geometry consumed by one <see cref="MaterialTabs" /> row.</summary>
public sealed record TabRowGeometryProfile(
    double ActiveIndicatorHeight,
    double ActiveIndicatorCornerRadius,
    double DividerHeight,
    bool IndicatorSpansLabel);

/// <summary>State-independent item brushes selected by the item's checked policy.</summary>
public sealed record TabItemVisualColors(
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush StateLayerColor);

/// <summary>Brushes consumed by the self-rendering tab row.</summary>
public sealed record TabRowVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush DividerColor);

internal sealed class ConstantTabItemGeometryRecipe : IRawValueRecipe<TabItemGeometryProfile>
{
    private readonly TabItemGeometryProfile _value;

    internal ConstantTabItemGeometryRecipe(
        double containerHeight,
        double iconAndLabelHeight,
        double iconSize,
        TypographyRole labelTextStyle)
    {
        if (!double.IsFinite(containerHeight) || containerHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(containerHeight));
        if (!double.IsFinite(iconAndLabelHeight) || iconAndLabelHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(iconAndLabelHeight));
        if (!double.IsFinite(iconSize) || iconSize < 0)
            throw new ArgumentOutOfRangeException(nameof(iconSize));

        _value = new TabItemGeometryProfile(
            containerHeight,
            iconAndLabelHeight,
            iconSize,
            labelTextStyle);
    }

    public IReadOnlyList<MaterialToken> Dependencies => Array.Empty<MaterialToken>();

    public TabItemGeometryProfile Evaluate(MaterialValueContext context) => _value;
}

internal sealed class TabItemGeometryRecipe : IRawValueRecipe<TabItemGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<TypographyRole> _labelTextStyle;
    private readonly double _iconAndLabelHeight;

    internal TabItemGeometryRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<double> iconSize,
        MaterialToken<TypographyRole> labelTextStyle,
        double iconAndLabelHeight)
    {
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        _labelTextStyle = labelTextStyle ?? throw new ArgumentNullException(nameof(labelTextStyle));
        if (!double.IsFinite(iconAndLabelHeight) || iconAndLabelHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(iconAndLabelHeight));

        _iconAndLabelHeight = iconAndLabelHeight;
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerHeight, iconSize, labelTextStyle]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TabItemGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_containerHeight),
        _iconAndLabelHeight,
        context.Get(_iconSize),
        context.Get(_labelTextStyle));
}

internal sealed class ConstantTabRowGeometryRecipe : IRawValueRecipe<TabRowGeometryProfile>
{
    private readonly TabRowGeometryProfile _value;

    internal ConstantTabRowGeometryRecipe(
        double activeIndicatorHeight,
        double activeIndicatorCornerRadius,
        double dividerHeight,
        bool indicatorSpansLabel)
    {
        if (!double.IsFinite(activeIndicatorHeight) || activeIndicatorHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(activeIndicatorHeight));
        if (!double.IsFinite(activeIndicatorCornerRadius) || activeIndicatorCornerRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(activeIndicatorCornerRadius));
        if (!double.IsFinite(dividerHeight) || dividerHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(dividerHeight));

        _value = new TabRowGeometryProfile(
            activeIndicatorHeight,
            activeIndicatorCornerRadius,
            dividerHeight,
            indicatorSpansLabel);
    }

    public IReadOnlyList<MaterialToken> Dependencies => Array.Empty<MaterialToken>();

    public TabRowGeometryProfile Evaluate(MaterialValueContext context) => _value;
}

internal sealed class TabRowGeometryRecipe : IRawValueRecipe<TabRowGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _dividerHeight;
    private readonly double _activeIndicatorHeight;
    private readonly double _activeIndicatorCornerRadius;
    private readonly bool _indicatorSpansLabel;

    internal TabRowGeometryRecipe(
        MaterialToken<double> dividerHeight,
        double activeIndicatorHeight,
        double activeIndicatorCornerRadius,
        bool indicatorSpansLabel)
    {
        _dividerHeight = dividerHeight ?? throw new ArgumentNullException(nameof(dividerHeight));
        if (!double.IsFinite(activeIndicatorHeight) || activeIndicatorHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(activeIndicatorHeight));
        if (!double.IsFinite(activeIndicatorCornerRadius) || activeIndicatorCornerRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(activeIndicatorCornerRadius));

        _activeIndicatorHeight = activeIndicatorHeight;
        _activeIndicatorCornerRadius = activeIndicatorCornerRadius;
        _indicatorSpansLabel = indicatorSpansLabel;
        _dependencies = Array.AsReadOnly<MaterialToken>([dividerHeight]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TabRowGeometryProfile Evaluate(MaterialValueContext context) => new(
        _activeIndicatorHeight,
        _activeIndicatorCornerRadius,
        context.Get(_dividerHeight),
        _indicatorSpansLabel);
}

internal sealed class TabItemVisualColorsRecipe : IRawValueRecipe<TabItemVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _activeIconColor;
    private readonly MaterialToken<Color> _activeLabelTextColor;
    private readonly MaterialToken<Color> _inactiveIconColor;
    private readonly MaterialToken<Color> _inactiveLabelTextColor;
    private readonly MaterialToken<Color> _stateLayerColor;

    internal TabItemVisualColorsRecipe(
        MaterialToken<Color> activeIconColor,
        MaterialToken<Color> activeLabelTextColor,
        MaterialToken<Color> inactiveIconColor,
        MaterialToken<Color> inactiveLabelTextColor,
        MaterialToken<Color> stateLayerColor)
    {
        _activeIconColor = activeIconColor ?? throw new ArgumentNullException(nameof(activeIconColor));
        _activeLabelTextColor = activeLabelTextColor ?? throw new ArgumentNullException(nameof(activeLabelTextColor));
        _inactiveIconColor = inactiveIconColor ?? throw new ArgumentNullException(nameof(inactiveIconColor));
        _inactiveLabelTextColor = inactiveLabelTextColor ?? throw new ArgumentNullException(nameof(inactiveLabelTextColor));
        _stateLayerColor = stateLayerColor ?? throw new ArgumentNullException(nameof(stateLayerColor));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            activeIconColor,
            activeLabelTextColor,
            inactiveIconColor,
            inactiveLabelTextColor,
            stateLayerColor,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TabItemVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _activeIconColor),
        Brush(context, _activeLabelTextColor),
        Brush(context, _inactiveIconColor),
        Brush(context, _inactiveLabelTextColor),
        Brush(context, _stateLayerColor));

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> token) =>
        new ImmutableSolidColorBrush(context.Get(token));
}

internal sealed class TabRowVisualColorsRecipe : IRawValueRecipe<TabRowVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _containerColor;
    private readonly MaterialToken<Color> _activeIndicatorColor;
    private readonly MaterialToken<Color> _dividerColor;

    internal TabRowVisualColorsRecipe(
        MaterialToken<Color> containerColor,
        MaterialToken<Color> activeIndicatorColor,
        MaterialToken<Color> dividerColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _activeIndicatorColor = activeIndicatorColor ?? throw new ArgumentNullException(nameof(activeIndicatorColor));
        _dividerColor = dividerColor ?? throw new ArgumentNullException(nameof(dividerColor));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerColor, activeIndicatorColor, dividerColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TabRowVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _containerColor),
        Brush(context, _activeIndicatorColor),
        Brush(context, _dividerColor));

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> token) =>
        new ImmutableSolidColorBrush(context.Get(token));
}
