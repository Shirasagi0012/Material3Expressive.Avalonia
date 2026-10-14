using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Material3Expressive.Components;

/// <summary>Material time entry content with application-bound minute-precision values.</summary>
public class TimePicker : TemplatedControl
{
    public static readonly StyledProperty<TimeOnly> SelectedTimeProperty = AvaloniaProperty.Register<TimePicker, TimeOnly>(nameof(SelectedTime), default, defaultBindingMode: BindingMode.TwoWay,
        coerce: (_, value) => new TimeOnly(value.Hour, value.Minute));
    public static readonly StyledProperty<bool> Is24HourProperty = AvaloniaProperty.Register<TimePicker, bool>(nameof(Is24Hour), false, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimePickerLayout> LayoutProperty = AvaloniaProperty.Register<TimePicker, TimePickerLayout>(nameof(Layout), TimePickerLayout.Automatic, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimePickerVariant> VariantProperty = AvaloniaProperty.Register<TimePicker, TimePickerVariant>(nameof(Variant), TimePickerVariant.Standard, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimeSelection> SelectionProperty = AvaloniaProperty.Register<TimePicker, TimeSelection>(nameof(Selection), TimeSelection.Hour, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<object?> ToggleProperty = AvaloniaProperty.Register<TimePicker, object?>(nameof(Toggle), null, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IDataTemplate?> ToggleTemplateProperty = AvaloniaProperty.Register<TimePicker, IDataTemplate?>(nameof(ToggleTemplate), null, defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<TimePicker, bool> IsInputValidProperty = AvaloniaProperty.RegisterDirect<TimePicker, bool>(nameof(IsInputValid), c => c.IsInputValid);
    private bool _isInputValid = true;
    public bool IsInputValid => _isInputValid;
    internal void SetInputValid(bool value) => SetAndRaise(IsInputValidProperty, ref _isInputValid, value);
    public TimeOnly SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }
    public TimePickerLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public TimePickerVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public TimeSelection Selection { get => GetValue(SelectionProperty); set => SetValue(SelectionProperty, value); }
    public object? Toggle { get => GetValue(ToggleProperty); set => SetValue(ToggleProperty, value); }
    public IDataTemplate? ToggleTemplate { get => GetValue(ToggleTemplateProperty); set => SetValue(ToggleTemplateProperty, value); }
    internal TimeEntryController Controller { get; }
    public TimePicker()
    {
        Controller = new TimeEntryController(this, TimeEntryKind.Dial);
        SetCurrentValue(Is24HourProperty, !System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('t'));

    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) { base.OnApplyTemplate(e); Controller.Attach(e.NameScope); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) { base.OnPropertyChanged(change); Controller?.Changed(change.Property); }
}
