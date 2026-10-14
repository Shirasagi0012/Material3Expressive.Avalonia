using ComposeAnimation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using System.Linq;
using System;
using System.Threading;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    private void OnDateTimePickerDemo(object? sender, RoutedEventArgs e) => new DateTimePickerDemo().Show(this);
    private void OnCommandBarDemo(object? sender, RoutedEventArgs e) => new CommandBarDemo().Show(this);
    private void OnMenuDemo(object? sender, RoutedEventArgs e) => new MenuDemo().Show(this);
    private void OnTooltipDemo(object? sender, RoutedEventArgs e) => new TooltipDemo().Show(this);
    private void OnSnackbarDemo(object? sender, RoutedEventArgs e) => new SnackbarDemo().Show(this);
    private void OnNavigationDrawerDemo(object? sender, RoutedEventArgs e) => new NavigationDrawerDemo().Show(this);
    private void OnTextFieldDemo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => new TextFieldDemo().Show(this);
    private void OnComboBoxDemo(object? sender, RoutedEventArgs e) => new ComboBoxDemo().Show(this);
    private void OnSecureTextFieldDemo(object? sender, RoutedEventArgs e) => new SecureTextFieldDemo().Show(this);
    private void OnNumericUpDownDemo(object? sender, RoutedEventArgs e) => new NumericUpDownDemo().Show(this);
    private void OnRepeatButtonDemo(object? sender, RoutedEventArgs e) => new RepeatButtonDemo().Show(this);
    private void OnHyperlinkButtonDemo(object? sender, RoutedEventArgs e) => new HyperlinkButtonDemo().Show(this);
    private void OnDropDownButtonDemo(object? sender, RoutedEventArgs e) => new DropDownButtonDemo().Show(this);
    private void OnGroupBoxDemo(object? sender, RoutedEventArgs e) => new GroupBoxDemo().Show(this);
    private void OnExpanderDemo(object? sender, RoutedEventArgs e) => new ExpanderDemo().Show(this);
    private void OnMenuBarDemo(object? sender, RoutedEventArgs e) => new MenuBarDemo().Show(this);
    private void OnTabRowDemo(object? sender, RoutedEventArgs e) => new TabRowDemo().Show(this);
    private void OnProgressIndicatorDemo(object? sender, RoutedEventArgs e) => new ProgressIndicatorDemo().Show(this);
    private void OnLoadingIndicatorDemo(object? sender, RoutedEventArgs e) => new LoadingIndicatorDemo().Show(this);

    private void OnFloatingToolbarDemo(object? sender, RoutedEventArgs e) => new FloatingToolbarDemo().Show(this);
    private void OnScrollBarDemo(object? sender, RoutedEventArgs e) => new ScrollBarDemo().Show(this);
    private void OnAlertDialogDemo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => new AlertDialogDemo().Show(this);
    private void OnPickerDialogDemo(object? sender, RoutedEventArgs e) => new PickerDialogDemo().Show(this);
    private void OnAppBarDemo(object? sender, RoutedEventArgs e) => new AppBarDemo().Show(this);
    private static readonly bool TraceWideRailLayout = false;
    private readonly RailLayoutTrace? _railLayoutTrace;
    private object? _scaffoldTopBar;
    private object? _scaffoldBottomBar;
    private readonly CancellationTokenSource _scaffoldSnackbarRequests = new();

    public MainWindow()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        _scaffoldTopBar = DemoScaffold.TopBar;
        _scaffoldBottomBar = DemoScaffold.BottomBar;
        // Animated.SetDurationScale(this, 10);
        
        // Temporary jitter diagnostics. Set TraceWideRailLayout to false to disable capture.
        if (TraceWideRailLayout)
            _railLayoutTrace = new RailLayoutTrace(this, DemoWideNavigationRail, DemoNavigationRail, DemoHiddenModalRail);
        Closed += (_, _) =>
        {
            _scaffoldSnackbarRequests.Cancel();
            _scaffoldSnackbarRequests.Dispose();
            _railLayoutTrace?.Dispose();
        };
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
    private async void OnScaffoldSnackbarClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await DemoScaffoldSnackbar.State.ShowSnackbarAsync(
                sender is FloatingActionButton ? "New item created" : "Scaffold positions this message above the FAB",
                withDismissAction: true, cancellationToken: _scaffoldSnackbarRequests.Token);
        }
        catch (OperationCanceledException) when (_scaffoldSnackbarRequests.IsCancellationRequested) { }
    }
    private void OnScaffoldDirectionClick(object? sender, RoutedEventArgs e) =>
        DemoScaffold.FlowDirection = DemoScaffold.FlowDirection == Avalonia.Media.FlowDirection.LeftToRight
            ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
    private void OnOpenDialog(object? sender, RoutedEventArgs e) => DemoDialog.IsOpen = true;
    private void OnCloseDialog(object? sender, RoutedEventArgs e) => DemoDialog.IsOpen = false;
    private void OnOpenNestedDialog(object? sender, RoutedEventArgs e) => DemoNestedDialog.IsOpen = true;
    private void OnCloseNestedDialog(object? sender, RoutedEventArgs e) => DemoNestedDialog.IsOpen = false;
    private void OnDialogDismissRequested(object? sender, DismissRequestedEventArgs e) => ((Dialog)sender!).IsOpen = false;

    private void OnChangeBadge(object? sender, RoutedEventArgs e)
    {
        DemoBadge.Content = DemoBadge.Content switch { null => "8", "8" => "999+", _ => null };
        Avalonia.Automation.AutomationProperties.SetName(DemoBadge,
            DemoBadge.Content is { } count ? $"{count} notifications" : "New notifications");
    }

    private void OnToggleExtendedFab(object? sender, RoutedEventArgs e) => DemoExtendedFab.Expanded = !DemoExtendedFab.Expanded;
    private void OnToggleFabVisibility(object? sender, RoutedEventArgs e) => DemoExtendedFab.AnimatedVisible = !DemoExtendedFab.AnimatedVisible;
    private void OnFabCheckedChange(object? sender, FabCheckedChangeRequestedEventArgs e)
    {
        DemoToggleFab.IsChecked = DemoFabMenu.Expanded = e.IsChecked;
    }
    private void OnFabMenuItem(object? sender, RoutedEventArgs e) => DemoToggleFab.IsChecked = DemoFabMenu.Expanded = false;

    private void OnChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not SelectableChip chip) return;
        chip.SetCurrentValue(SelectableChip.IsSelectedProperty, !chip.IsSelected);
        // Application-owned selection can add/remove the icon; the chip animates its visibility.
        chip.LeadingIcon = chip.IsSelected ? new Material3Expressive.Foundation.Rendering.MaterialSymbol
            { Symbol = MaterialSymbols.MaterialSymbolCodepoint.Check } : null;
    }

    private void OnCardClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Card card && ReferenceEquals(e.Source, card))
            DemoCardStatus.Text = $"{card.GetType().Name} clicked";
    }

    private void OnSingleSegmentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is SegmentedButton segment && segment.FindAncestorOfType<SingleChoiceSegmentedButtonRow>() is { } row)
            foreach (var item in row.Items.OfType<SegmentedButton>()) item.IsSelected = ReferenceEquals(item, segment);
    }

    private void OnMultiSegmentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is SegmentedButton segment) segment.SetCurrentValue(SegmentedButton.IsSelectedProperty, !segment.IsSelected);
    }

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
