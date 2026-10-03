using ComposeAnimation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using System.Linq;
using System;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    private static readonly bool TraceWideRailLayout = false;
    private readonly RailLayoutTrace? _railLayoutTrace;
    private object? _scaffoldTopBar;
    private object? _scaffoldBottomBar;
    private object? _scaffoldSnackbar;

    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        _scaffoldTopBar = DemoScaffold.TopBar;
        _scaffoldBottomBar = DemoScaffold.BottomBar;
        _scaffoldSnackbar = DemoScaffold.SnackbarHost;
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

    private void OnScaffoldPositionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string position } && Enum.TryParse<FabPosition>(position, out var value))
            DemoScaffold.FloatingActionButtonPosition = value;
    }
    private void OnScaffoldBarsClick(object? sender, RoutedEventArgs e)
    {
        var show = DemoScaffold.TopBar is null;
        DemoScaffold.TopBar = show ? _scaffoldTopBar : null;
        DemoScaffold.BottomBar = show ? _scaffoldBottomBar : null;
    }
    private void OnScaffoldSnackbarClick(object? sender, RoutedEventArgs e) =>
        DemoScaffold.SnackbarHost = DemoScaffold.SnackbarHost is null ? _scaffoldSnackbar : null;
    private void OnScaffoldDirectionClick(object? sender, RoutedEventArgs e) =>
        DemoScaffold.FlowDirection = DemoScaffold.FlowDirection == Avalonia.Media.FlowDirection.LeftToRight
            ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;

    private void OnGroupCheckBoxClick(object? sender, RoutedEventArgs e)
    {
        // Native CheckBox has already toggled. The application decides the group's meaning.
        var selected = GroupCheckBox.IsChecked != false;
        GroupFirst.SetCurrentValue(CheckBox.IsCheckedProperty, selected);
        GroupSecond.SetCurrentValue(CheckBox.IsCheckedProperty, selected);
        GroupCheckBox.SetCurrentValue(CheckBox.IsCheckedProperty, selected);
    }

    private void OnGroupChildClick(object? sender, RoutedEventArgs e)
    {
        GroupCheckBox.SetCurrentValue(CheckBox.IsCheckedProperty,
            GroupFirst.IsChecked == GroupSecond.IsChecked ? GroupFirst.IsChecked : null);
    }

    private void OnBarDestinationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is NavigationBarItem destination && destination.FindAncestorOfType<NavigationBar>() is { } bar)
            foreach (var item in bar.Items.OfType<NavigationBarItem>())
                item.IsSelected = ReferenceEquals(item, destination);
    }

    private void OnShortBarDestinationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ShortNavigationBarItem destination && destination.FindAncestorOfType<ShortNavigationBar>() is { } bar)
            foreach (var item in bar.Items.OfType<ShortNavigationBarItem>())
                item.IsSelected = ReferenceEquals(item, destination);
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
