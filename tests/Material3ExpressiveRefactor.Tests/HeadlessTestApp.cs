using Avalonia;
using Avalonia.Headless;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Material3ExpressiveRefactor.Tests.HeadlessTestAppBuilder))]

// The headless platform is a process-wide singleton with a dispatcher pinned to one thread; xunit
// runs collections in parallel by default and the loser of that race initialises a second
// compositor from the wrong thread. Same reasoning as the Material3Expressive.Tests suite.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// A bare application on purpose: the token system under test must not depend on any stock theme.
/// </summary>
public sealed class HeadlessTestApplication : Application;

public static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
