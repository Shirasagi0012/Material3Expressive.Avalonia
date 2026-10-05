using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Matches registrations and installs all ready motions after a host layout pass. Layout readers
/// report final content bounds in one host coordinate space, independently of animated drawing.
/// The future visual host supplies layout notifications, a preparation deadline and the backend.
/// </summary>
internal sealed class SharedTransitionCoordinator : IDisposable
{
    private readonly SharedTransitionScope _scope;
    private readonly ISharedTransitionBackend _backend;
    private readonly Func<SharedTransitionPair, IFiniteAnimationSpec<Rect>> _boundsSpec;
    private readonly HashSet<SharedTransitionParticipant> _registrations = [];
    private readonly List<SharedTransitionMotion> _motions = [];
    private SharedTransitionPreparation? _request;
    private long _laidOutGeneration = -1;
    private bool _disposed;
    private bool _resolving;

    internal SharedTransitionCoordinator(SharedTransitionScope scope, ISharedTransitionBackend backend,
        Func<SharedTransitionPair, IFiniteAnimationSpec<Rect>>? boundsSpec = null)
    {
        scope.ParentTransition.VerifyAccess();
        ObjectDisposedException.ThrowIf(scope.IsDisposed, scope);
        _scope = scope;
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _boundsSpec = boundsSpec ?? (_ => new SpringSpec<Rect>());
        scope.AttachCoordinator(this);
        scope.PreparationRequested += Requested;
        scope.HandoffReady += HandoffReady;
    }

