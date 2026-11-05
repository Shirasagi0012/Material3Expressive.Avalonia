using System.Runtime.CompilerServices;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal interface IAnimatedStateCommit
{
    Action CaptureCommit();
    void RejectCommitFrame(Exception error, bool report);
}

// One queue and demand waiter per phase in an existing frame environment. Before-frame
// commits follow DrainStale; after-frame commits follow the complete waiter sample batch.
internal sealed class AnimatedStateCoordinator
{
    private sealed class CoordinatorsForClock(FrameClock clock)
    {
        internal AnimatedStateCoordinator? Before;
        internal AnimatedStateCoordinator? After;
        internal AnimatedStateCoordinator Get(AnimationCommitPhase phase) => phase == AnimationCommitPhase.BeforeFrame
            ? Before ??= new(clock, phase) : After ??= new(clock, phase);
    }
    private static readonly ConditionalWeakTable<FrameClock, CoordinatorsForClock> Coordinators = new();
    private readonly FrameClock _clock;
    private readonly AnimationCommitPhase _phase;
    private readonly HashSet<IAnimatedStateCommit> _pending = [];
    private CancellationTokenSource? _wake;
    private bool _committing;
    private bool _failed;

    private AnimatedStateCoordinator(FrameClock clock, AnimationCommitPhase phase)
    {
        _clock = clock;
        _phase = phase;
        if (phase == AnimationCommitPhase.BeforeFrame) clock.RegisterBeforeFrame(Commit);
    }

    public static AnimatedStateCoordinator Get(AnimationScope scope, AnimationCommitPhase phase)
        => Coordinators.GetValue(scope.FrameClock, static clock => new(clock)).Get(phase);

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
            do
            {
                if (_phase == AnimationCommitPhase.BeforeFrame || !_clock.IsDispatchingFrame)
                    await _clock.WaitAsync(wake.Token);
                if (_phase == AnimationCommitPhase.AfterFrame)
                {
                    // Same boundary as AnimationScope.WaitForFrameEndAsync: an in-frame
                    // target joins this frame's commit; an external target waits for a frame.
                    // The old run samples first. Interruption policy belongs to the state.
                    await Dispatcher.Yield(FrameClock.ResumePriority);
                    wake.Token.ThrowIfCancellationRequested();
                    Commit();
                }
            }
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
