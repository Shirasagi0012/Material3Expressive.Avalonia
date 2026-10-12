using System.Reflection;
using Avalonia.Animation.Easings;
using Avalonia.Threading;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;

namespace ComposeAnimation.Test;

/// <summary>Stage 3 (D19-D22): state-only runtime types and the runs bound to a FrameScope.</summary>
public class RuntimeScopeBindingTest
{
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None).Unwrap();
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);
    private static TweenSpec<T> Linear<T>(double ms) => new(Ms(ms), easing: new LinearEasing());

    [Theory]
    [InlineData(typeof(Animatable<double>))]
    [InlineData(typeof(Transition))]
    [InlineData(typeof(Transition<bool>))]
    [InlineData(typeof(InfiniteTransition))]
    [InlineData(typeof(SeekableTransitionState<bool>))]
    public void StateTypes_HoldNoClockLifetimeOrScope(Type type)
    {
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Assert.False(typeof(FrameScope).IsAssignableFrom(field.FieldType), $"{t.Name}.{field.Name}");
                Assert.False(typeof(IFrameSource).IsAssignableFrom(field.FieldType), $"{t.Name}.{field.Name}");
                Assert.NotEqual(typeof(CancellationToken), field.FieldType);
            }
    }

    [Fact]
    public Task Animatable_ScopeDurationScaleChangeMidRun_AppliesFromNextFrameWithoutJump() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var value = Animatable.Create(0d);
        var run = value.AnimateToAsync(scope, 100, Linear<double>(100));
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(0));
        clock.Pump(Ms(40));
        Assert.Equal(40, value.Value, 8);
        scope.DurationScale = 2;
        clock.Pump(Ms(40)); // 40ms wall at scale 2 => +20ms play
        Assert.Equal(60, value.Value, 8);
        scope.DurationScale = 1;
        clock.Pump(Ms(20));
        Assert.Equal(80, value.Value, 8);
        clock.Pump(Ms(20));
        await run;
        Assert.Equal(100, value.Value);
        scope.Dispose();
        window.Close();
    });

    [Fact]
    public Task Transition_ScopeDurationScaleChangeMidRun_AppliesFromNextFrameWithoutJump() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var state = new MutableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Linear<double>(100));
        state.TargetState = true;
        using var cts = new CancellationTokenSource();
        var run = transition.RunAsync(scope, cts.Token);
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(0));
        clock.Pump(Ms(40));
        Assert.Equal(40, value.Value, 8);
        scope.DurationScale = 2;
        clock.Pump(Ms(40));
        Assert.Equal(60, value.Value, 8);
        transition.DurationScale = 0.5; // own scale times scope scale: 1
        clock.Pump(Ms(20));
        Assert.Equal(80, value.Value, 8);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        scope.Dispose();
        window.Close();
    });

    [Fact]
    public Task Seek_UsesTheScopeDurationScale() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        scope.DurationScale = 2;
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Linear<double>(100));
        var running = state.AnimateToAsync(scope, true);
        clock.Pump(Ms(0));
        clock.Pump(Ms(50));
        Assert.Equal(25, value.Value, 8);
        clock.Pump(Ms(150));
        await running;
        Assert.Equal(100, value.Value, 8);
        scope.Dispose();
        window.Close();
    });

    [Fact]
    public Task SeekRacingRunAsync_TakesStateMutexBeforeRunMutex_AndNeverDeadlocks() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var state = new SeekableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state);
        var value = transition.AnimateDouble(s => s ? 100 : 0, _ => Linear<double>(100));

        // A run holds the run mutex; a seek (state, then run mutex) takes it over.
        var run = transition.RunAsync(scope);
        Dispatcher.UIThread.RunJobs();
        var seek = state.AnimateToAsync(scope, true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        // A new run takes the run mutex from the seek while a second seek queues on the state
        // mutex. With the opposite order these three would wait on each other forever.
        var run2 = transition.RunAsync(scope);
        var seek2 = state.AnimateToAsync(scope, true);
        var all = Task.WhenAll(
            Swallow(seek), Swallow(run2), Swallow(seek2));
        for (var i = 0; i < 200 && !seek2.IsCompleted; i++)
            clock.Pump(Ms(16));
        await seek2;
        Assert.True(seek.IsCompleted);
        Assert.Equal(100, value.Value, 8);
        transition.Dispose();
        await all;
        window.Close();

        static async Task Swallow(Task task) { try { await task; } catch (OperationCanceledException) { } }
    });

    [Fact]
    public Task Takeover_OldRunsFinallyCompletesBeforeTheNewRunsFirstSample() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var events = new List<string>();
        var value = Animatable.Create(0d);
        var first = value.AnimateToAsync(scope, 100, Linear<double>(100));
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(0));
        clock.Pump(Ms(20));
        var observedFirst = first.ContinueWith(_ => events.Add("first ended"), TaskContinuationOptions.ExecuteSynchronously);
        var second = value.AnimateToAsync(scope, 0, Linear<double>(100), block: _ => events.Add("second sample"));
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(16));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await observedFirst;
        Assert.Equal("first ended", events[0]);
        Assert.Contains("second sample", events);
        value.Stop();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        window.Close();
    });

    [Fact]
    public Task InfiniteTransition_TakeoverAcrossScopes_ContinuesPlayTime() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var a = window.AnimationRoot.CreateChild();
        var b = window.AnimationRoot.CreateChild();
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100,
            new InfiniteRepeatableSpec<double>(Linear<double>(1000), RepeatMode.Restart));
        var first = transition.RunAsync(a);
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(0));
        clock.Pump(Ms(250));
        Assert.Equal(25, value.Value, 8);
        var second = transition.RunAsync(b);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(16)); // resumes: the takeover frame does not count inactive time
        clock.Pump(Ms(100));
        Assert.Equal(35, value.Value, 8);
        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        a.Dispose(); b.Dispose();
        window.Close();
    });

    [Fact]
    public Task Runs_RejectAnotherThread() => OnUi(() =>
    {
        var window = new AnimationTestWindow(new ManualFrameSource());
        var value = Animatable.Create(0d);
        Exception? error = null;
        // A dedicated thread: a blocking wait on Task.Run could inline it onto the UI thread.
        var thread = new Thread(() =>
        {
            try { error = value.AnimateToAsync(window.AnimationRoot, 1).Exception?.InnerException; }
            catch (Exception thrown) { error = thrown; }
        });
        thread.Start();
        thread.Join();
        Assert.IsType<InvalidOperationException>(error);
        window.Close();
        return Task.CompletedTask;
    });
}
