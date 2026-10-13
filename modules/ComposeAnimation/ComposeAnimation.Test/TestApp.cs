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

    /// <summary>Default <see cref="WaitUntilAsync"/> budget: 15 s, or <c>COMPOSE_ANIMATION_TEST_WAIT_MS</c>.</summary>
    internal static int DefaultWaitMilliseconds { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("COMPOSE_ANIMATION_TEST_WAIT_MS"), out var ms) && ms > 0 ? ms : 15000;

    /// <summary>
    /// Drains the UI queue until <paramref name="condition"/> holds. Headless dispatcher timers do
    /// not advance on their own, so this loops on RunJobs (and yields) instead of waiting. Bounded
    /// by time, not turns: a TaskCompletionSource with asynchronous continuations (the idle wake of
    /// InfiniteTransition) hops through the thread pool before it posts back, which a fixed turn
    /// budget can outrun when the pool is busy. The condition is evaluated once per turn and the
    /// last result is the verdict, so a condition with side effects is not re-run for the assert.
    /// </summary>
    internal static async Task WaitUntilAsync(Func<bool> condition, int? timeoutMilliseconds = null,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string expression = "")
    {
        var budget = timeoutMilliseconds ?? DefaultWaitMilliseconds;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        bool held;
        while (!(held = condition()) && watch.ElapsedMilliseconds < budget)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }
        Assert.True(held, $"Condition `{expression}` did not hold within {budget} ms ({watch.ElapsedMilliseconds} ms elapsed).");
    }
}
