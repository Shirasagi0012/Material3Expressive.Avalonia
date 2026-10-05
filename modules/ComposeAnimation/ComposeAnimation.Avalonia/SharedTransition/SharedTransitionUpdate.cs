namespace ComposeAnimation;

/// <summary>
/// A synchronous batch of shared presentation updates. Dispose commits the outermost batch;
/// Cancel abandons coordination for the entire batch, without rolling back application state.
/// Do not hold a batch across await. Dispose nested batches in reverse acquisition order.
/// </summary>
public sealed class SharedTransitionUpdate : IDisposable
{
    internal SharedTransitionScope? Owner { get; private set; }
    internal SharedTransitionUpdate(SharedTransitionScope owner) => Owner = owner;

    public void Dispose() => Owner?.EndUpdate(this);
    public void Cancel() => Owner?.CancelUpdate(this);
    internal void Revoke() => Owner = null;
}
