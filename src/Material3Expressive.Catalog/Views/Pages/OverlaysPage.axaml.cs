using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class OverlaysPage : UserControl
{
    public OverlaysPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelOverlays, PanelBadges, PanelPickers, PanelCarousel, PanelTooltipMenu);
    }

    private void OnShowDialog(object? sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = true;
    }

    private void OnCloseDialog(object? sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = false;
    }

    private void OnShowSheet(object? sender, RoutedEventArgs e)
    {
        Sheet.IsOpen = true;
    }

    private void OnShowSnackbar(object? sender, RoutedEventArgs e)
    {
        _ = SnackbarHost.State.ShowSnackbarAsync(
            "Conversation archived",
            actionLabel: "Undo");
    }

    private async void OnRefreshRequested(object? sender, EventArgs e)
    {
        if (PullRefresh.IsRefreshing)
            return;
        PullRefresh.IsRefreshing = true;
        await Task.Delay(1200);
        PullRefresh.IsRefreshing = false;
    }
}
