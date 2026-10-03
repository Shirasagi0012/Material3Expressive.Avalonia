using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Material colors for the native Avalonia RadioButton; selection and input remain native.</summary>
public static class RadioButtonAppearance
{
    public static readonly AttachedProperty<RadioButtonColors?> ColorsProperty =
        AvaloniaProperty.RegisterAttached<RadioButton, RadioButtonColors?>("Colors", typeof(RadioButtonAppearance));
    public static RadioButtonColors? GetColors(RadioButton owner) => owner.GetValue(ColorsProperty);
    public static void SetColors(RadioButton owner, RadioButtonColors? value) => owner.SetValue(ColorsProperty, value);
}

/// <summary>The four fixed Compose RadioButtonColors slots; null Colors uses dynamic theme tokens.</summary>
public sealed record RadioButtonColors(Color SelectedColor, Color UnselectedColor,
    Color DisabledSelectedColor, Color DisabledUnselectedColor);
