using System.ComponentModel;
using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Coordinates shared preparation and session lifetime against one caller-owned parent Transition.
/// Does not start, stop, seek, or dispose the parent's frame loop. Visual participant registration
/// and drawing are internal integration points; this class alone does not render shared content.
/// </summary>
public abstract class SharedTransitionScope : IDisposable
{
    private readonly List<SharedTransitionParticipant> _participants = [];
    private readonly List<SharedTransitionUpdate> _updates = [];
    private readonly List<SharedTransitionSession> _sessions = [];
    private Preparation? _preparation;
    private SharedTransitionCoordinator? _coordinator;
    private bool _disposed;
    private long _generation;
    private long _participantId;
    private long _batchRevision;
    private int _pendingCommits;

    private protected SharedTransitionScope(Transition parent)
    {
        parent.VerifyAccess();
        if (parent.IsSeeking) throw new InvalidOperationException("Shared preparation cannot attach during seeking.");
        ParentTransition = parent;
        parent.PropertyChanged += ParentChanged;
    }

    public static SharedTransitionScope Create<TState>(Transition<TState> parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        return new ParentScope<TState>(parent);
    }

    public Transition ParentTransition { get; }
    public long Generation => _generation;
    public bool IsUpdating => _updates.Count > 0 || _pendingCommits > 0;
    public bool IsPreparing => _preparation is not null;
    internal bool IsDisposed => _disposed;
    /// <summary>Latest preparation callback failure. The failed request is cancelled, not navigation.</summary>
    public Exception? LastPreparationError { get; private set; }
    /// <summary>Latest runtime session/hand-off failure; application navigation is not changed.</summary>
    public Exception? LastSessionError { get; private set; }

    // The layout coordinator consumes these requests after the parent has updated all children.
    internal event Action<SharedTransitionPreparation>? PreparationRequested;
    internal event Action<SharedTransitionSession>? HandoffReady;
    internal IReadOnlyList<SharedTransitionSession> Sessions => _sessions;

    internal void AttachCoordinator(SharedTransitionCoordinator coordinator)
    {
        VerifyAccess();
        if (_coordinator is not null || IsPreparing || _sessions.Count > 0)
            throw new InvalidOperationException("Attach one layout coordinator before starting shared preparation or sessions.");
        _coordinator = coordinator;
    }
    internal void DetachCoordinator(SharedTransitionCoordinator coordinator)
    {
        if (ReferenceEquals(_coordinator, coordinator)) _coordinator = null;
    }

