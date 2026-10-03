namespace Material3Expressive.Foundation.Overlays;

public enum DismissReason
{
    Escape,
    Back,
    PointerOutside,
}

/// <summary>A request only; the owner decides whether to change the open state.</summary>
public sealed class DismissRequestedEventArgs(DismissReason reason) : EventArgs
{
    public DismissReason Reason { get; } = reason;
}
