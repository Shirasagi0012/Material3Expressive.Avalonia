namespace ComposeAnimation;

/// <summary>
/// A visual that owns an enter/exit presentation scope. Lookup is by visual ancestor only.
/// </summary>
public interface IEnterExitScopeHost
{
    AnimatedVisibilityScope? Scope { get; }
}
