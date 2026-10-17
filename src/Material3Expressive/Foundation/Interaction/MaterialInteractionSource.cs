using Avalonia;
using Avalonia.Threading;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>A UI-thread interaction stream. End events refer to the exact start instance.
/// Observers receive future events; the source does not retain or replay state.</summary>
public sealed class MaterialInteractionSource
{
    public event Action<MaterialInteractionEvent>? Interaction;
    public void Emit(MaterialInteractionEvent interaction)
    {
        Dispatcher.UIThread.VerifyAccess();
        ArgumentNullException.ThrowIfNull(interaction);
        Interaction?.Invoke(interaction);
    }
}

public abstract class MaterialInteractionEvent
{
    private protected MaterialInteractionEvent() { }
    public sealed class Press(Point position) : MaterialInteractionEvent { public Point Position { get; } = position; }
    public sealed class Release(Press press) : MaterialInteractionEvent { public Press Start { get; } = press; }
    public sealed class CancelPress(Press press) : MaterialInteractionEvent { public Press Start { get; } = press; }
    public sealed class Focus : MaterialInteractionEvent;
    public sealed class Unfocus(Focus focus) : MaterialInteractionEvent { public Focus Start { get; } = focus; }
    public sealed class HoverEnter : MaterialInteractionEvent;
    public sealed class HoverExit(HoverEnter enter) : MaterialInteractionEvent { public HoverEnter Start { get; } = enter; }
    public sealed class DragStart : MaterialInteractionEvent;
    public sealed class DragStop(DragStart start) : MaterialInteractionEvent { public DragStart Start { get; } = start; }
    public sealed class DragCancel(DragStart start) : MaterialInteractionEvent { public DragStart Start { get; } = start; }
}
