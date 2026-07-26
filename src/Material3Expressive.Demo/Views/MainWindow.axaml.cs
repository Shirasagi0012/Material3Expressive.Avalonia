using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Demo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Reseeds the application theme. Everything that reads a colour role — the stock-control
    /// restyles, the Material controls, and the demo's own chrome — re-resolves from this one
    /// assignment.
    /// </summary>
    private void OnSeedChosen(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hex } || !Color.TryParse(hex, out var seed))
            return;

        if (Application.Current is not { } application)
            return;

        if (MaterialThemeScope.GetTheme(application) is { ColorScheme: TonalSpotScheme scheme })
            scheme.Color = seed;
    }

    private void OnDarkToggled(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = DarkToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void OnPageChanged(object? sender, SelectionChangedEventArgs e)
    {
        // SelectedIndex="0" fires this while the XAML is still being populated, before the named
        // fields exist.
        if (PageHost is null)
            return;

        PageHost.Content = Pages.SelectedIndex switch
        {
            1 => new TypographyView(),
            _ => new ButtonsView()
        };
    }
}
