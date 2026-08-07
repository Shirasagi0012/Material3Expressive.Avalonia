namespace Material3Expressive.Controls;

/// <summary>
/// Static indication policy for one interaction controller.
/// </summary>
/// <remarks>
/// This value controls how already-known interaction states are rendered. It is deliberately
/// separate from the dynamic input gate exposed by <see cref="MaterialInteractionController" />:
/// a component can suppress physical input without changing an externally-owned Dragged state,
/// and it can suppress one visual indication while continuing to accept input.
/// </remarks>
internal readonly record struct MaterialInteractionPolicy(
    bool IsRippleEnabled = true,
    bool IsFocusIndicationEnabled = true,
    bool IsHoverIndicationEnabled = true,
    bool IsDragIndicationEnabled = true);
