using System.ComponentModel;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal enum SharedSessionState { Running, AwaitingHandoff, Completed, Cancelled }
internal enum SharedSessionEndReason { Completed, Cancelled, Superseded, ParticipantRemoved, ScopeDisposed, Seeking, Failed }

/// <summary>
/// Owns a stable child timeline of the common parent. Endpoint links retain completion only;
/// they never sample animations. Geometry, content and the final drawing transaction remain
/// responsibilities of the coordinator/backend that consumes this session.
/// </summary>
internal sealed class SharedTransitionSession : IDisposable
{
    private readonly SharedTransitionScope _scope;
    private readonly Action<SharedTransitionSession, SharedSessionEndReason> _ending;
    private readonly List<CompletionLink> _links = [];
    private readonly List<TransitionPreparationLease> _retentionLeases = [];
    private bool _activated;
    private bool _checkScheduled;
    private long _readyGeneration = -1;

    internal SharedTransitionSession(SharedTransitionScope scope, long generation,
        SharedTransitionParticipant source, SharedTransitionParticipant target,
        Action<SharedTransitionSession, SharedSessionEndReason> ending)
    {
        _scope = scope; Generation = generation; Source = source; Target = target; _ending = ending;
        Transition = scope.CreateSessionTransition();
        foreach (var owner in new[] { source.Visibility, target.Visibility }.Distinct())
            _links.Add(new CompletionLink(this, owner));
        Transition.PropertyChanged += Changed;
        try
        {
            foreach (var link in _links) link.Attach();
        }
        catch
        {
            Transition.PropertyChanged -= Changed;
            foreach (var link in _links) link.Dispose();
            Transition.Dispose();
            throw;
        }
    }

    public Transition<int> Transition { get; }
    public long Generation { get; private set; }
    public SharedTransitionParticipant Source { get; private set; }
    public SharedTransitionParticipant Target { get; private set; }
    public SharedSessionState State { get; private set; } = SharedSessionState.Running;
    public SharedSessionEndReason? EndReason { get; private set; }
    public bool IsClosed => State is SharedSessionState.Completed or SharedSessionState.Cancelled;
    internal bool BelongsTo(SharedTransitionScope scope) => ReferenceEquals(_scope, scope);
    internal bool Uses(SharedTransitionParticipant participant) => participant == Source || participant == Target;

    internal void Activate()
    {
        if (IsClosed) return;
        foreach (var owner in new[] { Source.Visibility, Target.Visibility }.Distinct())
            _retentionLeases.Add(owner.BeginRuntimeRetention());
        _activated = true;
        ScheduleCheck();
    }

    internal void Retarget(long generation, SharedTransitionParticipant source, SharedTransitionParticipant target, Action update)
    {
        if (IsClosed) throw new ObjectDisposedException(nameof(SharedTransitionSession));
        _activated = false;
        Generation = generation; Source = source; Target = target;
        State = SharedSessionState.Running;
        _readyGeneration = -1;
        update(); // Existing DeferredAnimation.Update preserves values and velocity.
    }

    private void Changed(object? sender, PropertyChangedEventArgs args)
    {
        if (IsClosed) return;
        if (Transition.IsDisposed) { Cancel(SharedSessionEndReason.Cancelled); return; }
        if (Transition.IsSeeking) { Cancel(SharedSessionEndReason.Seeking); return; }
        if (!_activated) return;
        if (args.PropertyName == nameof(Transition.TotalDuration))
            foreach (var link in _links) link.DurationChanged();
        ScheduleCheck();
    }

    private void ScheduleCheck()
    {
        if (_checkScheduled || IsClosed) return;
        _checkScheduled = true;
        // DeferredAnimation updates issued by notification callbacks must drain before checking
        // completion, including when a previously finished session is redirected.
        _scope.ParentTransition.Mutate(() => _scope.ParentTransition.Publish(() =>
        {
            _checkScheduled = false;
            CheckCompletion();
        }));
    }

