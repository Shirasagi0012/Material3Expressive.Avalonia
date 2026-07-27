using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class ButtonsPage : UserControl
{
    public ButtonsPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelCommon, PanelIcon, PanelToggle, PanelGroups);
    }
}
