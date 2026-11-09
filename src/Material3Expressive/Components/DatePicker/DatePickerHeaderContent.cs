// DatePickerDefaults / DateRangePickerDefaults title and headline composables.
// Fixed AndroidX DatePicker.kt / DateRangePicker.kt; Apache-2.0.
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>Localized default date-picker title for a custom header or dialog.</summary>
/// <remarks>Typography and padding come from the containing layout, as in DatePickerDefaults.DatePickerTitle.</remarks>
public class DatePickerTitle : TextBlock
{
    public static readonly StyledProperty<DatePickerDisplayMode> DisplayModeProperty = DatePicker.DisplayModeProperty.AddOwner<DatePickerTitle>();
    public static readonly StyledProperty<CultureInfo> CultureProperty = DatePicker.CultureProperty.AddOwner<DatePickerTitle>();
    internal static readonly DirectProperty<DatePickerTitle, string> LocalizedTitleProperty =
        AvaloniaProperty.RegisterDirect<DatePickerTitle, string>(nameof(LocalizedTitle), x => x.LocalizedTitle);
    private string _localizedTitle = "";
    internal string LocalizedTitle => _localizedTitle;
    protected virtual bool IsRange => false;
    public DatePickerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public DatePickerTitle() => UpdateTitle();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayModeProperty || change.Property == CultureProperty) UpdateTitle();
    }
    private void UpdateTitle() => SetAndRaise(LocalizedTitleProperty, ref _localizedTitle,
        DateTimeStrings.Get(DisplayMode == DatePickerDisplayMode.Input
            ? IsRange ? "DateRangeInputTitle" : "DateInputTitle"
            : IsRange ? "DateRangePickerTitle" : "DatePickerTitle", Culture));
}

/// <summary>Localized title for a date-range picker or custom range header.</summary>
public sealed class DateRangePickerTitle : DatePickerTitle
{
    protected override bool IsRange => true;
    protected override Type StyleKeyOverride => typeof(DatePickerTitle);
}

/// <summary>Single-line selected-date headline, with a localized placeholder and polite accessible description.</summary>
public sealed class DatePickerHeadline : TextBlock
{
    public static readonly StyledProperty<DatePickerDisplayMode> DisplayModeProperty = DatePicker.DisplayModeProperty.AddOwner<DatePickerHeadline>();
    public static readonly StyledProperty<CultureInfo> CultureProperty = DatePicker.CultureProperty.AddOwner<DatePickerHeadline>();
    public static readonly StyledProperty<DateOnly?> SelectedDateProperty = DatePicker.SelectedDateProperty.AddOwner<DatePickerHeadline>();
    public static readonly StyledProperty<IDatePickerFormatter?> DateFormatterProperty = DatePicker.DateFormatterProperty.AddOwner<DatePickerHeadline>();
    internal static readonly DirectProperty<DatePickerHeadline, string> HeadlineTextProperty =
        AvaloniaProperty.RegisterDirect<DatePickerHeadline, string>(nameof(HeadlineText), x => x.HeadlineText);
    private string _headlineText = "";
    internal string HeadlineText => _headlineText;
    public DatePickerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public DateOnly? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
    public IDatePickerFormatter? DateFormatter { get => GetValue(DateFormatterProperty); set => SetValue(DateFormatterProperty, value); }
    public DatePickerHeadline() => UpdateHeadline();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayModeProperty || change.Property == CultureProperty ||
            change.Property == SelectedDateProperty || change.Property == DateFormatterProperty) UpdateHeadline();
    }
    private void UpdateHeadline()
    {
        var input = DisplayMode == DatePickerDisplayMode.Input;
        var text = DatePickerHeaderText.Format(SelectedDate, Culture, DateFormatter);
        var description = DatePickerHeaderText.Format(SelectedDate, Culture, DateFormatter, true)
            ?? DateTimeStrings.Get(input ? "DateInputNoInputDescription" : "DatePickerNoSelectionDescription", Culture);
        SetAndRaise(HeadlineTextProperty, ref _headlineText,
            text ?? DateTimeStrings.Get(input ? "DateInputHeadline" : "DatePickerHeadline", Culture));
        AutomationProperties.SetName(this, DateTimeStrings.Get(input ? "DateInputHeadlineDescription" : "DatePickerHeadlineDescription", Culture, description));
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    }
}

