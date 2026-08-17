using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material values used by the expressive list-item family.
/// </summary>
/// <remarks>
/// The profiles contain only immutable Raw-token projections. Interaction state, segmented
/// position, enabled precedence, bounds/RTL shape conversion and motion remain policies of
/// <see cref="MaterialListItem" /> and its panel.
/// </remarks>
public static class ListItemValueKeys
{
    /// <summary>Gets the geometry, spacing, elevation and typography-role profile.</summary>
    public static MaterialValueKey<ListItemGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.ListItem.GeometryProfile",
            new RawRecipePlan<ListItemGeometryProfile>(
                new ListItemGeometryProfileRecipe(
                    ListTokensKeys.ItemContainerExpressiveShape,
                    ListTokensKeys.ContainerShape,
                    ListTokensKeys.ItemSelectedContainerExpressiveShape,
                    ListTokensKeys.ItemPressedContainerExpressiveShape,
                    ListTokensKeys.ItemFocusedContainerExpressiveShape,
                    ListTokensKeys.ItemHoveredContainerExpressiveShape,
                    ReorderListTokensKeys.ItemShape,
                    ListTokensKeys.ItemContainerElevation,
                    ListTokensKeys.ItemDraggedContainerElevation,
                    ListTokensKeys.ItemOneLineContainerHeight,
                    ListTokensKeys.ItemTwoLineContainerHeight,
                    ListTokensKeys.ItemThreeLineContainerHeight,
                    ListTokensKeys.ItemLeadingSpace,
                    ListTokensKeys.ItemTrailingSpace,
                    ListTokensKeys.ItemTopSpace,
                    ListTokensKeys.ItemBottomSpace,
                    ListTokensKeys.ItemBetweenSpace,
                    ListTokensKeys.SegmentedGap,
                    ListTokensKeys.ItemLabelTextFont,
                    ListTokensKeys.ItemSupportingTextFont,
                    ListTokensKeys.ItemOverlineFont,
                    ListTokensKeys.ItemLeadingAvatarLabelFont,
                    ListTokensKeys.ItemTrailingSupportingTextFont)));

    /// <summary>Gets the state-independent and state-specific list-item brushes.</summary>
    public static MaterialValueKey<ListItemVisualColors> VisualColors { get; } =
        new(
            "Value.ListItem.VisualColors",
            new RawRecipePlan<ListItemVisualColors>(
                new ListItemVisualColorsRecipe(
                    ListTokensKeys.ItemContainerColor,
                    ListTokensKeys.ItemSegmentedContainerColor,
                    ListTokensKeys.ItemLabelTextColor,
                    ListTokensKeys.ItemLeadingIconColor,
                    ListTokensKeys.ItemTrailingIconColor,
                    ListTokensKeys.ItemOverlineColor,
                    ListTokensKeys.ItemSupportingTextColor,
                    ListTokensKeys.ItemSelectedContainerColor,
                    ListTokensKeys.ItemSelectedLabelTextColor,
                    ListTokensKeys.ItemSelectedLeadingIconColor,
                    ListTokensKeys.ItemSelectedTrailingIconColor,
                    ListTokensKeys.ItemSelectedOverlineColor,
                    ListTokensKeys.ItemSelectedSupportingTextColor,
                    ListTokensKeys.ItemContainerColor,
                    ListTokensKeys.ItemDisabledLabelTextColor,
                    ListTokensKeys.ItemDisabledLabelTextOpacity,
                    ListTokensKeys.ItemDisabledLeadingIconColor,
                    ListTokensKeys.ItemDisabledLeadingIconOpacity,
                    ListTokensKeys.ItemDisabledTrailingIconColor,
                    ListTokensKeys.ItemDisabledTrailingIconOpacity,
                    ListTokensKeys.ItemDisabledOverlineColor,
                    ListTokensKeys.ItemDisabledOverlineOpacity,
                    ListTokensKeys.ItemDisabledSupportingTextColor,
                    ListTokensKeys.ItemDisabledSupportingTextOpacity,
                    ReorderListTokensKeys.ItemContainerColor,
                    ReorderListTokensKeys.ItemLabelTextColor,
                    ReorderListTokensKeys.ItemLeadingIconColor,
                    ReorderListTokensKeys.ItemTrailingIconColor,
                    ReorderListTokensKeys.ItemOverlineColor,
                    ReorderListTokensKeys.ItemSupportingTextColor,
                    SystemColorTokens.Shadow)));
}

