using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Null slots follow the current SegmentedButton tokens.</summary>
public sealed record SegmentedButtonColors
{
    public Color? ActiveContainerColor { get; init; }
    public Color? ActiveContentColor { get; init; }
    public Color? ActiveBorderColor { get; init; }
    public Color? InactiveContainerColor { get; init; }
    public Color? InactiveContentColor { get; init; }
    public Color? InactiveBorderColor { get; init; }
    public Color? DisabledActiveContainerColor { get; init; }
    public Color? DisabledActiveContentColor { get; init; }
    public Color? DisabledActiveBorderColor { get; init; }
    public Color? DisabledInactiveContainerColor { get; init; }
    public Color? DisabledInactiveContentColor { get; init; }
    public Color? DisabledInactiveBorderColor { get; init; }
}
