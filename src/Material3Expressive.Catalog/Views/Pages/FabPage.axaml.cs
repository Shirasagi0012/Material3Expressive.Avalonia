using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class FabPage : UserControl
{
    public FabPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelFab, PanelMenu);
    }
}
