// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// DatePicker.kt:261-470, DateInput.kt:59-250, TimePicker.kt:871-1180.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Material3Expressive.Controls;

/// <summary>Whether a date-picking surface shows the calendar or its typed alternative.</summary>
public enum DatePickerDisplayMode
{
    Picker,
    Input
}

/// <summary>
/// Shared committed state for a date picker and date input.
/// </summary>
/// <remarks>
/// The application restores this state by constructing it with its persisted selection, displayed
/// month, and display mode. Text being edited remains an input-control transaction and is not
/// silently serialized by the control.
/// </remarks>
public sealed class MaterialDatePickerState : INotifyPropertyChanged
{
    private DateTime? _selectedDate;
    private DateTime _displayMonth;
    private DatePickerDisplayMode _displayMode;

    public MaterialDatePickerState(
        DateTime? selectedDate = null,
        DateTime? displayedMonth = null,
        DatePickerDisplayMode displayMode = DatePickerDisplayMode.Picker)
    {
        _selectedDate = selectedDate?.Date;
        _displayMonth = MonthOf(displayedMonth ?? selectedDate ?? DateTime.Today);
        _displayMode = displayMode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The committed date, or null when there is no selection.</summary>
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            var next = value?.Date;
            if (_selectedDate == next)
                return;

            _selectedDate = next;
            OnPropertyChanged();
            if (next is { })
                DisplayMonth = next.Value;
        }
    }

    /// <summary>The first day of the month shown by a calendar surface.</summary>
    public DateTime DisplayMonth
    {
        get => _displayMonth;
        set
        {
            var next = MonthOf(value);
            if (_displayMonth == next)
                return;

            _displayMonth = next;
            OnPropertyChanged();
        }
    }

    /// <summary>The dialog surface currently selected by the application.</summary>
    public DatePickerDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode == value)
                return;

            _displayMode = value;
            OnPropertyChanged();
        }
    }

    private static DateTime MonthOf(DateTime value) =>
        new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Shared committed state for a typed or graphical date range picker.</summary>
public sealed class MaterialDateRangePickerState : INotifyPropertyChanged
{
    private DateTime? _startDate;
    private DateTime? _endDate;
    private DateTime _displayMonth;
    private DatePickerDisplayMode _displayMode;

    public MaterialDateRangePickerState(
        DateTime? startDate = null,
        DateTime? endDate = null,
        DateTime? displayedMonth = null,
        DatePickerDisplayMode displayMode = DatePickerDisplayMode.Picker)
    {
        ValidateRange(startDate, endDate);
        _startDate = startDate?.Date;
        _endDate = endDate?.Date;
        var initialMonth = displayedMonth ?? startDate ?? endDate ?? DateTime.Today;
        _displayMonth = MonthOf(initialMonth);
        _displayMode = displayMode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime? StartDate
    {
        get => _startDate;
        set => UpdateRange(value, _endDate);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => UpdateRange(_startDate, value);
    }

    public DateTime DisplayMonth
    {
        get => _displayMonth;
        set
        {
            var next = MonthOf(value);
            if (_displayMonth == next)
                return;

            _displayMonth = next;
            OnPropertyChanged();
        }
    }

    public DatePickerDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode == value)
                return;

            _displayMode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Atomically replaces both endpoints, avoiding an invalid intermediate range.</summary>
    public void UpdateRange(DateTime? startDate, DateTime? endDate)
    {
        startDate = startDate?.Date;
        endDate = endDate?.Date;
        ValidateRange(startDate, endDate);

        var startChanged = _startDate != startDate;
        var endChanged = _endDate != endDate;
        if (!startChanged && !endChanged)
            return;

        _startDate = startDate;
        _endDate = endDate;
        if (startChanged)
            OnPropertyChanged(nameof(StartDate));
        if (endChanged)
            OnPropertyChanged(nameof(EndDate));
        if (startDate is { })
            DisplayMonth = startDate.Value;
    }

    private static void ValidateRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate?.Date > endDate?.Date)
            throw new ArgumentException("StartDate must not be after EndDate.");
    }

    private static DateTime MonthOf(DateTime value) =>
        new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Shared committed state for a clock dial and typed time input.</summary>
public sealed class MaterialTimePickerState : INotifyPropertyChanged
{
    private TimeSpan _time;
    private TimePickerField _field;
    private bool _is24Hour;

    public MaterialTimePickerState(
        TimeSpan time = default,
        bool is24Hour = true,
        TimePickerField field = TimePickerField.Hour)
    {
        ValidateTime(time);
        _time = time;
        _is24Hour = is24Hour;
        _field = field;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The committed time of day.</summary>
    public TimeSpan Time
    {
        get => _time;
        set
        {
            ValidateTime(value);
            if (_time == value)
                return;

            _time = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The time component currently selected by the dial or typed editor.</summary>
    public TimePickerField Field
    {
        get => _field;
        set
        {
            if (_field == value)
                return;

            _field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Whether inputs show a 24-hour value instead of an AM/PM selector.</summary>
    public bool Is24Hour
    {
        get => _is24Hour;
        set
        {
            if (_is24Hour == value)
                return;

            _is24Hour = value;
            OnPropertyChanged();
        }
    }

    private static void ValidateTime(TimeSpan value)
    {
        if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(value), "Time must be within one day.");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
