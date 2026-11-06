using Avalonia;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Components;

/// <summary>A noninteractive two/three-state checkbox, for rows that own their selection action.</summary>
/// <remarks>Corresponds to Checkbox/TriStateCheckbox with a null callback. It does not add
/// minimum interactive space, focus, input handling, ripple or toggle automation.</remarks>
public class CheckBoxDisplay : TemplatedControl
{
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<CheckBoxDisplay, bool?>(nameof(IsChecked), false);
    public static readonly StyledProperty<CheckBoxColors?> ColorsProperty =
        AvaloniaProperty.Register<CheckBoxDisplay, CheckBoxColors?>(nameof(Colors));
    public static readonly StyledProperty<CheckBoxStroke?> CheckmarkStrokeProperty =
        AvaloniaProperty.Register<CheckBoxDisplay, CheckBoxStroke?>(nameof(CheckmarkStroke));
    public static readonly StyledProperty<CheckBoxStroke?> OutlineStrokeProperty =
        AvaloniaProperty.Register<CheckBoxDisplay, CheckBoxStroke?>(nameof(OutlineStroke));

    public bool? IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public CheckBoxColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public CheckBoxStroke? CheckmarkStroke { get => GetValue(CheckmarkStrokeProperty); set => SetValue(CheckmarkStrokeProperty, value); }
    public CheckBoxStroke? OutlineStroke { get => GetValue(OutlineStrokeProperty); set => SetValue(OutlineStrokeProperty, value); }
}
