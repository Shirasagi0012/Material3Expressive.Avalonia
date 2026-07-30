using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class FabPage : UserControl
{
    public FabPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelFab, PanelMenu, PanelToolbar);
    }

    private void OnToggleToolbars(object? sender, RoutedEventArgs e)
    {
        HorizontalToolbar.IsExpanded = !HorizontalToolbar.IsExpanded;
        VerticalToolbar.IsExpanded = HorizontalToolbar.IsExpanded;
    }
}
