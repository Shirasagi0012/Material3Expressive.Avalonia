using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class NavigationDrawerDemo : Window
{
    public NavigationDrawerDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private void ToggleTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private async void ToggleDrawer(object? sender, RoutedEventArgs e)
    {
        var state = (sender as Control)?.Tag as string == "modal" ? Modal.State : Dismissible.State;
        try { if (state.TargetValue == DrawerValue.Closed) await state.OpenAsync(); else await state.CloseAsync(); }
        catch (OperationCanceledException) { }
    }
    private void Navigate(object? sender, RoutedEventArgs e)
    {
        if (sender is NavigationDrawerItem item && item.FindAncestorOfType<NavigationDrawer>() is { } drawer)
            foreach (var destination in drawer.GetVisualDescendants().OfType<NavigationDrawerItem>()) destination.IsSelected = ReferenceEquals(destination, item);
    }
}
