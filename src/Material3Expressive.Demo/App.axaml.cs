using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Material3Expressive.Demo.ViewModels;
using Material3Expressive.Demo.Views;

namespace Material3Expressive.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avalonia 12 makes DataAnnotations validation opt-in (AppBuilder.WithDataAnnotationsValidation),
            // so there is nothing to unregister here — the CommunityToolkit validator is the only one running.
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}