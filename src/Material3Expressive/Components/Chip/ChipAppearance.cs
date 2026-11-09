using Avalonia;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>Shadow heights in DIP. Selection does not change this policy upstream.</summary>
public sealed record ChipElevation(double Default = 0, double Pressed = 0, double Focused = 0,
    double Hovered = 0, double Dragged = 0, double Disabled = 0)
{
    public ChipElevation() : this(0, 0, 0, 0, 0, 0) { }
}

/// <summary>Complete rest, selected, and pressed outlines. Corner shapes morph; arbitrary paths switch directly.</summary>
public sealed record ChipShapes(MaterialShape Shape, MaterialShape SelectedShape, MaterialShape PressedShape)
{
    public MaterialShape Shape { get; init; } = Shape ?? throw new ArgumentNullException(nameof(Shape));
    public MaterialShape SelectedShape { get; init; } = SelectedShape ?? throw new ArgumentNullException(nameof(SelectedShape));
    public MaterialShape PressedShape { get; init; } = PressedShape ?? throw new ArgumentNullException(nameof(PressedShape));
    public ChipShapes() : this(new RoundedCornerShape(), new RoundedCornerShape(), new RoundedCornerShape()) { }
    public ChipShapes(MaterialShape shape, MaterialShape selectedShape) : this(shape, selectedShape, selectedShape) { }
    /// <summary>Compatibility constructor. The legacy full corner value 1000 means 50%.</summary>
    public ChipShapes(CornerRadius shape, CornerRadius selectedShape, CornerRadius pressedShape)
        : this(FromCorners(shape), FromCorners(selectedShape), FromCorners(pressedShape)) { }
    private static RoundedCornerShape FromCorners(CornerRadius corners)
    {
        static RelativeScalar Corner(double value) => value == 1000
            ? new(.5, RelativeUnit.Relative) : new(value, RelativeUnit.Absolute);
        return new() { TopStart = Corner(corners.TopLeft), TopEnd = Corner(corners.TopRight),
            BottomEnd = Corner(corners.BottomRight), BottomStart = Corner(corners.BottomLeft) };
    }
}

/// <summary>Complete ChipArrangement logical leading/trailing gaps.</summary>
public sealed record ChipSpacing(double Leading = 0, double Trailing = 0)
{
    public ChipSpacing() : this(0, 0) { }
}
