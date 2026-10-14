using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Material3Expressive.Components;

/// <summary>Material date entry content. Opening, dismissal and committing to a form are application-owned.</summary>
public class DatePicker : TemplatedControl
{
    public static readonly StyledProperty<DatePickerDisplayMode> DisplayModeProperty =
        AvaloniaProperty.Register<DatePicker, DatePickerDisplayMode>(nameof(DisplayMode), DatePickerDisplayMode.Picker);

    public static readonly StyledProperty<DateOnly?> DisplayedMonthProperty =
        AvaloniaProperty.Register<DatePicker, DateOnly?>(nameof(DisplayedMonth), null);

    public static readonly StyledProperty<int> MinYearProperty =
        AvaloniaProperty.Register<DatePicker, int>(nameof(MinYear), 1900);

    public static readonly StyledProperty<int> MaxYearProperty =
        AvaloniaProperty.Register<DatePicker, int>(nameof(MaxYear), 2100);

    public static readonly StyledProperty<bool> ShowModeToggleProperty =
        AvaloniaProperty.Register<DatePicker, bool>(nameof(ShowModeToggle), true);

    public static readonly StyledProperty<bool> ShowHeadlineProperty =
        AvaloniaProperty.Register<DatePicker, bool>(nameof(ShowHeadline), true);

    public static readonly StyledProperty<CultureInfo> CultureProperty =
        AvaloniaProperty.Register<DatePicker, CultureInfo>(nameof(Culture), CultureInfo.CurrentCulture);

    public static readonly StyledProperty<Func<DateOnly, bool>?> IsDateSelectableProperty =
        AvaloniaProperty.Register<DatePicker, Func<DateOnly, bool>?>(nameof(IsDateSelectable), null);

    public static readonly StyledProperty<Func<int, bool>?> IsYearSelectableProperty =
        AvaloniaProperty.Register<DatePicker, Func<int, bool>?>(nameof(IsYearSelectable), null);

    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<DatePicker, object?>(nameof(Title), null);

    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        AvaloniaProperty.Register<DatePicker, IDataTemplate?>(nameof(TitleTemplate), null);

    public static readonly StyledProperty<object?> HeadlineProperty =
        AvaloniaProperty.Register<DatePicker, object?>(nameof(Headline), null);

    public static readonly StyledProperty<IDataTemplate?> HeadlineTemplateProperty =
        AvaloniaProperty.Register<DatePicker, IDataTemplate?>(nameof(HeadlineTemplate), null);

    public static readonly StyledProperty<DateOnly?> SelectedDateProperty =
        AvaloniaProperty.Register<DatePicker, DateOnly?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

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

    public DateOnly? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    internal DateEntryController Controller { get; }

    public DatePicker()
    {
        Controller = new DateEntryController(this, false);
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
