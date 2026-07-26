using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Material3Expressive.Demo.Views;

public partial class NavigationView : UserControl
{
    public NavigationView()
    {
        InitializeComponent();
    }

    private void OnOpenDrawer(object? sender, RoutedEventArgs e)
    {
        Drawer.IsOpen = !Drawer.IsOpen;
    }
}
