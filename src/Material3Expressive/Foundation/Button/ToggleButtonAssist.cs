using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Controls;

namespace Material3Expressive.Foundation;

/// <summary>Internal defaults projection properties for native Material ToggleButton controls.</summary>
/// <remarks>
/// The public fields are an AXAML metadata contract, not a general token override API. Their
/// normal write path is internal and owned by <see cref="ToggleButtonDefaultsAdapter" />; public
/// visual customization stays on regular Avalonia properties and styles.
/// </remarks>
public static class ToggleButtonAssist
{
    /// <summary>Internal activation marker installed by the Material ToggleButton control theme.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<bool> UseMaterialDefaultsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, bool>(
            "UseMaterialDefaults", typeof(ToggleButtonAssist), defaultValue: false);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonColors?>(
            "DefaultColors", typeof(ToggleButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonMetrics?>(
            "DefaultMetrics", typeof(ToggleButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ToggleButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonShapes?>(
            "DefaultShapes", typeof(ToggleButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, SpringSpec>(
            "DefaultShapeMotion", typeof(ToggleButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, Thickness>(
            "DefaultOutlineThickness", typeof(ToggleButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IImmutableBrush?> DefaultStateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IImmutableBrush?>(
            "DefaultStateLayerBrush", typeof(ToggleButtonAssist));

    /// <summary>Overrides the state-layer brush projected by the Material ToggleButton theme.</summary>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, IBrush?>(
            "StateLayerBrush", typeof(ToggleButtonAssist));

    static ToggleButtonAssist()
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
