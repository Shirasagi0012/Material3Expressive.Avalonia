using Avalonia;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace Reply;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithRobotoFont()
        .UseRoundedSymbols()
        .LogToTrace();
}
