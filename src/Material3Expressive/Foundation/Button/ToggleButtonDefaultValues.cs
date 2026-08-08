using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Foundation;

/// <summary>Theme-resolved state colours published by the ToggleButton defaults adapter.</summary>
/// <remarks>
/// This is a library-owned fallback profile, not a public token override bag. It is public only
/// because compiled library AXAML must resolve the type of its attached-property contract.
/// ToggleButton needs separate unchecked, checked and disabled slots; ordinary Button's colour
/// profile cannot express that state contract.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ToggleButtonColors(
    IImmutableBrush UncheckedContainer,
    IImmutableBrush UncheckedContent,
    IImmutableBrush CheckedContainer,
    IImmutableBrush CheckedContent,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledContent,
    IImmutableBrush Outline,
    IImmutableBrush DisabledOutline,
    bool HasOutline)
{
    public IImmutableBrush Container(bool enabled, bool isChecked) =>
        !enabled ? DisabledContainer
        : isChecked ? CheckedContainer
        : UncheckedContainer;

    public IImmutableBrush Content(bool enabled, bool isChecked) =>
        !enabled ? DisabledContent
        : isChecked ? CheckedContent
        : UncheckedContent;

    public IImmutableBrush StateLayer(bool isChecked) =>
        isChecked ? CheckedContent : UncheckedContent;
}

/// <summary>The size-dependent semantic metrics published by the ToggleButton defaults adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ToggleButtonMetrics(
    double ContainerHeight,
    double ToggleContainerHeight,
    Thickness ContentPadding,
    double OutlineWidth,
    TypographyRole LabelTextStyle);

/// <summary>The three geometry roles required by a ToggleButton's checked-aware presenter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ToggleButtonShapes(
    ShapeRole Unchecked,
    ShapeRole Pressed,
    ShapeRole Checked);
