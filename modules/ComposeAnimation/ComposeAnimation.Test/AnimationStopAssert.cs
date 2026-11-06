using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

internal static class AnimationStopAssert
{
    internal static async Task StoppedAsync(Task task)
    {
        await task;
        Assert.True(task.IsCompletedSuccessfully);
        if (task is Task<AnimationEndReason> result) Stopped(result.Result);
    }

    internal static async Task StoppedAsync<T>(Task<AnimationResult<T>> task)
        => Stopped((await task).EndReason);

    private static void Stopped(AnimationEndReason reason)
        => Assert.True(reason is AnimationEndReason.Interrupted or AnimationEndReason.ScopeEnded or AnimationEndReason.Canceled,
            $"Expected a stopped run, got {reason}.");
}
