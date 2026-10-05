using Avalonia;

namespace Material3Expressive.Components;

/// <summary>Shadow heights in DIP. Selection does not change this policy upstream.</summary>
public sealed record ChipElevation(double Default = 0, double Pressed = 0, double Focused = 0,
    double Hovered = 0, double Dragged = 0, double Disabled = 0)
{
    public ChipElevation() : this(0, 0, 0, 0, 0, 0) { }
}

/// <summary>Complete rest, selected, and pressed corners. Null Shapes disables morphing.</summary>
public sealed record ChipShapes(CornerRadius Shape, CornerRadius SelectedShape, CornerRadius PressedShape)
{
    public ChipShapes() : this(default, default, default) { }
}

/// <summary>Complete ChipArrangement logical leading/trailing gaps.</summary>
public sealed record ChipSpacing(double Leading = 0, double Trailing = 0)
{
    public ChipSpacing() : this(0, 0) { }
}
