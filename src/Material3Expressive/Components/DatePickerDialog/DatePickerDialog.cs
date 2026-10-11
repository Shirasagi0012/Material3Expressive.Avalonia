using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;

namespace Material3Expressive.Components;

/// <summary>A date-picker dialog container. Content and action handling belong to the caller.</summary>
public class DatePickerDialog : BasicAlertDialog
{
    public static readonly StyledProperty<object?> ConfirmButtonProperty = AvaloniaProperty.Register<DatePickerDialog, object?>(nameof(ConfirmButton));
    public static readonly StyledProperty<object?> DismissButtonProperty = AvaloniaProperty.Register<DatePickerDialog, object?>(nameof(DismissButton));
    public static readonly StyledProperty<IDataTemplate?> ConfirmButtonTemplateProperty = AvaloniaProperty.Register<DatePickerDialog, IDataTemplate?>(nameof(ConfirmButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> DismissButtonTemplateProperty = AvaloniaProperty.Register<DatePickerDialog, IDataTemplate?>(nameof(DismissButtonTemplate));
    public static readonly StyledProperty<double> TonalElevationProperty = AvaloniaProperty.Register<DatePickerDialog, double>(nameof(TonalElevation));

    public object? ConfirmButton { get => GetValue(ConfirmButtonProperty); set => SetValue(ConfirmButtonProperty, value); }
    public object? DismissButton { get => GetValue(DismissButtonProperty); set => SetValue(DismissButtonProperty, value); }
    public IDataTemplate? ConfirmButtonTemplate { get => GetValue(ConfirmButtonTemplateProperty); set => SetValue(ConfirmButtonTemplateProperty, value); }
    public IDataTemplate? DismissButtonTemplate { get => GetValue(DismissButtonTemplateProperty); set => SetValue(DismissButtonTemplateProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }

    static DatePickerDialog() => UsePlatformDefaultWidthProperty.OverrideDefaultValue<DatePickerDialog>(false);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // The presenter owns content inheritance inside the Surface, as in AlertDialog.
        if (change.Property == ContentProperty && change.NewValue is ILogical content)
            LogicalChildren.Remove(content);
        if (change.Property == DismissButtonProperty || change.Property == DismissButtonTemplateProperty)
            PseudoClasses.Set(":has-dismiss", DismissButton is not null || DismissButtonTemplate is not null);
    }
}
