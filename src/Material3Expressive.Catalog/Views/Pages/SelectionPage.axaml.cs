using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class SelectionPage : UserControl
{
    public SelectionPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelControls, PanelSlider);
    }
}