    internal IReadOnlyList<SharedTransitionMotion> Motions => _motions;
    internal SharedTransitionMatchResult? LastResolution { get; private set; }
    internal string LastRequestSummary { get; private set; } = "-";
    internal SharedTransitionParticipant Register(object key, Transition<EnterExitState> visibility, Func<Rect?> readLayoutBounds,
        Func<bool>? isLayoutAffectedByAncestorFlight = null)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(readLayoutBounds);
        var participant = _scope.RegisterParticipant(key, visibility, readLayoutBounds, isLayoutAffectedByAncestorFlight);
        _registrations.Add(participant);
        participant.Unregistered += Unregistered;
        return participant;
    }
    private void Unregistered(SharedTransitionParticipant participant) => _registrations.Remove(participant);

    private void Requested(SharedTransitionPreparation request)
    {
        // Even same-generation republication requires another completed layout: a late template
        // can register while the host is still applying its new target tree.
        _request = request;
        LastRequestSummary = string.Join(" | ", request.Participants.Select(p =>
            $"{p.Key}:{p.Visibility.CurrentState}->{p.Visibility.TargetState}"));
        _laidOutGeneration = -1;
        LastResolution = null;
    }

    internal void LayoutCompleted()
    {
        VerifyAccess();
        if (_resolving || _scope.IsUpdating) return;
        _resolving = true;
        try
        {
            if (CurrentRequest() is { } request)
            {
                _laidOutGeneration = request.Generation;
                Resolve(request, false);
            }
            else if (!_scope.IsPreparing)
            {
                foreach (var motion in _motions.ToArray())
                {
                    if (motion.Session.IsClosed) continue;
                    try
                    {
                        // A native/Scale child of a remeasuring flight exposes the ancestor's
                        // current presentation here, not a new resting destination. Keep the
                        // committed target until that ancestor hands its layout back; otherwise
                        // tracking would overwrite the stable endpoint recovered on reversal.
                        if (motion.Pair.Target.IsLayoutAffectedByAncestorFlight) continue;
                        if (motion.Pair.Target.ReadLayoutBounds() is { } target) motion.UpdateLayout(target);
                        else if (!_backend.IsLayoutPending(motion.Pair.Target))
                            motion.Session.Cancel(SharedSessionEndReason.Cancelled);
                    }
                    catch (Exception error)
                    {
                        _scope.ReportSessionError(error);
                        motion.Session.Cancel(SharedSessionEndReason.Failed);
                    }
                }
            }
        }
        finally { _resolving = false; }
    }

    /// <summary>A host deadline may accept ready keys and fall back for unresolved keys; stale deadlines do nothing.</summary>
    internal bool ExpirePreparation(long generation)
    {
        VerifyAccess();
        if (_resolving || CurrentRequest() is not { } request || request.Generation != generation || _scope.IsUpdating) return false;
        if (_laidOutGeneration != generation) return _scope.CancelPreparation(generation);
        _resolving = true;
        try { return Resolve(request, true); }
        finally { _resolving = false; }
    }

    private SharedTransitionPreparation? CurrentRequest()
    {
        if (_request is { } request && !request.CancellationToken.IsCancellationRequested &&
            request.Generation == _scope.Generation && _scope.IsPreparing) return request;
        _request = null;
        return null;
    }

    private bool Resolve(SharedTransitionPreparation request, bool allowFallback)
    {
        try
        {
            var result = SharedTransitionMatcher.Resolve(request, _motions, p => p.ReadLayoutBounds());
            LastResolution = result;
            if (result.NeedsLayout && !allowFallback) return false;
            var committed = _scope.TryCommitSessions(request.Generation, batch =>
            {
                foreach (var pair in result.Pairs)
                {
                    if (!_backend.Accepts(pair)) continue;
                    var existing = _motions.SingleOrDefault(m => !m.Session.IsClosed && m.Session.Uses(pair.Source) && m.Session.Uses(pair.Target));
                    var spec = _boundsSpec(pair);
                    if (existing is not null)
                        batch.Retarget(existing.Session, pair.Source, pair.Target, () => existing.Retarget(pair, spec));
                    else
                    {
                        SharedTransitionMotion? motion = null;
                        batch.Start(pair.Source, pair.Target, session =>
                        {
                            motion = new(session, pair, spec);
                            _motions.Add(motion);
                            motion.Presentation = _backend.Create(motion) ?? throw new InvalidOperationException("The backend returned no presentation.");
                            // Creation can reenter navigation or detach an endpoint. A presentation
                            // returned after its session was cancelled still needs exactly one cleanup.
                            if (session.IsClosed)
                            {
                                var presentation = motion.Presentation;
                                motion.Presentation = null;
                                presentation.End(session.EndReason ?? SharedSessionEndReason.Cancelled);
                            }
                        }, (_, reason) =>
                        {
                            if (motion is null) return;
                            _motions.Remove(motion);
                            motion.Presentation?.End(reason);
                        });
                    }
                }
            });
            if (ReferenceEquals(_request, request) && request.CancellationToken.IsCancellationRequested) _request = null;
            return committed;
        }
        catch (Exception error)
        {
            _scope.ReportPreparationError(error);
            _scope.CancelPreparation(request.Generation);
            return false;
        }
    }

    private void HandoffReady(SharedTransitionSession session)
    {
        var motion = _motions.SingleOrDefault(m => m.Session == session);
        motion?.Presentation?.HandoffReady(new(session.Generation, motion.LayoutRevision));
    }
    private void VerifyAccess()
    {
        _scope.ParentTransition.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed || _scope.IsDisposed, this);
    }
    public void Dispose()
    {
        _scope.ParentTransition.Clock.VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        _scope.PreparationRequested -= Requested;
        _scope.HandoffReady -= HandoffReady;
        _scope.DetachCoordinator(this);
        if (CurrentRequest() is { } request) _scope.CancelPreparation(request.Generation);
        _request = null;
        LastResolution = null;
        foreach (var motion in _motions.ToArray())
            motion.Session.Cancel(_scope.IsDisposed ? SharedSessionEndReason.ScopeDisposed : SharedSessionEndReason.Cancelled);
        foreach (var participant in _registrations.ToArray()) participant.Dispose();
    }
}
