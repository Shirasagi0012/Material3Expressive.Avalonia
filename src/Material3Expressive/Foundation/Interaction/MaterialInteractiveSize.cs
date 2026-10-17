using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>Inherited minimum interactive layout size. Null preserves each component's default.</summary>
public static class MaterialInteractiveSize
{
    public static readonly AttachedProperty<double?> MinimumProperty =
        AvaloniaProperty.RegisterAttached<Control, double?>("Minimum", typeof(MaterialInteractiveSize), inherits: true);
    public static double? GetMinimum(Control control) => control.GetValue(MinimumProperty);
    public static void SetMinimum(Control control, double? value) => control.SetValue(MinimumProperty, value);
    internal static double Resolve(Control control, double fallback = 48) =>
        Math.Max(0, GetMinimum(control) is { } value && !double.IsNaN(value) ? value : fallback);
}
