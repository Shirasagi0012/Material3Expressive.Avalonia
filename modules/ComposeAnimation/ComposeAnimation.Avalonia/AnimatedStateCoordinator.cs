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
    private FrameClock.Registration? _wake;
    private int _generation;
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
        Arm();
    }

    public void Remove(IAnimatedStateCommit state)
    {
        _pending.Remove(state);
        if (_pending.Count == 0 && !_committing)
        {
            _generation++; _wake?.Dispose(); _wake = null;
        }
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

    private void Arm()
    {
        var generation = ++_generation;
        _wake = _clock.CreateRegistration(_ => Arrived(), Fail);
        if (_phase == AnimationCommitPhase.AfterFrame && _clock.IsDispatchingFrame) Arrived();
        else _wake.Request();

        void Arrived()
        {
            if (_phase == AnimationCommitPhase.AfterFrame) Dispatcher.UIThread.Post(Complete, FrameClock.ResumePriority);
            else Complete();
        }
        void Complete()
        {
            if (generation != _generation) return;
            try { if (_phase == AnimationCommitPhase.AfterFrame) Commit(); }
            catch (Exception error) { Fail(error); return; }
            _wake?.Dispose(); _wake = null;
            if (!_failed && _pending.Count != 0) Arm();
        }
        void Fail(Exception error)
        {
            if (generation != _generation) return;
            _failed = _clock.IsUnavailable;
            _wake?.Dispose(); _wake = null;
            var pending = _pending.ToArray();
            _pending.Clear();
            foreach (var state in pending) state.RejectCommitFrame(error, report: !_failed);
            if (!_failed && _pending.Count != 0 && _wake is null) Arm();
        }
    }
}
