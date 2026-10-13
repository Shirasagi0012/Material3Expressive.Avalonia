namespace ComposeAnimation;

/// <summary>
/// Implemented by library components that own a <see cref="AnimationScope.Get"/> scope. Keeps
/// component policy out of <see cref="AnimationScope"/>.
/// </summary>
internal interface IAnimationScopeHost
{
    /// <summary>
    /// True while the host's scope must not be rebuilt yet (for example, during an open move
    /// transaction). The drain parks the stale scope; call <see cref="AnimationScope.RebuildPolicyChanged"/>
    /// when this turns false.
    /// <para>Q-E (stage 4): <see cref="AnimatedBounds"/> defers only while its move transaction is
    /// open (<c>_pendingMove</c>), because the move's coordinate basis is not committed yet. After
    /// Commit the drain rebuilds and the flight resumes in the new scope (D17), following the
    /// destination's DurationScale and lifetime. Nothing parks a flight across frames any more.</para>
    /// </summary>
    bool DefersScopeRebuild => false;

    /// <summary>The component's own reaction, run directly before the routed <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
    void OnScopeChanged(AnimationScopeChangedEventArgs e);
}
