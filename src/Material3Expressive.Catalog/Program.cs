using Avalonia;
using System;
using Avalonia.Media;
using MaterialTypography.Roboto;

namespace Material3Expressive.Catalog;

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
            .With(new FontManagerOptions
            {
                DefaultFamilyName = RobotoFontCollection.FamilyUri
            })
            .UsePlatformDetect()
            .WithRobotoFont()
            .UseMaterial3Expressive();

#if DEBUG
        builder = builder.WithDeveloperTools();
#endif

        return builder.LogToTrace();
    }
}
