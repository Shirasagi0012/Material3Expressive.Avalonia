using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation;

/// <summary>Controls surface tonal tinting in a subtree without changing accumulated elevation or shadows.</summary>
public static class MaterialTonalElevation
{
    // ColorScheme.kt LocalTonalElevationEnabled at docs/upstream-baseline.json.
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(MaterialTonalElevation), true, inherits: true);

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
}
