using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Catalog;

public partial class TooltipDemo : Window
{
    public TooltipDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        CurvedTooltip.CaretShape = (_, _) => Avalonia.Media.PathGeometry.Parse(
            "M -12,0 C -12,5 -4,12 0,12 C 4,12 12,5 12,0 Z");
        // Keep hover/focus requests from rejecting this programmatic demo's Default priority.
        Programmatic.State = new TooltipState(isPersistent: false, mutatorMutex: new());
        RichHelp.State = new TooltipState(isPersistent: true, mutatorMutex: new());
        InteractionLabel.InteractionSource = LabelChip.InteractionSource = new();
        IconLabel.InteractionSource = LabelIcon.InteractionSource = new();
        SegmentLabel.InteractionSource = LabelDay.InteractionSource = LabelWeek.InteractionSource = new();
        SwitchLabel.InteractionSource = new();
        SwitchAppearance.SetInteractionSource(LabelSwitch, SwitchLabel.InteractionSource);
        ButtonLabel.InteractionSource = new();
        MaterialButton.SetInteractionSource(LabelButton, ButtonLabel.InteractionSource);
        ToggleLabel.InteractionSource = new();
        MaterialButton.SetInteractionSource(LabelToggle, ToggleLabel.InteractionSource);
        SplitLabel.InteractionSource = new();
        MaterialButton.SetInteractionSource(LabelSplitLeading, SplitLabel.InteractionSource);
        MaterialButton.SetInteractionSource(LabelSplitTrailing, SplitLabel.InteractionSource);
        BarLabel.InteractionSource = LabelBarItem.InteractionSource = new();
        RailLabel.InteractionSource = LabelRailItem.InteractionSource = new();
        ShortBarLabel.InteractionSource = LabelShortBarItem.InteractionSource = new();
        FabLabel.InteractionSource = LabelFab.InteractionSource = new();
        DrawerLabel.InteractionSource = LabelDrawerItem.InteractionSource = new();
        TabLabel.InteractionSource = LabelTab.InteractionSource = new();
        SliderAppearance.SetInteractionSource(LabelSingleSlider, SingleSliderLabel.InteractionSource = new());
        RangeStartLabel.InteractionSource = LabelRangeStartThumb.InteractionSource = LabelRange.StartThumbInteractionSource = new();
        RangeEndLabel.InteractionSource = LabelRangeEndThumb.InteractionSource = LabelRange.EndThumbInteractionSource = new();
    }

    private void OnChoosePeriod(object? sender, RoutedEventArgs e)
    {
        LabelDay.IsSelected = ReferenceEquals(sender, LabelDay);
        LabelWeek.IsSelected = ReferenceEquals(sender, LabelWeek);
    }

    private async void OnShow(object? sender, RoutedEventArgs e)
    {
        try { await Programmatic.State.ShowAsync(); }
        catch (OperationCanceledException) { }
    }

    private async void OnShowRich(object? sender, RoutedEventArgs e)
    {
        try { await RichHelp.State.ShowAsync(); }
        catch (OperationCanceledException) { }
    }

    private void OnDismissRich(object? sender, RoutedEventArgs e) => RichHelp.State.Dismiss();

    private void OnChangeTooltipCorners(object? sender, RoutedEventArgs e)
    {
        if (ShapedTooltip.Shape is not CutCornerShape shape)
            ShapedTooltip.Shape = shape = new CutCornerShape(4);
        shape.TopStart = new(shape.TopStart.Scalar > 12 ? 4 : 20, Avalonia.RelativeUnit.Absolute);
        shape.BottomEnd = shape.TopStart;
    }

    private void OnClearTooltipShape(object? sender, RoutedEventArgs e) => ShapedTooltip.ClearValue(PlainTooltip.ShapeProperty);

    private void OnTransformAnchor(object? sender, RoutedEventArgs e) =>
        TransformedLabel.RenderTransform = TransformedLabel.RenderTransform is null ? new Avalonia.Media.ScaleTransform(1.25, 1.25) : null;

    private void OnTransformParent(object? sender, RoutedEventArgs e) =>
        TransformedLabelParent.RenderTransform = TransformedLabelParent.RenderTransform is null ? new Avalonia.Media.TranslateTransform(32, 12) : null;
}
