// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// DateRangeInput.kt and DateInput.kt:116-250,282-359.

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

/// <summary>Two locale-aware date fields that commit one ordered range.</summary>
[TemplatePart(StartPartName, typeof(MaterialDateInput))]
[TemplatePart(EndPartName, typeof(MaterialDateInput))]
public class MaterialDateRangeInput : TemplatedControl
{
    private const string StartPartName = "PART_Start";
    private const string EndPartName = "PART_End";

    public static readonly DirectProperty<MaterialDateRangeInput, MaterialDateRangePickerState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateRangeInput, MaterialDateRangePickerState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    public static readonly StyledProperty<CultureInfo?> CultureProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, CultureInfo?>(nameof(Culture));

    public static readonly StyledProperty<string?> DateFormatProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, string?>(nameof(DateFormat));

    public static readonly StyledProperty<DateTime> MinimumDateProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, DateTime>(
            nameof(MinimumDate), new DateTime(1900, 1, 1));

    public static readonly StyledProperty<DateTime> MaximumDateProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, DateTime>(
            nameof(MaximumDate), new DateTime(2100, 12, 31));

    public static readonly StyledProperty<string?> StartTextProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, string?>(
            nameof(StartText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> EndTextProperty =
        AvaloniaProperty.Register<MaterialDateRangeInput, string?>(
            nameof(EndText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<MaterialDateRangeInput, bool> IsInputValidProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateRangeInput, bool>(
            nameof(IsInputValid), owner => owner.IsInputValid);

    public static readonly DirectProperty<MaterialDateRangeInput, string?> ValidationErrorProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateRangeInput, string?>(
            nameof(ValidationError), owner => owner.ValidationError);

    private MaterialDateRangePickerState _state = new();
    private MaterialDateInput? _startInput;
    private MaterialDateInput? _endInput;
    private bool _isEditing;
    private bool _updatingStateFromInput;
    private DateTime? _originalStart;
    private DateTime? _originalEnd;
    private bool _isInputValid = true;
    private string? _validationError;

    public MaterialDateRangeInput()
    {
        _state.PropertyChanged += OnStateChanged;
        AddHandler(
            KeyDownEvent,
            OnChildKeyDown,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    public MaterialDateRangePickerState State
    {
        get => _state;
        set => SetState(value);
    }

    public CultureInfo? Culture
    {
        get => GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public string? DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public DateTime MinimumDate
    {
        get => GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public DateTime MaximumDate
    {
        get => GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public string? StartText
    {
        get => GetValue(StartTextProperty);
        set => SetValue(StartTextProperty, value);
    }

    public string? EndText
    {
        get => GetValue(EndTextProperty);
        set => SetValue(EndTextProperty, value);
    }

    public bool IsInputValid => _isInputValid;

    public string? ValidationError => _validationError;

    public bool FocusInput() => _startInput?.FocusInput() == true;

    /// <summary>Commits both endpoints only when each date is valid and start is not after end.</summary>
    public bool CommitEdit()
    {
        if (_startInput is null || _endInput is null)
            return false;

        var startValid = _startInput.CommitEdit();
        var endValid = _endInput.CommitEdit();
        if (!startValid || !endValid)
        {
            SetValidation(_startInput.ValidationError ?? _endInput.ValidationError);
            return false;
        }

        var start = _startInput.SelectedDate;
        var end = _endInput.SelectedDate;
        if (start?.Date > end?.Date)
        {
            const string error = "Start date must not be after end date.";
            _startInput.IsError = true;
            _startInput.SupportingText = error;
            _endInput.IsError = true;
            _endInput.SupportingText = error;
            SetValidation(error);
            return false;
        }

        _updatingStateFromInput = true;
        try
        {
            _state.UpdateRange(start, end);
        }
        finally
        {
            _updatingStateFromInput = false;
        }
        _isEditing = false;
        SetValidation(null);
        return true;
    }

    /// <summary>Restores both endpoints present when range editing began.</summary>
    public void CancelEdit()
    {
        if (_isEditing)
        {
            _updatingStateFromInput = true;
            try
            {
                _state.UpdateRange(_originalStart, _originalEnd);
            }
            finally
            {
                _updatingStateFromInput = false;
            }
        }

        _isEditing = false;
        SyncChildrenFromState();
        SetValidation(null);
    }

    public void ResetState() => SetState(null);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _startInput = e.NameScope.Find<MaterialDateInput>(StartPartName);
        _endInput = e.NameScope.Find<MaterialDateInput>(EndPartName);
        SyncChildrenFromState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsKeyboardFocusWithinProperty)
        {
            if (IsKeyboardFocusWithin)
                BeginEdit();
            else if (_isEditing)
            {
                CommitEdit();
                _isEditing = false;
            }
        }
    }

    private void BeginEdit()
    {
        if (_isEditing)
            return;

        _originalStart = _state.StartDate;
        _originalEnd = _state.EndDate;
        _isEditing = true;
    }

    private void OnChildKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            e.Handled = CommitEdit();
        else if (e.Key == Key.Escape)
            CancelEdit();
    }

    private void SyncChildrenFromState()
    {
        if (_startInput is null || _endInput is null)
            return;

        _startInput.State = new MaterialDatePickerState(_state.StartDate);
        _endInput.State = new MaterialDatePickerState(_state.EndDate);
        SetCurrentValue(StartTextProperty, _startInput.Text);
        SetCurrentValue(EndTextProperty, _endInput.Text);
    }

    private void SetValidation(string? error)
    {
        var previousValid = _isInputValid;
        var previousError = _validationError;
        _isInputValid = error is null;
        _validationError = error;

        if (previousValid != _isInputValid)
            RaisePropertyChanged(IsInputValidProperty, previousValid, _isInputValid);
        if (previousError != error)
            RaisePropertyChanged(ValidationErrorProperty, previousError, error);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        var endpointChanged =
            e.PropertyName == nameof(MaterialDateRangePickerState.StartDate)
            || e.PropertyName == nameof(MaterialDateRangePickerState.EndDate);
        if (!endpointChanged)
            return;

        if (!_updatingStateFromInput)
            _isEditing = false;
        if (!_isEditing)
        {
            SyncChildrenFromState();
        }
    }

    private void SetState(MaterialDateRangePickerState? value)
    {
        var next = value ?? new MaterialDateRangePickerState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        _isEditing = false;
        SyncChildrenFromState();
    }
}
