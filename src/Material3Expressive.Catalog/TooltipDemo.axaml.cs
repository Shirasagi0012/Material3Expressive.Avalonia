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
