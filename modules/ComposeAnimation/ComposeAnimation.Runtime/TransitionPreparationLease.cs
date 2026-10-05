namespace ComposeAnimation.Runtime;

/// <summary>
/// A completion reservation for presentation preparation, not a timed animation.
/// All access uses the owning transition's clock thread. A revoked lease cannot be reused.
/// </summary>
public sealed class TransitionPreparationLease : IDisposable
{
    private Transition? _owner;

    internal bool IsRuntime { get; }
    internal TransitionPreparationLease(Transition owner, bool runtime = false) { _owner = owner; IsRuntime = runtime; }

    /// <summary>False after release, transition disposal, or an explicit manual seek/snap.</summary>
    public bool IsActive => _owner is not null && !_owner.IsDisposed;

    /// <summary>Releases this reservation only; real animations remain registered.</summary>
    public void Dispose() => _owner?.ReleasePreparation(this);

    internal void Revoke() => _owner = null;
}
