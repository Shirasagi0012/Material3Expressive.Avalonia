using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Material3Expressive.Catalog.Views.Pages;
using Material3Expressive.Controls;

namespace Material3Expressive.Catalog.Views;

public partial class MainWindow : Window
{
    // Pages are kept alive once visited, so switching back restores scroll and control state.
    private readonly UserControl?[] _pages = new UserControl?[10];

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnThemeToggled(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void OnRailItemChecked(object? sender, RoutedEventArgs e)
    {
        // SelectedIndex="0" checks the first item while the XAML is still being populated,
        // before the named fields exist.
        if (sender is not MaterialNavigationItem { IsChecked: true } item || PageHost is null)
            return;

        PageHost.Content = GetPage(Rail.Items.IndexOf(item));
    }

    private UserControl GetPage(int index) => _pages[index] ??= index switch
    {
        1 => new FabPage(),
        2 => new SelectionPage(),
        3 => new TextFieldsPage(),
        4 => new ChipsPage(),
        5 => new CardsPage(),
        6 => new ProgressPage(),
        7 => new Pages.NavigationPage(),
        8 => new OverlaysPage(),
        9 => new SurfaceListPage(),
        _ => new ButtonsPage()
    };
}