    internal bool TryCommitSessions(long generation, Action<SharedTransitionSessionBatch> install)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(install);
        var batch = new SharedTransitionSessionBatch(this, generation);
        try
        {
            var committed = TryCommitPreparation(generation, () =>
            {
                install(batch);
                batch.Seal();
            });
            if (committed)
            {
                foreach (var previous in _sessions.Where(s => s.Generation < generation).ToArray())
                    previous.Cancel(SharedSessionEndReason.Superseded);
                batch.Activate();
            }
            else batch.Cancel(SharedSessionEndReason.Superseded);
            return committed;
        }
        catch
        {
            batch.Cancel(SharedSessionEndReason.Failed);
            throw;
        }
        finally { batch.Close(); }
    }

    internal void ValidateEndpoints(SharedTransitionParticipant source, SharedTransitionParticipant target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (source == target || !source.IsActive || !target.IsActive || !_participants.Contains(source) || !_participants.Contains(target) ||
            !Equals(source.Key, target.Key))
            throw new ArgumentException("A session requires two live registrations with the same key in this shared scope.");
    }

    internal void NotifyHandoffReady(SharedTransitionSession session)
    {
        try { HandoffReady?.Invoke(session); }
        catch (Exception error) { ReportSessionError(error); session.Cancel(SharedSessionEndReason.Failed); }
    }
    internal void ReportSessionError(Exception error) => LastSessionError = error;
    internal void ReportPreparationError(Exception error) => LastPreparationError = error;
    internal void AddSession(SharedTransitionSession session) => _sessions.Add(session);
    internal void RemoveSession(SharedTransitionSession session) => _sessions.Remove(session);
    internal void ReleaseDetachedParticipants()
    {
        if (_preparation is not null) return;
        foreach (var participant in _participants.Where(p => p.IsDetached &&
                     !_sessions.Any(s => !s.IsClosed && s.Uses(p))).ToArray())
            participant.Dispose();
    }
    internal abstract Transition<int> CreateSessionTransition();

    public SharedTransitionUpdate BeginUpdate()
    {
        VerifyAccess();
        if (ParentTransition.IsSeeking) throw new InvalidOperationException("Shared preparation cannot begin during seeking.");
        var first = !IsUpdating;
        if (first) _batchRevision++;
        var update = new SharedTransitionUpdate(this);
        _updates.Add(update);
        if (first)
        {
            var revision = _batchRevision;
            // Mark the batch open before enrollment can publish property notifications.
            // When called from an existing notification, this runs ahead of the caller's
            // queued target changes, and a queued commit keeps automatic hooks suppressed.
            try { ParentTransition.Mutate(() =>
            {
                if (!_disposed && revision == _batchRevision) BeginPreparation();
            }); }
            catch { CancelCurrent(); throw; }
        }
        return update;
    }

    internal void EndUpdate(SharedTransitionUpdate update)
    {
        ParentTransition.VerifyThreadAccess();
        if (update.Owner != this) return;
        if (_updates.Count == 0 || !ReferenceEquals(_updates[^1], update))
            throw new InvalidOperationException("Dispose nested updates in reverse acquisition order.");
        _updates.RemoveAt(_updates.Count - 1);
        update.Revoke();
        if (_updates.Count == 0)
        {
            var revision = _batchRevision;
            _pendingCommits++;
            ParentTransition.Mutate(() =>
            {
                if (_disposed || revision != _batchRevision) return;
                _pendingCommits--;
                if (!IsUpdating) PublishPreparation();
            });
        }
    }

    internal void CancelUpdate(SharedTransitionUpdate update)
    {
        ParentTransition.VerifyThreadAccess();
        if (update.Owner != this) return;
        CancelCurrent();
        // Previously queued target writes still belong to the cancelled batch. Suppress
        // automatic preparation until those writes have drained, without rolling them back.
        var revision = _batchRevision;
        _pendingCommits++;
        ParentTransition.Mutate(() =>
        {
            if (!_disposed && revision == _batchRevision) _pendingCommits--;
        });
    }

    internal SharedTransitionParticipant RegisterParticipant(object key, Transition<EnterExitState> visibility,
        Func<Rect?>? readLayoutBounds = null, Func<bool>? isLayoutAffectedByAncestorFlight = null)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(visibility);
        visibility.VerifyAccess();
        var ancestor = visibility.ParentTransition;
        while (ancestor is not null && ancestor != ParentTransition) ancestor = ancestor.ParentTransition;
        if (ancestor is null) throw new ArgumentException("The visibility transition must descend from the shared parent.", nameof(visibility));
        var participant = new SharedTransitionParticipant(this, ++_participantId, key, visibility, readLayoutBounds,
            isLayoutAffectedByAncestorFlight);
        _participants.Add(participant);
        // A late template belongs to the current request. Its identity must not be substituted
        // for an old registration just because the key (or control instance) is reused.
        if (_preparation is { } preparation)
        {
            AddLease(preparation, visibility);
            // A late visual template may register while the outer explicit transaction's
            // queued commit is draining. Always enqueue a republication; PublishPreparation's
            // notification checks the current transaction/generation when it executes. Skipping
            // here loses the newly registered target and leaves the coordinator with a stale
            // MissingTarget request.
            PublishPreparation();
        }
        return participant;
    }

    internal void Unregister(SharedTransitionParticipant participant)
    {
        ParentTransition.VerifyThreadAccess();
        if (!_participants.Remove(participant)) return;
        foreach (var session in _sessions.Where(s => s.Uses(participant)).ToArray())
            session.Cancel(SharedSessionEndReason.ParticipantRemoved);
        // Scope recreation unregisters the old identity immediately before registering its
        // replacement in the same notification drain. Cancelling synchronously here kills the
        // preparation that was created for the navigation intent. Defer the removal decision until
        // queued mutations have observed any replacement; a real removal (no same-key participant)
        // still cancels the request.
        if (_preparation is { } preparation)
        {
            var generation = preparation.Generation;
            var key = participant.Key;
            if (_participants.Any(candidate => Equals(candidate.Key, key) &&
                ReferenceEquals(candidate.Visibility, participant.Visibility) && candidate.IsActive))
            {
                CancelCurrent();
                return;
            }
            ParentTransition.Mutate(() =>
            {
                if (_preparation?.Generation == generation &&
                    !_participants.Any(candidate => Equals(candidate.Key, key) && candidate.IsActive))
                    CancelCurrent();
            });
        }
    }

    private protected void BeforeTargetChange()
    {
        if (_disposed || IsUpdating || _participants.Count == 0) return;
        if (ParentTransition.IsSeeking) return;
        BeginPreparation();
    }

    private protected void AfterTargetChange()
    {
        if (!_disposed && !IsUpdating && _preparation is not null) PublishPreparation();
    }

    private void BeginPreparation()
    {
        LastPreparationError = null;
        var next = new Preparation(++_generation,
            _participants.ToArray().Select(p => new SharedParticipantSnapshot(p.Id, p.Key,
                p.Visibility.CurrentState, p.Visibility.TargetState, CaptureLayout(p))).ToArray(),
            error => LastPreparationError = error);
        try
        {
            AddLease(next, ParentTransition);
            foreach (var participant in _participants) AddLease(next, participant.Visibility);
        }
        catch
        {
            next.Dispose();
            throw;
        }
        var previous = _preparation;
        _preparation = next;
        // Acquire replacements before releasing old reservations, even for A -> B -> A.
        previous?.Dispose();
    }

    private Rect? CaptureLayout(SharedTransitionParticipant participant)
    {
        try { return participant.ReadLayoutBounds(); }
        catch (Exception error) { LastPreparationError = error; return null; }
    }

    private static void AddLease(Preparation preparation, Transition transition)
    {
        if (!preparation.Leases.ContainsKey(transition))
            preparation.Leases.Add(transition, transition.BeginPreparation());
    }

    private void PublishPreparation()
    {
        var preparation = _preparation;
        if (preparation is null) return;
        if (_participants.Count == 0) { CancelCurrent(); return; }
        // Mutations made inside a Transition notification are queued. Publishing via Mutate
        // ensures that lease enrollment and descendant target updates finish before the request.
        ParentTransition.Mutate(() => ParentTransition.Publish(() =>
        {
            if (_disposed || IsUpdating || !ReferenceEquals(_preparation, preparation)) return;
            if (!preparation.IsActive) { CancelCurrent(); return; }
            var request = new SharedTransitionPreparation(preparation.Generation, preparation.Before,
                _participants.ToArray(), preparation.Cancellation.Token);
            try { PreparationRequested?.Invoke(request); }
            catch (Exception error)
            {
                // Do not throw out of the parent's notification batch: its queued lease removals
                // must be allowed to drain. The failed shared request falls back independently.
                LastPreparationError = error;
                CancelPreparation(request.Generation);
            }
        }));
    }

    /// <summary>Installs real session animations/participation before releasing preparation.</summary>
    internal bool TryCommitPreparation(long generation, Action installSession)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(installSession);
        if (IsUpdating || _preparation is not { } preparation || preparation.Generation != generation || !preparation.IsActive)
            return false;
        try
        {
            installSession();
            // Reentrant target changes must not release the newer request's reservations.
            if (!ReferenceEquals(_preparation, preparation)) return false;
            _preparation = null;
            preparation.Dispose();
            ReleaseDetachedParticipants();
            return true;
        }
        catch
        {
            CancelPreparation(generation);
            throw;
        }
    }

    internal bool CancelPreparation(long generation)
    {
        ParentTransition.VerifyThreadAccess();
        if (_preparation?.Generation != generation) return false;
        CancelCurrent();
        return true;
    }

    private void ParentChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (ParentTransition.IsDisposed) { Dispose(); return; }
        if (ParentTransition.IsSeeking)
            foreach (var session in _sessions.ToArray()) session.Cancel(SharedSessionEndReason.Seeking);
        if (ParentTransition.IsSeeking || (_preparation is { IsActive: false })) CancelCurrent();
    }

    private void CancelCurrent()
    {
        _batchRevision++;
        _pendingCommits = 0;
        foreach (var update in _updates) update.Revoke();
        _updates.Clear();
        var preparation = _preparation;
        _preparation = null;
        preparation?.Dispose();
        foreach (var session in _sessions.Where(s => s.Generation != Generation).ToArray())
            session.Cancel(SharedSessionEndReason.Superseded);
        ReleaseDetachedParticipants();
    }

    private void VerifyAccess()
    {
        ParentTransition.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        ParentTransition.VerifyThreadAccess();
        if (_disposed) return;
        _disposed = true;
        ParentTransition.PropertyChanged -= ParentChanged;
        Disconnect();
        try { CancelCurrent(); }
        finally
        {
            _coordinator?.Dispose();
            foreach (var session in _sessions.ToArray()) session.Cancel(SharedSessionEndReason.ScopeDisposed);
            foreach (var participant in _participants.ToArray()) participant.Dispose();
            PreparationRequested = null;
            HandoffReady = null;
        }
    }

    private protected abstract void Disconnect();

    private sealed class ParentScope<TState> : SharedTransitionScope
    {
        private readonly Transition<TState> _parent;
        public ParentScope(Transition<TState> parent) : base(parent)
        {
            _parent = parent;
            parent.TargetChanging += BeforeTargetChange;
            parent.TargetChanged += AfterTargetChange;
        }
        private protected override void Disconnect()
        {
            _parent.TargetChanging -= BeforeTargetChange;
            _parent.TargetChanged -= AfterTargetChange;
        }
        internal override Transition<int> CreateSessionTransition()
            => _parent.CreateChildTransition(_ => 0, "SharedSession");
    }

    private sealed class Preparation(long generation, SharedParticipantSnapshot[] before, Action<Exception> reportError) : IDisposable
    {
        private bool _disposed;
        public long Generation { get; } = generation;
        public SharedParticipantSnapshot[] Before { get; } = before;
        public Dictionary<Transition, TransitionPreparationLease> Leases { get; } = [];
        public CancellationTokenSource Cancellation { get; } = new();
        public bool IsActive => !_disposed && Leases.Values.All(x => x.IsActive);
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var lease in Leases.Values) lease.Dispose();
            Leases.Clear();
            try { Cancellation.Cancel(); }
            catch (AggregateException error) { reportError(error); }
            finally { Cancellation.Dispose(); }
        }
    }
}

