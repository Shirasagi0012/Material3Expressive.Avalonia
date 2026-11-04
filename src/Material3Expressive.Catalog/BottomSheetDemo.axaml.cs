using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class BottomSheetDemo : Window
{
    public BottomSheetDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private async void OnShow(object? sender, RoutedEventArgs e) => await Run(Sheet.State.ShowAsync());
    private async void OnExpand(object? sender, RoutedEventArgs e) => await Run(Sheet.State.ExpandAsync());
    private async void OnPartial(object? sender, RoutedEventArgs e) => await Run(Sheet.State.PartialExpandAsync());
    private async void OnHide(object? sender, RoutedEventArgs e) => await Run(Sheet.State.HideAsync());
    private void OnDismissed(object? sender, RoutedEventArgs e) => Status.Text = "Dismiss requested";
    private void OnShortContent(object? sender, RoutedEventArgs e)
    { if (SheetContent is not null) SheetContent.Height = sender is CheckBox { IsChecked: true } ? 180 : 400; }
    private async Task Run(Task action)
    {
        try { await action; Status.Text = $"Settled: {Sheet.State.CurrentValue}"; }
        catch (OperationCanceledException) { /* A replacement action, drag or unload cancels this operation. */ }
    }
}
