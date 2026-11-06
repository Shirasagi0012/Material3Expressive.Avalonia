// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Globalization;
using Avalonia.Data;
using Avalonia.Controls.Templates;
using ComposeAnimation;
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
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed class TimeEntryController(TemplatedControl owner, TimeEntryKind kind)
{
    private Panel? _body;
    private readonly Control?[] _fields = new Control?[2];
    private readonly TimeSelectorButton?[] _inputSelectors = new TimeSelectorButton?[2];
    private readonly TextBlock?[] _supportingLabels = new TextBlock?[2];
    private readonly string[] _text = new string[2];
    private readonly int[] _inputValues = new int[2];
    private readonly (int Start, int End)[] _inputSelections = new (int, int)[2];
    private readonly bool[] _valid = [true, true];
    private readonly TimePeriodButton?[] _periods = new TimePeriodButton?[2];
    private ClockFace? _clock;
    private TopLevel? _window;
    private bool _horizontal, _portrait, _updating, _input, _watchingWindow;
    private TimeOnly? _lastValue;
    private readonly IDisposable?[] _scrollToTime = new IDisposable?[2];
    private readonly IDisposable?[] _timeToScroll = new IDisposable?[2];
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
            owner.AttachedToVisualTree += (_, _) => {
                WatchWindow();
                for (var i = 0; i < 2; i++) QueueTimeToScroll(i);
            };
            owner.DetachedFromVisualTree += (_, _) => { CancelScrollEffects(); WatchWindow(); };
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
            if (kind == TimeEntryKind.Input) FocusSelectedInput();
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
            CancelScrollEffects();
            _lastValue = null;
            _body.Children.Clear();
            _clock = null;
            Array.Clear(_fields);
            Array.Clear(_inputSelectors);
            Array.Clear(_supportingLabels);
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
            // TimeScrollImpl's Row keeps the outer direction; its fields override only
            // their contents to LTR. Do not force the row's child order to LTR as well.
            if (kind == TimeEntryKind.Scroll) numbers[!Visual.FlowDirectionProperty] = owner[!Visual.FlowDirectionProperty];
            bool hasToggle = owner.GetValue(TimePicker.ToggleProperty) is { }
                || owner.GetValue(TimePicker.ToggleTemplateProperty) is { };
            bool sideControls = hasToggle && Vibrant && kind != TimeEntryKind.Dial;
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
                            VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center
                        };
                        BindDimension(field, Control.WidthProperty, width);
                        BindDimension(field, Control.HeightProperty, fieldHeight);
                        field.Classes.Add("time-field");
                        field.Classes.Set("vibrant", Vibrant);
                        field.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimeInputFieldTheme"));
                        field.Bind(TextBox.CaretBrushProperty, new MultiBinding {
                            Bindings = { new Binding { Source = field }, new DynamicResourceExtension(Sys.Color.Primary) },
                            Converter = new TimeInputCaretBrushConverter()
                        });
                        field.GotFocus += (_, _) => {
                            Select((TimeSelection)index);
                        };
                        field.TextChanged += (_, _) => InputChanged(field, index);
                        field.PropertyChanged += (_, e) => {
                            // Selection-only changes are accepted by timeInputOnChange too.
                            // Keep the previous range until a replacement is accepted.
                            if (!_updating && field.Text == _text[index]
                                && (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty))
                                _inputSelections[index] = (field.SelectionStart, field.SelectionEnd);
                        };
                        field.KeyDown += (_, e) => {
                            // shouldSwitchFocusToMinute: a further digit at the end of a
                            // complete, valid hour belongs to the minute editor.
                            if (index == 0 && _valid[0] && field.SelectionStart == 2 && field.Text?.Length == 2
                                && e.KeySymbol is { Length: 1 } symbol && symbol[0] is >= '0' and <= '9'
                                && TryParseInputNumber(field.Text, out var hour)
                                && (Is24Hour ? hour is >= 0 and <= 23 : hour is >= 1 and <= 12))
                            {
                                Select(TimeSelection.Minute);
                                _fields[1]?.Focus();
                            }
                            if (e.Key == Key.Enter)
                            {
                                if (index == 0 && _valid[0])
                                {
                                    Select(TimeSelection.Minute);
                                    _fields[1]?.Focus();
                                }

                                e.Handled = true;
                            }

                        };
                        AutomationProperties.SetName(field, DateTimeStrings.Get(i == 0 ? "TimePickerHourTextField" : "TimePickerMinuteTextField"));
                        _fields[i] = field;
                        // TimePickerTextField retains its editor but only exposes it for the
                        // selected field. The other field is a clickable TimeSelector.
                        var selector = new TimeSelectorButton { Name = i == 0 ? "PART_HourInputSelector" : "PART_MinuteInputSelector" };
                        BindDimension(selector, Control.WidthProperty, width);
                        BindDimension(selector, Control.HeightProperty, fieldHeight);
                        selector.Classes.Set("vibrant", Vibrant);
                        selector.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimeInputSelectorTheme"));
                        AutomationProperties.SetName(selector, DateTimeStrings.Get(i == 0 ? "TimePickerHourSelection" : "TimePickerMinuteSelection"));
                        selector.Click += (_, _) => Select((TimeSelection)index);
                        _inputSelectors[i] = selector;
                        column.Children.Add(new Grid { Children = { field, selector } });
                        break;
                    }
                    case TimeEntryKind.Scroll:
                    {
                        var clock24 = Is24Hour;
                        var offset = i == 0 && !clock24 ? 1 : 0;
                        var state = new ScrollFieldState(i == 0 ? clock24 ? 24 : 12 : 60,
                            i == 0 ? HourForDisplay - offset : Value.Minute);
                        var field = new ScrollField {
                            Name = i == 0 ? "PART_HourScroll" : "PART_MinuteScroll",
                            State = state, FlowDirection = FlowDirection.LeftToRight,
                            OptionDescription = option => DateTimeStrings.Get(index == 0
                                ? clock24 ? "TimePicker24HourSuffix" : "TimePickerHourSuffix"
                                : "TimePickerMinuteSuffix", null, option + offset)
                        };
                        // Twelve-hour options are 0..11, while their visible labels are 1..12.
                        if (offset != 0)
                            field.ItemTemplate = new FuncDataTemplate<ScrollFieldItem>((item, _) => new ScrollFieldNumber {
                                Index = item!.Index + offset,
                                [!ScrollFieldNumber.IsSelectedProperty] = item[!ScrollFieldItem.IsSelectedProperty],
                                [!ScrollFieldNumber.SelectedContentColorProperty] = field[!ScrollField.SelectedContentColorProperty],
                                [!ScrollFieldNumber.UnselectedContentColorProperty] = field[!ScrollField.UnselectedContentColorProperty]
                            });
                        BindDimension(field, ScrollField.ItemHeightProperty, Comp.TimeScroll.ItemHeight);
                        BindDimension(field, Control.WidthProperty, width);
                        BindDimension(field, Control.HeightProperty, fieldHeight);
                        field.Classes.Add("time-scroll");
                        AutomationProperties.SetName(field,
                            DateTimeStrings.Get(i == 0 ? "TimePickerHourSelection" : "TimePickerMinuteSelection"));
                        state.PropertyChanged += (_, change) => {
                            if (change.Property != ScrollFieldState.SelectedOptionProperty
                                || _fields[index] != field || !owner.IsAttachedToVisualTree()) return;
                            var selected = index == 0 ? clock24 ? state.SelectedOption
                                : (state.SelectedOption + 1) % 12 + (IsPm ? 12 : 0) : state.SelectedOption;
                            // A programmatic scroll to the current time has no write to perform.
                            // Do not retain an obsolete echo until the following frame.
                            if (selected == (index == 0 ? Value.Hour : Value.Minute)) return;
                            _scrollToTime[index]?.Dispose();
                            // LaunchedEffect(selectedOption): publish at the composition boundary,
                            // not while Pager is updating its page and offset together.
                            _scrollToTime[index] = AnimationScope.Get(owner).ScheduleFrameCommit(() => {
                                _scrollToTime[index] = null;
                                if (index == 0) SetHour(clock24 ? state.SelectedOption
                                    : (state.SelectedOption + 1) % 12 + (IsPm ? 12 : 0));
                                else SetMinute(state.SelectedOption);
                            });
                        };
                        _fields[i] = field;
                        column.Children.Add(field);
                        break;
                    }
                    case TimeEntryKind.Dial:
                    default:
                    {
                        var button = new TimeSelectorButton {
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
                    var label = new TimeSupportingText {
                        Text = DateTimeStrings.Get(i == 0 ? "TimePickerHour" : "TimePickerMinute"),
                        Name = i == 0 ? "PART_HourSupportingText" : "PART_MinuteSupportingText",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    BindDimension(label, Control.WidthProperty, width);
                    BindDimension(label, Control.MarginProperty, x => new Thickness(0, x[0], 0, 0), Comp.TimeInput.SupportingTextTopSpacing);
                    label.Classes.Add("time-support");
                    _supportingLabels[i] = label;
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
            if (sideControls)
            {
                var side = new TimeSideControlLayout { VerticalAlignment = VerticalAlignment.Top };
                BindDimension(side, Control.HeightProperty, fieldHeight);
                if (!Is24Hour)
                    for (int i = 0; i < 2; i++)
                    {
                        int period = i;
                        var button = new TimePeriodButton {
                            Content = DateTimeStrings.Get(i == 0 ? "TimePickerAM" : "TimePickerPM"),
                            MinWidth = 0
                        };
                        button.Classes.Add("side-control");
                        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTimePeriodTheme"));
                        button.Click += (_, _) => SetHour(Value.Hour % 12 + period * 12);
                        _periods[i] = button;
                        side.Children.Add(button);
                    }
                side.Children.Add(new ContentPresenter {
                    Content = owner.GetValue(TimePicker.ToggleProperty),
                    ContentTemplate = owner.GetValue(TimePicker.ToggleTemplateProperty),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                });
                display.Children.Add(side);
            }
            else if (!Is24Hour)
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
                    TimeEntryKind.Input => Vibrant ? Comp.TimeInput.VibrantPeriodSelectorSpacing : Comp.TimeInput.PeriodSelectorSpacing,
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

            if (hasToggle && !sideControls)
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
        if (kind == TimeEntryKind.Input) FocusSelectedInput();
    }

    private void FocusSelectedInput()
    {
        var field = _fields[(int)Selection];
        // LaunchedEffect(selection) runs once the selected editor is composed. The
        // corresponding native focus request must wait until it is in the visual tree.
        Dispatcher.UIThread.Post(() => {
            if (field == _fields[(int)Selection] && field is { IsEffectivelyVisible: true }
                && field.IsAttachedToVisualTree()) field.Focus();
        }, DispatcherPriority.Loaded);
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

    private sealed class TimeInputCaretBrushConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var value = new TokenBindingConverter(Sys.Color.Primary, null, null).Convert(values, typeof(Color), null, culture);
            if (value is not Color color) return AvaloniaProperty.UnsetValue;
            // TimePickerTextField uses primary even in the error state, with the
            // first and last 10% of the native caret painted transparently.
            return new LinearGradientBrush {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops {
                    new(Colors.Transparent, 0), new(Colors.Transparent, .1), new(color, .1),
                    new(color, .9), new(Colors.Transparent, .9), new(Colors.Transparent, 1)
                }
            };
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
                        var valueChanged = _lastValue is not { } previous
                            || (i == 0 ? previous.Hour != Value.Hour : previous.Minute != Value.Minute);
                        if (syncInputs && valueChanged || _text[i] is null)
                        {
                            _text[i] = n.ToString("00", CultureInfo.CurrentCulture);
                            _inputValues[i] = i == 0 ? Value.Hour : Value.Minute;
                            _inputSelections[i] = default;
                            _valid[i] = true;
                        }

                        box.Text = _text[i];
                        box.SelectionStart = Math.Min(_inputSelections[i].Start, box.Text?.Length ?? 0);
                        box.SelectionEnd = Math.Min(_inputSelections[i].End, box.Text?.Length ?? 0);
                        MaterialTextField.SetIsError(box, !_valid[i]);
                        break;
                    case TimeSelectorButton button:
                        button.Content = n.ToString("00", CultureInfo.CurrentCulture);
                        button.IsSelected = (int)Selection == i;
                        button.Classes.Set("selected", (int)Selection == i);
                        break;
                    case ScrollField:
                        if (_lastValue is not { } last || (i == 0 ? last.Hour != Value.Hour : last.Minute != Value.Minute))
                            QueueTimeToScroll(i);
                        break;
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
            _lastValue = Value;
        }
        finally
        {
            _updating = false;
        }

        SetValidity();
    }

    private void CancelScrollEffects()
    {
        for (var i = 0; i < 2; i++)
        {
            _scrollToTime[i]?.Dispose(); _scrollToTime[i] = null;
            _timeToScroll[i]?.Dispose(); _timeToScroll[i] = null;
        }
    }

    private void QueueTimeToScroll(int index)
    {
        if (_fields[index] is not ScrollField { State: { } state } || !owner.IsAttachedToVisualTree()) return;
        _timeToScroll[index]?.Dispose();
        _timeToScroll[index] = AnimationScope.Get(owner).ScheduleFrameCommit(() => {
            _timeToScroll[index] = null;
            var option = index == 0 ? Is24Hour ? Value.Hour : HourForDisplay - 1 : Value.Minute;
            // Keep TimeScrollImpl's asymmetry. Hour always requests a default-priority
            // scroll (rejected during a user drag); minute explicitly excludes scrolling.
            if (index == 0 || state.SelectedOption != option && !state.IsScrollInProgress)
                _ = state.ScrollToOptionAsync(option);
        });
    }

    private static bool TryParseInputNumber(string text, out int number)
    {
        // Kotlin/JVM digitOf accepts Unicode decimal digits; .NET Int32.TryParse
        // accepts ASCII digits only, even when the culture advertises native digits.
        var digits = new string(text.Select(c => {
            var digit = CharUnicodeInfo.GetDecimalDigitValue(c);
            return digit >= 0 ? (char)('0' + digit) : c;
        }).ToArray());
        return Int32.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
    }

    private void InputChanged(TextBox field, int index)
    {
        if (_updating) return;
        string text = field.Text ?? "";
        string old = _text[index] ?? "";
        if (text == old) return;
        // Preserve timeInputOnChange's overwrite-at-start branch before parsing.
        // The value is parsed before the text is reduced; leading zeros and a sign
        // are therefore not interchangeable with a blanket two-character limit.
        string parsedText = text.Length == 3 && field.SelectionStart == 1 ? text[..1] : text;
        int number = 0;
        if (parsedText.Length > 0 && (!TryParseInputNumber(parsedText, out number) || number > 99))
        {
            var selection = _inputSelections[index];
            _updating = true;
            try { field.Text = old; field.SelectionStart = selection.Start; field.SelectionEnd = selection.End; }
            finally { _updating = false; }
            return;
        }

        _text[index] = text.Length <= 2 ? text : text[..1];
        _input = true;
        try
        {
            if (index == 0)
            {
                int hour = number == 12 && IsPm ? 12 : number == 12 && !Is24Hour ? 0 : number + (!Is24Hour && IsPm ? 12 : 0);
                _inputValues[0] = hour;
                _valid[0] = Is24Hour ? hour is >= 0 and <= 23 : IsPm ? hour is >= 12 and <= 23 : hour is >= 0 and <= 11;
                if (_valid[0]) SetHour(hour);
                if (_valid[0] && owner is not TimeInput { TouchExplorationEnabled: true } && (text.Length == 2
                     || (!Is24Hour && number is >= 2 and <= 9 && text.Length >= old.Length)))
                {
                    Select(TimeSelection.Minute);
                    _fields[1]?.Focus();
                }
            }
            else
            {
                _inputValues[1] = number;
                _valid[1] = number is >= 0 and <= 59;
                if (_valid[1]) SetMinute(number);
            }

            SetValidity();
            if (field.Text != _text[index])
            {
                var start = Math.Min(field.SelectionStart, _text[index].Length);
                var end = Math.Min(field.SelectionEnd, _text[index].Length);
                _updating = true;
                try { field.Text = _text[index]; field.SelectionStart = start; field.SelectionEnd = end; }
                finally { _updating = false; }
            }
            _inputSelections[index] = (field.SelectionStart, field.SelectionEnd);
        }
        finally
        {
            _input = false;
        }
    }

    private void SetValidity()
    {
        for (var i = 0; i < 2; i++)
        {
            if (_inputSelectors[i] is { } selector && _fields[i] is TextBox field)
            {
                MaterialTextField.SetIsError(field, !_valid[i]);
                MaterialTextField.SetErrorMessage(field, _valid[i] ? "" : DateTimeStrings.Get(i == 0
                    ? Is24Hour ? "TimePicker24HourError" : "TimePickerHourError" : "TimePickerMinuteError"));
                field.IsVisible = (int)Selection == i;
                selector.IsSelected = field.IsVisible;
                selector.IsVisible = !field.IsVisible;
                selector.Classes.Set("invalid", !_valid[i]);
                var value = i == 0 ? HourForDisplay : Value.Minute;
                if (!_valid[i]) value = _inputValues[i];
                // Kotlin's %02d minimum width includes the sign; .NET's "00"
                // counts digit placeholders only (and would render -1 as -01).
                selector.Content = value.ToString(value < 0 ? "0" : "00", CultureInfo.CurrentCulture);
            }
            if (_supportingLabels[i] is { } label)
            {
                label.IsVisible = !Vibrant || !_valid[i];
                label.Classes.Set("invalid", !_valid[i]);
                label.Text = DateTimeStrings.Get(_valid[i] ? i == 0 ? "TimePickerHour" : "TimePickerMinute"
                    : i == 0 ? Is24Hour ? "TimePicker24HourError" : "TimePickerHourError" : "TimePickerMinuteError");
                AutomationProperties.SetAccessibilityView(label, _valid[i] ? AccessibilityView.Raw : AccessibilityView.Content);
                AutomationProperties.SetLiveSetting(label, _valid[i] ? AutomationLiveSetting.Off : AutomationLiveSetting.Polite);
            }
        }
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
