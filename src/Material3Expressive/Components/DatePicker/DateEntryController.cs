// Portions translated from AndroidX DatePicker.kt, DateRangePicker.kt and DateInput.kt.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;

namespace Material3Expressive.Components;

internal sealed class DateEntryController(TemplatedControl owner, bool range)
{
    private Panel? _body;
    private DateEntryContent? _switcher;
    private ItemsControl? _rangeList;
    private ContentPresenter? _title, _headline;
    private Button? _toggle;
    private bool _updating, _writingInput, _years;
    private DateOnly _month;
    private readonly List<WeakReference<DateMonthPanel>> _months = [];
    private StackPanel? _calendar;
    private Button? _monthButton, _previousMonth, _nextMonth;
    private Panel? _yearButtons;
    private AnimatedVisibility? _yearPane;
    private readonly List<TextBox> _inputs = [];

    private DateRange Selection => owner switch {
        DateRangePicker picker => picker.SelectedRange,
        DatePicker picker => new DateRange(picker.SelectedDate, null),
        _ => default
    };

    private string Text(string key, params object[] args) => DateTimeStrings.Get(key, Culture, args);

    private CultureInfo Culture => owner.GetValue(DatePicker.CultureProperty);
    private int FirstYear => Math.Clamp(owner.GetValue(DatePicker.MinYearProperty), 1, 9999);
    private int LastYear => Math.Clamp(owner.GetValue(DatePicker.MaxYearProperty), FirstYear, 9999);
    private DatePickerDisplayMode Mode => owner.GetValue(DatePicker.DisplayModeProperty);

    internal void Attach(INameScope scope)
    {
        if (_toggle is { }) _toggle.Click -= ToggleMode;

        _switcher = scope.Get<DateEntryContent>("PART_Body");
        _title = scope.Get<ContentPresenter>("PART_Title");
        _headline = scope.Get<ContentPresenter>("PART_Headline");
        _toggle = scope.Get<Button>("PART_ModeToggle");

        _switcher.Range = range;
        _toggle.Click += ToggleMode;

        NormalizeSelection();

        DateOnly date = owner.GetValue(DatePicker.DisplayedMonthProperty)
         ?? Selection.Start ?? DateOnly.FromDateTime(DateTime.Now);
        _month = new DateOnly(Math.Clamp(date.Year, FirstYear, LastYear), date.Month, 1);
        owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, _month);

