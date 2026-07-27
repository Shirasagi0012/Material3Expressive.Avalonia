using Avalonia.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class TextFieldsPage : UserControl
{
    public TextFieldsPage()
    {
        InitializeComponent();
        TabSwitcher.Wire(Tabs, PanelField, PanelSearch);
    }
}
