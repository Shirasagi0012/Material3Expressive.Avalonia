using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Material3Expressive.Styles;

[assembly: AvaloniaTestApplication(typeof(Material3Expressive.Tests.HeadlessTestAppBuilder))]

namespace Material3Expressive.Tests;

/// <summary>
/// The test application carries the real <see cref="MaterialStyles" />, including the TopLevel
/// injection style, so tests exercise the same wiring an app gets.
/// </summary>
public sealed class HeadlessTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new MaterialStyles());
    }
}

public static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<HeadlessTestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
