using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Foundation;

/// <summary>Theme-resolved colours published by the Button defaults adapter.</summary>
/// <remarks>
/// This is a library-owned fallback profile, not a public token override bag. It is public only
/// because compiled library AXAML must resolve the type of its attached-property contract.
/// Applications customise ordinary visual properties such as
/// <see cref="Avalonia.Controls.Primitives.TemplatedControl.Background" /> through Avalonia styles
/// and local values.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonColors(
    IImmutableBrush Container,
    IImmutableBrush Content,
    IImmutableBrush Icon,
    IImmutableBrush Outline,
    IImmutableBrush StateLayer,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledContent,
    IImmutableBrush DisabledIcon,
    IImmutableBrush DisabledOutline);

/// <summary>The size-dependent semantic metrics published by the Button defaults adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonMetrics(
    double ContainerHeight,
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    ShapeRole SquareContainerShape,
    Thickness ContentPadding,
    double IconSize,
    double IconLabelSpace,
    double OutlineWidth,
    TypographyRole LabelTextStyle);

/// <summary>The geometry roles a Button presenter needs for its runtime shape selection.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonShapes(
    ShapeRole Container,
    ShapeRole Pressed,
    ShapeRole Square);

/// <summary>The elevation profile selected by a Button presenter from interaction state.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ButtonElevation(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Disabled)
{
    public double For(bool enabled, InteractionState interaction)
    {
        if (!enabled)
            return Disabled;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}
