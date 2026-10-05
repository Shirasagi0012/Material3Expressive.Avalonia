using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>The rendering adapter owns drawing gates, captures, content animations and handoff scheduling.</summary>
internal interface ISharedTransitionBackend
{
    // Must clean up partial drawing resources if creation throws. Initial drawing uses Pair.SourceBounds;
    // Runtime mutations (including the forced initial sample) may still be queued at this point.
    ISharedTransitionPresentation Create(SharedTransitionMotion motion);
    bool Accepts(SharedTransitionPair pair) => true;
    bool IsLayoutPending(SharedTransitionParticipant participant) => false;
}

internal interface ISharedTransitionPresentation
{
    void Retarget(SharedTransitionPair pair);
    void HandoffReady(SharedTransitionHandoff handoff);
    void End(SharedSessionEndReason reason);
}

internal readonly record struct SharedTransitionHandoff(long Generation, long LayoutRevision);

internal sealed class SharedTransitionMotion
{
    private readonly Dictionary<SharedTransitionParticipant, Rect> _restingBounds = [];

    internal SharedTransitionMotion(SharedTransitionSession session, SharedTransitionPair pair, IFiniteAnimationSpec<Rect> spec)
    {
        Session = session;
        Pair = pair;
        _restingBounds[pair.Source] = pair.SourceBounds;
        _restingBounds[pair.Target] = pair.TargetBounds;
        Bounds = session.Transition.CreateDeferredAnimation(new RectConverter(), "SharedBounds").Animate(
            _ => spec, pair.SourceBounds, null, _ => pair.TargetBounds);
    }
    internal SharedTransitionSession Session { get; }
    internal SharedTransitionPair Pair { get; private set; }
    internal long LayoutRevision { get; private set; }
    internal Transition<int>.TransitionAnimationState<Rect> Bounds { get; }
    internal ISharedTransitionPresentation? Presentation { get; set; }
    internal bool TryGetRestingBounds(SharedTransitionParticipant participant, out Rect bounds)
        => _restingBounds.TryGetValue(participant, out bounds);
    internal bool TryComplete(SharedTransitionHandoff handoff)
        => handoff.LayoutRevision == LayoutRevision && Session.TryComplete(handoff.Generation);
    internal void Retarget(SharedTransitionPair pair, IFiniteAnimationSpec<Rect> spec)
    {
        Pair = pair;
        _restingBounds[pair.Target] = pair.TargetBounds;
        LayoutRevision++;
        Bounds.Update(_ => pair.TargetBounds, _ => spec);
        Presentation?.Retarget(pair);
    }
    internal void UpdateLayout(Rect target)
    {
        if (Pair.TargetBounds == target) return;
        // Withdraw a pending handoff before queuing updates, even within Runtime notifications.
        Session.Retarget(Session.Generation, Session.Source, Session.Target, () =>
        {
            Pair = Pair with { TargetBounds = target };
            _restingBounds[Pair.Target] = target;
            LayoutRevision++;
            Bounds.Update(_ => target);
            Presentation?.Retarget(Pair);
        });
        Session.Activate();
    }
    internal void InvalidateLayout()
    {
        if (Session.IsClosed) return;
        // The host rectangle can stay equal while a local lookahead domain publishes a new
        // target generation. Withdraw the old handoff without restarting the bounds trajectory.
        Session.Retarget(Session.Generation, Session.Source, Session.Target, () =>
        {
            LayoutRevision++;
            Presentation?.Retarget(Pair);
        });
        Session.Activate();
    }
}
