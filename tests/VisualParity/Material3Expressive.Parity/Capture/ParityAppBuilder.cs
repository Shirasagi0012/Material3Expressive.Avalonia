using Avalonia;
using Avalonia.Headless;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace Material3Expressive.Parity;

public sealed class ParityApplication : Application;

public static class ParityAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<ParityApplication>()
        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions
            { UseHeadlessDrawing = false, ShouldRenderOnUIThread = true, Fps = 60 }).UseRoundedSymbols().WithRobotoFont();
}

