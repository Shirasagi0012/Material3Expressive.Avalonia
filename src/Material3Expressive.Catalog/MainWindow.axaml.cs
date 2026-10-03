using ComposeAnimation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3Expressive.Components;
using System.Linq;
using System;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        // Animated.SetDurationScale(this, 4);
    }

    private void OnDestinationClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in DemoNavigationRail.Items.OfType<NavigationRailItem>())
            item.IsSelected = ReferenceEquals(item, sender);
    }

    private async void OnToggleWideRail(object? sender, RoutedEventArgs e)
    {
        try { await DemoWideNavigationRail.State.ToggleAsync(); }
        catch (OperationCanceledException) { } // A later toggle or window disposal supersedes this operation.
    }

    private void OnWideDestinationClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in DemoWideNavigationRail.Items.OfType<WideNavigationRailItem>())
            item.IsSelected = ReferenceEquals(item, sender);
    }

    private async void OnToggleModalRail(object? sender, RoutedEventArgs e)
    {
        var rail = (sender as Control)?.Tag as string == "hidden" ? DemoHiddenModalRail : DemoModalRail;
        try { await rail.State.ToggleAsync(); }
        catch (OperationCanceledException) { }
    }

    private void OnModalDestinationClick(object? sender, RoutedEventArgs e)
    {
        var rail = DemoModalRail.Items.Contains(sender) ? DemoModalRail : DemoHiddenModalRail;
        foreach (var item in rail.Items.OfType<WideNavigationRailItem>()) item.IsSelected = ReferenceEquals(item, sender);
    }
}
