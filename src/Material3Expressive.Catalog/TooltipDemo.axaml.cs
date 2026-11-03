using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class TooltipDemo : Window
{
    public TooltipDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
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
}