/// <summary>Separate start, delimiter and end date headline content, with a merged polite accessible description.</summary>
/// <remarks>Selection is presentation input only; unlike DateRangePicker, this content does not normalize it.</remarks>
public sealed class DateRangePickerHeadline : TemplatedControl
{
    public static readonly StyledProperty<DatePickerDisplayMode> DisplayModeProperty = DatePicker.DisplayModeProperty.AddOwner<DateRangePickerHeadline>();
    public static readonly StyledProperty<CultureInfo> CultureProperty = DatePicker.CultureProperty.AddOwner<DateRangePickerHeadline>();
    public static readonly StyledProperty<DateRange> SelectedRangeProperty = DateRangePicker.SelectedRangeProperty.AddOwner<DateRangePickerHeadline>();
    public static readonly StyledProperty<IDatePickerFormatter?> DateFormatterProperty = DatePicker.DateFormatterProperty.AddOwner<DateRangePickerHeadline>();
    internal static readonly DirectProperty<DateRangePickerHeadline, string> StartTextProperty =
        AvaloniaProperty.RegisterDirect<DateRangePickerHeadline, string>(nameof(StartText), x => x.StartText);
    internal static readonly DirectProperty<DateRangePickerHeadline, string> EndTextProperty =
        AvaloniaProperty.RegisterDirect<DateRangePickerHeadline, string>(nameof(EndText), x => x.EndText);
    private string _startText = "", _endText = "";
    internal string StartText => _startText;
    internal string EndText => _endText;
    public DatePickerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public DateRange SelectedRange { get => GetValue(SelectedRangeProperty); set => SetValue(SelectedRangeProperty, value); }
    public IDatePickerFormatter? DateFormatter { get => GetValue(DateFormatterProperty); set => SetValue(DateFormatterProperty, value); }
    public DateRangePickerHeadline() => UpdateHeadline();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayModeProperty || change.Property == CultureProperty ||
            change.Property == SelectedRangeProperty || change.Property == DateFormatterProperty) UpdateHeadline();
    }
    private void UpdateHeadline()
    {
        var start = DatePickerHeaderText.Format(SelectedRange.Start, Culture, DateFormatter);
        var end = DatePickerHeaderText.Format(SelectedRange.End, Culture, DateFormatter);
        var empty = DateTimeStrings.Get(DisplayMode == DatePickerDisplayMode.Input ? "DateInputNoInputDescription" : "DatePickerNoSelectionDescription", Culture);
        var startDescription = DatePickerHeaderText.Format(SelectedRange.Start, Culture, DateFormatter, true) ?? empty;
        var endDescription = DatePickerHeaderText.Format(SelectedRange.End, Culture, DateFormatter, true) ?? empty;
        var startLabel = DateTimeStrings.Get("DateRangePickerStartHeadline", Culture);
        var endLabel = DateTimeStrings.Get("DateRangePickerEndHeadline", Culture);
        SetAndRaise(StartTextProperty, ref _startText, start ?? startLabel);
        SetAndRaise(EndTextProperty, ref _endText, end ?? endLabel);
        AutomationProperties.SetName(this, $"{startLabel}: {startDescription}, {endLabel}: {endDescription}");
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new HeadlinePeer(this);
    private sealed class HeadlinePeer(DateRangePickerHeadline owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
    }
}

internal static class DatePickerHeaderText
{
    internal static string? Format(DateOnly? date, CultureInfo culture, IDatePickerFormatter? formatter, bool description = false) =>
        formatter is not null ? formatter.FormatDate(date, culture, description)
            : date?.ToString(description ? "D" : "ddd, MMM d", culture);
}

// The fixed Row measures its unweighted children in order with the remaining width.
// Horizontal StackPanel measures with infinity, which would lose wrapping in a narrow custom header.
internal sealed class DateRangeHeadlinePanel : Panel
{
    private static readonly MaterialHorizontalArrangement Arrangement = MaterialHorizontalArrangement.SpacedBy(4);
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d;
        var height = 0d;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            child.Measure(new Size(Math.Max(0, availableSize.Width - width), availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            if (index < Children.Count - 1) width += Math.Min(4, Math.Max(0, availableSize.Width - width));
        }
        return new Size(width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = LayoutHelper.GetLayoutScale(this);
        var sizes = Children.Select(child => MathUtils.DipToPx(child.DesiredSize.Width, density)).ToArray();
        var positions = new int[sizes.Length];
        // Avalonia mirrors this panel and its children together. Supply LTR positions
        // here, retaining the children's own RTL text direction without a second flip.
        Arrangement.Arrange(density, MathUtils.DipToPx(finalSize.Width, density), sizes, FlowDirection.LeftToRight, positions);
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var size = child.DesiredSize;
            var top = MathUtils.RoundToInt((MathUtils.DipToPx(finalSize.Height, density) - MathUtils.DipToPx(size.Height, density)) / 2f) / density;
            child.Arrange(new Rect(positions[index] / density, top, size.Width, size.Height));
        }
        return finalSize;
    }
}
