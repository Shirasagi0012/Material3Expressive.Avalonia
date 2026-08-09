using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record ListItemMetrics(
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
    TypographyRole TrailingTextStyle,
    MotionRole ColorMotion,
    MotionRole ShapeMotion,
    MotionRole ElevationMotion);

internal sealed record ListItemVisualColors(
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
    IImmutableBrush DraggedSupportingContentColor);

/// <summary>Component-owned policy for the expressive list-item anatomy.</summary>
internal static class ListItemDefaults
{
    public static ListItemMetrics Metrics() => new(
        ListTokens.ItemContainerExpressiveShape,
        ListTokens.ContainerShape,
        ListTokens.ItemSelectedContainerExpressiveShape,
        ListTokens.ItemPressedContainerExpressiveShape,
        ListTokens.ItemFocusedContainerExpressiveShape,
        ListTokens.ItemHoveredContainerExpressiveShape,
        ReorderListTokens.ItemShape,
        ListTokens.ItemContainerElevation,
        ListTokens.ItemDraggedContainerElevation,
        ListTokens.ItemOneLineContainerHeight,
        ListTokens.ItemTwoLineContainerHeight,
        ListTokens.ItemThreeLineContainerHeight,
        ListTokens.ItemLeadingSpace,
        ListTokens.ItemTrailingSpace,
        ListTokens.ItemTopSpace,
        ListTokens.ItemBottomSpace,
        ListTokens.ItemBetweenSpace,
        ListTokens.SegmentedGap,
        ListTokens.ItemLabelTextFont,
        ListTokens.ItemSupportingTextFont,
        ListTokens.ItemOverlineFont,
        ListTokens.ItemLeadingAvatarLabelFont,
        ListTokens.ItemTrailingSupportingTextFont,
        MotionRole.DefaultEffects,
        MotionRole.FastSpatial,
        MotionRole.FastSpatial);

    public static ListItemVisualColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var scheme = theme.ColorScheme;
        return new(
            ListTokens.ItemContainerColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSegmentedContainerColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemLabelTextColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemLeadingIconColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemTrailingIconColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemOverlineColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSupportingTextColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedContainerColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedLabelTextColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedLeadingIconColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedTrailingIconColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedOverlineColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemSelectedSupportingTextColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemContainerColor.ResolveBrush(scheme, isDark),
            ListTokens.ItemDisabledLabelTextColor.ResolveBrush(scheme, isDark, ListTokens.ItemDisabledLabelTextOpacity),
            ListTokens.ItemDisabledLeadingIconColor.ResolveBrush(scheme, isDark, ListTokens.ItemDisabledLeadingIconOpacity),
            ListTokens.ItemDisabledTrailingIconColor.ResolveBrush(scheme, isDark, ListTokens.ItemDisabledTrailingIconOpacity),
            ListTokens.ItemDisabledOverlineColor.ResolveBrush(scheme, isDark, ListTokens.ItemDisabledOverlineOpacity),
            ListTokens.ItemDisabledSupportingTextColor.ResolveBrush(scheme, isDark, ListTokens.ItemDisabledSupportingTextOpacity),
            ReorderListTokens.ItemContainerColor.ResolveBrush(scheme, isDark),
            ReorderListTokens.ItemLabelTextColor.ResolveBrush(scheme, isDark),
            ReorderListTokens.ItemLeadingIconColor.ResolveBrush(scheme, isDark),
            ReorderListTokens.ItemTrailingIconColor.ResolveBrush(scheme, isDark),
            ReorderListTokens.ItemOverlineColor.ResolveBrush(scheme, isDark),
            ReorderListTokens.ItemSupportingTextColor.ResolveBrush(scheme, isDark));
    }
}
