using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Layout defaults for Avalonia native picker themes; independent of Compose picker tokens.
public sealed class NativePickerTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.NativeDatePicker.MinimumWidth] = 296.0d,
        [Comp.NativeTimePicker.MinimumWidth] = 240.0d,
        [Comp.NativePicker.FieldMinimumHeight] = 56.0d,
        [Comp.NativePicker.FieldPadding] = new Thickness(16, 8),
        [Comp.NativePicker.FieldDividerMargin] = new Thickness(4, 0),
        [Comp.NativePicker.PresenterWidth] = 320.0d,
        [Comp.NativePicker.PresenterHeight] = 304.0d,
        [Comp.NativePicker.PresenterMaxHeight] = 304.0d,
        [Comp.NativePicker.WheelMargin] = new Thickness(8, 0),
        [Comp.NativePicker.WheelColumnSpacing] = 8.0d,
        [Comp.NativePicker.WheelItemHeight] = 48.0d,
        [Comp.NativePicker.RepeatButtonHeight] = 24.0d,
        [Comp.NativePicker.RepeatButtonMinimumHeight] = 24.0d,
        [Comp.NativePicker.ActionRowHeight] = 64.0d,
        [Comp.NativePicker.ActionPadding] = new Thickness(8),
        [Comp.NativeCalendarDatePicker.MinimumWidth] = 280.0d,
        [Comp.NativeCalendarDatePicker.OpenButtonMinimumWidth] = 48.0d,
        [Comp.NativeCalendar.ContentWidth] = 336.0d,
        [Comp.NativeCalendar.ContentMargin] = new Thickness(12),
        [Comp.NativeCalendar.HeaderHeight] = 48.0d,
        [Comp.NativeCalendar.NavigationColumnWidth] = 48.0d,
        [Comp.NativeCalendar.WeekdayRowHeight] = 40.0d,
        [Comp.NativeCalendar.DayRowHeight] = 48.0d,
        [Comp.NativeCalendar.DayMinimumWidth] = 40.0d,
        [Comp.NativeCalendar.DayMinimumHeight] = 40.0d,
        [Comp.NativeCalendar.DayMargin] = new Thickness(2),
        [Comp.NativeCalendar.YearMinimumWidth] = 64.0d,
        [Comp.NativeCalendar.YearMinimumHeight] = 48.0d,
        [Comp.NativeCalendar.YearPadding] = new Thickness(8),
        [Comp.NativeCalendar.NavigationButtonMinimumWidth] = 48.0d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);

    public event EventHandler? Changed { add { } remove { } }
}
