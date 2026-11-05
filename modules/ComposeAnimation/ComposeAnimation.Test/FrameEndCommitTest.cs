using Avalonia.Threading;

namespace ComposeAnimation.Test;

public class FrameEndCommitTest
{
    [Fact]
    public Task FrameEndCommitsAfterAllSamplesWithoutWaitingAnExtraFrame() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        var scope = window.AnimationRoot.CreateChild();
        var log = new List<string>();
        var first = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            log.Add("first sample");
            await scope.WaitForFrameEndAsync(ct);
            log.Add("commit");
        });
        var second = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            log.Add("second sample");
        });
        source.SendFrame(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["first sample", "second sample", "commit"], log);
        await Task.WhenAll(first, second);
        Assert.Equal(0, source.PendingFrameCount);

        var betweenFrames = scope.WaitForFrameEndAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.False(betweenFrames.IsCompleted);
        source.SendFrame(TimeSpan.FromMilliseconds(32));
        Dispatcher.UIThread.RunJobs();
        await betweenFrames;
        Assert.Equal(0, source.PendingFrameCount);
        scope.Dispose(); window.Close();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CancellationAfterSamplingPreventsQueuedCommit(bool endScope) => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        using var cancellation = new CancellationTokenSource();
        var window = new AnimationTestWindow(source);
        var scope = window.AnimationRoot.CreateChild();
        var committed = false;
        var pending = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            await scope.WaitForFrameEndAsync(cancellation.Token);
            committed = true;
        });
        var cancel = window.AnimationRoot.Launch(async ct =>
        {
            await window.AnimationRoot.NextFrameAsync(ct);
            if (endScope) scope.Dispose(); else cancellation.Cancel();
        });
        source.SendFrame(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
        await cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(committed);
        Assert.Equal(0, source.PendingFrameCount);
        scope.Dispose(); window.Close();
    });
}
