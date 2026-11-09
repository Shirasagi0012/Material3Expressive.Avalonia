using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class DateTimePickerDemo : Window
{
    public DateTimePickerDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Date.SelectedDate=DateOnly.FromDateTime(DateTime.Today);
        Range.SelectedRange=new(Date.SelectedDate,Date.SelectedDate?.AddDays(7));
        Dial.SelectedTime=new(14,30);
    }
    private void ToggleTheme(object? sender,RoutedEventArgs e)=>RequestedThemeVariant=ActualThemeVariant==ThemeVariant.Dark?ThemeVariant.Light:ThemeVariant.Dark;
    private void ToggleDirection(object? sender,RoutedEventArgs e)=>FlowDirection=FlowDirection==FlowDirection.LeftToRight?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
    private void ToggleClock(object? sender,RoutedEventArgs e)=>Dial.Is24Hour=!Dial.Is24Hour;
    private void PreviousRangeMonth(object? sender, RoutedEventArgs e) => Range.TryScrollToPreviousMonth();
    private void NextRangeMonth(object? sender, RoutedEventArgs e) => Range.TryScrollToNextMonth();
    private void ToggleDateLanguage(object? sender, RoutedEventArgs e)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(Date.Culture.Name == "zh-CN" ? "en-US" : "zh-CN");
        Date.Culture = culture;
        Range.Culture = culture;
    }
    private void ToggleDateFormatter(object? sender, RoutedEventArgs e)
    {
        var formatter = Date.DateFormatter is null ? new IsoDateFormatter() : null;
        Date.DateFormatter = formatter; Range.DateFormatter = formatter;
    }
    private sealed class IsoDateFormatter : Components.IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? month, System.Globalization.CultureInfo culture) => month?.ToString("yyyy-MM", culture);
        public string? FormatDate(DateOnly? date, System.Globalization.CultureInfo culture, bool forContentDescription = false) =>
            date?.ToString(forContentDescription ? "D" : "yyyy-MM-dd", culture);
    }
    private void ToggleEntryMode(object? sender, RoutedEventArgs e)
    {
        Uncontained.IsVisible = !Uncontained.IsVisible;
        UncontainedScroll.IsVisible = !Uncontained.IsVisible;
    }
}
