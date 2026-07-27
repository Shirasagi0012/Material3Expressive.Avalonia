using Avalonia;
using Avalonia.Headless;
using Avalonia.Skia;
using Avalonia.Themes.Fluent;
using Material3Expressive.Styles;

namespace Material3Expressive.VisualTesting.Rendering;

/// <summary>A separate application because the normal tests use the non-Skia headless backend.</summary>
public sealed class VisualTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new MaterialStyles());
    }
}

public static class VisualTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<VisualTestApplication>()
            .UseMaterial3Expressive()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
                ShouldRenderOnUIThread = true,
                Fps = 60
            });
    }
}
