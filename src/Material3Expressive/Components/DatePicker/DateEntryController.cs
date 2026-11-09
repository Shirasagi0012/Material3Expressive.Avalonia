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
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using MaterialSymbols;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

internal sealed class DateEntryController(TemplatedControl owner, bool range)
{
    public static readonly FuncMultiValueConverter<object?, object> GridLengthConverter = new(values =>
        values.FirstOrDefault() is double size ? new GridLength(size) : AvaloniaProperty.UnsetValue);

    // Definitions have no resource tree; resolve from their grid rather than a sibling's
    // effective Width/Height, which applications may independently override.
    private static void BindGridLengthToken(AvaloniaObject target, AvaloniaProperty property, Control host, MaterialTokenKey key)
    {
        target.Bind(property, new MultiBinding {
            Priority = BindingPriority.LocalValue,
            Converter = new FuncMultiValueConverter<object?, object>(_ =>
                TokenResolver.TryResolve(host, key, host.ActualThemeVariant, out var value) && value is double size
                    ? new GridLength(size) : AvaloniaProperty.UnsetValue),
            Bindings = { host.GetResourceObservable(key).ToBinding() }
        });
    }

    // Resolve at each consuming element; the resource binding only signals invalidation.
    private static void BindToken(StyledElement target, AvaloniaProperty property, MaterialTokenKey key)
    {
        var binding = new MultiBinding {
            Priority = BindingPriority.LocalValue,
            Converter = new TokenBindingConverter(key, null, null),
            Bindings = {
                new Binding { RelativeSource = new RelativeSource(RelativeSourceMode.Self) },
                new DynamicResourceExtension(key)
            }
        };
        target.Bind(property, binding);
    }

    private Panel? _body;
    private DateEntryContent? _switcher;
    private DateRangeMonthList? _rangeList;
    private ContentPresenter? _title, _headline;
    private readonly DatePickerTitle _defaultTitle = range ? new DateRangePickerTitle() : new DatePickerTitle();
    private readonly Control _defaultHeadline = range ? new DateRangePickerHeadline() : new DatePickerHeadline();
    private IDisposable? _titleColorBinding, _headlineColorBinding;
    private Button? _toggle;
    private bool _updating, _writingInput, _years;
    private DateOnly _month;
    private readonly List<WeakReference<DateMonthPanel>> _months = [];
    private StackPanel? _calendar;
    private DateMonthStrip? _monthStrip;
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

    private IDatePickerFormatter? DateFormatter => owner.GetValue(DatePicker.DateFormatterProperty);
    private string? FormatDate(DateOnly? date, bool description = false) =>
        DatePickerHeaderText.Format(date, Culture, DateFormatter, description);
    private string FormatMonth(DateOnly month) => DateFormatter is { } formatter
        ? formatter.FormatMonthYear(month, Culture) ?? "-"
        : month.ToString("Y", Culture);

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
        _titleColorBinding?.Dispose();
        _headlineColorBinding?.Dispose();
        _titleColorBinding = _defaultTitle.Bind(TextBlock.ForegroundProperty, _title.GetObservable(TextBlock.ForegroundProperty), BindingPriority.Template);
        _headlineColorBinding = _defaultHeadline.Bind(TextBlock.ForegroundProperty, _headline.GetObservable(TextBlock.ForegroundProperty), BindingPriority.Template);
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
            else if (range && _rangeList is { } list && Mode == DatePickerDisplayMode.Picker)
                PositionRangeMonth(list, _month);
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
        else if (property == DatePicker.DateFormatterProperty)
        {
            // Formatting is presentation only: preserve the editor draft, focus, month strip and scroll position.
            UpdateHeader();
            if (_monthButton is not null) _monthButton.Content = FormatMonth(_month);
            foreach (var reference in _months)
                if (reference.TryGetTarget(out var month)) month.UpdateDescriptions(date => FormatDate(date, true) ?? "");
            if (_rangeList is not null)
                foreach (var label in _rangeList.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("date-month-title")))
                    if (label.Tag is DateOnly month) label.Text = FormatMonth(month);
            foreach (var box in _inputs.OfType<DateInputTextBox>())
                if (box.RejectedDate is DateOnly invalidDate)
                {
                    var error = Text("DateInputInvalidNotAllowed", DateFormatter is { } formatter
                        ? formatter.FormatDate(invalidDate, Culture) ?? "null" : invalidDate.ToString("d", Culture));
                    MaterialTextField.SetSupportingText(box, error);
                    MaterialTextField.SetErrorMessage(box, error);
                }
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
        _defaultTitle.DisplayMode = Mode;
        _defaultTitle.Culture = Culture;
        _defaultHeadline.SetValue(DatePicker.DisplayModeProperty, Mode);
        _defaultHeadline.SetValue(DatePicker.CultureProperty, Culture);
        _defaultHeadline.SetValue(DatePicker.DateFormatterProperty, DateFormatter);
        if (_defaultHeadline is DateRangePickerHeadline rangeHeadline) rangeHeadline.SelectedRange = Selection;
        else ((DatePickerHeadline)_defaultHeadline).SelectedDate = Selection.Start;