internal sealed record SharedParticipantSnapshot(long Id, object Key, EnterExitState CurrentState, EnterExitState TargetState,
    Rect? LayoutBounds = null);
internal sealed record SharedTransitionPreparation(long Generation, IReadOnlyList<SharedParticipantSnapshot> Before,
    IReadOnlyList<SharedTransitionParticipant> Participants, CancellationToken CancellationToken);

internal sealed class SharedTransitionParticipant : IDisposable
{
    private SharedTransitionScope? _owner;
    private Func<Rect?>? _readLayoutBounds;
    private Func<bool>? _isLayoutAffectedByAncestorFlight;
    internal SharedTransitionParticipant(SharedTransitionScope owner, long id, object key, Transition<EnterExitState> visibility,
        Func<Rect?>? readLayoutBounds, Func<bool>? isLayoutAffectedByAncestorFlight)
    {
        _owner = owner; Id = id; Key = key; Visibility = visibility;
        _readLayoutBounds = readLayoutBounds;
        _isLayoutAffectedByAncestorFlight = isLayoutAffectedByAncestorFlight;
        visibility.PropertyChanged += Changed;
    }
    public long Id { get; }
    public object Key { get; }
    public Transition<EnterExitState> Visibility { get; }
    public bool IsActive => _owner is not null && !Visibility.IsDisposed;
    internal bool IsDetached { get; private set; }
    internal void Detach()
    {
        IsDetached = true;
        _owner?.ReleaseDetachedParticipants();
    }
    internal event Action<SharedTransitionParticipant>? Unregistered;
    internal bool IsLayoutAffectedByAncestorFlight => IsActive && _isLayoutAffectedByAncestorFlight?.Invoke() == true;
    internal Rect? ReadLayoutBounds()
    {
        var bounds = IsActive ? _readLayoutBounds?.Invoke() : null;
        return bounds is { } value && double.IsFinite(value.X) && double.IsFinite(value.Y) &&
            double.IsFinite(value.Width) && double.IsFinite(value.Height) && value.Width > 0 && value.Height > 0
            ? value : null;
    }
    private void Changed(object? sender, PropertyChangedEventArgs args)
    {
        if (Visibility.IsDisposed) Dispose();
    }
    public void Dispose()
    {
        Visibility.VerifyThreadAccess();
        var owner = _owner;
        if (owner is null) return;
        _owner = null;
        _readLayoutBounds = null;
        _isLayoutAffectedByAncestorFlight = null;
        Visibility.PropertyChanged -= Changed;
        try { owner.Unregister(this); }
        finally { Unregistered?.Invoke(this); Unregistered = null; }
    }
}
