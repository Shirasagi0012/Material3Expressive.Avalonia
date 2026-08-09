using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Internal defaults projection properties for native Material ToggleButton controls.</summary>
/// <remarks>
/// The public fields are an AXAML metadata contract, not a general token override API. Their
/// normal write path is internal and owned by <see cref="ToggleButtonDefaultsAdapter" />; public
/// visual customization stays on regular Avalonia properties and styles.
/// </remarks>
public static class M3ToggleButton
{
    /// <summary>Internal activation marker installed by the Material ToggleButton control theme.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<bool> UseMaterialDefaultsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, bool>(
            "UseMaterialDefaults", typeof(M3ToggleButton), defaultValue: false);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonColors?>(
            "DefaultColors", typeof(M3ToggleButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonMetrics?>(
            "DefaultMetrics", typeof(M3ToggleButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonShapes?>(
            "DefaultShapes", typeof(M3ToggleButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, SpringSpec>(
            "DefaultShapeMotion", typeof(M3ToggleButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, Thickness>(
            "DefaultOutlineThickness", typeof(M3ToggleButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IImmutableBrush?> DefaultStateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IImmutableBrush?>(
            "DefaultStateLayerBrush", typeof(M3ToggleButton));

    /// <summary>Overrides the state-layer brush projected by the Material ToggleButton theme.</summary>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IBrush?>(
            "StateLayerBrush", typeof(M3ToggleButton));

    static M3ToggleButton()
    {
        UseMaterialDefaultsProperty.Changed.AddClassHandler<ToggleButton>(static (toggle, _) =>
        {
            if (toggle.GetValue(UseMaterialDefaultsProperty))
                ToggleButtonDefaultsAdapter.GetOrCreate(toggle).Activate();
            else if (ToggleButtonDefaultsAdapter.TryGet(toggle, out var adapter))
                adapter?.Deactivate();
        });
    }

    internal static ToggleButtonColors? GetDefaultColors(ToggleButton toggle) =>
        toggle.GetValue(DefaultColorsProperty);

    internal static void SetDefaultColors(ToggleButton toggle, ToggleButtonColors? value) =>
        toggle.SetValue(DefaultColorsProperty, value);

    internal static ToggleButtonMetrics? GetDefaultMetrics(ToggleButton toggle) =>
        toggle.GetValue(DefaultMetricsProperty);

    internal static void SetDefaultMetrics(ToggleButton toggle, ToggleButtonMetrics? value) =>
        toggle.SetValue(DefaultMetricsProperty, value);

    internal static ToggleButtonShapes? GetDefaultShapes(ToggleButton toggle) =>
        toggle.GetValue(DefaultShapesProperty);

    internal static void SetDefaultShapes(ToggleButton toggle, ToggleButtonShapes? value) =>
        toggle.SetValue(DefaultShapesProperty, value);

    internal static SpringSpec GetDefaultShapeMotion(ToggleButton toggle) =>
        toggle.GetValue(DefaultShapeMotionProperty);

    internal static void SetDefaultShapeMotion(ToggleButton toggle, SpringSpec value) =>
        toggle.SetValue(DefaultShapeMotionProperty, value);

    internal static Thickness GetDefaultOutlineThickness(ToggleButton toggle) =>
        toggle.GetValue(DefaultOutlineThicknessProperty);

    internal static void SetDefaultOutlineThickness(ToggleButton toggle, Thickness value) =>
        toggle.SetValue(DefaultOutlineThicknessProperty, value);

    internal static IImmutableBrush? GetDefaultStateLayerBrush(ToggleButton toggle) =>
        toggle.GetValue(DefaultStateLayerBrushProperty);

    internal static void SetDefaultStateLayerBrush(ToggleButton toggle, IImmutableBrush? value) =>
        toggle.SetValue(DefaultStateLayerBrushProperty, value);

    public static IBrush? GetStateLayerBrush(ToggleButton toggle) =>
        toggle.GetValue(StateLayerBrushProperty);

    public static void SetStateLayerBrush(ToggleButton toggle, IBrush? value) =>
        toggle.SetValue(StateLayerBrushProperty, value);
}