        var titleTemplate = owner.GetValue(DatePicker.TitleTemplateProperty);
        var headlineTemplate = owner.GetValue(DatePicker.HeadlineTemplateProperty);
        _title.Content = owner.GetValue(DatePicker.TitleProperty)
            ?? (titleTemplate is null ? _defaultTitle : _defaultTitle.LocalizedTitle);
        _title.ContentTemplate = titleTemplate;
        // Existing custom templates continue receiving their string data context. Only
        // the default visual now reuses the public, independently usable content.
        _headline.Content = owner.GetValue(DatePicker.HeadlineProperty) ?? (headlineTemplate is null
            ? _defaultHeadline : _defaultHeadline is DateRangePickerHeadline pair
                ? $"{pair.StartText} – {pair.EndText}" : ((DatePickerHeadline)_defaultHeadline).HeadlineText);

        if (owner.GetValue(DatePicker.HeadlineProperty) is null && owner.GetValue(DatePicker.HeadlineTemplateProperty) is null)
        {
            AutomationProperties.SetName(_headline, AutomationProperties.GetName(_defaultHeadline));
            AutomationProperties.SetLiveSetting(_headline, AutomationLiveSetting.Polite);
        }
        else
        {
            _headline.ClearValue(AutomationProperties.NameProperty);
            _headline.ClearValue(AutomationProperties.LiveSettingProperty);
        }

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
            _monthStrip = null;
            _calendar = null;
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
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto")
        };
        BindToken(navigation, Layoutable.HeightProperty, Comp.DatePickerModal.MonthNavigationHeight);
        BindToken(navigation, Layoutable.MarginProperty, Comp.DatePickerModal.CalendarHorizontalPadding);
        Button previous = MonthArrow(MaterialSymbolCodepoint.KeyboardArrowLeft, () => MoveMonth(-1));
        Button next = MonthArrow(MaterialSymbolCodepoint.KeyboardArrowRight, () => MoveMonth(1));
        BindGridLengthToken(navigation.ColumnDefinitions[1], ColumnDefinition.WidthProperty,
            navigation, Comp.DatePickerModal.NavigationButtonSize);
        BindGridLengthToken(navigation.ColumnDefinitions[2], ColumnDefinition.WidthProperty,
            navigation, Comp.DatePickerModal.NavigationButtonSize);
        var calendar = new StackPanel();
        var years = new WrapPanel { Orientation = Orientation.Horizontal };
        var yearDivider = new Separator { Focusable = true };
        Button? month = null;
        void FocusDisplayedYear()
        {
            var selected = years.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Tag, _month.Year));
            if (selected?.Focus(NavigationMethod.Tab) != true)
                TopLevel.GetTopLevel(owner)?.FocusManager?.TryMoveFocus(NavigationDirection.Down);
        }
        void ExitYears(bool backwards)
        {
            if (backwards) month?.Focus(NavigationMethod.Tab, KeyModifiers.Shift);
            else TopLevel.GetTopLevel(owner)?.FocusManager?.TryMoveFocus(NavigationDirection.Next,
                new FindNextElementOptions { FocusedElement = yearDivider });
        }
        yearDivider.GotFocus += (_, e) =>
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) FocusDisplayedYear();
            else ExitYears(false);
        };
        BindToken(years, Layoutable.WidthProperty, Comp.DatePickerModal.YearGridWidth);
        BindToken(years, Layoutable.MarginProperty, Comp.DatePickerModal.YearGridVerticalMargin);
        for (int year = FirstYear; year <= LastYear; year++)
        {
            int selected = year;
            Button button = Button(year.ToString(Culture), () => {
                _years = false;
                if (_yearPane is { }) _yearPane.Visible = false;
                if (_previousMonth is { }) _previousMonth.IsVisible = _nextMonth!.IsVisible = true;
                var target = new DateOnly(selected, _month.Month, 1);
                _monthStrip?.JumpToMonth(target);
            });
            button.Tag = year;
            button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialDateYearTheme"));
            BindToken(button, Layoutable.WidthProperty, Comp.DatePickerModal.YearCellWidth);
            BindToken(button, Layoutable.HeightProperty, Comp.DatePickerModal.YearCellHeight);
            button.Classes.Set("selected", year == _month.Year);
            button.Classes.Set("today", year == DateTime.Today.Year);
            button.IsEnabled = owner.GetValue(DatePicker.IsYearSelectableProperty)?.Invoke(year) ?? true;
            AutomationProperties.SetName(button, Text("DatePickerNavigateToYearDescription", year));
            button.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Tab && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0)
                {
                    ExitYears(e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true;
                }
                else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
                {
                    var direction = e.Key switch { Key.Left => NavigationDirection.Left, Key.Right => NavigationDirection.Right,
                        Key.Up => NavigationDirection.Up, _ => NavigationDirection.Down };
                    TopLevel.GetTopLevel(owner)?.FocusManager?.TryMoveFocus(direction);
                    e.Handled = true;
                }
            };
            years.Children.Add(button);
        }

        var scroll = new ScrollViewer {
            Content = years, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        BindToken(scroll, Layoutable.HeightProperty, Comp.DatePickerModal.YearScrollHeight);
        var yearContent = new StackPanel { Children = { scroll, yearDivider } };
        yearContent.Bind(Panel.BackgroundProperty, new Binding(nameof(TemplatedControl.Background)) { Source = owner });
        var yearVisibility = new AnimatedVisibility {
            Visible = _years, VerticalAlignment = VerticalAlignment.Top, ClipToBounds = true, Content = yearContent,
            Enter = new ExpandVertically { Spec = _switcher!.SizeSpec(true) } +
                new FadeIn { InitialAlpha = 0.6, Spec = _switcher.EffectsSpec() },
            Exit = new ShrinkVertically { Spec = _switcher.SizeSpec(true) } + new FadeOut { Spec = _switcher.EffectsSpec(true) }
        };
        month = Button(FormatMonth(_month), () => {
            _years = !_years;
            yearVisibility.Visible = _years;
            previous.IsVisible = next.IsVisible = !_years;
            if (_years)
            {
                Dispatcher.UIThread.Post(() => {
                    if (!_years || !years.IsAttachedToVisualTree()) return;
                    scroll.Offset = new Vector(0, Math.Max(0, ((_month.Year - FirstYear - 3) / 3) * years.Children.OfType<Button>().First().Height));
                    FocusDisplayedYear();
                });
            }
        });
        month.KeyDown += (_, e) =>
        {
            if (_years && e.Key == Key.Tab && e.KeyModifiers == KeyModifiers.None)
            {
                FocusDisplayedYear(); e.Handled = true;
            }
        };
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
        var strip = new DateMonthStrip(new(FirstYear, 1, 1), new(LastYear, 12, 1), _month,
            Month, value => { SetMonth(value); ShowDisplayedMonth(); }, () => new ComposeAnimation.Runtime.SpringSpec<float>(
                _switcher!.Effects.DampingRatio, _switcher.Effects.Stiffness, .01f));
        BindToken(strip, Layoutable.MarginProperty, Comp.DatePickerModal.CalendarHorizontalPadding);
        _monthStrip = strip;
        calendar.Children.Add(strip);
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
        _monthButton.Content = FormatMonth(_month);
        _monthStrip?.ShowMonth(_month);
        _previousMonth.IsEnabled = _monthStrip?.CanScrollPrevious ?? false;
        _nextMonth.IsEnabled = _monthStrip?.CanScrollNext ?? false;
        if (_yearButtons is null) return;
        foreach (Button button in _yearButtons.Children.OfType<Button>())
            button.Classes.Set("selected", button.Tag is int year && year == _month.Year);
    }

    private Button MonthArrow(MaterialSymbolCodepoint symbol, Action click)
    {
        var button = new IconButton { Symbol = symbol, Padding = new Thickness(0) };
        BindToken(button, Layoutable.WidthProperty, Comp.DatePickerModal.NavigationButtonSize);
        BindToken(button, Layoutable.HeightProperty, Comp.DatePickerModal.NavigationButtonSize);
        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialStandardIconButtonTheme"));
        button.Click += (_, _) => click();
        return button;
    }

    private void BuildRange()
    {
        var content = new DateRangeCalendarPanel { RowDefinitions = new RowDefinitions("Auto,*") };
        Control weekdays = WeekDays();
        content.Children.Add(weekdays);
        BindGridLengthToken(content.RowDefinitions[0], RowDefinition.HeightProperty,
            content, Comp.DatePickerModal.WeekdayRowHeight);
        Control CreateMonth(DateOnly date)
        {
            var panel = new StackPanel();
            var label = new DateRangeMonthHeading(ScrollRangeMonth, CanScrollRangeMonth, () => Culture)
                { Text = FormatMonth(date), Tag = date };
            BindToken(label, Layoutable.MarginProperty, Comp.DatePickerModal.RangeMonthTitleMargin);
            label.Classes.Add("date-month-title");
            panel.Children.Add(label);
            panel.Children.Add(Month(date));
            return panel;
        }
        DateRangeMonthList? list = null;
        list = new DateRangeMonthList(new(FirstYear, 1, 1), new(LastYear, 12, 1), _month, CreateMonth, month =>
        {
            if (_updating || !ReferenceEquals(_rangeList, list) || list?.IsAttachedToVisualTree() != true) return;
            _month = month;
            _updating = true;
            try
            {
                owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, _month);
            }
            finally
            {
                _updating = false;
            }
        });
        _rangeList = list;
        Grid.SetRow(list, 1);
        content.Children.Add(list);
        _body!.Children.Add(content);
    }

    private bool CanScrollRangeMonth(int direction) => Mode == DatePickerDisplayMode.Picker &&
        owner.IsEffectivelyEnabled && _rangeList is { } list && list.IsAttachedToVisualTree() &&
        (direction < 0 ? list.CanScrollPrevious : list.CanScrollNext);

    internal bool ScrollRangeMonth(int direction)
    {
        if (!CanScrollRangeMonth(direction) || _rangeList is not { } list) return false;
        // DateRangePicker.customScrollActions targets the first visible item, not the
        // heading whose action was invoked. Both actions remain exposed at the bounds.
        var index = (_month.Year - FirstYear) * 12 + _month.Month - 1;
        PositionRangeMonth(list, new DateOnly(FirstYear, 1, 1).AddMonths(Math.Clamp(index + direction, 0, (LastYear - FirstYear + 1) * 12 - 1)));
        return true;
    }

    private void PositionRangeMonth(DateRangeMonthList list, DateOnly month)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(_rangeList, list) || !list.IsAttachedToVisualTree()) return;
            list.ScrollToMonth(month);
        }, DispatcherPriority.Loaded);
    }

    private Control WeekDays()
    {
        var row = new UniformGrid { Rows = 1, Columns = 7 };
        BindToken(row, Layoutable.HeightProperty, Comp.DatePickerModal.WeekdayRowHeight);
        BindToken(row, Layoutable.MarginProperty, Comp.DatePickerModal.CalendarHorizontalPadding);
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
        var panel = new DateMonthPanel(month, Culture, Selectable, Select, FocusDate,
            range ? null : () => _nextMonth?.Focus(NavigationMethod.Tab, KeyModifiers.Shift),
            range ? null : () => _monthStrip?.IsScrollInProgress == true,
            date => FormatDate(date, true) ?? "");
        BindToken(panel, Layoutable.MarginProperty, Comp.DatePickerModal.CalendarHorizontalPadding);
        panel.UpdateSelection(Selection);
        _months.RemoveAll(reference => !reference.TryGetTarget(out _));
        _months.Add(new WeakReference<DateMonthPanel>(panel));
        return panel;
    }

    private async void FocusDate(DateOnly date)
    {
        if (date.Year < FirstYear || date.Year > LastYear) return;
        var month = new DateOnly(date.Year, date.Month, 1);
        if (range) _rangeList?.BringMonthIntoView(month);
        else if (month != _month && _monthStrip is { } strip)
        {
            var forward = month > _month;
            await strip.AnimateToAsync(month);
            if (!Selectable(date))
            {
                var days = Enumerable.Range(1, DateTime.DaysInMonth(month.Year, month.Month))
                    .Select(day => new DateOnly(month.Year, month.Month, day));
                var enabled = (forward ? days : days.Reverse()).Where(Selectable).Select(day => (DateOnly?)day).FirstOrDefault();
                if (enabled is null) return;
                date = enabled.Value;
            }
        }
        Dispatcher.UIThread.Post(() => _body?.GetVisualDescendants()
           .OfType<Button>()
           .FirstOrDefault(button => Equals(button.Tag, date))
          ?.Focus());
    }

    private void MoveMonth(int delta)
    {
        if ((delta < 0 && _month == new DateOnly(FirstYear, 1, 1)) ||
            (delta > 0 && _month == new DateOnly(LastYear, 12, 1))) return;
        if (_monthStrip is { } strip) _ = strip.AnimateToAsync(_month.AddMonths(delta));
    }

    private void SetMonth(DateOnly month)
    {
        owner.SetCurrentValue(DatePicker.DisplayedMonthProperty, month);
    }

    private void BuildInputs()
    {
        var row = new StackPanel {
            Orientation = range ? Orientation.Horizontal : Orientation.Vertical
        };
        BindToken(row, StackPanel.SpacingProperty, Comp.DatePickerModal.InputSpacing);
        BindToken(row, Layoutable.MarginProperty, Comp.DatePickerModal.InputMargin);
        string pattern = InputPattern(Culture);
        string digits = DigitPattern(pattern);
        for (int index = 0; index < (range ? 2 : 1); index++)
        {
            int fieldIndex = index;
            var box = new DateInputTextBox(pattern) {
                Name = range ? index == 0 ? "PART_StartInput" : "PART_EndInput" : "PART_DateInput",
                MinWidth = 0, MaxLength = pattern.Length, VerticalAlignment = VerticalAlignment.Top
            };
            BindToken(box, Layoutable.WidthProperty, range ? Comp.DatePickerModal.RangeInputWidth : Comp.DatePickerModal.SingleInputWidth);
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
        box.RejectedDate = null;
        if (text.Length == 0 || text.Length < digits.Length) error = null;
        else if (!DateOnly.TryParseExact(text, digits, Culture, DateTimeStyles.None, out DateOnly date))
            error = Text("DateInputInvalidForPattern", pattern.ToUpperInvariant());
        else if (date.Year < FirstYear || date.Year > LastYear) error = Text("DateInputInvalidYearRange", FirstYear, LastYear);
        else if (!Selectable(date))
        {
            box.RejectedDate = date;
            error = Text("DateInputInvalidNotAllowed", DateFormatter is { } formatter
                ? formatter.FormatDate(date, Culture) ?? "null" : date.ToString("d", Culture));
        }
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
        var button = new Button { Content = text };
        BindToken(button, Layoutable.MinWidthProperty, Comp.DatePickerModal.MonthButtonMinWidth);
        BindToken(button, TemplatedControl.PaddingProperty, Comp.DatePickerModal.MonthButtonHorizontalPadding);
        BindToken(button, Layoutable.HeightProperty, Comp.DatePickerModal.MonthButtonHeight);
        button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialTextButtonTheme"));
        button.Click += (_, _) => click();
        return button;
    }

    // TextBox has no VisualTransformation. Coerce the displayed text inside its existing edit
    // operation, before base.CoerceText snapshots it, so inserting delimiters does not clear undo.
    private sealed class DateInputTextBox(string pattern) : TextBox
    {
        internal DateOnly? RejectedDate { get; set; }
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
    public static readonly StyledProperty<double> DayCellWidthProperty =
        AvaloniaProperty.Register<DateMonthPanel, double>(nameof(DayCellWidth), 48);
    public static readonly StyledProperty<double> DayCellHeightProperty =
        AvaloniaProperty.Register<DateMonthPanel, double>(nameof(DayCellHeight), 48);
    public static readonly StyledProperty<double> RangeIndicatorHeightProperty =
        AvaloniaProperty.Register<DateMonthPanel, double>(nameof(RangeIndicatorHeight), 40);
    public static readonly StyledProperty<double> RangeIndicatorTopOffsetProperty =
        AvaloniaProperty.Register<DateMonthPanel, double>(nameof(RangeIndicatorTopOffset), 4);

    public double DayCellWidth { get => GetValue(DayCellWidthProperty); set => SetValue(DayCellWidthProperty, value); }
    public double DayCellHeight { get => GetValue(DayCellHeightProperty); set => SetValue(DayCellHeightProperty, value); }
    public double RangeIndicatorHeight { get => GetValue(RangeIndicatorHeightProperty); set => SetValue(RangeIndicatorHeightProperty, value); }
    public double RangeIndicatorTopOffset { get => GetValue(RangeIndicatorTopOffsetProperty); set => SetValue(RangeIndicatorTopOffsetProperty, value); }

    static DateMonthPanel()
    {
        AffectsMeasure<DateMonthPanel>(DayCellWidthProperty, DayCellHeightProperty);
        AffectsArrange<DateMonthPanel>(DayCellWidthProperty, DayCellHeightProperty);
        AffectsRender<DateMonthPanel>(DayCellWidthProperty, DayCellHeightProperty,
            RangeIndicatorHeightProperty, RangeIndicatorTopOffsetProperty, RangeBrushProperty);
    }

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

    internal void UpdateDescriptions(Func<DateOnly, string> description)
    {
        foreach (var button in Children.OfType<Button>())
            if (button.Tag is DateOnly date) AutomationProperties.SetName(button, description(date));
    }

    internal DateMonthPanel(
        DateOnly month,
        CultureInfo culture,
        Func<DateOnly, bool> selectable,
        Action<DateOnly> select,
        Action<DateOnly> focus,
        Action? returnFocus = null,
        Func<bool>? scrolling = null,
        Func<DateOnly, string>? description = null
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
            AutomationProperties.SetName(button, description?.Invoke(date) ?? date.ToString("D", culture));
            button.Click += (_, _) => select(date);
            Children.Add(button);
            button.KeyDown += (_, e) => {
                if (returnFocus is not null)
                {
                    var enabled = Children.Where(child => child.IsEffectivelyEnabled).ToArray();
                    var index = Array.IndexOf(enabled, button);
                    if (e.Key == Key.Tab && index == 0 && e.KeyModifiers == KeyModifiers.Shift)
                    {
                        returnFocus(); e.Handled = true; return;
                    }
                    if (e.Key == Key.Tab && index == enabled.Length - 1 && e.KeyModifiers == KeyModifiers.None)
                    {
                        TopLevel.GetTopLevel(this)?.FocusManager?.TryMoveFocus(NavigationDirection.Down);
                        e.Handled = true; return;
                    }
                    if ((index == 0 || index == enabled.Length - 1) && scrolling?.Invoke() == true)
                    {
                        e.Handled = true; return;
                    }
                    if (e.Key is Key.Left or Key.Right)
                    {
                        var forward = (e.Key == Key.Right) != (FlowDirection == FlowDirection.RightToLeft);
                        var target = index + (forward ? 1 : -1);
                        if (target >= 0 && target < enabled.Length) enabled[target].Focus(NavigationMethod.Directional);
                        else
                        {
                            var edge = forward ? new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))
                                : new DateOnly(date.Year, date.Month, 1);
                            if (forward && edge < DateOnly.MaxValue || !forward && edge > DateOnly.MinValue)
                                focus(edge.AddDays(forward ? 1 : -1));
                        }
                        e.Handled = true; return;
                    }
                }
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
        foreach (Button child in Children) child.Measure(new Size(DayCellWidth, DayCellHeight));
        return new Size(Double.IsInfinity(available.Width) ? 7 * DayCellWidth : available.Width, 6 * DayCellHeight);
    }

    protected override Size ArrangeOverride(Size size)
    {
        double gap = (size.Width - 7 * DayCellWidth) / 8;
        for (int i = 0; i < Children.Count; i++)
        {
            int cell = i + _offset;
            double x = gap + cell % 7 * (DayCellWidth + gap);
            Children[i].Arrange(new Rect(x, cell / 7 * DayCellHeight, DayCellWidth, DayCellHeight));
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
        double gap = (Bounds.Width - 7 * DayCellWidth) / 8;
        for (int row = 0; row < 6; row++)
        {
            int first = Math.Max(0, row * 7 - _offset);
            int last = Math.Min(Children.Count - 1, row * 7 + 6 - _offset);
            if (first > last) continue;
            int lo = Math.Max(first, start.DayNumber - _month.DayNumber);
            int hi = Math.Min(last, end.DayNumber - _month.DayNumber);
            if (lo > hi) continue;
            double x1 = gap + (lo + _offset) % 7 * (DayCellWidth + gap) + (lo == start.DayNumber - _month.DayNumber ? DayCellWidth / 2 : 0);
            double x2 = gap + (hi + _offset) % 7 * (DayCellWidth + gap) + (hi == end.DayNumber - _month.DayNumber ? DayCellWidth / 2 : DayCellWidth + gap);
            context.DrawRectangle(RangeBrush, null, new Rect(x1, row * DayCellHeight + RangeIndicatorTopOffset, Math.Max(0, x2 - x1), RangeIndicatorHeight));
        }
    }
}
