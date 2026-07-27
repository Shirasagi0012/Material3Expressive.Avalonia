using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class ProgressPage : UserControl
{
    public ProgressPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelLinear, PanelCircular);
    }
}
