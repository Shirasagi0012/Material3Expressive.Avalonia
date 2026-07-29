using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3Expressive.Controls;

namespace Material3Expressive.Catalog.Views.Pages;

public partial class SurfaceListPage : UserControl
{
    public SurfaceListPage()
    {
        InitializeComponent();
    }

    private void OnClickItem(object? sender, RoutedEventArgs e)
    {
        InteractionStatus.Text = "Click invoked its application action without changing a selection state.";
    }

    private void OnLongClickItem(object? sender, RoutedEventArgs e)
    {
        InteractionStatus.Text = "Long press invoked the row's secondary action without also invoking Click.";
    }

    private void OnSingleSelectionClick(object? sender, RoutedEventArgs e)
    {
        var isPersonal = ReferenceEquals(sender, PersonalListItem);
        PersonalListItem.IsSelected = isPersonal;
        WorkListItem.IsSelected = !isPersonal;
        InteractionStatus.Text = isPersonal
            ? "The host selected Personal."
            : "The host selected Work.";
    }
}
