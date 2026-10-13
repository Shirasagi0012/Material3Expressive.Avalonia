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

/// <summary>
/// The one way tests run on the headless UI thread. The <see cref="Func{Task}"/> overload unwraps
/// the dispatched task so an async body is awaited (binding an async lambda to the
/// <see cref="Action"/> overload would make it async void; <c>AsyncVoidGuardTest</c> rejects that).
/// </summary>
internal static class Ui
{
    internal static Task OnUi(Action action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    internal static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None).Unwrap();

    /// <summary>
    /// Drains the UI queue until <paramref name="condition"/> holds. Headless dispatcher timers do
    /// not advance on their own, so this loops on RunJobs (and yields) instead of waiting.
    /// </summary>
    internal static async Task WaitUntilAsync(Func<bool> condition, int maximumTurns = 100)
    {
        for (var i = 0; i < maximumTurns && !condition(); i++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }
        Assert.True(condition(), $"Condition did not hold within {maximumTurns} dispatcher turns.");
    }
}
