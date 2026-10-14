// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;

namespace Material3Expressive.Components;

internal sealed class TimeEntryController(TemplatedControl owner, TimeEntryKind kind)
{
    private Panel? _body;
    private readonly Control?[] _fields = new Control?[2];
    private readonly string[] _text = new string[2];
    private readonly bool[] _valid = [true, true];
    private readonly Button?[] _periods = new Button?[2];
    private ClockFace? _clock;
    private TopLevel? _window;
    private bool _horizontal, _updating, _input, _watchingWindow;
    internal TimeOnly Value => owner.GetValue(TimePicker.SelectedTimeProperty);
    internal bool Is24Hour => owner.GetValue(TimePicker.Is24HourProperty);
    internal bool IsPm => Value.Hour >= 12;
    internal TimeSelection Selection => owner.GetValue(TimePicker.SelectionProperty);

    private bool Vibrant =>
        kind == TimeEntryKind.Scroll || owner.GetValue(TimePicker.VariantProperty) == TimePickerVariant.Vibrant;

    private int HourForDisplay => Is24Hour ? Value.Hour : Value.Hour % 12 == 0 ? 12 : Value.Hour % 12;

    internal void Attach(INameScope scope)
    {
        _body = scope.Get<Panel>("PART_Body");
        if (!_watchingWindow)
        {
            _watchingWindow = true;
            owner.AttachedToVisualTree += (_, _) => WatchWindow();
            owner.DetachedFromVisualTree += (_, _) => WatchWindow();
        }

        WatchWindow();
        Build();
    }

    private void WatchWindow()
    {
        var window = TopLevel.GetTopLevel(owner);
        if (window == _window) return;
        if (_window is { }) _window.PropertyChanged -= WindowChanged;
        _window = window;
        if (_window is { }) _window.PropertyChanged += WindowChanged;
    }

