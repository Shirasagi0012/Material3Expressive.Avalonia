using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class MutatorMutexTest
{
    [Fact]
    public async Task EqualPriority_InterruptsCurrentMutation()
    {
        var mutex = new MutatorMutex();
        var firstStarted = new TaskCompletionSource();
        var neverComplete = new TaskCompletionSource();

        var first = mutex.MutateAsync(async ct =>
        {
            firstStarted.SetResult();
            await neverComplete.Task.WaitAsync(ct);
            return 1;
        });
        await firstStarted.Task;

        var second = mutex.MutateAsync(ct => Task.FromResult(2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Equal(2, await second);
    }

    [Fact]
    public async Task NewMutation_StartsOnlyAfterPreviousCleanup()
    {
        var mutex = new MutatorMutex();
        var firstStarted = new TaskCompletionSource();
        var neverComplete = new TaskCompletionSource();
        var cleanupDone = false;

        var first = mutex.MutateAsync(async ct =>
        {
            firstStarted.SetResult();
            try
            {
                await neverComplete.Task.WaitAsync(ct);
                return 0;
            }
            finally
            {
                cleanupDone = true;
            }
        });
        await firstStarted.Task;

        var second = mutex.MutateAsync(ct =>
        {
            Assert.True(cleanupDone, "New mutation must not start before the interrupted one's cleanup.");
            return Task.FromResult(1);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await second;
    }

    [Fact]
    public async Task LowerPriority_IsRejectedWithoutRunning()
    {
        var mutex = new MutatorMutex();
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        var first = mutex.MutateAsync(async ct =>
        {
            started.SetResult();
            await release.Task;
            return 1;
        }, MutatePriority.UserInput);
        await started.Task;

        var bodyRan = false;
        var rejected = mutex.MutateAsync(ct =>
        {
            bodyRan = true;
            return Task.FromResult(2);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejected);
        Assert.False(bodyRan);

        release.SetResult();
        Assert.Equal(1, await first);
    }

    [Fact]
    public async Task CancelCurrent_CancelsRunningMutation()
    {
        var mutex = new MutatorMutex();
        var started = new TaskCompletionSource();

        var task = mutex.MutateAsync(async ct =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return 1;
        });
        await started.Task;

        mutex.CancelCurrent();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Null(mutex.CurrentCompletion);
    }

    [Fact]
    public async Task CallerToken_CancelsMutation()
    {
        var mutex = new MutatorMutex();
        var started = new TaskCompletionSource();
        using var cts = new CancellationTokenSource();

        var task = mutex.MutateAsync(async ct =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return 1;
        }, cancellationToken: cts.Token);
        await started.Task;

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