    private void CheckCompletion()
    {
        if (!_activated || IsClosed || Transition.IsDisposed) return;
        if (Transition.IsSeeking) { Cancel(SharedSessionEndReason.Seeking); return; }
        if (!Finished(Transition)) { State = SharedSessionState.Running; _readyGeneration = -1; return; }
        State = SharedSessionState.AwaitingHandoff;
        if (_scope.IsPreparing || _scope.IsUpdating || Generation != _scope.Generation || _readyGeneration == Generation) return;
        _readyGeneration = Generation;
        _scope.NotifyHandoffReady(this);
    }

    private static bool Finished(Transition transition) => !transition.IsPreparing && !transition.IsRunning &&
        transition.Animations.All(a => a.IsFinished) && transition.Transitions.All(Finished);

    internal bool TryComplete(long generation)
    {
        _scope.ParentTransition.VerifyThreadAccess();
        if (IsClosed || !_activated || !Finished(Transition) || State != SharedSessionState.AwaitingHandoff || generation != Generation ||
            generation != _scope.Generation || _scope.IsPreparing || _scope.IsUpdating) return false;
        End(SharedSessionEndReason.Completed);
        return State == SharedSessionState.Completed;
    }

    internal void Cancel(SharedSessionEndReason reason) => End(reason);
    public void Dispose() => End(SharedSessionEndReason.Cancelled);

    private void End(SharedSessionEndReason reason)
    {
        _scope.ParentTransition.VerifyThreadAccess();
        if (IsClosed) return;
        State = reason == SharedSessionEndReason.Completed ? SharedSessionState.Completed : SharedSessionState.Cancelled;
        EndReason = reason;
        Transition.PropertyChanged -= Changed;
        _scope.RemoveSession(this);
        try
        {
            // Links remain unfinished throughout this callback. The backend can commit its final
            // sample and drawing gates before either visibility branch is allowed to complete.
            _ending(this, reason);
        }
        catch (Exception error)
        {
            State = SharedSessionState.Cancelled; EndReason = SharedSessionEndReason.Failed;
            _scope.ReportSessionError(error);
        }
        finally
        {
            foreach (var link in _links) link.Dispose();
            foreach (var lease in _retentionLeases) lease.Dispose();
            _retentionLeases.Clear();
            Transition.Dispose();
            _scope.ReleaseDetachedParticipants();
        }
    }

    private sealed class CompletionLink : ITransitionAnimationNode, IDisposable
    {
        private readonly SharedTransitionSession _session;
        private readonly Transition _owner;
        private bool _released;
        public CompletionLink(SharedTransitionSession session, Transition owner)
        {
            _session = session; _owner = owner;
        }
        public void Attach()
        {
            if (!_released) _owner.Mutate(() => { if (!_released) _owner.AddAnimation(this); });
        }
        public string Label => "SharedSessionParticipation";
        // Mirror the actual session duration, never invent a timeout or sample the timeline twice.
        public TimeSpan Duration => _session.Transition.TotalDuration;
        public bool IsFinished => _released;
        public void Pulse(TimeSpan playTime, bool scaleToEnd) { }
        public void Seek(TimeSpan playTime) => _session.Cancel(SharedSessionEndReason.Seeking);
        public void Reconfigure(bool reset, bool seeking, bool preserveCurrentValue)
        {
            if (seeking) _session.Cancel(SharedSessionEndReason.Seeking);
        }
        public void Detach()
        {
            _released = true;
            _session.Cancel(SharedSessionEndReason.ParticipantRemoved);
        }
        public void DurationChanged()
        {
            if (!_released && CanMutateOwner()) _owner.Mutate(() => _owner.AnimationChanged());
        }
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            if (CanMutateOwner()) _owner.Mutate(() => _owner.RemoveAnimation(this));
        }
        private bool CanMutateOwner()
        {
            for (Transition? current = _owner; current is not null; current = current.ParentTransition)
                if (current.IsDisposed) return false;
            return true;
        }
    }
}
