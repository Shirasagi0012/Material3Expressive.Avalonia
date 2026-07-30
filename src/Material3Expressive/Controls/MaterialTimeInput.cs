// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// TimePicker.kt:302-332,950-1180,1498-1665.

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3Expressive.Controls;

public enum TimePickerPeriod
{
    Am,
    Pm
}

/// <summary>A two-field typed alternative to <see cref="MaterialTimePicker"/>.</summary>
[TemplatePart(HourPartName, typeof(MaterialTextField))]
[TemplatePart(MinutePartName, typeof(MaterialTextField))]
[TemplatePart(AmPartName, typeof(MaterialSegmentedButton))]
[TemplatePart(PmPartName, typeof(MaterialSegmentedButton))]
public class MaterialTimeInput : TemplatedControl
{
    private const string HourPartName = "PART_Hour";
    private const string MinutePartName = "PART_Minute";
    private const string AmPartName = "PART_Am";
    private const string PmPartName = "PART_Pm";

    public static readonly DirectProperty<MaterialTimeInput, MaterialTimePickerState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialTimeInput, MaterialTimePickerState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<MaterialTimeInput, TimeSpan>(
            nameof(Time),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            validate: static value => value >= TimeSpan.Zero && value < TimeSpan.FromDays(1));

    public static readonly StyledProperty<bool> Use24HourClockProperty =
        AvaloniaProperty.Register<MaterialTimeInput, bool>(
            nameof(Use24HourClock), true, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<TimePickerPeriod> PeriodProperty =
        AvaloniaProperty.Register<MaterialTimeInput, TimePickerPeriod>(
            nameof(Period), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> HourTextProperty =
        AvaloniaProperty.Register<MaterialTimeInput, string?>(
            nameof(HourText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> MinuteTextProperty =
        AvaloniaProperty.Register<MaterialTimeInput, string?>(
            nameof(MinuteText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<MaterialTimeInput, bool> IsInputValidProperty =
        AvaloniaProperty.RegisterDirect<MaterialTimeInput, bool>(
            nameof(IsInputValid), owner => owner.IsInputValid);

    public static readonly DirectProperty<MaterialTimeInput, string?> ValidationErrorProperty =
        AvaloniaProperty.RegisterDirect<MaterialTimeInput, string?>(
            nameof(ValidationError), owner => owner.ValidationError);

    private MaterialTimePickerState _state = new();
    private MaterialTextField? _hourField;
    private MaterialTextField? _minuteField;
    private MaterialSegmentedButton? _amButton;
    private MaterialSegmentedButton? _pmButton;
    private bool _updatingFromState;
    private bool _syncingText;
    private bool _isEditing;
    private TimeSpan _editOriginal;
    private bool _isInputValid = true;
    private string? _validationError;

    public MaterialTimeInput()
    {
        _state.PropertyChanged += OnStateChanged;
        SetCurrentValue(TimeProperty, _state.Time);
        SyncTextFromState();
    }

    /// <summary>The non-null committed state that can also be assigned to a clock dial.</summary>
    public MaterialTimePickerState State
    {
        get => _state;
        set => SetState(value);
    }

    /// <summary>The committed time. Invalid draft fields never replace this value.</summary>
    public TimeSpan Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public bool Use24HourClock
    {
        get => GetValue(Use24HourClockProperty);
        set => SetValue(Use24HourClockProperty, value);
    }

    public TimePickerPeriod Period
    {
        get => GetValue(PeriodProperty);
        set => SetValue(PeriodProperty, value);
    }

    public string? HourText
    {
        get => GetValue(HourTextProperty);
        set => SetValue(HourTextProperty, value);
    }

    public string? MinuteText
    {
        get => GetValue(MinuteTextProperty);
        set => SetValue(MinuteTextProperty, value);
    }

    public bool IsInputValid => _isInputValid;

    public string? ValidationError => _validationError;

    /// <summary>Moves keyboard focus to the hour field.</summary>
    public bool FocusInput() => _hourField?.FocusHostedEditor() == true;

    /// <summary>Commits both fields when they form a valid time of day.</summary>
    public bool CommitEdit()
    {
        if (!TryParseDraft(out var parsed, out var hourError, out var minuteError))
        {
            SetValidation(hourError, minuteError);
            return false;
        }

        SetValidation(null, null);
        _updatingFromState = true;
        try
        {
            _state.Time = parsed;
            SetCurrentValue(TimeProperty, parsed);
        }
        finally
        {
            _updatingFromState = false;
        }

        _isEditing = false;
        SyncTextFromState();
        return true;
    }

    /// <summary>Discards both draft fields and restores the time present when editing began.</summary>
    public void CancelEdit()
    {
        if (_isEditing)
        {
            _updatingFromState = true;
            try
            {
                _state.Time = _editOriginal;
                SetCurrentValue(TimeProperty, _editOriginal);
            }
            finally
            {
                _updatingFromState = false;
            }
        }

        _isEditing = false;
        SetValidation(null, null);
        SyncTextFromState();
    }

    /// <summary>Replaces external state with a fresh, control-owned default.</summary>
    public void ResetState() => SetState(null);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DetachTemplateHandlers();
        base.OnApplyTemplate(e);

        _hourField = e.NameScope.Find<MaterialTextField>(HourPartName);
        _minuteField = e.NameScope.Find<MaterialTextField>(MinutePartName);
        _amButton = e.NameScope.Find<MaterialSegmentedButton>(AmPartName);
        _pmButton = e.NameScope.Find<MaterialSegmentedButton>(PmPartName);

        if (_hourField is { })
            _hourField.AddHandler(GotFocusEvent, OnHourFocused, RoutingStrategies.Bubble);
        if (_minuteField is { })
            _minuteField.AddHandler(GotFocusEvent, OnMinuteFocused, RoutingStrategies.Bubble);
        if (_amButton is { })
            _amButton.Click += OnAmClicked;
        if (_pmButton is { })
            _pmButton.Click += OnPmClicked;

        SyncTextFromState();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Enter)
        {
            e.Handled = CommitEdit();
        }
        else if (e.Key == Key.Escape)
        {
            CancelEdit();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TimeProperty && !_updatingFromState)
        {
            _state.Time = Time;
            if (!_isEditing)
                SyncTextFromState();
        }
        else if (change.Property == Use24HourClockProperty && !_updatingFromState)
        {
            _state.Is24Hour = Use24HourClock;
            if (!_isEditing)
                SyncTextFromState();
        }
        else if (change.Property == PeriodProperty && !_syncingText)
        {
            SyncPeriodButtons();
            ValidateDraftWithoutMessages();
        }
        else if ((change.Property == HourTextProperty || change.Property == MinuteTextProperty)
                 && !_syncingText)
        {
            if (IsKeyboardFocusWithin && !_isEditing)
                BeginEdit();

            ValidateDraftWithoutMessages();
            if (change.Property == HourTextProperty
                && _hourField?.IsKeyboardFocusWithin == true
                && (HourText?.Length ?? 0) >= 2
                && TryParseHour(out _))
            {
                _state.Field = TimePickerField.Minute;
                _minuteField?.FocusHostedEditor();
            }
        }
        else if (change.Property == IsKeyboardFocusWithinProperty && !IsKeyboardFocusWithin)
        {
            CommitEdit();
            _isEditing = false;
        }
    }

    private void BeginEdit()
    {
        if (_isEditing)
            return;

        _editOriginal = _state.Time;
        _isEditing = true;
    }

    private bool TryParseDraft(
        out TimeSpan parsed,
        out string? hourError,
        out string? minuteError)
    {
        hourError = null;
        minuteError = null;

        if (!TryParseHour(out var hour))
            hourError = Use24HourClock ? "Hour must be 00–23." : "Hour must be 01–12.";

        if (!int.TryParse(MinuteText, NumberStyles.None, CultureInfo.InvariantCulture, out var minute)
            || minute is < 0 or > 59)
        {
            minuteError = "Minute must be 00–59.";
        }

        if (hourError is not null || minuteError is not null)
        {
            parsed = default;
            return false;
        }

        if (!Use24HourClock)
        {
            hour %= 12;
            if (Period == TimePickerPeriod.Pm)
                hour += 12;
        }

        parsed = new TimeSpan(hour, minute, 0);
        return true;
    }

    private bool TryParseHour(out int hour)
    {
        if (!int.TryParse(HourText, NumberStyles.None, CultureInfo.InvariantCulture, out hour))
            return false;

        return Use24HourClock ? hour is >= 0 and <= 23 : hour is >= 1 and <= 12;
    }

    private void ValidateDraftWithoutMessages()
    {
        var valid = TryParseDraft(out _, out _, out _);
        SetValidation(valid ? null : string.Empty, valid ? null : string.Empty);
    }

    private void SetValidation(string? hourError, string? minuteError)
    {
        var error = !string.IsNullOrEmpty(hourError) ? hourError
            : !string.IsNullOrEmpty(minuteError) ? minuteError
            : null;
        var valid = hourError is null && minuteError is null;
        var previousValid = _isInputValid;
        var previousError = _validationError;
        _isInputValid = valid;
        _validationError = error;

        if (_hourField is { })
        {
            _hourField.IsError = !string.IsNullOrEmpty(hourError);
            _hourField.SupportingText = string.IsNullOrEmpty(hourError) ? null : hourError;
        }
        if (_minuteField is { })
        {
            _minuteField.IsError = !string.IsNullOrEmpty(minuteError);
            _minuteField.SupportingText = string.IsNullOrEmpty(minuteError) ? null : minuteError;
        }

        if (previousValid != valid)
            RaisePropertyChanged(IsInputValidProperty, previousValid, valid);
        if (previousError != error)
            RaisePropertyChanged(ValidationErrorProperty, previousError, error);
    }

    private void SyncTextFromState()
    {
        _syncingText = true;
        try
        {
            var hour = _state.Time.Hours;
            SetCurrentValue(
                HourTextProperty,
                (_state.Is24Hour ? hour : hour % 12 == 0 ? 12 : hour % 12).ToString(
                    "00",
                    CultureInfo.InvariantCulture));
            SetCurrentValue(
                MinuteTextProperty,
                _state.Time.Minutes.ToString("00", CultureInfo.InvariantCulture));
            SetCurrentValue(Use24HourClockProperty, _state.Is24Hour);
            SetCurrentValue(
                PeriodProperty,
                _state.Time.Hours >= 12 ? TimePickerPeriod.Pm : TimePickerPeriod.Am);

            if (_amButton is { })
                _amButton.IsChecked = Period == TimePickerPeriod.Am;
            if (_pmButton is { })
                _pmButton.IsChecked = Period == TimePickerPeriod.Pm;
        }
        finally
        {
            _syncingText = false;
        }

        SetValidation(null, null);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        var committedValueChanged =
            e.PropertyName == nameof(MaterialTimePickerState.Time)
            || e.PropertyName == nameof(MaterialTimePickerState.Is24Hour);
        var changedByThisInput = _updatingFromState;
        _updatingFromState = true;
        try
        {
            if (e.PropertyName == nameof(MaterialTimePickerState.Time))
                SetCurrentValue(TimeProperty, _state.Time);
            if (e.PropertyName == nameof(MaterialTimePickerState.Is24Hour))
                SetCurrentValue(Use24HourClockProperty, _state.Is24Hour);
        }
        finally
        {
            _updatingFromState = false;
        }

        if (committedValueChanged && !changedByThisInput)
            _isEditing = false;
        if (!_isEditing)
            SyncTextFromState();
    }

    private void SetState(MaterialTimePickerState? value)
    {
        var next = value ?? new MaterialTimePickerState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        _isEditing = false;

        _updatingFromState = true;
        try
        {
            SetCurrentValue(TimeProperty, _state.Time);
            SetCurrentValue(Use24HourClockProperty, _state.Is24Hour);
        }
        finally
        {
            _updatingFromState = false;
        }

        SyncTextFromState();
    }

    private void OnHourFocused(object? sender, RoutedEventArgs e)
    {
        BeginEdit();
        _state.Field = TimePickerField.Hour;
    }

    private void OnMinuteFocused(object? sender, RoutedEventArgs e)
    {
        BeginEdit();
        _state.Field = TimePickerField.Minute;
    }

    private void OnAmClicked(object? sender, RoutedEventArgs e)
    {
        BeginEdit();
        SetCurrentValue(PeriodProperty, TimePickerPeriod.Am);
        SyncPeriodButtons();
    }

    private void OnPmClicked(object? sender, RoutedEventArgs e)
    {
        BeginEdit();
        SetCurrentValue(PeriodProperty, TimePickerPeriod.Pm);
        SyncPeriodButtons();
    }

    private void SyncPeriodButtons()
    {
        if (_amButton is { })
            _amButton.IsChecked = Period == TimePickerPeriod.Am;
        if (_pmButton is { })
            _pmButton.IsChecked = Period == TimePickerPeriod.Pm;
    }

    private void DetachTemplateHandlers()
    {
        if (_hourField is { })
            _hourField.RemoveHandler(GotFocusEvent, OnHourFocused);
        if (_minuteField is { })
            _minuteField.RemoveHandler(GotFocusEvent, OnMinuteFocused);
        if (_amButton is { })
            _amButton.Click -= OnAmClicked;
        if (_pmButton is { })
            _pmButton.Click -= OnPmClicked;
    }
}
