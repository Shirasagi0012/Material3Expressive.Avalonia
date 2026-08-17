using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Toggle-only semantic defaults for a native Material icon button.</summary>
/// <remarks>
/// The shared variant, size, width, and shape axes live on <see cref="M3IconButton" />. The
/// public fields here are an AXAML metadata contract, not a generic token override API. Their
/// normal write path is internal and owned by <see cref="ToggleIconButtonDefaultsAdapter" />;
/// application visual customisation stays on ordinary Avalonia properties and styles.
/// </remarks>
public static class M3ToggleIconButton
{
    private static readonly ToggleIconButtonVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    /// <summary>Typed checked/unchecked colours supplied by the Material toggle theme.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleIconButtonVisualColors> VisualColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleIconButtonVisualColors>(
            "VisualColors",
            typeof(M3ToggleIconButton),
            defaultValue: DefaultVisualColors);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<ToggleIconButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleIconButtonColors?>(
            "DefaultColors", typeof(M3ToggleIconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<IconButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IconButtonMetrics?>(
            "DefaultMetrics", typeof(M3ToggleIconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<IconButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IconButtonShapes?>(
            "DefaultShapes", typeof(M3ToggleIconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, SpringSpec>(
            "DefaultShapeMotion", typeof(M3ToggleIconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, Thickness>(
            "DefaultOutlineThickness", typeof(M3ToggleIconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<IImmutableBrush?> DefaultStateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IImmutableBrush?>(
            "DefaultStateLayerBrush", typeof(M3ToggleIconButton));

    /// <summary>Overrides the state-layer brush projected by the Material icon toggle theme.</summary>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IBrush?>(
            "StateLayerBrush", typeof(M3ToggleIconButton));

    internal static ToggleIconButtonColors? GetDefaultColors(ToggleButton owner) =>
        owner.GetValue(DefaultColorsProperty);

    internal static void SetDefaultColors(ToggleButton owner, ToggleIconButtonColors? value) =>
        owner.SetValue(DefaultColorsProperty, value);

    internal static IconButtonMetrics? GetDefaultMetrics(ToggleButton owner) =>
        owner.GetValue(DefaultMetricsProperty);

    internal static void SetDefaultMetrics(ToggleButton owner, IconButtonMetrics? value) =>
        owner.SetValue(DefaultMetricsProperty, value);

    internal static IconButtonShapes? GetDefaultShapes(ToggleButton owner) =>
        owner.GetValue(DefaultShapesProperty);

    internal static void SetDefaultShapes(ToggleButton owner, IconButtonShapes? value) =>
        owner.SetValue(DefaultShapesProperty, value);

    internal static SpringSpec GetDefaultShapeMotion(ToggleButton owner) =>
        owner.GetValue(DefaultShapeMotionProperty);

    internal static void SetDefaultShapeMotion(ToggleButton owner, SpringSpec value) =>
        owner.SetValue(DefaultShapeMotionProperty, value);

    internal static Thickness GetDefaultOutlineThickness(ToggleButton owner) =>
        owner.GetValue(DefaultOutlineThicknessProperty);

    internal static void SetDefaultOutlineThickness(ToggleButton owner, Thickness value) =>
        owner.SetValue(DefaultOutlineThicknessProperty, value);

    internal static IImmutableBrush? GetDefaultStateLayerBrush(ToggleButton owner) =>
        owner.GetValue(DefaultStateLayerBrushProperty);

    internal static void SetDefaultStateLayerBrush(ToggleButton owner, IImmutableBrush? value) =>
        owner.SetValue(DefaultStateLayerBrushProperty, value);

    public static IBrush? GetStateLayerBrush(ToggleButton owner) =>
        owner.GetValue(StateLayerBrushProperty);

    public static void SetStateLayerBrush(ToggleButton owner, IBrush? value) =>
        owner.SetValue(StateLayerBrushProperty, value);

    internal static ToggleIconButtonVisualColors GetVisualColors(ToggleButton owner) =>
        owner.GetValue(VisualColorsProperty);

    internal static void SetVisualColors(ToggleButton owner, ToggleIconButtonVisualColors value) =>
        owner.SetValue(VisualColorsProperty, value);

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
