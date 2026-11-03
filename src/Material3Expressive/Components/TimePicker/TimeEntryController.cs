// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;
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
    private readonly TimePeriodButton?[] _periods = new TimePeriodButton?[2];
    private ClockFace? _clock;
    private TopLevel? _window;
    private bool _horizontal, _portrait, _updating, _input, _watchingWindow;
    internal TimeOnly Value => owner.GetValue(TimePicker.SelectedTimeProperty);
    internal bool Is24Hour => owner.GetValue(TimePicker.Is24HourProperty);
    internal bool IsPm => Value.Hour >= 12;
    internal TimeSelection Selection => owner.GetValue(TimePicker.SelectionProperty);

    internal bool AutoSwitchToMinute => owner is TimePicker { TouchExplorationEnabled: false }
        && Material3Expressive.Foundation.Interaction.MaterialInputMode.For(owner)?.IsKeyboard != true;

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
        if (e.Property != TopLevel.ClientSizeProperty) return;
        if (UsesHorizontalLayout() != _horizontal
            || kind == TimeEntryKind.Dial && Vibrant && Is24Hour && UsesPortraitWidth() != _portrait)
            Build();
    }

    private bool UsesPortraitWidth() => _window is null || _window.ClientSize.Width <= _window.ClientSize.Height;

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

    internal void LeaveDial(bool backwards)
    {
        if (backwards) _fields[(int)Selection]?.Focus(NavigationMethod.Tab);
        else if (Selection == TimeSelection.Hour) _fields[1]?.Focus(NavigationMethod.Tab);
        else if (!Is24Hour) _periods[0]?.Focus(NavigationMethod.Tab);
        else
        {
            _fields[1]?.Focus(NavigationMethod.Tab);
            TopLevel.GetTopLevel(owner)?.FocusManager?.TryMoveFocus(NavigationDirection.Next);
        }
    }

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
            _portrait = UsesPortraitWidth();

            var main = new StackPanel {
                Orientation = _horizontal
                    ? Orientation.Horizontal
                    : Orientation.Vertical,

                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0,
                Margin = default
            };

            var display = new StackPanel {
                Orientation = _horizontal
                    ? Orientation.Vertical
                    : Orientation.Horizontal,

                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var numbers = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };
            bool hasToggle = owner.GetValue(TimePicker.ToggleProperty) is { };
            MaterialTokenKey<double> fieldHeight = kind switch {
                TimeEntryKind.Scroll => hasToggle ? Comp.TimeScroll.TimeFieldWithToggleContainerHeight : Comp.TimeScroll.TimeFieldContainerHeight,
                TimeEntryKind.Input => Vibrant
                    ? hasToggle ? Comp.TimeInput.VibrantTimeFieldWithToggleContainerHeight : Comp.TimeInput.VibrantTimeFieldContainerHeight
                    : Comp.TimeInput.TimeFieldContainerHeight,
                _ => Vibrant
                    ? hasToggle ? Comp.TimePicker.VibrantTimeSelectorWithToggleContainerHeight : Comp.TimePicker.VibrantTimeSelectorContainerHeight
                    : Comp.TimePicker.TimeSelectorContainerHeight
            };
            MaterialTokenKey<double> width = kind switch {
                TimeEntryKind.Scroll => Comp.TimeScroll.TimeFieldContainerWidth,
                TimeEntryKind.Input => Vibrant ? Comp.TimeInput.VibrantTimeFieldContainerWidth : Comp.TimeInput.TimeFieldContainerWidth,
                _ => Vibrant
                    ? Is24Hour && _portrait ? Comp.TimePicker.VibrantTimeSelector24HVerticalContainerWidth : Comp.TimePicker.VibrantTimeSelectorContainerWidth
                    : Comp.TimePicker.TimeSelectorContainerWidth
            };
            if (kind == TimeEntryKind.Dial)
                BindDimension(main, StackPanel.SpacingProperty, Vibrant && _horizontal ? Comp.TimePicker.VibrantHorizontalLayoutSpacing : Comp.TimePicker.LayoutSpacing);
            if (Vibrant)
                BindDimension(main, Control.MarginProperty, x => new Thickness(x[0]), kind switch {
                    TimeEntryKind.Scroll => Comp.TimeScroll.Margin,
                    TimeEntryKind.Input => Comp.TimeInput.VibrantMargin,
                    _ => _horizontal ? Comp.TimePicker.VibrantHorizontalMargin : Comp.TimePicker.VibrantVerticalMargin
                });
            BindDimension(display, StackPanel.SpacingProperty, kind switch {
                TimeEntryKind.Scroll => Comp.TimeScroll.DisplaySpacing,
                TimeEntryKind.Input => Vibrant ? Comp.TimeInput.VibrantDisplaySpacing : Comp.TimeInput.DisplaySpacing,
                _ => _horizontal ? Comp.TimePicker.HorizontalDisplaySpacing : Vibrant ? Comp.TimePicker.VibrantDisplaySpacing : Comp.TimePicker.DisplaySpacing
            });
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var column = new StackPanel();
                switch (kind)
                {
                    case TimeEntryKind.Input:
                    {
                        // Fill the editor viewport and center the text within it. Centering a narrow
                        // presenter instead loses its layout offset to SmoothScroll's composition translation.
                        var field = new TextBox {
                            Name = i == 0 ? "PART_HourInput" : "PART_MinuteInput", MinWidth = 0,
                            Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, MaxLength = 3
                        };
                        BindDimension(field, Control.WidthProperty, width);
                        BindDimension(field, Control.HeightProperty, fieldHeight);
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
                            ShouldLoop = true, ItemFormat = "mm"
                        };
                        BindDimension(field, DateTimePickerPanel.ItemHeightProperty, Comp.TimeScroll.ItemHeight);
                        field.Classes.Add("time-scroll");
                        field.SelectionChanged += (_, _) => {
                            if (_updating) return;
                            if (index == 0) SetHour(Is24Hour ? field.SelectedValue : field.SelectedValue % 12 + (IsPm ? 12 : 0));
                            else SetMinute(field.SelectedValue);
                        };
                        var scroll = new ScrollViewer {
                            Content = field,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
                        };
                        BindDimension(scroll, Control.WidthProperty, width);
                        BindDimension(scroll, Control.HeightProperty, fieldHeight);
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
                            Name = i == 0 ? "PART_HourSelector" : "PART_MinuteSelector"
                        };
                        BindDimension(button, Control.WidthProperty, width);
                        BindDimension(button, Control.HeightProperty, fieldHeight);
                        button.Classes.Set("vibrant", Vibrant);
                        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimeSelectorTheme"));
                        var touchActivation = false;
                        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => touchActivation = e.Pointer.Type == PointerType.Touch,
                            Avalonia.Interactivity.RoutingStrategies.Tunnel);
                        button.AddHandler(InputElement.KeyDownEvent, (_, _) => touchActivation = false,
                            Avalonia.Interactivity.RoutingStrategies.Tunnel);
                        button.GotFocus += (_, _) => _clock?.SetDialFocusable(false);
                        button.Click += (_, _) =>
                        {
                            if (touchActivation) _clock?.SetDialFocusable(false);
                            Select((TimeSelection)index);
                            // ClockText's canFocus excludes InputMode.Touch.
                            if (!touchActivation) _clock?.Activate();
                        };
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
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    BindDimension(label, Control.MarginProperty, x => new Thickness(0, x[0], 0, 0), Comp.TimeInput.SupportingTextTopSpacing);
                    label.Classes.Add("time-support");
                    column.Children.Add(label);
                }

                numbers.Children.Add(column);
                if (i == 0)
                {
                    var separator = new TextBlock {
                        Text = ":", TextAlignment = TextAlignment.Center,
                        RenderTransform = new TranslateTransform(0, -4), // DisplaySeparator Modifier.offset(y = -4.dp).
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    BindDimension(separator, Control.WidthProperty, kind switch {
                        TimeEntryKind.Scroll => Comp.TimeScroll.SeparatorWidth,
                        TimeEntryKind.Input => Vibrant ? Comp.TimeInput.VibrantSeparatorWidth : Comp.TimeInput.SeparatorWidth,
                        _ => Vibrant ? Comp.TimePicker.VibrantSeparatorWidth : Comp.TimePicker.SeparatorWidth
                    });
                    BindDimension(separator, Control.HeightProperty, fieldHeight);
                    // Preserve the original integer division for the baseline and odd heights.
                    BindDimension(separator, TextBlock.PaddingProperty, x => new Thickness(0, Math.Truncate((x[0] - 64) / 2), 0, 0), fieldHeight);
                    separator.Classes.Add("time-separator");
                    numbers.Children.Add(separator);
                }
            }

            display.Children.Add(numbers);
            if (!Is24Hour)
            {
                var periods = new StackPanel {
                    Orientation = _horizontal ? Orientation.Horizontal : Orientation.Vertical,
                    FlowDirection = FlowDirection.LeftToRight // HorizontalPeriodToggle uses absolute place(), including RTL.
                };
                MaterialTokenKey<double> periodWidth = _horizontal ? Comp.TimePicker.PeriodSelectorHorizontalContainerWidth : kind switch {
                    TimeEntryKind.Scroll => Comp.TimeScroll.PeriodSelectorContainerWidth,
                    TimeEntryKind.Input => Vibrant ? Comp.TimeInput.VibrantPeriodSelectorContainerWidth : Comp.TimeInput.PeriodSelectorContainerWidth,
                    _ => Vibrant ? Comp.TimePicker.VibrantPeriodSelectorVerticalContainerWidth : Comp.TimePicker.PeriodSelectorVerticalContainerWidth
                };
                MaterialTokenKey<double> periodHeight = _horizontal
                    ? Vibrant ? Comp.TimePicker.VibrantPeriodSelectorHorizontalContainerHeight : Comp.TimePicker.PeriodSelectorHorizontalContainerHeight
                    : fieldHeight;
                MaterialTokenKey<double> periodSpacing = kind switch {
                    TimeEntryKind.Scroll => Comp.TimeScroll.PeriodSelectorSpacing,
                    TimeEntryKind.Input => Comp.TimeInput.PeriodSelectorSpacing,
                    _ => Vibrant ? _horizontal ? Comp.TimePicker.VibrantPeriodSelectorHorizontalSpacing : Comp.TimePicker.VibrantPeriodSelectorVerticalSpacing : Comp.TimePicker.PeriodSelectorSpacing
                };
                BindDimension(periods, Control.WidthProperty, periodWidth);
                BindDimension(periods, Control.HeightProperty, periodHeight);
                BindDimension(periods, StackPanel.SpacingProperty, periodSpacing);
                for (int i = 0; i < 2; i++)
                {
                    int period = i;
                    var button = new TimePeriodButton {
                        Content = DateTimeStrings.Get(i == 0 ? "TimePickerAM" : "TimePickerPM"),
                        Padding = new Thickness(0), MinWidth = 0
                    };
                    BindDimension(button, Control.HeightProperty, x => _horizontal ? x[0] : (x[0] - x[1]) / 2, periodHeight, periodSpacing);
                    BindDimension(button, Control.WidthProperty, x => _horizontal ? (x[0] - x[1]) / 2 : x[0], periodWidth, periodSpacing);
                    button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimePeriodTheme"));
                    button.Click += (_, _) => SetHour(Value.Hour % 12 + period * 12);
                    if (kind == TimeEntryKind.Dial) button.GotFocus += (_, _) => _clock?.SetDialFocusable(false);
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
                _clock = new ClockFace(this) { Margin = default };
                BindDimension(_clock, Control.WidthProperty, Comp.TimePicker.ClockDialContainerSize);
                BindDimension(_clock, Control.HeightProperty, Comp.TimePicker.ClockDialContainerSize);
                if (!_horizontal)
                    BindDimension(_clock, Control.MarginProperty, x => new Thickness(0, 0, 0, x[0]), Comp.TimePicker.ClockDialBottomSpacing);
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

    internal static void BindDimension(Control target, AvaloniaProperty property, MaterialTokenKey<double> key) =>
        BindDimension(target, property, x => x[0], key);

    internal static void BindDimension(Control target, AvaloniaProperty property,
        Func<double[], object> project, params MaterialTokenKey<double>[] keys)
    {
        var binding = new MultiBinding {
            Priority = BindingPriority.LocalValue,
            Converter = new DimensionConverter(keys, project)
        };
        binding.Bindings.Add(new Binding { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
        foreach (var key in keys) binding.Bindings.Add(new DynamicResourceExtension(key));
        target.Bind(property, binding);
    }

    private sealed class DimensionConverter(MaterialTokenKey<double>[] keys, Func<double[], object> project) : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var resolved = new double[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var value = new TokenBindingConverter(keys[i], null, null).Convert(values, typeof(double), null, culture);
                if (value is not double number) return AvaloniaProperty.UnsetValue;
                resolved[i] = number;
            }
            return project(resolved);
        }
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

            for (int i = 0; i < 2; i++)
                if (_periods[i] is { } period)
                {
                    period.SetCurrentValue(ToggleButton.IsCheckedProperty, IsPm == (i == 1));
                    period.Classes.Set("selected", IsPm == (i == 1));
                    period.ZIndex = IsPm == (i == 1) ? 0 : 1;
                }
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
