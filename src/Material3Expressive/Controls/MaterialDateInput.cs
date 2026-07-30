// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// DateInput.kt:59-250,282-359,395-448.

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Input;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>A locale-aware typed alternative to <see cref="MaterialDatePicker"/>.</summary>
/// <remarks>
/// The displayed text is a cancelable desktop editing transaction. Enter or leaving the editor
/// commits a complete valid date; Escape restores the value present when editing began. Sharing
/// <see cref="State"/> with a calendar keeps graphical and typed surfaces synchronized.
/// </remarks>
public class MaterialDateInput : MaterialTextField
{
    public static readonly DirectProperty<MaterialDateInput, MaterialDatePickerState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateInput, MaterialDatePickerState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MaterialDateInput, DateTime?>(
            nameof(SelectedDate), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<CultureInfo?> CultureProperty =
        AvaloniaProperty.Register<MaterialDateInput, CultureInfo?>(nameof(Culture));

    public static readonly StyledProperty<string?> DateFormatProperty =
        AvaloniaProperty.Register<MaterialDateInput, string?>(nameof(DateFormat));

    public static readonly StyledProperty<DateTime> MinimumDateProperty =
        AvaloniaProperty.Register<MaterialDateInput, DateTime>(
            nameof(MinimumDate), new DateTime(1900, 1, 1));

    public static readonly StyledProperty<DateTime> MaximumDateProperty =
        AvaloniaProperty.Register<MaterialDateInput, DateTime>(
            nameof(MaximumDate), new DateTime(2100, 12, 31));

    public static readonly DirectProperty<MaterialDateInput, bool> IsInputValidProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateInput, bool>(
            nameof(IsInputValid), owner => owner.IsInputValid);

    public static readonly DirectProperty<MaterialDateInput, string?> ValidationErrorProperty =
        AvaloniaProperty.RegisterDirect<MaterialDateInput, string?>(
            nameof(ValidationError), owner => owner.ValidationError);

    private MaterialDatePickerState _state = new();
    private bool _updatingFromState;
    private bool _syncingText;
    private bool _isEditing;
    private DateTime? _editOriginal;
    private bool _isInputValid = true;
    private string? _validationError;

    public MaterialDateInput()
    {
        _state.PropertyChanged += OnStateChanged;
        SetCurrentValue(VariantProperty, TextFieldVariant.Outlined);
        SetCurrentValue(LabelProperty, "Date");
        SetCurrentValue(SelectedDateProperty, _state.SelectedDate);
        SyncTextFromState();
    }

    /// <summary>The non-null committed state that can also be assigned to a calendar.</summary>
    public MaterialDatePickerState State
    {
        get => _state;
        set => SetState(value);
    }

    /// <summary>The committed date. Invalid or incomplete draft text never replaces this value.</summary>
    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>The culture used for default parsing and formatting.</summary>
    public CultureInfo? Culture
    {
        get => GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    /// <summary>
    /// An optional exact date pattern. When null, the selected culture's short-date pattern is used.
    /// </summary>
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

    /// <summary>Whether the current draft is empty or a complete date inside the allowed range.</summary>
    public bool IsInputValid => _isInputValid;

    /// <summary>The current validation message, or null while valid.</summary>
    public string? ValidationError => _validationError;

    /// <summary>Moves keyboard focus to the native single-line editor.</summary>
    public bool FocusInput() => FocusEditor();

    /// <summary>
    /// Commits the current draft. Returns false and leaves the committed date unchanged on error.
    /// </summary>
    public bool CommitEdit()
    {
        if (!TryParseDraft(out var parsed, out var error))
        {
            SetValidation(false, error);
            return false;
        }

        SetValidation(true, null);
        _updatingFromState = true;
        try
        {
            _state.SelectedDate = parsed;
            SetCurrentValue(SelectedDateProperty, parsed);
        }
        finally
        {
            _updatingFromState = false;
        }

        _isEditing = false;
        SyncTextFromState();
        return true;
    }

    /// <summary>Discards the draft and restores the date present when editing began.</summary>
    public void CancelEdit()
    {
        if (_isEditing)
        {
            _updatingFromState = true;
            try
            {
                _state.SelectedDate = _editOriginal;
                SetCurrentValue(SelectedDateProperty, _editOriginal);
            }
            finally
            {
                _updatingFromState = false;
            }
        }

        _isEditing = false;
        SetValidation(true, null);
        SyncTextFromState();
    }

    /// <summary>Replaces external state with a fresh, control-owned default.</summary>
    public void ResetState() => SetState(null);

    protected override void OnEditorFocusChanged(bool isFocused)
    {
        base.OnEditorFocusChanged(isFocused);

        if (isFocused)
            BeginEdit();
        else
        {
            CommitEdit();
            _isEditing = false;
        }
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

        if (change.Property == SelectedDateProperty && !_updatingFromState)
        {
            _state.SelectedDate = SelectedDate;
            if (!_isEditing)
                SyncTextFromState();
        }
        else if (change.Property == TextProperty && !_syncingText)
        {
            if (Editor?.IsFocused == true && !_isEditing)
                BeginEdit();

            if (TryParseDraft(out _, out _))
                SetValidation(true, null);
            else
                SetValidation(false, null);
        }
        else if (change.Property == CultureProperty || change.Property == DateFormatProperty)
        {
            if (!_isEditing)
                SyncTextFromState();
        }
        else if (change.Property == MinimumDateProperty || change.Property == MaximumDateProperty)
        {
            if (MinimumDate.Date > MaximumDate.Date)
                throw new InvalidOperationException("MinimumDate must not be after MaximumDate.");

            if (!_isEditing)
                SyncTextFromState();
        }
    }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    private string EffectiveFormat =>
        string.IsNullOrWhiteSpace(DateFormat)
            ? EffectiveCulture.DateTimeFormat.ShortDatePattern
            : DateFormat!;

    private void BeginEdit()
    {
        if (_isEditing)
            return;

        _editOriginal = _state.SelectedDate;
        _isEditing = true;
    }

    private bool TryParseDraft(out DateTime? parsed, out string? error)
    {
        var draft = Text?.Trim() ?? string.Empty;
        if (draft.Length == 0)
        {
            parsed = null;
            error = null;
            return true;
        }

        if (!DateTime.TryParseExact(
                draft,
                EffectiveFormat,
                EffectiveCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var date))
        {
            parsed = null;
            error = $"Enter a date in {EffectiveFormat} format.";
            return false;
        }

        date = date.Date;
        if (date < MinimumDate.Date || date > MaximumDate.Date)
        {
            parsed = null;
            error = string.Format(
                EffectiveCulture,
                "Date must be between {0:d} and {1:d}.",
                MinimumDate,
                MaximumDate);
            return false;
        }

        parsed = date;
        error = null;
        return true;
    }

    private void SetValidation(bool valid, string? error)
    {
        var previousValid = _isInputValid;
        var previousError = _validationError;
        _isInputValid = valid;
        _validationError = error;

        SetCurrentValue(IsErrorProperty, error is not null);
        SetCurrentValue(SupportingTextProperty, error);

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
            SetCurrentValue(
                TextProperty,
                _state.SelectedDate?.ToString(EffectiveFormat, EffectiveCulture) ?? string.Empty);
            if (string.IsNullOrEmpty(Placeholder))
                SetCurrentValue(PlaceholderProperty, EffectiveFormat.ToUpper(EffectiveCulture));
        }
        finally
        {
            _syncingText = false;
        }

        SetValidation(true, null);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MaterialDatePickerState.SelectedDate))
            return;

        var changedByThisInput = _updatingFromState;
        _updatingFromState = true;
        try
        {
            SetCurrentValue(SelectedDateProperty, _state.SelectedDate);
        }
        finally
        {
            _updatingFromState = false;
        }

        if (!changedByThisInput)
            _isEditing = false;
        if (!_isEditing)
            SyncTextFromState();
    }

    private void SetState(MaterialDatePickerState? value)
    {
        var next = value ?? new MaterialDatePickerState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        _isEditing = false;

        _updatingFromState = true;
        try
        {
            SetCurrentValue(SelectedDateProperty, _state.SelectedDate);
        }
        finally
        {
            _updatingFromState = false;
        }

        SyncTextFromState();
    }
}
