using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Material3Expressive.Components;

/// <summary>Material date entry content. Opening, dismissal and committing to a form are application-owned.</summary>
public class DateRangePicker : TemplatedControl
{
    public static readonly StyledProperty<DatePickerDisplayMode> DisplayModeProperty =
        DatePicker.DisplayModeProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<DateOnly?> DisplayedMonthProperty =
        DatePicker.DisplayedMonthProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<int> MinYearProperty =
        DatePicker.MinYearProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<int> MaxYearProperty =
        DatePicker.MaxYearProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<bool> ShowModeToggleProperty =
        DatePicker.ShowModeToggleProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<bool> ShowHeadlineProperty =
        DatePicker.ShowHeadlineProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<CultureInfo> CultureProperty =
        DatePicker.CultureProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<Func<DateOnly, bool>?> IsDateSelectableProperty =
        DatePicker.IsDateSelectableProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<Func<int, bool>?> IsYearSelectableProperty =
        DatePicker.IsYearSelectableProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<object?> TitleProperty =
        DatePicker.TitleProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        DatePicker.TitleTemplateProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<object?> HeadlineProperty =
        DatePicker.HeadlineProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<IDataTemplate?> HeadlineTemplateProperty =
        DatePicker.HeadlineTemplateProperty.AddOwner<DateRangePicker>();

    public static readonly StyledProperty<DateRange> SelectedRangeProperty =
        AvaloniaProperty.Register<DateRangePicker, DateRange>(nameof(SelectedRange), defaultBindingMode: BindingMode.TwoWay);

    public DatePickerDisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public DateOnly? DisplayedMonth
    {
        get => GetValue(DisplayedMonthProperty);
        set => SetValue(DisplayedMonthProperty, value);
    }

    public int MinYear
    {
        get => GetValue(MinYearProperty);
        set => SetValue(MinYearProperty, value);
    }

    public int MaxYear
    {
        get => GetValue(MaxYearProperty);
        set => SetValue(MaxYearProperty, value);
    }

    public bool ShowModeToggle
    {
        get => GetValue(ShowModeToggleProperty);
        set => SetValue(ShowModeToggleProperty, value);
    }

    public bool ShowHeadline
    {
        get => GetValue(ShowHeadlineProperty);
        set => SetValue(ShowHeadlineProperty, value);
    }

    public CultureInfo Culture
    {
        get => GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public Func<DateOnly, bool>? IsDateSelectable
    {
        get => GetValue(IsDateSelectableProperty);
        set => SetValue(IsDateSelectableProperty, value);
    }

    public Func<int, bool>? IsYearSelectable
    {
        get => GetValue(IsYearSelectableProperty);
        set => SetValue(IsYearSelectableProperty, value);
    }

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    public object? Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public IDataTemplate? HeadlineTemplate
    {
        get => GetValue(HeadlineTemplateProperty);
        set => SetValue(HeadlineTemplateProperty, value);
    }

    public DateRange SelectedRange
    {
        get => GetValue(SelectedRangeProperty);
        set => SetValue(SelectedRangeProperty, value);
    }

    internal DateEntryController Controller { get; }

    public DateRangePicker()
    {
        Controller = new DateEntryController(this, true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Controller.Attach(e.NameScope);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        Controller?.Changed(change.Property);
    }
}
