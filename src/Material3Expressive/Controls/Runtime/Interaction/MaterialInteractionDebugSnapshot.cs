using Avalonia;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

internal readonly record struct MaterialInteractionDebugSnapshot(
    InteractionState Interaction,
    int RippleCount,
    Point? LastRippleCenter);
