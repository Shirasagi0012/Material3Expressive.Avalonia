using System.Runtime.CompilerServices;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal interface IAnimatedStateCommit
{
    Action CaptureCommit();
    void RejectCommitFrame(Exception error, bool report);
}

// One queue and demand waiter per existing frame environment. DrainStale is registered
// by AnimationScope first; these commits precede FrameClock's waiter snapshot.
internal sealed class AnimatedStateCoordinator
{
    private static readonly ConditionalWeakTable<FrameClock, AnimatedStateCoordinator> Coordinators = new();
    private readonly FrameClock _clock;
    private readonly HashSet<IAnimatedStateCommit> _pending = [];
    private CancellationTokenSource? _wake;
    private bool _committing;
    private bool _failed;

    private AnimatedStateCoordinator(FrameClock clock)
    {
        _clock = clock;
        clock.RegisterBeforeFrame(Commit);
    }

    public static AnimatedStateCoordinator Get(AnimationScope scope)
        => Coordinators.GetValue(scope.FrameClock, static clock => new(clock));

    public void Schedule(IAnimatedStateCommit state)
    {
        if (_failed) return;
        _pending.Add(state);
        if (_wake is not null) return;
        var wake = _wake = new CancellationTokenSource();
        _ = WakeAsync(wake);
    }

    public void Remove(IAnimatedStateCommit state)
    {
        _pending.Remove(state);
        if (_pending.Count == 0 && !_committing) _wake?.Cancel();
    }

    private void Commit()
    {
        if (_pending.Count == 0) return;
        var batch = _pending.Select(state => state.CaptureCommit()).ToArray();
        _pending.Clear();
        _committing = true;
        try { foreach (var commit in batch) commit(); }
        finally { _committing = false; }
    }

    private async Task WakeAsync(CancellationTokenSource wake)
    {
        try
        {
            do { await _clock.WaitAsync(wake.Token); }
            while (_pending.Count != 0);
        }
        catch (OperationCanceledException) when (wake.IsCancellationRequested) { }
        catch (Exception error)
        {
            // Source failure is already reported and permanently disables this clock.
            // A foreign before-frame hook may fail only this batch; it must not disable
            // future commits after that hook is removed.
            _failed = _clock.IsUnavailable;
            var pending = _pending.ToArray();
            _pending.Clear();
            foreach (var state in pending) state.RejectCommitFrame(error, report: !_failed);
        }
        finally
        {
            if (ReferenceEquals(_wake, wake)) _wake = null;
            wake.Dispose();
            if (!_failed && _pending.Count != 0 && _wake is null)
            {
                var next = _wake = new CancellationTokenSource();
                _ = WakeAsync(next);
            }
        }
    }
}