/// <summary>Raw-derived geometry and typography-role inputs for a list item.</summary>
public sealed record ListItemGeometryProfile(
    ShapeRole RestingShape,
    ShapeRole SegmentedOuterShape,
    ShapeRole SelectedShape,
    ShapeRole PressedShape,
    ShapeRole FocusedShape,
    ShapeRole HoveredShape,
    ShapeRole DraggedShape,
    double ContainerElevation,
    double DraggedContainerElevation,
    double OneLineContainerHeight,
    double TwoLineContainerHeight,
    double ThreeLineContainerHeight,
    double LeadingSpace,
    double TrailingSpace,
    double TopSpace,
    double BottomSpace,
    double BetweenSpace,
    double SegmentedGap,
    TypographyRole ContentTextStyle,
    TypographyRole SupportingTextStyle,
    TypographyRole OverlineTextStyle,
    TypographyRole LeadingTextStyle,
    TypographyRole TrailingTextStyle)
{
    /// <summary>Gets the logical start/top/end/bottom content insets.</summary>
    public Thickness ContentPadding =>
        new(LeadingSpace, TopSpace, TrailingSpace, BottomSpace);
}

/// <summary>Immutable list-item brushes for normal, selected, disabled and dragged states.</summary>
public sealed record ListItemVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush SegmentedContainerColor,
    IImmutableBrush ContentColor,
    IImmutableBrush LeadingContentColor,
    IImmutableBrush TrailingContentColor,
    IImmutableBrush OverlineContentColor,
    IImmutableBrush SupportingContentColor,
    IImmutableBrush SelectedContainerColor,
    IImmutableBrush SelectedContentColor,
    IImmutableBrush SelectedLeadingContentColor,
    IImmutableBrush SelectedTrailingContentColor,
    IImmutableBrush SelectedOverlineContentColor,
    IImmutableBrush SelectedSupportingContentColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledContentColor,
    IImmutableBrush DisabledLeadingContentColor,
    IImmutableBrush DisabledTrailingContentColor,
    IImmutableBrush DisabledOverlineContentColor,
    IImmutableBrush DisabledSupportingContentColor,
    IImmutableBrush DraggedContainerColor,
    IImmutableBrush DraggedContentColor,
    IImmutableBrush DraggedLeadingContentColor,
    IImmutableBrush DraggedTrailingContentColor,
    IImmutableBrush DraggedOverlineContentColor,
    IImmutableBrush DraggedSupportingContentColor,
    Color ShadowColor);

