using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ModalBottomSheetDemo : Window
{
    public ModalBottomSheetDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private void OnOpen(object? sender, RoutedEventArgs e) => Sheet.IsOpen = true;
    private void OnDismissed(object? sender, RoutedEventArgs e) => Sheet.IsOpen = false;
    private async void OnExpand(object? sender, RoutedEventArgs e)
    {
        try { await Sheet.State.ExpandAsync(); }
        catch (OperationCanceledException) { }
    }
    private async void OnDone(object? sender, RoutedEventArgs e)
    {
        try { await Sheet.State.HideAsync(); if (!Sheet.State.IsVisible) Sheet.IsOpen = false; }
        catch (OperationCanceledException) { }
    }
}
