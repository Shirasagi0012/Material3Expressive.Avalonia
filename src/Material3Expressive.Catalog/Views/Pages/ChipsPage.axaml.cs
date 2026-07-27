using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class ChipsPage : UserControl
{
    public ChipsPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelVariants, PanelStates);
    }
}
