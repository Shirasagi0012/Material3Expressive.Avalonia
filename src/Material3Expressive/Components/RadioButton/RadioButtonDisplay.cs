using Avalonia;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Components;

/// <summary>A noninteractive radio indicator, for parents that own selection and grouping.</summary>
/// <remarks>Corresponds to RadioButton with a null callback. State changes retain the Material
/// color and dot animations, without minimum interactive space, focus or selection actions.</remarks>
public class RadioButtonDisplay : TemplatedControl
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<RadioButtonDisplay, bool>(nameof(IsChecked));
    public static readonly StyledProperty<RadioButtonColors?> ColorsProperty =
        AvaloniaProperty.Register<RadioButtonDisplay, RadioButtonColors?>(nameof(Colors));

    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public RadioButtonColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
}
