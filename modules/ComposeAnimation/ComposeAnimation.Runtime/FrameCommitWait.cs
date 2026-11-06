using System.Threading.Tasks.Sources;

namespace ComposeAnimation.Runtime;

// A one-shot composition boundary, not an animation loop. Unlike Task awaits, its
// continuation must stay inside the ordered frame batch regardless of the caller's context.
internal sealed class FrameCommitWait : IValueTaskSource<bool>
{
    private ManualResetValueTaskSourceCore<bool> _result = new() { RunContinuationsAsynchronously = false };
    internal ValueTask<bool> Task => new(this, _result.Version);
    internal void Complete(AnimationEndReason reason, Exception? error)
    {
        if (error is null) _result.SetResult(reason == AnimationEndReason.Finished);
        else _result.SetException(error);
    }
    public bool GetResult(short token) => _result.GetResult(token);
    public ValueTaskSourceStatus GetStatus(short token) => _result.GetStatus(token);
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _result.OnCompleted(continuation, state, token, flags & ~ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
}
