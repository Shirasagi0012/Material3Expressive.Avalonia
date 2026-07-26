using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 date picker: a month grid with a headline showing the selection.
/// </summary>
/// <remarks>
/// The grid is drawn rather than built from forty-two child controls. A month is a fixed geometry
/// redrawn whenever the month changes, and the alternative is creating and destroying controls on
/// every navigation for no gain — each cell is a circle and a number.
/// <para>
/// The week starts wherever the current culture says it does, and the day and weekday names come
/// from the culture too, so a picker in a German locale reads as a German calendar without being
/// told.
/// </para>
/// </remarks>
public class MaterialDatePicker : TemplatedControl
{
    /// <inheritdoc cref="SelectedDate" />
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MaterialDatePicker, DateTime?>(
            nameof(SelectedDate), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="DisplayMonth" />
    public static readonly StyledProperty<DateTime> DisplayMonthProperty =
        AvaloniaProperty.Register<MaterialDatePicker, DateTime>(nameof(DisplayMonth), DateTime.Today);

    /// <inheritdoc cref="Headline" />
    public static readonly StyledProperty<string?> HeadlineProperty =
        AvaloniaProperty.Register<MaterialDatePicker, string?>(nameof(Headline), "Select date");

    private const int DaysPerWeek = 7;
    private const int MaxWeeks = 6;

    private readonly MaterialThemeWatcher _theme;

    private PickerTokens _tokens = PickerTokens.Date;
    private PickerColors _colors = PickerTokens.Date.Resolve(null, false);

    public MaterialDatePicker()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTokens();

        Focusable = true;
        ApplyTokens();
    }

    static MaterialDatePicker()
    {
        AffectsRender<MaterialDatePicker>(SelectedDateProperty, DisplayMonthProperty);
    }

    /// <summary>The chosen date, or null.</summary>
    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>Which month the grid is showing. Any day in it will do.</summary>
    public DateTime DisplayMonth
    {
        get => GetValue(DisplayMonthProperty);
        set => SetValue(DisplayMonthProperty, value);
    }

    /// <summary>The prompt above the selection.</summary>
    public string? Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    /// <summary>The area the month grid occupies, below the headline.</summary>
    protected Rect GridArea => new(
        0,
        HeadlineHeight,
        _tokens.DaySize * DaysPerWeek,
        _tokens.DaySize * (MaxWeeks + 1));

    private double HeadlineHeight =>
        _theme.Theme.Typography.GetTypeScale(_tokens.HeadlineTextStyle).LineHeight
        + _theme.Theme.Typography.GetTypeScale(_tokens.SupportingTextStyle).LineHeight
        + 32;

    public override void Render(DrawingContext context)
    {
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);
        MaterialContainer.Draw(context, Bounds.Size, corners, _colors.ContainerColor);

        DrawHeadline(context);
        DrawGrid(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(_tokens.DaySize * DaysPerWeek, HeadlineHeight + _tokens.DaySize * (MaxWeeks + 1));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DayAt(e.GetPosition(this)) is not { } day)
            return;

