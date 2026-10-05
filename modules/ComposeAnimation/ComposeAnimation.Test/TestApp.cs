using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ComposeAnimation.Test;

public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}

internal static class HeadlessApp
{
    private static readonly Lazy<HeadlessUnitTestSession> SessionLazy =
        new(() => HeadlessUnitTestSession.StartNew(typeof(TestApp)));

    internal static HeadlessUnitTestSession Session => SessionLazy.Value;
}
