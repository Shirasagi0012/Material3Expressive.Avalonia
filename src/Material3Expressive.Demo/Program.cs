using Avalonia;
using System;

using Material3Expressive;
using MaterialTypography.Roboto;

namespace Material3Expressive.Demo;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseRobotoFont()
            .UseMaterial3Expressive();

#if DEBUG
        builder = builder.WithDeveloperTools();
#endif

        return builder.LogToTrace();
    }
}
