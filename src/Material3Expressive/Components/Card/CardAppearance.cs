using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Card.kt's four color slots. Null entries follow the current theme defaults.</summary>
public sealed record CardColors
{
    public Color? ContainerColor { get; init; }
    public Color? ContentColor { get; init; }
    public Color? DisabledContainerColor { get; init; }
    public Color? DisabledContentColor { get; init; }
}

/// <summary>Card.kt's six shadow heights in DIP. Null entries follow the variant's tokens.</summary>
public sealed record CardElevation
{
    public double? Default { get; init; }
    public double? Pressed { get; init; }
    public double? Focused { get; init; }
    public double? Hovered { get; init; }
    public double? Dragged { get; init; }
    public double? Disabled { get; init; }
}
