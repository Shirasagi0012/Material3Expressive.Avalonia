using Avalonia;

namespace Material3Expressive.Controls;

internal readonly record struct MaterialInteractionGeometry(
    Rect Area,
    Rect? FocusRingArea,
    CornerRadius? FocusRingCornerRadius);
