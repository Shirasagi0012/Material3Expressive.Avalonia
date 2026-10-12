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
    /// </summary>
    bool DefersScopeRebuild => false;

    /// <summary>The component's own reaction, run directly before the routed <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
    void OnScopeChanged(AnimationScopeChangedEventArgs e);
}
