using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelAppBars, PanelTabs, PanelBarRail, PanelDrawer);
    }

    private void OnOpenDrawer(object? sender, RoutedEventArgs e)
    {
        Drawer.IsOpen = !Drawer.IsOpen;
    }
}
