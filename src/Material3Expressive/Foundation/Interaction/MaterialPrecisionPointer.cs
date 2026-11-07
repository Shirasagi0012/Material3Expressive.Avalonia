using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>
/// Explicit inherited precision-pointer sizing environment. Defaults to false, matching fixed
/// Compose Desktop. Consumed by Button, ToggleButton, AlertDialog and expressive dropdown items; it does not
/// detect devices or change minimum interactive layout size.
/// </summary>
public static class MaterialPrecisionPointer
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(MaterialPrecisionPointer), inherits: true);

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
}
