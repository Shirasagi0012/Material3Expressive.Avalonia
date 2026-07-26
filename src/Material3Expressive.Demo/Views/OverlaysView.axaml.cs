using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Material3Expressive.Demo.Views;

public partial class OverlaysView : UserControl
{
    public OverlaysView()
    {
        InitializeComponent();
    }

    private void OnShowDialog(object? sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = true;
    }

    private void OnCloseDialog(object? sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = false;
    }

    private void OnShowSheet(object? sender, RoutedEventArgs e)
    {
        Sheet.IsOpen = true;
    }

    private void OnShowSnackbar(object? sender, RoutedEventArgs e)
    {
        Snackbar.IsOpen = true;
    }

    private void OnSearch(object? sender, RoutedEventArgs e)
    {
        Search.IsOpen = !Search.IsOpen;
    }
}
