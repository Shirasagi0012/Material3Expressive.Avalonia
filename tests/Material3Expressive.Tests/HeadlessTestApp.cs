using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Material3Expressive.Styles;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Material3Expressive.Tests.HeadlessTestAppBuilder))]

// The headless platform is a process-wide singleton with a dispatcher pinned to one thread. xunit
// runs collections in parallel by default, so two collections can race to stand it up and the
// loser initialises a second compositor from the wrong thread — which surfaces as an unrelated
// test failing in cleanup with "the calling thread cannot access this object". The suite runs in
// under a second either way, so serialising costs nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
            .UseMaterial3Expressive()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
