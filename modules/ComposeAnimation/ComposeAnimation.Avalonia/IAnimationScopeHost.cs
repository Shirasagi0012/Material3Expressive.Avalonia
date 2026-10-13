namespace ComposeAnimation;

/// <summary>
/// Implemented by library components that own a <see cref="AnimationScope.Get"/> scope. Keeps
/// component policy out of <see cref="AnimationScope"/>.
/// </summary>
internal interface IAnimationScopeHost
{
    /// <summary>The component's own reaction, run directly before the routed <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
    void OnScopeChanged(AnimationScopeChangedEventArgs e);
}