internal sealed class ListItemGeometryProfileRecipe : IRawValueRecipe<ListItemGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<ShapeRole> _restingShape;
    private readonly MaterialToken<ShapeRole> _segmentedOuterShape;
    private readonly MaterialToken<ShapeRole> _selectedShape;
    private readonly MaterialToken<ShapeRole> _pressedShape;
    private readonly MaterialToken<ShapeRole> _focusedShape;
    private readonly MaterialToken<ShapeRole> _hoveredShape;
    private readonly MaterialToken<ShapeRole> _draggedShape;
    private readonly MaterialToken<double> _containerElevation;
    private readonly MaterialToken<double> _draggedContainerElevation;
    private readonly MaterialToken<double> _oneLineContainerHeight;
    private readonly MaterialToken<double> _twoLineContainerHeight;
    private readonly MaterialToken<double> _threeLineContainerHeight;
    private readonly MaterialToken<double> _leadingSpace;
    private readonly MaterialToken<double> _trailingSpace;
    private readonly MaterialToken<double> _topSpace;
    private readonly MaterialToken<double> _bottomSpace;
    private readonly MaterialToken<double> _betweenSpace;
    private readonly MaterialToken<double> _segmentedGap;
    private readonly MaterialToken<TypographyRole> _contentTextStyle;
    private readonly MaterialToken<TypographyRole> _supportingTextStyle;
    private readonly MaterialToken<TypographyRole> _overlineTextStyle;
    private readonly MaterialToken<TypographyRole> _leadingTextStyle;
    private readonly MaterialToken<TypographyRole> _trailingTextStyle;

    internal ListItemGeometryProfileRecipe(
        MaterialToken<ShapeRole> restingShape,
        MaterialToken<ShapeRole> segmentedOuterShape,
        MaterialToken<ShapeRole> selectedShape,
        MaterialToken<ShapeRole> pressedShape,
        MaterialToken<ShapeRole> focusedShape,
        MaterialToken<ShapeRole> hoveredShape,
        MaterialToken<ShapeRole> draggedShape,
        MaterialToken<double> containerElevation,
        MaterialToken<double> draggedContainerElevation,
        MaterialToken<double> oneLineContainerHeight,
        MaterialToken<double> twoLineContainerHeight,
        MaterialToken<double> threeLineContainerHeight,
        MaterialToken<double> leadingSpace,
        MaterialToken<double> trailingSpace,
        MaterialToken<double> topSpace,
        MaterialToken<double> bottomSpace,
        MaterialToken<double> betweenSpace,
        MaterialToken<double> segmentedGap,
        MaterialToken<TypographyRole> contentTextStyle,
        MaterialToken<TypographyRole> supportingTextStyle,
        MaterialToken<TypographyRole> overlineTextStyle,
        MaterialToken<TypographyRole> leadingTextStyle,
        MaterialToken<TypographyRole> trailingTextStyle)
    {
        _restingShape = restingShape ?? throw new ArgumentNullException(nameof(restingShape));
        _segmentedOuterShape = segmentedOuterShape ?? throw new ArgumentNullException(nameof(segmentedOuterShape));
        _selectedShape = selectedShape ?? throw new ArgumentNullException(nameof(selectedShape));
        _pressedShape = pressedShape ?? throw new ArgumentNullException(nameof(pressedShape));
        _focusedShape = focusedShape ?? throw new ArgumentNullException(nameof(focusedShape));
        _hoveredShape = hoveredShape ?? throw new ArgumentNullException(nameof(hoveredShape));
        _draggedShape = draggedShape ?? throw new ArgumentNullException(nameof(draggedShape));
        _containerElevation = containerElevation ?? throw new ArgumentNullException(nameof(containerElevation));
        _draggedContainerElevation = draggedContainerElevation ?? throw new ArgumentNullException(nameof(draggedContainerElevation));
        _oneLineContainerHeight = oneLineContainerHeight ?? throw new ArgumentNullException(nameof(oneLineContainerHeight));
        _twoLineContainerHeight = twoLineContainerHeight ?? throw new ArgumentNullException(nameof(twoLineContainerHeight));
        _threeLineContainerHeight = threeLineContainerHeight ?? throw new ArgumentNullException(nameof(threeLineContainerHeight));
        _leadingSpace = leadingSpace ?? throw new ArgumentNullException(nameof(leadingSpace));
        _trailingSpace = trailingSpace ?? throw new ArgumentNullException(nameof(trailingSpace));
        _topSpace = topSpace ?? throw new ArgumentNullException(nameof(topSpace));
        _bottomSpace = bottomSpace ?? throw new ArgumentNullException(nameof(bottomSpace));
        _betweenSpace = betweenSpace ?? throw new ArgumentNullException(nameof(betweenSpace));
        _segmentedGap = segmentedGap ?? throw new ArgumentNullException(nameof(segmentedGap));
        _contentTextStyle = contentTextStyle ?? throw new ArgumentNullException(nameof(contentTextStyle));
        _supportingTextStyle = supportingTextStyle ?? throw new ArgumentNullException(nameof(supportingTextStyle));
        _overlineTextStyle = overlineTextStyle ?? throw new ArgumentNullException(nameof(overlineTextStyle));
        _leadingTextStyle = leadingTextStyle ?? throw new ArgumentNullException(nameof(leadingTextStyle));
        _trailingTextStyle = trailingTextStyle ?? throw new ArgumentNullException(nameof(trailingTextStyle));

        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            restingShape,
            segmentedOuterShape,
            selectedShape,
            pressedShape,
            focusedShape,
            hoveredShape,
            draggedShape,
            containerElevation,
            draggedContainerElevation,
            oneLineContainerHeight,
            twoLineContainerHeight,
            threeLineContainerHeight,
            leadingSpace,
            trailingSpace,
            topSpace,
            bottomSpace,
            betweenSpace,
            segmentedGap,
            contentTextStyle,
            supportingTextStyle,
            overlineTextStyle,
            leadingTextStyle,
            trailingTextStyle,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ListItemGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_restingShape),
        context.Get(_segmentedOuterShape),
        context.Get(_selectedShape),
        context.Get(_pressedShape),
        context.Get(_focusedShape),
        context.Get(_hoveredShape),
        context.Get(_draggedShape),
        context.Get(_containerElevation),
        context.Get(_draggedContainerElevation),
        context.Get(_oneLineContainerHeight),
        context.Get(_twoLineContainerHeight),
        context.Get(_threeLineContainerHeight),
        context.Get(_leadingSpace),
        context.Get(_trailingSpace),
        context.Get(_topSpace),
        context.Get(_bottomSpace),
        context.Get(_betweenSpace),
        context.Get(_segmentedGap),
        context.Get(_contentTextStyle),
        context.Get(_supportingTextStyle),
        context.Get(_overlineTextStyle),
        context.Get(_leadingTextStyle),
        context.Get(_trailingTextStyle));
}

