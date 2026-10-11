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
                : System.Array.IndexOf(desktop.Args ?? [], "--expanders") >= 0
                ? new ExpanderDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--group-boxes") >= 0
                ? new GroupBoxDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--drop-down-buttons") >= 0
                ? new DropDownButtonDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--hyperlink-buttons") >= 0
                ? new HyperlinkButtonDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--repeat-buttons") >= 0
                ? new RepeatButtonDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--numeric-up-down") >= 0
                ? new NumericUpDownDemo()
                : System.Array.IndexOf(desktop.Args ?? [], "--text-fields") >= 0
                ? new TextFieldDemo()
                : new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
