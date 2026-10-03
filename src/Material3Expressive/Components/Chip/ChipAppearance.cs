using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Optional overrides of Chip.kt's thirteen color slots; omitted values follow tokens.</summary>
public sealed record ChipColors
{
    public Color? ContainerColor { get; init; }
    public Color? LabelColor { get; init; }
    public Color? LeadingIconColor { get; init; }
    public Color? TrailingIconColor { get; init; }
    public Color? DisabledContainerColor { get; init; }
    public Color? DisabledLabelColor { get; init; }
    public Color? DisabledLeadingIconColor { get; init; }
    public Color? DisabledTrailingIconColor { get; init; }
    public Color? SelectedContainerColor { get; init; }
    public Color? DisabledSelectedContainerColor { get; init; }
    public Color? SelectedLabelColor { get; init; }
    public Color? SelectedLeadingIconColor { get; init; }
    public Color? SelectedTrailingIconColor { get; init; }
}

/// <summary>Shadow heights in DIP. Selection does not change this policy upstream.</summary>
public sealed record ChipElevation(double Default = 0, double Pressed = 0, double Focused = 0,
    double Hovered = 0, double Dragged = 8, double Disabled = 0)
{
    public ChipElevation() : this(0, 0, 0, 0, 8, 0) { }
}

/// <summary>Opt in to the rounded shape overload. Omitted corners resolve the current ChipsTokens.</summary>
public sealed class ChipShapes : MarkupExtension
{
    public CornerRadius? Shape { get; init; }
    public CornerRadius? SelectedShape { get; init; }
    public CornerRadius? PressedShape { get; init; }
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>ChipArrangement's logical leading/trailing gaps; null uses the overload's defaults.</summary>
public sealed record ChipSpacing(double Leading = 8, double Trailing = 8)
{
    public ChipSpacing() : this(8, 8) { }
}
