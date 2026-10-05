using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace Catalog.Framework;

/// <summary>手动推进的动画时钟，用于自动化模式下的确定性逐帧驱动。</summary>
public sealed class ProbeClock : IAnimationFrameClock
{
    private readonly List<TaskCompletionSource<TimeSpan>> _pending = [];
    private TimeSpan _time;

    public int PendingFrameCount => _pending.Count;

    public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

    public async ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<TimeSpan>();
        _pending.Add(completion);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try { return await completion.Task; }
        finally { _pending.Remove(completion); }
    }

    /// <summary>推进时钟并唤醒当前所有等待帧的动画循环。须在 UI 线程调用。</summary>
    public void Step(TimeSpan elapsed)
    {
        VerifyAccess();
        _time += elapsed;
        var batch = _pending.ToArray();
        _pending.Clear();
        foreach (var completion in batch) completion.TrySetResult(_time);
    }
}
