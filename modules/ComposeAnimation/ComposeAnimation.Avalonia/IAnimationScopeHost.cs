namespace ComposeAnimation;

/// <summary>
/// Implemented by library components that own a <see cref="AnimationScope.Get"/> scope. Keeps
/// component policy out of <see cref="AnimationScope"/>.
/// </summary>
internal interface IAnimationScopeHost
{
    /// <summary>
    /// True while the host's scope must not be rebuilt yet (for example, runs retained across a
    /// move). The drain parks the stale scope; call <see cref="AnimationScope.RebuildPolicyChanged"/>
    /// when this turns false.
    /// <para>A parked flight keeps the pre-move scope (stage 2 decision, Q-E): its runs finish
    /// under that scope's DurationScale and lifetime, and are released if the old ancestor ends.
    /// Runs now take a scope, so the flight could move to the new scope at rebuild; that would
    /// reopen the decision and touch <see cref="AnimatedBounds"/>'s move retention
    /// (<c>_retainedMove</c>/<c>_pendingMove</c>), so it is left for stage 4.</para>
    /// </summary>
    bool DefersScopeRebuild => false;

    /// <summary>The component's own reaction, run directly before the routed <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
    void OnScopeChanged(AnimationScopeChangedEventArgs e);
}
