using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Material3Expressive.Components;

/// <summary>Material time entry content with application-bound minute-precision values.</summary>
public class TimeScroll : TemplatedControl
{
    public static readonly StyledProperty<TimeOnly> SelectedTimeProperty = TimePicker.SelectedTimeProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<bool> Is24HourProperty = TimePicker.Is24HourProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<TimePickerLayout> LayoutProperty = TimePicker.LayoutProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<TimePickerVariant> VariantProperty = TimePicker.VariantProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<TimeSelection> SelectionProperty = TimePicker.SelectionProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<object?> ToggleProperty = TimePicker.ToggleProperty.AddOwner<TimeScroll>();
    public static readonly StyledProperty<IDataTemplate?> ToggleTemplateProperty = TimePicker.ToggleTemplateProperty.AddOwner<TimeScroll>();
    public static readonly DirectProperty<TimeScroll, bool> IsInputValidProperty = AvaloniaProperty.RegisterDirect<TimeScroll, bool>(nameof(IsInputValid), c => c.IsInputValid);
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
    public TimeScroll()
    {
        Controller = new TimeEntryController(this, TimeEntryKind.Scroll);
        SetCurrentValue(Is24HourProperty, !System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('t'));
        SetCurrentValue(VariantProperty, TimePickerVariant.Vibrant);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) { base.OnApplyTemplate(e); Controller.Attach(e.NameScope); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) { base.OnPropertyChanged(change); Controller?.Changed(change.Property); }
}
