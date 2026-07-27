using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class CardsPage : UserControl
{
    public CardsPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelVariants, PanelCompositions);
    }
}
