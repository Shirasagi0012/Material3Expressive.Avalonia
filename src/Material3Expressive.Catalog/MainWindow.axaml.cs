using ComposeAnimation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3Expressive.Components;
using System.Linq;
using System;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    private static readonly bool TraceWideRailLayout = false;
    private readonly RailLayoutTrace? _railLayoutTrace;

    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        // Animated.SetDurationScale(this, 10);
        
        // Temporary jitter diagnostics. Set TraceWideRailLayout to false to disable capture.
        if (TraceWideRailLayout)
            _railLayoutTrace = new RailLayoutTrace(this, DemoWideNavigationRail, DemoNavigationRail, DemoHiddenModalRail);
        Closed += (_, _) => _railLayoutTrace?.Dispose();
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
        var rail = (sender as Control)?.Tag as string == "hidden" ? DemoHiddenModalRail :DemoNavigationRail ;
        try { await rail.State.ToggleAsync(); }
        catch (OperationCanceledException) { }
    }

    private void OnModalDestinationClick(object? sender, RoutedEventArgs e)
    {
        var rail = DemoNavigationRail.Items.Contains(sender) ? DemoNavigationRail : DemoHiddenModalRail;
        foreach (var item in rail.Items.OfType<WideNavigationRailItem>()) item.IsSelected = ReferenceEquals(item, sender);
    }
}
