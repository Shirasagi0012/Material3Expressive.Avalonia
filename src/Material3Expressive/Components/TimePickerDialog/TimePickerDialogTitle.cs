using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

public enum TimePickerDisplayMode { Picker, Input, Scroll }

/// <summary>The localized LabelMedium title and bottom padding used by TimePickerDialogDefaults.Title.</summary>
public class TimePickerDialogTitle : TextBlock
{
    public static readonly StyledProperty<TimePickerDisplayMode> DisplayModeProperty =
        AvaloniaProperty.Register<TimePickerDialogTitle, TimePickerDisplayMode>(nameof(DisplayMode));
    public static readonly StyledProperty<CultureInfo?> CultureProperty =
        AvaloniaProperty.Register<TimePickerDialogTitle, CultureInfo?>(nameof(Culture));
    internal static readonly DirectProperty<TimePickerDialogTitle, string> LocalizedTitleProperty =
        AvaloniaProperty.RegisterDirect<TimePickerDialogTitle, string>(nameof(LocalizedTitle), x => x.LocalizedTitle);
    private string _localizedTitle = "";
    internal string LocalizedTitle => _localizedTitle;
    public TimePickerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }
    /// <summary>Null uses CurrentUICulture when the mode or culture changes.</summary>
    public CultureInfo? Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public TimePickerDialogTitle() => UpdateTitle();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayModeProperty || change.Property == CultureProperty) UpdateTitle();
    }
    private void UpdateTitle() => SetAndRaise(LocalizedTitleProperty, ref _localizedTitle, DateTimeStrings.Get(DisplayMode switch
    {
        TimePickerDisplayMode.Input => "TimeInputDialogTitle",
        TimePickerDisplayMode.Scroll => "TimeScrollDialogTitle",
        _ => "TimePickerDialogTitle"
    }, Culture));
}
