namespace ComposeAnimation;

/// <summary>One synchronous installation of all matched pairs for a preparation generation.</summary>
internal sealed class SharedTransitionSessionBatch(SharedTransitionScope scope, long generation)
{
    private readonly List<SharedTransitionSession> _sessions = [];
    private readonly HashSet<SharedTransitionParticipant> _endpoints = [];
    private bool _closed;

    internal SharedTransitionSession Start(SharedTransitionParticipant source, SharedTransitionParticipant target,
        Action<SharedTransitionSession> configure, Action<SharedTransitionSession, SharedSessionEndReason> ending)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(ending);
        Enroll(source, target);
        var session = new SharedTransitionSession(scope, generation, source, target, ending);
        _sessions.Add(session);
        scope.AddSession(session);
        configure(session);
        return session;
    }

    internal void Retarget(SharedTransitionSession session, SharedTransitionParticipant source,
        SharedTransitionParticipant target, Action update)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(update);
        if (!session.BelongsTo(scope) || session.IsClosed || !session.Uses(source) || !session.Uses(target))
            throw new ArgumentException("Retargeting requires the same live session and endpoint identities.", nameof(session));
        Enroll(source, target);
        _sessions.Add(session);
        session.Retarget(generation, source, target, update);
    }

    private void Enroll(SharedTransitionParticipant source, SharedTransitionParticipant target)
    {
        scope.ParentTransition.VerifyAccess();
        ObjectDisposedException.ThrowIf(_closed, this);
        scope.ValidateEndpoints(source, target);
        if (_endpoints.Contains(source) || _endpoints.Contains(target))
            throw new InvalidOperationException("Each registration can belong to only one pair in an installation.");
        _endpoints.Add(source);
        _endpoints.Add(target);
    }

    internal void Seal()
    {
        Close();
        if (_sessions.Any(s => s.IsClosed || s.Transition.IsDisposed || s.Transition.IsSeeking || !s.Source.IsActive || !s.Target.IsActive))
            throw new InvalidOperationException("A session endpoint was removed during installation.");
    }
    internal void Close() => _closed = true;
    internal void Activate()
    {
        foreach (var session in _sessions) session.Activate();
    }
    internal void Cancel(SharedSessionEndReason reason)
    {
        Close();
        foreach (var session in _sessions) session.Cancel(reason);
    }
}