    private void WindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TopLevel.ClientSizeProperty
         || owner.GetValue(TimePicker.LayoutProperty) != TimePickerLayout.Automatic) return;
        if (UsesHorizontalLayout() == _horizontal) return;
        Build();
    }

    private bool UsesHorizontalLayout()
    {
        TimePickerLayout layout = owner.GetValue(TimePicker.LayoutProperty);
        return kind == TimeEntryKind.Dial && (layout == TimePickerLayout.Horizontal
         || (layout == TimePickerLayout.Automatic && _window is { } window
             && window.ClientSize.Width > window.ClientSize.Height));
    }

    internal void Changed(AvaloniaProperty property)
    {
        if (_body is null || _updating) return;

        if (property == TimePicker.SelectedTimeProperty)
            Update(!_input);
        else if (property == TimePicker.SelectionProperty)
        {
            Update();
            if (kind == TimeEntryKind.Input && _input) _fields[(int)Selection]?.Focus();
        }
        else if (property == TimePicker.VariantProperty
              || property == TimePicker.LayoutProperty
              || property == TimePicker.Is24HourProperty
              || property == TimePicker.ToggleProperty
              || property == TimePicker.ToggleTemplateProperty) Build();
    }

    internal void Select(TimeSelection selection) => owner.SetCurrentValue(TimePicker.SelectionProperty, selection);
    internal void SetHour(int hour) => owner.SetCurrentValue(TimePicker.SelectedTimeProperty, new TimeOnly(hour, Value.Minute));

    internal void SetMinute(int minute) =>
        owner.SetCurrentValue(TimePicker.SelectedTimeProperty, new TimeOnly(Value.Hour, minute));

    private void Build()
    {
        if (_body is null) return;
        _updating = true;
        try
        {
            _body.Children.Clear();
            _clock = null;
            Array.Clear(_fields);
            Array.Clear(_periods);
            _horizontal = UsesHorizontalLayout();

            var main = new StackPanel {
                Orientation = _horizontal
                    ? Orientation.Horizontal
                    : Orientation.Vertical,

                HorizontalAlignment = HorizontalAlignment.Center,

                Spacing = kind == TimeEntryKind.Dial
                    ? Vibrant && _horizontal
                        ? 52
                        : 36
                    : 0,

                Margin = Vibrant
                    ? new Thickness(_horizontal ? 24 : 12)
                    : default
            };

            var display = new StackPanel {
                Orientation = _horizontal
                    ? Orientation.Vertical
                    : Orientation.Horizontal,

                Spacing = _horizontal
                    ? 16
                    : Vibrant
                        ? 16
                        : 4,

                VerticalAlignment = VerticalAlignment.Center
            };
            var numbers = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };
            bool hasToggle = owner.GetValue(TimePicker.ToggleProperty) is { };
            int fieldHeight = Vibrant ? hasToggle ? 140 : 120 : kind == TimeEntryKind.Dial ? 80 : 72;
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var column = new StackPanel();

                int width = kind == TimeEntryKind.Scroll
                    ? 100
                    : Vibrant
                        ? Is24Hour && kind == TimeEntryKind.Dial && !_horizontal
                            ? 132
                            : 100
                        : kind == TimeEntryKind.Dial && Is24Hour && !_horizontal ? 114 : 96;

                switch (kind)
                {
                    case TimeEntryKind.Input:
                    {
                        // Fill the editor viewport and center the text within it. Centering a narrow
                        // presenter instead loses its layout offset to SmoothScroll's composition translation.
                        var field = new TextBox {
                            Name = i == 0 ? "PART_HourInput" : "PART_MinuteInput", Width = width, Height = fieldHeight, MinWidth = 0,
                            Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, MaxLength = 3
                        };
                        field.Classes.Add("time-field");
                        field.Classes.Set("vibrant", Vibrant);
                        field.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimeInputFieldTheme"));
                        field.GotFocus += (_, _) => {
                            Select((TimeSelection)index);
                            field.SelectAll();
                        };
                        field.TextChanged += (_, _) => InputChanged(field, index);
                        field.KeyDown += (_, e) => {
                            if (e.Key == Key.Enter)
                            {
                                if (index == 0)
                                {
                                    Select(TimeSelection.Minute);
                                    _fields[1]?.Focus();
                                }

                                e.Handled = true;
                            }

                            if (index == 1 && e.Key == Key.Back && field.CaretIndex == 0)
                            {
                                Select(TimeSelection.Hour);
                                _fields[0]?.Focus();
                                e.Handled = true;
                            }
                        };
                        AutomationProperties.SetName(field, DateTimeStrings.Get(i == 0 ? "TimePickerHour" : "TimePickerMinute"));
                        _fields[i] = field;
                        column.Children.Add(field);
                        break;
                    }
                    case TimeEntryKind.Scroll:
                    {
                        var field = new DateTimePickerPanel {
                            Name = i == 0 ? "PART_HourScroll" : "PART_MinuteScroll", PanelType = DateTimePickerPanelType.Minute,
                            MinimumValue = i == 0 && !Is24Hour ? 1 : 0, MaximumValue = i == 0 ? Is24Hour ? 23 : 12 : 59,
                            ShouldLoop = true, ItemHeight = 40, ItemFormat = "mm"
                        };
                        field.Classes.Add("time-scroll");
                        field.SelectionChanged += (_, _) => {
                            if (_updating) return;
                            if (index == 0) SetHour(Is24Hour ? field.SelectedValue : field.SelectedValue % 12 + (IsPm ? 12 : 0));
                            else SetMinute(field.SelectedValue);
                        };
                        var scroll = new ScrollViewer {
                            Content = field, Width = width, Height = fieldHeight,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
                        };
                        var surface = new MaterialSurface { Child = scroll, CornerRadius = new CornerRadius(16) };
                        surface.Classes.Add("time-scroll-surface");
                        _fields[i] = field;
                        column.Children.Add(surface);
                        AutomationProperties.SetName(field,
                            DateTimeStrings.Get(i == 0 ? "TimePickerHourSelection" : "TimePickerMinuteSelection"));
                        break;
                    }
                    case TimeEntryKind.Dial:
                    default:
                    {
                        var button = new Button {
                            Name = i == 0 ? "PART_HourSelector" : "PART_MinuteSelector", Width = width, Height = fieldHeight
                        };
                        button.Classes.Set("vibrant", Vibrant);
                        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimeSelectorTheme"));
                        button.Click += (_, _) => Select((TimeSelection)index);
                        AutomationProperties.SetName(button,
                            DateTimeStrings.Get(i == 0 ? "TimePickerHourSelection" : "TimePickerMinuteSelection"));
                        _fields[i] = button;
                        column.Children.Add(button);
                        break;
                    }
                }

                if (kind == TimeEntryKind.Input)
                {
                    var label = new TextBlock {
                        Text = DateTimeStrings.Get(i == 0 ? "TimePickerHour" : "TimePickerMinute"),
                        Margin = new Thickness(0, 7, 0, 0), HorizontalAlignment = HorizontalAlignment.Center
                    };
                    label.Classes.Add("time-support");
                    column.Children.Add(label);
                }

                numbers.Children.Add(column);
                if (i == 0)
                {
                    var separator = new TextBlock {
                        Text = ":", Width = Vibrant ? 16 : 24, Height = fieldHeight, TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(0, (fieldHeight - 64) / 2, 0, 0)
                    };
                    separator.Classes.Add("time-separator");
                    numbers.Children.Add(separator);
                }
            }

            display.Children.Add(numbers);
            if (!Is24Hour)
            {
                var periods = new StackPanel {
                    Orientation = _horizontal ? Orientation.Horizontal : Orientation.Vertical,
                    Width = _horizontal ? 216 : Vibrant ? 56 : 52, Height = _horizontal ? Vibrant ? 48 : 38 : fieldHeight,
                    Spacing = 4
                };
                for (int i = 0; i < 2; i++)
                {
                    int period = i;
                    var button = new Button {
                        Content = DateTimeStrings.Get(i == 0 ? "TimePickerAM" : "TimePickerPM"),
                        Height = _horizontal ? periods.Height : (periods.Height - 4) / 2,
                        Width = _horizontal ? (periods.Width - 4) / 2 : periods.Width, Padding = new Thickness(0), MinWidth = 0
                    };
                    button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimePeriodTheme"));
                    button.Click += (_, _) => SetHour(Value.Hour % 12 + period * 12);
                    _periods[i] = button;
                    periods.Children.Add(button);
                }

                display.Children.Add(periods);
            }

            if (hasToggle)
                display.Children.Add(new ContentPresenter {
                    Content = owner.GetValue(TimePicker.ToggleProperty),
                    ContentTemplate = owner.GetValue(TimePicker.ToggleTemplateProperty)
                });
            main.Children.Add(display);
            if (kind == TimeEntryKind.Dial)
            {
                _clock = new ClockFace(this) {
                    Width = 256, Height = 256, Margin = _horizontal ? default : new Thickness(0, 0, 0, 24)
                };
                _clock.Classes.Set("vibrant", Vibrant);
                main.Children.Add(_clock);
            }

            _body.Children.Add(main);
        }
        finally
        {
            _updating = false;
        }

        Update();
    }

    private void Update(bool syncInputs = false)
    {
        if (_updating) return;
        _updating = true;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                int n = i == 0 ? HourForDisplay : Value.Minute;
                switch (_fields[i])
                {
                    case TextBox box when !_input:
                        if (syncInputs || _text[i] is null)
                        {
                            _text[i] = n.ToString("00", CultureInfo.CurrentCulture);
                            _valid[i] = true;
                        }

                        box.Text = _text[i];
                        MaterialTextField.SetIsError(box, !_valid[i]);
                        break;
                    case Button button:
                        button.Content = n.ToString("00", CultureInfo.CurrentCulture);
                        button.Classes.Set("selected", (int)Selection == i);
                        break;
                    case DateTimePickerPanel panel: panel.SelectedValue = n; break;
                }
            }

            for (int i = 0; i < 2; i++) _periods[i]?.Classes.Set("selected", IsPm == (i == 1));
            _clock?.Update();
        }
        finally
        {
            _updating = false;
        }

        SetValidity();
    }

    private void InputChanged(TextBox field, int index)
    {
        if (_updating) return;
        string text = field.Text ?? "";
        string old = _text[index] ?? "";
        if (text == old) return;
        string candidate = text;
        if (text.Length == 3 && field.CaretIndex == 1) candidate = text[..1];
        if (candidate.Length > 2 || candidate.Any(c => !Char.IsDigit(c))
         || (candidate.Length > 0 && !Int32.TryParse(candidate, out _)))
        {
            _updating = true;
            field.Text = old;
            _updating = false;
            return;
        }

        int number = candidate.Length == 0 ? 0 : Int32.Parse(candidate, CultureInfo.CurrentCulture);
        _text[index] = candidate;
        _input = true;
        try
        {
            if (index == 0)
            {
                int hour = number == 12 && IsPm ? 12 : number == 12 && !Is24Hour ? 0 : number + (!Is24Hour && IsPm ? 12 : 0);
                _valid[0] = Is24Hour ? hour is >= 0 and <= 23 : IsPm ? hour is >= 12 and <= 23 : hour is >= 0 and <= 11;
                if (_valid[0]) SetHour(hour);
                if (_valid[0] && (candidate.Length == 2
                     || (!Is24Hour && number is >= 2 and <= 9 && candidate.Length >= old.Length)))
                {
                    Select(TimeSelection.Minute);
                    _fields[1]?.Focus();
                }
            }
            else
            {
                _valid[1] = number <= 59;
                if (_valid[1]) SetMinute(number);
            }

            MaterialTextField.SetIsError(field, !_valid[index]);
            MaterialTextField.SetErrorMessage(field,
                _valid[index] ? "" : DateTimeStrings.Get(index == 0 ? "TimePickerHourError" : "TimePickerMinuteError"));
            SetValidity();
        }
        finally
        {
            _input = false;
        }
    }

    private void SetValidity()
    {
        bool valid = _valid[0] && _valid[1];
        foreach (Button? period in _periods)
            if (period is { })
                period.IsEnabled = _valid[0];
        switch (owner)
        {
            case TimeInput c: c.SetInputValid(valid); break;
            case TimePicker c: c.SetInputValid(valid); break;
            case TimeScroll c: c.SetInputValid(valid); break;
        }
    }
}
