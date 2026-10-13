using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Material3Expressive.Catalog;

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
            desktop.MainWindow = System.Array.IndexOf(desktop.Args ?? [], "--app-bars") >= 0
                ? new AppBarDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--secure-text-fields") >= 0
                ? new SecureTextFieldDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--text-fields") >= 0
                ? new TextFieldDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--combo-box") >= 0
                ? new ComboBoxDemo()
                : new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
