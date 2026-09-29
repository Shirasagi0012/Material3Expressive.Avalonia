using System;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>
/// The interactions a control can be under at once.
/// </summary>
/// <remarks>
/// A flags enum because they genuinely overlap — a focused button can be hovered and pressed at the
/// same time. Which one the state layer <i>shows</i> is a separate question, answered by the
/// arrival-ordered stack in <see cref="MaterialInteractionController" />.
/// </remarks>
[Flags]
public enum InteractionState
{
    None = 0,
    Hovered = 1 << 0,
    Focused = 1 << 1,
    Pressed = 1 << 2,
    Dragged = 1 << 3
}
