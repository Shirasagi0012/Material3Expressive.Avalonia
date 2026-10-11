using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

/// <summary>A time-picker dialog container. The caller supplies title, editor and actions.</summary>
public class TimePickerDialog : Dialog
{
    public static readonly StyledProperty<object?> ConfirmButtonProperty = AvaloniaProperty.Register<TimePickerDialog, object?>(nameof(ConfirmButton));
    public static readonly StyledProperty<object?> DismissButtonProperty = AvaloniaProperty.Register<TimePickerDialog, object?>(nameof(DismissButton));
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<TimePickerDialog, object?>(nameof(Title));
    public static readonly StyledProperty<object?> ModeToggleButtonProperty = AvaloniaProperty.Register<TimePickerDialog, object?>(nameof(ModeToggleButton));
    public static readonly StyledProperty<IDataTemplate?> ConfirmButtonTemplateProperty = AvaloniaProperty.Register<TimePickerDialog, IDataTemplate?>(nameof(ConfirmButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> DismissButtonTemplateProperty = AvaloniaProperty.Register<TimePickerDialog, IDataTemplate?>(nameof(DismissButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<TimePickerDialog, IDataTemplate?>(nameof(TitleTemplate));
    public static readonly StyledProperty<IDataTemplate?> ModeToggleButtonTemplateProperty = AvaloniaProperty.Register<TimePickerDialog, IDataTemplate?>(nameof(ModeToggleButtonTemplate));

    public object? ConfirmButton { get => GetValue(ConfirmButtonProperty); set => SetValue(ConfirmButtonProperty, value); }
    public object? DismissButton { get => GetValue(DismissButtonProperty); set => SetValue(DismissButtonProperty, value); }
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? ModeToggleButton { get => GetValue(ModeToggleButtonProperty); set => SetValue(ModeToggleButtonProperty, value); }
    public IDataTemplate? ConfirmButtonTemplate { get => GetValue(ConfirmButtonTemplateProperty); set => SetValue(ConfirmButtonTemplateProperty, value); }
    public IDataTemplate? DismissButtonTemplate { get => GetValue(DismissButtonTemplateProperty); set => SetValue(DismissButtonTemplateProperty, value); }
    public IDataTemplate? TitleTemplate { get => GetValue(TitleTemplateProperty); set => SetValue(TitleTemplateProperty, value); }
    public IDataTemplate? ModeToggleButtonTemplate { get => GetValue(ModeToggleButtonTemplateProperty); set => SetValue(ModeToggleButtonTemplateProperty, value); }

    static TimePickerDialog() => UsePlatformDefaultWidthProperty.OverrideDefaultValue<TimePickerDialog>(false);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty && change.NewValue is ILogical content)
            LogicalChildren.Remove(content);
        if (change.Property == DismissButtonProperty || change.Property == DismissButtonTemplateProperty)
            PseudoClasses.Set(":has-dismiss", DismissButton is not null || DismissButtonTemplate is not null);
        if (change.Property == ModeToggleButtonProperty || change.Property == ModeToggleButtonTemplateProperty)
            PseudoClasses.Set(":has-mode-toggle", ModeToggleButton is not null || ModeToggleButtonTemplate is not null);
    }
}
