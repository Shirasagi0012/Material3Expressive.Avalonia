using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Catalog;

public partial class MenuDemo : Window
{
    public MenuDemo()
    {
        VisualAnimationScope.Initialize(this); InitializeComponent();
        FirstGroup.Shapes = MenuDefaults.GroupShape(0, 2); LastGroup.Shapes = MenuDefaults.GroupShape(1, 2);
        ReadOnlyItem.Shapes = NameItem.Shapes = MenuDefaults.ItemShape(0, 2);
        DetailsItem.Shapes = DateItem.Shapes = MenuDefaults.ItemShape(1, 2);
    }
    private void OnDropdown(object? sender, RoutedEventArgs e) => Dropdown.IsOpen = true;
    private void OnGrouped(object? sender, RoutedEventArgs e) => Grouped.IsOpen = true;
    private void OnDropdownDismiss(object? sender, DismissRequestedEventArgs e) => ((DropdownMenuPopup)sender!).IsOpen = false;
    private void OnDropdownAction(object? sender, RoutedEventArgs e)
    {
        Status.Text = $"Executed: {((DropdownMenuItem)sender!).Content}";
        Dropdown.IsOpen = Grouped.IsOpen = false;
    }
    private void OnCheckedChange(object? sender, MenuCheckedChangeEventArgs e)
    { ReadOnlyItem.IsChecked = e.Value; Status.Text = $"Read only: {e.Value}"; }
    private void OnSelectName(object? sender, RoutedEventArgs e)
    { NameItem.IsSelected = true; DateItem.IsSelected = false; }
    private void OnSelectDate(object? sender, RoutedEventArgs e)
    { NameItem.IsSelected = false; DateItem.IsSelected = true; }
    private void OnAction(object? sender, RoutedEventArgs e) => Status.Text = $"Executed: {((MenuItem)sender!).Header}";
    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender!;
        Status.Text = $"{item.Header}: {item.IsChecked}";
    }
    private void OnTheme(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void OnDirection(object? sender, RoutedEventArgs e) =>
        FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

