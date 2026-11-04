using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class BottomSheetScaffoldDemo : Window
{
    public BottomSheetScaffoldDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private async void OnExpand(object? sender, RoutedEventArgs e) => await Run(Scaffold.State.BottomSheetState.ExpandAsync());
    private async void OnCollapse(object? sender, RoutedEventArgs e) => await Run(Scaffold.State.BottomSheetState.PartialExpandAsync());
    private async void OnSnackbar(object? sender, RoutedEventArgs e) => await Run(Scaffold.State.SnackbarHostState.ShowSnackbarAsync("A place was saved.", cancellationToken: AnimationScope.Get(this).Lifetime));
    private void OnPeekChanged(object? sender, RoutedEventArgs e)
    { if (Scaffold is not null) Scaffold.SheetPeekHeight = sender is CheckBox { IsChecked: true } ? 112 : 56; }
    private static async Task Run(Task action)
    { try { await action; } catch (OperationCanceledException) { /* A replacement action or unload cancels this operation. */ } }
}