internal sealed class ListItemVisualColorsRecipe : IRawValueRecipe<ListItemVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _containerColor;
    private readonly MaterialToken<Color> _segmentedContainerColor;
    private readonly MaterialToken<Color> _contentColor;
    private readonly MaterialToken<Color> _leadingContentColor;
    private readonly MaterialToken<Color> _trailingContentColor;
    private readonly MaterialToken<Color> _overlineContentColor;
    private readonly MaterialToken<Color> _supportingContentColor;
    private readonly MaterialToken<Color> _selectedContainerColor;
    private readonly MaterialToken<Color> _selectedContentColor;
    private readonly MaterialToken<Color> _selectedLeadingContentColor;
    private readonly MaterialToken<Color> _selectedTrailingContentColor;
    private readonly MaterialToken<Color> _selectedOverlineContentColor;
    private readonly MaterialToken<Color> _selectedSupportingContentColor;
    private readonly MaterialToken<Color> _disabledContainerColor;
    private readonly MaterialToken<Color> _disabledContentColor;
    private readonly MaterialToken<double> _disabledContentOpacity;
    private readonly MaterialToken<Color> _disabledLeadingContentColor;
    private readonly MaterialToken<double> _disabledLeadingContentOpacity;
    private readonly MaterialToken<Color> _disabledTrailingContentColor;
    private readonly MaterialToken<double> _disabledTrailingContentOpacity;
    private readonly MaterialToken<Color> _disabledOverlineContentColor;
    private readonly MaterialToken<double> _disabledOverlineContentOpacity;
    private readonly MaterialToken<Color> _disabledSupportingContentColor;
    private readonly MaterialToken<double> _disabledSupportingContentOpacity;
    private readonly MaterialToken<Color> _draggedContainerColor;
    private readonly MaterialToken<Color> _draggedContentColor;
    private readonly MaterialToken<Color> _draggedLeadingContentColor;
    private readonly MaterialToken<Color> _draggedTrailingContentColor;
    private readonly MaterialToken<Color> _draggedOverlineContentColor;
    private readonly MaterialToken<Color> _draggedSupportingContentColor;
    private readonly MaterialToken<Color> _shadowColor;

    internal ListItemVisualColorsRecipe(
        MaterialToken<Color> containerColor,
        MaterialToken<Color> segmentedContainerColor,
        MaterialToken<Color> contentColor,
        MaterialToken<Color> leadingContentColor,
        MaterialToken<Color> trailingContentColor,
        MaterialToken<Color> overlineContentColor,
        MaterialToken<Color> supportingContentColor,
        MaterialToken<Color> selectedContainerColor,
        MaterialToken<Color> selectedContentColor,
        MaterialToken<Color> selectedLeadingContentColor,
        MaterialToken<Color> selectedTrailingContentColor,
        MaterialToken<Color> selectedOverlineContentColor,
        MaterialToken<Color> selectedSupportingContentColor,
        MaterialToken<Color> disabledContainerColor,
        MaterialToken<Color> disabledContentColor,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<Color> disabledLeadingContentColor,
        MaterialToken<double> disabledLeadingContentOpacity,
        MaterialToken<Color> disabledTrailingContentColor,
        MaterialToken<double> disabledTrailingContentOpacity,
        MaterialToken<Color> disabledOverlineContentColor,
        MaterialToken<double> disabledOverlineContentOpacity,
        MaterialToken<Color> disabledSupportingContentColor,
        MaterialToken<double> disabledSupportingContentOpacity,
        MaterialToken<Color> draggedContainerColor,
        MaterialToken<Color> draggedContentColor,
        MaterialToken<Color> draggedLeadingContentColor,
        MaterialToken<Color> draggedTrailingContentColor,
        MaterialToken<Color> draggedOverlineContentColor,
        MaterialToken<Color> draggedSupportingContentColor,
        MaterialToken<Color> shadowColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _segmentedContainerColor = segmentedContainerColor ?? throw new ArgumentNullException(nameof(segmentedContainerColor));
        _contentColor = contentColor ?? throw new ArgumentNullException(nameof(contentColor));
        _leadingContentColor = leadingContentColor ?? throw new ArgumentNullException(nameof(leadingContentColor));
        _trailingContentColor = trailingContentColor ?? throw new ArgumentNullException(nameof(trailingContentColor));
        _overlineContentColor = overlineContentColor ?? throw new ArgumentNullException(nameof(overlineContentColor));
        _supportingContentColor = supportingContentColor ?? throw new ArgumentNullException(nameof(supportingContentColor));
        _selectedContainerColor = selectedContainerColor ?? throw new ArgumentNullException(nameof(selectedContainerColor));
        _selectedContentColor = selectedContentColor ?? throw new ArgumentNullException(nameof(selectedContentColor));
        _selectedLeadingContentColor = selectedLeadingContentColor ?? throw new ArgumentNullException(nameof(selectedLeadingContentColor));
        _selectedTrailingContentColor = selectedTrailingContentColor ?? throw new ArgumentNullException(nameof(selectedTrailingContentColor));
        _selectedOverlineContentColor = selectedOverlineContentColor ?? throw new ArgumentNullException(nameof(selectedOverlineContentColor));
        _selectedSupportingContentColor = selectedSupportingContentColor ?? throw new ArgumentNullException(nameof(selectedSupportingContentColor));
        _disabledContainerColor = disabledContainerColor ?? throw new ArgumentNullException(nameof(disabledContainerColor));
        _disabledContentColor = disabledContentColor ?? throw new ArgumentNullException(nameof(disabledContentColor));
        _disabledContentOpacity = disabledContentOpacity ?? throw new ArgumentNullException(nameof(disabledContentOpacity));
        _disabledLeadingContentColor = disabledLeadingContentColor ?? throw new ArgumentNullException(nameof(disabledLeadingContentColor));
        _disabledLeadingContentOpacity = disabledLeadingContentOpacity ?? throw new ArgumentNullException(nameof(disabledLeadingContentOpacity));
        _disabledTrailingContentColor = disabledTrailingContentColor ?? throw new ArgumentNullException(nameof(disabledTrailingContentColor));
        _disabledTrailingContentOpacity = disabledTrailingContentOpacity ?? throw new ArgumentNullException(nameof(disabledTrailingContentOpacity));
        _disabledOverlineContentColor = disabledOverlineContentColor ?? throw new ArgumentNullException(nameof(disabledOverlineContentColor));
        _disabledOverlineContentOpacity = disabledOverlineContentOpacity ?? throw new ArgumentNullException(nameof(disabledOverlineContentOpacity));
        _disabledSupportingContentColor = disabledSupportingContentColor ?? throw new ArgumentNullException(nameof(disabledSupportingContentColor));
        _disabledSupportingContentOpacity = disabledSupportingContentOpacity ?? throw new ArgumentNullException(nameof(disabledSupportingContentOpacity));
        _draggedContainerColor = draggedContainerColor ?? throw new ArgumentNullException(nameof(draggedContainerColor));
        _draggedContentColor = draggedContentColor ?? throw new ArgumentNullException(nameof(draggedContentColor));
        _draggedLeadingContentColor = draggedLeadingContentColor ?? throw new ArgumentNullException(nameof(draggedLeadingContentColor));
        _draggedTrailingContentColor = draggedTrailingContentColor ?? throw new ArgumentNullException(nameof(draggedTrailingContentColor));
        _draggedOverlineContentColor = draggedOverlineContentColor ?? throw new ArgumentNullException(nameof(draggedOverlineContentColor));
        _draggedSupportingContentColor = draggedSupportingContentColor ?? throw new ArgumentNullException(nameof(draggedSupportingContentColor));
        _shadowColor = shadowColor ?? throw new ArgumentNullException(nameof(shadowColor));

        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            containerColor,
            segmentedContainerColor,
            contentColor,
            leadingContentColor,
            trailingContentColor,
            overlineContentColor,
            supportingContentColor,
            selectedContainerColor,
            selectedContentColor,
            selectedLeadingContentColor,
            selectedTrailingContentColor,
            selectedOverlineContentColor,
            selectedSupportingContentColor,
            disabledContainerColor,
            disabledContentColor,
            disabledContentOpacity,
            disabledLeadingContentColor,
            disabledLeadingContentOpacity,
            disabledTrailingContentColor,
            disabledTrailingContentOpacity,
            disabledOverlineContentColor,
            disabledOverlineContentOpacity,
            disabledSupportingContentColor,
            disabledSupportingContentOpacity,
            draggedContainerColor,
            draggedContentColor,
            draggedLeadingContentColor,
            draggedTrailingContentColor,
            draggedOverlineContentColor,
            draggedSupportingContentColor,
            shadowColor,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ListItemVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _containerColor),
        Brush(context, _segmentedContainerColor),
        Brush(context, _contentColor),
        Brush(context, _leadingContentColor),
        Brush(context, _trailingContentColor),
        Brush(context, _overlineContentColor),
        Brush(context, _supportingContentColor),
        Brush(context, _selectedContainerColor),
        Brush(context, _selectedContentColor),
        Brush(context, _selectedLeadingContentColor),
        Brush(context, _selectedTrailingContentColor),
        Brush(context, _selectedOverlineContentColor),
        Brush(context, _selectedSupportingContentColor),
        Brush(context, _disabledContainerColor),
        Brush(context, _disabledContentColor, context.Get(_disabledContentOpacity)),
        Brush(context, _disabledLeadingContentColor, context.Get(_disabledLeadingContentOpacity)),
        Brush(context, _disabledTrailingContentColor, context.Get(_disabledTrailingContentOpacity)),
        Brush(context, _disabledOverlineContentColor, context.Get(_disabledOverlineContentOpacity)),
        Brush(context, _disabledSupportingContentColor, context.Get(_disabledSupportingContentOpacity)),
        Brush(context, _draggedContainerColor),
        Brush(context, _draggedContentColor),
        Brush(context, _draggedLeadingContentColor),
        Brush(context, _draggedTrailingContentColor),
        Brush(context, _draggedOverlineContentColor),
        Brush(context, _draggedSupportingContentColor),
        context.Get(_shadowColor));

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> color,
        double opacity = 1) =>
        new ImmutableSolidColorBrush(WithOpacity(context.Get(color), opacity));

    private static Color WithOpacity(Color color, double opacity) =>
        opacity >= 1
            ? color
            : Color.FromArgb(
                (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
                color.R,
                color.G,
                color.B);
}