        Build();
    }

    private void ToggleMode(object? sender, RoutedEventArgs e)
    {
        owner.SetCurrentValue(DatePicker.DisplayModeProperty,
            Mode == DatePickerDisplayMode.Picker ? DatePickerDisplayMode.Input : DatePickerDisplayMode.Picker);
    }

    internal void Changed(AvaloniaProperty property)
    {
        if (_body is null || _updating) return;

        if (property == DatePicker.SelectedDateProperty || property == DateRangePicker.SelectedRangeProperty)
        {
            NormalizeSelection();
            UpdateHeader();

            foreach (WeakReference<DateMonthPanel> reference in _months)
            {
                if (reference.TryGetTarget(out DateMonthPanel? month))
                    month.UpdateSelection(Selection);
            }

            if (!_writingInput) UpdateInputs();
        }
        else if (property == DatePicker.DisplayedMonthProperty)
        {
            if (owner.GetValue(DatePicker.DisplayedMonthProperty) is var (year, month, _))
                _month = new DateOnly(Math.Clamp(year, FirstYear, LastYear), month, 1);

            // Replacing the whole entry tears down its enter/exit scope. The month grid can change in place.
            if (!range && _calendar is { } && Mode == DatePickerDisplayMode.Picker)
                ShowDisplayedMonth();
            else
                Build();
        }
        else if (property == DatePicker.DisplayModeProperty)
        {
            if (Selection.Start is var (year, month, _))
                _month = new DateOnly(year, month, 1);

            _years = false;
            Build();
        }
        else if (property == DatePicker.CultureProperty
              || property == DatePicker.MinYearProperty
              || property == DatePicker.MaxYearProperty
              || property == DatePicker.IsDateSelectableProperty
              || property == DatePicker.IsYearSelectableProperty)
        {
            _month = new DateOnly(Math.Clamp(_month.Year, FirstYear, LastYear), _month.Month, 1);
            NormalizeSelection();
            Build();
        }
        else if (property == DatePicker.TitleProperty
              || property == DatePicker.HeadlineProperty
              || property == DatePicker.TitleTemplateProperty
              || property == DatePicker.HeadlineTemplateProperty
              || property == DatePicker.ShowModeToggleProperty
              || property == DatePicker.ShowHeadlineProperty)
            UpdateHeader();
    }

    private void NormalizeSelection()
    {
        DateRange value = Selection;

        DateOnly? Valid(DateOnly? date) => date is { } d && d.Year >= FirstYear && d.Year <= LastYear ? date : null;

        value = new DateRange(Valid(value.Start), Valid(value.End));
        if (range && (value.Start is null || value.End < value.Start)) value = default;
        if (value != Selection) WriteSelection(value);
    }

    private void WriteSelection(DateRange value)
    {
        if (owner is DateRangePicker rp) rp.SetCurrentValue(DateRangePicker.SelectedRangeProperty, value);
        else owner.SetCurrentValue(DatePicker.SelectedDateProperty, value.Start);
    }

    internal void Select(DateOnly date)
    {
        if (!Selectable(date)) return;
        DateRange current = Selection;
        WriteSelection(!range || current.Start is null || current.End is { } || date < current.Start
            ? new DateRange(date, null)
            : current with { End = date });
    }

    private bool Selectable(DateOnly date)
    {
        return date.Year >= FirstYear && date.Year <= LastYear
         && (owner.GetValue(DatePicker.IsYearSelectableProperty)?.Invoke(date.Year) ?? true)
         && (owner.GetValue(DatePicker.IsDateSelectableProperty)?.Invoke(date) ?? true);
    }

    private void UpdateHeader()
    {
        if (_title is null || _headline is null || _toggle is null) return;
        _title.Content = owner.GetValue(DatePicker.TitleProperty) ?? (Mode == DatePickerDisplayMode.Input
            ? Text(range ? "DateRangeInputTitle" : "DateInputTitle")
            : Text(range ? "DateRangePickerTitle" : "DatePickerTitle"));

        _title.ContentTemplate = owner.GetValue(DatePicker.TitleTemplateProperty);

        string Format(DateOnly? date, string empty) => date?.ToString("ddd, MMM d", Culture) ?? empty;

        _headline.Content = owner.GetValue(DatePicker.HeadlineProperty) ?? (range
            ? $"{Format(Selection.Start, Text("DateRangePickerStartHeadline"))} – {Format(Selection.End, Text("DateRangePickerEndHeadline"))}"
            : Format(Selection.Start, Text(Mode == DatePickerDisplayMode.Input ? "DateInputHeadline" : "DatePickerHeadline")));

        _headline.ContentTemplate = owner.GetValue(DatePicker.HeadlineTemplateProperty);
        _headline.IsVisible = owner.GetValue(DatePicker.ShowHeadlineProperty);

        _toggle.IsVisible = owner.GetValue(DatePicker.ShowModeToggleProperty);
        _toggle.Content = new MaterialSymbol {
            Symbol = Mode == DatePickerDisplayMode.Picker
                ? MaterialSymbolCodepoint.Edit
                : MaterialSymbolCodepoint.CalendarMonth
        };

        AutomationProperties.SetName(_toggle,
            Text(Mode == DatePickerDisplayMode.Picker ? "DatePickerSwitchToInputMode" : "DatePickerSwitchToCalendarMode"));
    }

    private void Build()
    {
        if (_switcher is null) return;
        _updating = true;
        try
        {
            _body = new Grid { Tag = Mode };
            _rangeList = null;
            _months.Clear();
            _inputs.Clear();
            UpdateHeader();
            owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, _month);

            if (Mode == DatePickerDisplayMode.Input)
                BuildInputs();
            else if (range)
                BuildRange();
            else
                BuildSingle();

            _switcher.Content = _body;
        }
        finally
        {
            _updating = false;
        }
    }

    private void BuildSingle()
    {
        var content = new StackPanel();
        var navigation = new Grid {
            ColumnDefinitions = new ColumnDefinitions("*,48,48"), Height = 56, Margin = new Thickness(12, 0)
        };
        Button previous = MonthArrow(MaterialSymbolCodepoint.KeyboardArrowLeft, () => MoveMonth(-1));
        Button next = MonthArrow(MaterialSymbolCodepoint.KeyboardArrowRight, () => MoveMonth(1));
        var calendar = new StackPanel();
        var years = new WrapPanel { Orientation = Orientation.Horizontal, Width = 336, Margin = new Thickness(0, 16) };
        for (int year = FirstYear; year <= LastYear; year++)
        {
            int selected = year;
            Button button = Button(year.ToString(Culture), () => {
                _years = false;
                if (_yearPane is { }) _yearPane.Visible = false;
                if (_previousMonth is { }) _previousMonth.IsVisible = _nextMonth!.IsVisible = true;
                var target = new DateOnly(selected, _month.Month, 1);
                if (target != _month) SetMonth(target);
            });
            button.Tag = year;
            button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialDateYearTheme"));
            button.Width = 112;
            button.Height = 52;
            button.Classes.Set("selected", year == _month.Year);
            button.Classes.Set("today", year == DateTime.Today.Year);
            button.IsEnabled = owner.GetValue(DatePicker.IsYearSelectableProperty)?.Invoke(year) ?? true;
            AutomationProperties.SetName(button, Text("DatePickerNavigateToYearDescription", year));
            years.Children.Add(button);
        }

        var scroll = new ScrollViewer {
            Content = years, Height = 335, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var yearContent = new StackPanel { Children = { scroll, new Separator() } };
        yearContent.Bind(Panel.BackgroundProperty, new Binding(nameof(TemplatedControl.Background)) { Source = owner });
        var yearVisibility = new AnimatedVisibility {
            Visible = _years, VerticalAlignment = VerticalAlignment.Top, ClipToBounds = true, Content = yearContent,
            Enter = new ExpandVertically { Spec = _switcher!.SizeSpec(true) } +
                new FadeIn { InitialAlpha = 0.6, Spec = _switcher.EffectsSpec() },
            Exit = new ShrinkVertically { Spec = _switcher.SizeSpec(true) } + new FadeOut { Spec = _switcher.EffectsSpec(true) }
        };
        Button month = Button(_month.ToString("Y", Culture), () => {
            _years = !_years;
            yearVisibility.Visible = _years;
            previous.IsVisible = next.IsVisible = !_years;
            if (_years)
            {
                Dispatcher.UIThread.Post(() =>
                    scroll.Offset = new Vector(0, Math.Max(0, ((_month.Year - FirstYear) / 3 - 3) * 52)));
            }
        });
        month.HorizontalAlignment = HorizontalAlignment.Left;
        previous.IsEnabled = _month > new DateOnly(FirstYear, 1, 1);
        next.IsEnabled = _month < new DateOnly(LastYear, 12, 1);
        previous.IsVisible = next.IsVisible = !_years;
        AutomationProperties.SetName(previous, Text("DatePickerSwitchToPreviousMonth"));
        AutomationProperties.SetName(next, Text("DatePickerSwitchToNextMonth"));
        AutomationProperties.SetName(month, Text("DatePickerSwitchToYearSelection"));
        AutomationProperties.SetName(yearVisibility, Text("DatePickerYearPickerPaneTitle"));
        Grid.SetColumn(previous, 1);
        Grid.SetColumn(next, 2);
        navigation.Children.Add(month);
        navigation.Children.Add(previous);
        navigation.Children.Add(next);
        content.Children.Add(navigation);
        calendar.Children.Add(WeekDays());
        DateMonthPanel days = Month(_month);
        WireMonthGestures(days);
        calendar.Children.Add(days);
        content.Children.Add(new Grid { Children = { calendar, yearVisibility } });
        _calendar = calendar;
        _monthButton = month;
        _previousMonth = previous;
        _nextMonth = next;
        _yearButtons = years;
        _yearPane = yearVisibility;
        _body!.Children.Add(content);
    }

    private void ShowDisplayedMonth()
    {
        if (_calendar is null || _monthButton is null || _previousMonth is null || _nextMonth is null) return;
        _monthButton.Content = _month.ToString("Y", Culture);
        _previousMonth.IsEnabled = _month > new DateOnly(FirstYear, 1, 1);
        _nextMonth.IsEnabled = _month < new DateOnly(LastYear, 12, 1);
        if (_calendar.Children.Count > 1) _calendar.Children.RemoveAt(1);
        DateMonthPanel days = Month(_month);
        WireMonthGestures(days);
        _calendar.Children.Add(days);
        if (_yearButtons is null) return;
        foreach (Button button in _yearButtons.Children.OfType<Button>())
            button.Classes.Set("selected", button.Tag is int year && year == _month.Year);
    }

    private void WireMonthGestures(Control days)
    {
        days.PointerWheelChanged += (_, e) => {
            if (e.Delta.Y != 0 || e.Delta.X != 0)
            {
                MoveMonth(e.Delta.Y + e.Delta.X < 0 ? 1 : -1);
                e.Handled = true;
            }
        };
        Point? press = null;
        bool swiping = false;
        days.AddHandler(InputElement.PointerPressedEvent, (_, e) => {
            if (e.GetCurrentPoint(days).Properties.IsLeftButtonPressed)
            {
                press = e.GetPosition(days);
                swiping = false;
            }
        }, RoutingStrategies.Tunnel);
        days.AddHandler(InputElement.PointerMovedEvent, (_, e) => {
            if (press is { } start && Math.Abs(e.GetPosition(days).X - start.X) > 12)
            {
                swiping = true;
                e.Pointer.Capture(days);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        days.AddHandler(InputElement.PointerReleasedEvent, (_, e) => {
            if (swiping && press is { } start)
            {
                double dx = e.GetPosition(days).X - start.X;
                e.Pointer.Capture(null);
                e.Handled = true;
                if (Math.Abs(dx) > 48) MoveMonth((dx < 0 ? 1 : -1) * (owner.FlowDirection == FlowDirection.RightToLeft ? -1 : 1));
            }

            press = null;
            swiping = false;
        }, RoutingStrategies.Tunnel);
    }

    private Button MonthArrow(MaterialSymbolCodepoint symbol, Action click)
    {
        var button = new IconButton { Symbol = symbol, Width = 48, Height = 48, Padding = new Thickness(0) };
        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialStandardIconButtonTheme"));
        button.Click += (_, _) => click();
        return button;
    }

    private void BuildRange()
    {
        var content = new Grid { RowDefinitions = new RowDefinitions("48,*") };
        content.Children.Add(WeekDays());
        DateOnly[] items = Enumerable.Range(0, (LastYear - FirstYear + 1) * 12)
           .Select(i => new DateOnly(FirstYear + i / 12, i % 12 + 1, 1))
           .ToArray();
        var list = new ItemsControl { ItemsSource = items };
        _rangeList = list;
        list.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
        list.ItemTemplate = new FuncDataTemplate<DateOnly>((date, _) => {
            var panel = new StackPanel();
            var label = new TextBlock { Text = date.ToString("Y", Culture), Margin = new Thickness(24, 20, 0, 8) };
            label.Classes.Add("date-month-title");
            panel.Children.Add(label);
            panel.Children.Add(Month(date));
            return panel;
        });
        var scroll = new ScrollViewer {
            Content = list, Height = 400, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(scroll, 1);
        content.Children.Add(scroll);
        _body!.Children.Add(content);
        list.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => {
            list.ScrollIntoView(items[(_month.Year - FirstYear) * 12 + _month.Month - 1]);
        });
        scroll.ScrollChanged += (_, _) => {
            if (_updating || !scroll.IsAttachedToVisualTree()) return;
            (DateMonthPanel panel, Point? point) visible = list.GetVisualDescendants()
               .OfType<DateMonthPanel>()
               .Select(panel => (panel, point: panel.TranslatePoint(default, scroll)))
               .Where(item =>
                    item.point is { } point && point.Y + item.panel.Bounds.Height > 0 && point.Y < scroll.Viewport.Height)
               .OrderBy(item => item.point!.Value.Y)
               .FirstOrDefault();
            if (visible.panel is null) return;
            _month = visible.panel.Month;
            _updating = true;
            try
            {
                owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, _month);
            }
            finally
            {
                _updating = false;
            }
        };
    }

    private Control WeekDays()
    {
        var row = new UniformGrid { Rows = 1, Columns = 7, Height = 48, Margin = new Thickness(12, 0) };
        int first = (int)Culture.DateTimeFormat.FirstDayOfWeek;
        for (int day = 0; day < 7; day++)
        {
            string name = Culture.DateTimeFormat.GetShortestDayName((DayOfWeek)((first + day) % 7));
            var text = new TextBlock {
                Text = name, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            text.Classes.Add("date-weekday");
            row.Children.Add(text);
        }

        return row;
    }

    private DateMonthPanel Month(DateOnly month)
    {
        var panel = new DateMonthPanel(month, Culture, Selectable, Select, FocusDate) { Margin = new Thickness(12, 0) };
        panel.UpdateSelection(Selection);
        _months.RemoveAll(reference => !reference.TryGetTarget(out _));
        _months.Add(new WeakReference<DateMonthPanel>(panel));
        return panel;
    }

    private void FocusDate(DateOnly date)
    {
        if (date.Year < FirstYear || date.Year > LastYear) return;
        var month = new DateOnly(date.Year, date.Month, 1);
        if (range) _rangeList?.ScrollIntoView(month);
        else if (month != _month) SetMonth(month);
        Dispatcher.UIThread.Post(() => _body?.GetVisualDescendants()
           .OfType<Button>()
           .FirstOrDefault(button => Equals(button.Tag, date))
          ?.Focus());
    }

    private void MoveMonth(int delta)
    {
        if ((delta < 0 && _month == new DateOnly(FirstYear, 1, 1)) ||
            (delta > 0 && _month == new DateOnly(LastYear, 12, 1))) return;
        SetMonth(_month.AddMonths(delta));
    }

    private void SetMonth(DateOnly month)
    {
        owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, month);
    }

    private void BuildInputs()
    {
        var row = new StackPanel {
            Orientation = range ? Orientation.Horizontal : Orientation.Vertical, Spacing = 8,
            Margin = new Thickness(24, 24, 24, 16)
        };
        string pattern = InputPattern(Culture);
        string digits = DigitPattern(pattern);
        for (int index = 0; index < (range ? 2 : 1); index++)
        {
            int fieldIndex = index;
            var box = new DateInputTextBox(pattern) {
                Name = range ? index == 0 ? "PART_StartInput" : "PART_EndInput" : "PART_DateInput", Width = range ? 152 : 312,
                MinWidth = 0, MaxLength = pattern.Length, VerticalAlignment = VerticalAlignment.Top
            };
            box.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialOutlinedTextFieldTheme"));
            MaterialTextField.SetLabel(box,
                Text(range ? index == 0 ? "DateRangePickerStartHeadline" : "DateRangePickerEndHeadline" : "DateInputLabel"));
            box.PlaceholderText = pattern.ToUpperInvariant();
            box.AddHandler(InputElement.KeyDownEvent, (_, e) => {
                // Separators belong to the display. Native deletion must also remove a digit,
                // otherwise formatting would put a just-deleted separator straight back.
                if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0
                 || box.SelectionStart != box.SelectionEnd || box.Text is not { } text) return;
                int caret = box.CaretIndex;
                if (e.Key == Key.Back && caret > 0 && !Char.IsDigit(text[caret - 1]))
                {
                    box.SelectionStart = Math.Max(0, caret - 2);
                    box.SelectionEnd = caret;
                }
                else if (e.Key == Key.Delete && caret < text.Length && !Char.IsDigit(text[caret]))
                {
                    box.SelectionStart = caret;
                    box.SelectionEnd = Math.Min(text.Length, caret + 2);
                }
            }, RoutingStrategies.Tunnel);
            box.TextChanged += (_, _) => InputChanged(box, fieldIndex, pattern, digits);
            _inputs.Add(box);
            row.Children.Add(box);
        }

        _body!.Children.Add(row);
        UpdateInputs();
    }

    internal static string InputPattern(CultureInfo culture)
    {
        string pattern = culture.DateTimeFormat.ShortDatePattern;
        char[] order = Regex.Matches(pattern, "[dMy]+")
           .Select(m => m.Value[0])
           .Where(c => c is 'd' or 'M' or 'y')
           .Distinct()
           .ToArray();
        string separator = pattern.Contains('.') ? "." : pattern.Contains('-') ? "-" : "/";
        return String.Join(separator, order.Select(c => c == 'y' ? "yyyy" : c == 'M' ? "MM" : "dd"));
    }

    private static string DigitPattern(string pattern) => Regex.Replace(pattern, "[^dMy]", "");

    private void UpdateInputs()
    {
        _writingInput = true;
        try
        {
            string pattern = InputPattern(Culture);
            string digits = DigitPattern(pattern);
            for (int i = 0; i < _inputs.Count; i++)
            {
                string text = (i == 0 ? Selection.Start : Selection.End)?.ToString(digits, Culture) ?? "";
                _inputs[i].Text = FormatInput(text, pattern);
                _inputs[i].CaretIndex = _inputs[i].Text!.Length;
            }
        }
        finally
        {
            _writingInput = false;
        }
    }

    private static string FormatInput(string digits, string pattern)
    {
        var result = new StringBuilder(pattern.Length);
        int index = 0;
        foreach (char c in pattern)
        {
            if (c is 'd' or 'M' or 'y')
            {
                if (index == digits.Length) break;
                result.Append(digits[index++]);
            }
            else
                result.Append(c);
        }

        return result.ToString();
    }

    private void InputChanged(DateInputTextBox box, int index, string pattern, string digits)
    {
        if (_writingInput || _updating) return;
        box.MapEditPositions();
        string displayText = box.Text ?? "";
        string text = new(displayText.Where(Char.IsDigit).ToArray());
        DateOnly? parsed = null;
        string? error = null;
        if (text.Length == 0 || text.Length < digits.Length) error = null;
        else if (!DateOnly.TryParseExact(text, digits, Culture, DateTimeStyles.None, out DateOnly date))
            error = Text("DateInputInvalidForPattern", pattern.ToUpperInvariant());
        else if (date.Year < FirstYear || date.Year > LastYear) error = Text("DateInputInvalidYearRange", FirstYear, LastYear);
        else if (!Selectable(date)) error = Text("DateInputInvalidNotAllowed", date.ToString("d", Culture));
        else if (range && ((index == 0 && Selection.End is { } end && date > end) ||
                     (index == 1 && Selection.Start is { } start && date < start)))
            error = Text("DateRangeInputInvalidRangeInput");
        else parsed = date;
        MaterialTextField.SetIsError(box, error is { });
        MaterialTextField.SetSupportingText(box, error);
        MaterialTextField.SetErrorMessage(box, error ?? "");
        _writingInput = true;
        try
        {
            WriteSelection(index == 0
                ? new DateRange(text.Length == digits.Length ? parsed : null, Selection.End)
                : new DateRange(Selection.Start, text.Length == digits.Length ? parsed : null));
        }
        finally
        {
            _writingInput = false;
        }
    }

    private static Button Button(object text, Action click)
    {
        var button = new Button { Content = text, MinWidth = 48, Padding = new Thickness(8, 0), Height = 48 };
        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTextButtonTheme"));
        button.Click += (_, _) => click();
        return button;
    }

    // TextBox has no VisualTransformation. Coerce the displayed text inside its existing edit
    // operation, before base.CoerceText snapshots it, so inserting delimiters does not clear undo.
    private sealed class DateInputTextBox(string pattern) : TextBox
    {
        private string? _unformatted;
        private readonly char _delimiter = pattern.First(c => c is not ('d' or 'M' or 'y'));
        private readonly int _digitCount = pattern.Count(c => c is 'd' or 'M' or 'y');
        protected override Type StyleKeyOverride => typeof(TextBox);

        protected override string? CoerceText(string? value)
        {
            string input = value ?? "";
            string digits = new(input.Where(Char.IsDigit).ToArray());
            if (digits.Length > _digitCount || input.Any(c => !Char.IsDigit(c) && c != _delimiter))
            {
                _unformatted = null;
                return base.CoerceText(Text);
            }

            string formatted = FormatInput(digits, pattern);
            _unformatted = input == formatted ? null : input;
            return base.CoerceText(formatted);
        }

        internal void MapEditPositions()
        {
            if (_unformatted is not { } input) return;
            _unformatted = null;
            int first = pattern.IndexOf(_delimiter);
            int second = pattern.LastIndexOf(_delimiter);

            int Map(int offset)
            {
                int original = input.Take(offset).Count(Char.IsDigit);
                return Math.Min(Text?.Length ?? 0, original < first ? original : original < second ? original + 1 : original + 2);
            }

            int caret = Map(CaretIndex);
            int start = Map(SelectionStart);
            int end = Map(SelectionEnd);
            CaretIndex = caret;
            SelectionStart = start;
            SelectionEnd = end;
        }
    }
}

internal sealed class DateMonthPanel : Control
{
    private readonly List<Button> Children = [];

    public static readonly StyledProperty<IBrush?> RangeBrushProperty =
        AvaloniaProperty.Register<DateMonthPanel, IBrush?>(nameof(RangeBrush));

    public IBrush? RangeBrush
    {
        get => GetValue(RangeBrushProperty);
        set => SetValue(RangeBrushProperty, value);
    }

    private readonly DateOnly _month;
    internal DateOnly Month => _month;
    private readonly int _offset;
    private DateRange _selection;

    internal DateMonthPanel(
        DateOnly month,
        CultureInfo culture,
        Func<DateOnly, bool> selectable,
        Action<DateOnly> select,
        Action<DateOnly> focus
    )
    {
        _month = month;
        _offset = ((int)month.DayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        for (int day = 1; day <= DateTime.DaysInMonth(month.Year, month.Month); day++)
        {
            var date = new DateOnly(month.Year, month.Month, day);
            var button = new Button { Content = day.ToString(culture), IsEnabled = selectable(date), Tag = date };
            button.Bind(ThemeProperty, new DynamicResourceExtension("MaterialDateDayTheme"));
            button.Classes.Set("today", date == DateOnly.FromDateTime(DateTime.Now));
            AutomationProperties.SetName(button, date.ToString("D", culture));
            button.Click += (_, _) => select(date);
            Children.Add(button);
            button.KeyDown += (_, e) => {
                int delta = e.Key switch {
                    Key.Left => FlowDirection == FlowDirection.RightToLeft ? 1 : -1,
                    Key.Right => FlowDirection == FlowDirection.RightToLeft ? -1 : 1, Key.Up => -7, Key.Down => 7, _ => 0
                };
                if (delta != 0 && date.DayNumber + delta >= 0 && date.DayNumber + delta <= DateOnly.MaxValue.DayNumber)
                {
                    focus(date.AddDays(delta));
                    e.Handled = true;
                }
                else if (e.Key is Key.PageUp or Key.PageDown)
                {
                    int change = e.Key == Key.PageUp ? -1 : 1;
                    if ((date.Year == 1 && date.Month == 1 && change < 0) ||
                        (date.Year == 9999 && date.Month == 12 && change > 0)) return;
                    focus(date.AddMonths(change));
                    e.Handled = true;
                }
                else if (e.Key is Key.Home or Key.End)
                {
                    focus(new DateOnly(date.Year, date.Month,
                        e.Key == Key.Home ? 1 : DateTime.DaysInMonth(date.Year, date.Month)));
                    e.Handled = true;
                }
            };
            LogicalChildren.Add(button);
            VisualChildren.Add(button);
        }
    }

    internal void UpdateSelection(DateRange value)
    {
        _selection = value;
        foreach (Button child in Children.OfType<Button>())
        {
            var date = (DateOnly)child.Tag!;
            child.Classes.Set("selected", date == value.Start || date == value.End);
            child.Classes.Set("in-range", date >= value.Start && date <= value.End);
        }

        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size available)
    {
        foreach (Button child in Children) child.Measure(new Size(48, 48));
        return new Size(Double.IsInfinity(available.Width) ? 336 : available.Width, 288);
    }

    protected override Size ArrangeOverride(Size size)
    {
        double gap = (size.Width - 7 * 48) / 8;
        for (int i = 0; i < Children.Count; i++)
        {
            int cell = i + _offset;
            double x = gap + cell % 7 * (48 + gap);
            Children[i].Arrange(new Rect(x, cell / 7 * 48, 48, 48));
        }

        return size;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // A drawn transparent fill gives the calendar a hit surface between day buttons too.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        if (_selection.Start is not { } start || _selection.End is not { } end || end < _month ||
            start > _month.AddDays(Children.Count - 1)) return;
        double gap = (Bounds.Width - 336) / 8;
        for (int row = 0; row < 6; row++)
        {
            int first = Math.Max(0, row * 7 - _offset);
            int last = Math.Min(Children.Count - 1, row * 7 + 6 - _offset);
            if (first > last) continue;
            int lo = Math.Max(first, start.DayNumber - _month.DayNumber);
            int hi = Math.Min(last, end.DayNumber - _month.DayNumber);
            if (lo > hi) continue;
            double x1 = gap + (lo + _offset) % 7 * (48 + gap) + (lo == start.DayNumber - _month.DayNumber ? 24 : 0);
            double x2 = gap + (hi + _offset) % 7 * (48 + gap) + (hi == end.DayNumber - _month.DayNumber ? 24 : 48 + gap);
            context.DrawRectangle(RangeBrush, null, new Rect(x1, row * 48 + 4, Math.Max(0, x2 - x1), 40));
        }
    }
}
