using System.Runtime.ExceptionServices;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class ClockDrivenAnimationTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InitializationStop_WaitsForCallbackAndPreservesFailures(bool fail) => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        using var cancellation = new CancellationTokenSource();
        try
        {
            var order = new List<string>();
            var initializationError = new InvalidOperationException("initialization failed");
            var cleanupError = new InvalidOperationException("cleanup failed");
            var driver = new ClockAnimation(window.AnimationRoot, cancellation.Token,
                () => AnimationEndReason.Canceled,
                (_, _) => order.Add("unexpected sample"),
                _ =>
                {
                    order.Add("cleanup");
                    if (fail) throw cleanupError;
                });
            driver.Start(initialize: () =>
            {
                order.Add("initialize");
                cancellation.Cancel();
                Assert.False(driver.Completion.IsCompleted);
                order.Add("callback exits");
                if (fail) throw initializationError;
            });

            if (fail)
            {
                var errors = await Assert.ThrowsAsync<AggregateException>(() => driver.Completion);
                Assert.Equal(new Exception[] { initializationError, cleanupError }, errors.InnerExceptions);
            }
            else Assert.Equal(AnimationEndReason.Canceled, await driver.Completion);
            Assert.Equal(new[] { "initialize", "callback exits", "cleanup" }, order);
            Assert.Equal(0, source.PendingFrameCount);
        }
        finally { window.Close(); }
    });

    [Fact]
    public Task SamplesKeepTheirOwnOperationContextOnTheSharedClock() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var context = new AsyncLocal<string>();
        var seen = new List<string?>();
        context.Value = "first";
        var first = window.AnimationRoot.RunFramesAsync(_ => { seen.Add(context.Value); return false; });
        context.Value = "second";
        var second = window.AnimationRoot.RunFramesAsync(_ => { seen.Add(context.Value); return false; });
        context.Value = "outside";
        source.Pump(TimeSpan.FromMilliseconds(16));
        await first; await second;
        Assert.Equal(new[] { "first", "second" }, seen);
        Assert.Equal("outside", context.Value);
        window.Close();
    });

    [Fact]
    public async Task ExplicitStopContract_DoesNotHideAnAsyncBodyCancellationException()
    {
        var error = new OperationCanceledException("not a requested stop");
        var mutex = new MutatorMutex();
        var result = mutex.MutateAsync<bool>(async _ =>
        {
            await Task.Yield();
            throw error;
        }, interruptedResult: () => false);
        await ((Task)result).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        Assert.True(result.IsFaulted);
        Assert.Same(error, result.Exception!.InnerException);
        Assert.Null(mutex.CurrentCompletion);
    }

    [Fact]
    public Task AlreadyStoppedInfiniteRun_DoesNotSnapAtZeroScale() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        window.AnimationRoot.DurationScale = 0;
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100, new InfiniteRepeatableSpec<double>(new TweenSpec<double>(TimeSpan.FromSeconds(1))));
        var result = await transition.RunAsync(window.AnimationRoot, new CancellationToken(true));
        Assert.Equal(AnimationEndReason.Canceled, result);
        Assert.Equal(0, value.Value);
        Assert.Equal(0, source.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task DeferredCommit_StaysInsideFrameBatchAndStopsWithoutExceptions() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var scope = window.AnimationRoot.CreateChild();
        var order = new List<string>();
        using var after = window.AnimationRoot.FrameClock.RegisterAfterWaiter(() => order.Add("after"));
        Task commit;
        using (AvaloniaSynchronizationContext.Ensure(Dispatcher.UIThread, DispatcherPriority.Render))
            commit = Commit();
        async Task Commit()
        {
            if (await scope.WaitForNextFrameAsync()) order.Add("commit");
        }
        source.Pump(TimeSpan.FromMilliseconds(16));
        await commit;
        Assert.Equal(new[] { "commit", "after" }, order);
        var pending = scope.WaitForNextFrameAsync();
        scope.Dispose();
        Assert.False(await pending);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, source.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task ReplacementIdleDisposalAndCallerStop_DoNotThrowCancellation() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var scope = window.AnimationRoot.CreateChild();
        var value = Animatable.Create(0d);
        var exceptions = 0;
        var thread = Environment.CurrentManagedThreadId;
        void Observe(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (Environment.CurrentManagedThreadId == thread && e.Exception is OperationCanceledException) exceptions++;
        }
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        try
        {
            var first = value.AnimateToAsync(scope, 10);
            source.Pump(TimeSpan.FromMilliseconds(16));
            var second = value.AnimateToAsync(scope, 20);
            Assert.Equal(AnimationEndReason.Interrupted, (await first).EndReason);
            using var idle = new Transition<bool>(false);
            var idleRun = idle.RunAsync(scope);
            scope.Dispose();
            Assert.Equal(AnimationEndReason.ScopeEnded, (await second).EndReason);
            Assert.Equal(AnimationEndReason.ScopeEnded, await idleRun);
            using var cancellation = new CancellationTokenSource();
            var canceled = value.AnimateToAsync(window.AnimationRoot, 30, cancellationToken: cancellation.Token);
            cancellation.Cancel();
            Assert.Equal(AnimationEndReason.Canceled, (await canceled).EndReason);
            Assert.False(value.IsRunning);
            Assert.Equal(0, value.Velocity);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, source.PendingFrameCount);
            Assert.Equal(0, exceptions);
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= Observe; window.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CallbackFailuresRemainFaultsIncludingUnrequestedCancellation(bool cancellation) => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var value = Animatable.Create(0d);
        Exception error = cancellation ? new OperationCanceledException("callback failure") : new InvalidOperationException("callback failure");
        var task = value.AnimateToAsync(window.AnimationRoot, 10, block: _ => throw error);
        source.Pump(TimeSpan.FromMilliseconds(16));
        Assert.True(task.IsFaulted);
        Assert.Same(error, task.Exception!.InnerException);
        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        Assert.False(value.IsRunning);
        Assert.Equal(0, source.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task ReentrantReplacement_WaitsForSampleAndCleanupBeforeTakingOver() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var value = Animatable.Create(0d);
        var order = new List<string>();
        Task<AnimationResult<double>>? second = null;
        var first = value.AnimateToAsync(window.AnimationRoot, 10, block: _ =>
        {
            order.Add("old sample");
            second = value.AnimateToAsync(window.AnimationRoot, 20, block: _ => order.Add("new sample"));
            order.Add("old callback returned");
        });
        source.Pump(TimeSpan.FromMilliseconds(16));
        Assert.Equal(AnimationEndReason.Interrupted, (await first).EndReason);
        Assert.Equal(new[] { "old sample", "old callback returned", "new sample" }, order);
        Assert.Equal(20, value.TargetValue);
        value.Stop();
        Assert.Equal(AnimationEndReason.Interrupted, (await second!).EndReason);
        window.Close();
    });
}