        SelectedDate = day;
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedDateProperty && SelectedDate is { } selected)
            DisplayMonth = new DateTime(selected.Year, selected.Month, 1);
    }

    private void DrawHeadline(DrawingContext context)
    {
        var typography = _theme.Theme.Typography;
        var culture = CultureInfo.CurrentCulture;

        var prompt = Text(
            Headline ?? string.Empty,
            typography.GetTypeScale(_tokens.SupportingTextStyle).FontSize,
            _colors.SupportingTextColor);

        var selection = Text(
            SelectedDate?.ToString("ddd, d MMM", culture) ?? "—",
            typography.GetTypeScale(_tokens.HeadlineTextStyle).FontSize,
            _colors.HeadlineColor);

        context.DrawText(prompt, new Point(24, 16));
        context.DrawText(selection, new Point(24, 16 + prompt.Height + 8));

        var y = HeadlineHeight - 1;
        context.DrawLine(
            new ImmutablePen(_colors.DividerColor, 1),
            new Point(0, y),
            new Point(Bounds.Width, y));
    }

    private void DrawGrid(DrawingContext context)
    {
        var culture = CultureInfo.CurrentCulture;
        var typography = _theme.Theme.Typography;
        var daySize = _tokens.DaySize;
        var grid = GridArea;
        var fontSize = typography.GetTypeScale(_tokens.DayTextStyle).FontSize;

        // The weekday row, starting on whichever day the culture calls first.
        var firstDay = (int)culture.DateTimeFormat.FirstDayOfWeek;
        for (var i = 0; i < DaysPerWeek; i++)
        {
            var day = (DayOfWeek)((firstDay + i) % DaysPerWeek);
            var name = culture.DateTimeFormat.GetShortestDayName(day);
            var text = Text(name, fontSize, _colors.WeekdayColor);

            context.DrawText(text, new Point(
                grid.X + i * daySize + (daySize - text.Width) / 2,
                grid.Y + (daySize - text.Height) / 2));
        }

        var month = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        var lead = ((int)month.DayOfWeek - firstDay + DaysPerWeek) % DaysPerWeek;
        var days = DateTime.DaysInMonth(month.Year, month.Month);

        for (var day = 1; day <= days; day++)
        {
            var index = lead + day - 1;
            var cell = new Rect(
                grid.X + index % DaysPerWeek * daySize,
                grid.Y + (index / DaysPerWeek + 1) * daySize,
                daySize,
                daySize);

            var date = month.AddDays(day - 1);
            var isSelected = SelectedDate?.Date == date.Date;
            var isToday = date.Date == DateTime.Today;

            if (isSelected)
            {
                var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.DayShape, cell.Size);
                using (context.PushTransform(Matrix.CreateTranslation(cell.X, cell.Y)))
                    MaterialContainer.Draw(context, cell.Size, corners, _colors.SelectedDayContainerColor);
            }
            else if (isToday)
            {
                // Today is a ring rather than a fill, so it cannot be mistaken for the selection.
                var radius = daySize / 2 - _tokens.TodayOutlineWidth / 2;
                context.DrawEllipse(
                    null,
                    new ImmutablePen(_colors.TodayOutlineColor, _tokens.TodayOutlineWidth),
                    cell.Center,
                    radius,
                    radius);
            }

            var text = Text(
                day.ToString(culture),
                fontSize,
                isSelected ? _colors.SelectedDayColor : _colors.DayColor);

            context.DrawText(text, new Point(
                cell.X + (cell.Width - text.Width) / 2,
                cell.Y + (cell.Height - text.Height) / 2));
        }
    }

    /// <summary>Which date, if any, is under a point.</summary>
    private DateTime? DayAt(Point point)
    {
        var grid = GridArea;
        var daySize = _tokens.DaySize;

        // The first row is the weekday header, not a week.
        if (!grid.Contains(point) || point.Y < grid.Y + daySize)
            return null;

        var column = (int)((point.X - grid.X) / daySize);
        var row = (int)((point.Y - grid.Y) / daySize) - 1;

        var month = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        var firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var lead = ((int)month.DayOfWeek - firstDay + DaysPerWeek) % DaysPerWeek;

        var day = row * DaysPerWeek + column - lead + 1;

        return day >= 1 && day <= DateTime.DaysInMonth(month.Year, month.Month)
            ? month.AddDays(day - 1)
            : null;
    }

    private FormattedText Text(string value, double size, IImmutableBrush brush)
    {
        var typography = _theme.Theme.Typography;

        return new FormattedText(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.DayTextStyle)),
            size,
            brush);
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetPickerTokens(PickerVariant.Date);
        _colors = _theme.Theme.GetPickerColors(PickerVariant.Date, _theme.IsDark);

        InvalidateMeasure();
        InvalidateVisual();
    }
}
