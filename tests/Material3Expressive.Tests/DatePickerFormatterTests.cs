using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Tests;

public class DatePickerFormatterTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void FormatterUpdatesHeadlinesMonthsAndDescriptionsWithoutReplacingCalendar(bool range)
    {
        TemplatedControl picker = range ? new DateRangePicker { SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20)) }
            : new DatePicker { SelectedDate = new(2024, 2, 10) };
        picker.SetValue(DatePicker.CultureProperty, CultureInfo.GetCultureInfo("en-US"));
        picker.SetValue(DatePicker.MinYearProperty, 2024); picker.SetValue(DatePicker.MaxYearProperty, 2024);
        picker.SetValue(DatePicker.DisplayedMonthProperty, new DateOnly(2024, 2, 1));
        picker.SetValue(DatePicker.DateFormatterProperty, new Formatter("A"));
        using var clock = new MotionTestClockScope(); var window = Show(picker);
        try
        {
            var headline = picker.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Headline");
            Assert.Equal(range ? "A10 – A20" : "A10", headline.Content);
            Assert.Contains("A-description-10", AutomationProperties.GetName(headline));
            var day = picker.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Tag, new DateOnly(2024, 2, 10)));
            var calendar = day.GetVisualAncestors().OfType<DateMonthPanel>().Single();
            Assert.Equal("A-description-10", AutomationProperties.GetName(day));
            picker.SetValue(DatePicker.DateFormatterProperty, new Formatter("B")); Flush(window);
            Assert.Equal(range ? "B10 – B20" : "B10", headline.Content);
            Assert.Equal("B-description-10", AutomationProperties.GetName(day));
            Assert.Contains(calendar, picker.GetVisualDescendants());
            if (range) Assert.Contains(picker.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "B-month-2");
            else Assert.Contains(picker.GetVisualDescendants().OfType<Button>(), b => Equals(b.Content, "B-month-2"));
            picker.ClearValue(DatePicker.DateFormatterProperty); Flush(window);
            Assert.DoesNotContain("B-description", AutomationProperties.GetName(day));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NullFormatterResultsUsePlaceholdersButEmptyStringsRemainEmpty()
    {
        var picker = new DatePicker { SelectedDate = new(2024, 2, 10), DateFormatter = new Formatter(null),
            Culture = CultureInfo.GetCultureInfo("en-US") };
        using var clock = new MotionTestClockScope(); var window = Show(picker);
        try
        {
            var headline = picker.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Headline");
            Assert.Equal("Selected date", headline.Content);
            Assert.Contains(picker.GetVisualDescendants().OfType<Button>(), b => Equals(b.Content, "-"));
            picker.DateFormatter = new EmptyFormatter(); Flush(window);
            Assert.Equal("", headline.Content);
            picker.Headline = "Application heading"; Flush(window);
            Assert.Equal("Application heading", headline.Content);
            Assert.Null(AutomationProperties.GetName(headline));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ChangingFormatterPreservesInputDraftFocusAndSelectionAndUpdatesRejectedDateMessage()
    {
        var picker = new DatePicker { DisplayMode = DatePickerDisplayMode.Input, Culture = CultureInfo.GetCultureInfo("en-US"),
            DateFormatter = new Formatter("A"), IsDateSelectable = _ => false };
        using var clock = new MotionTestClockScope(); var window = Show(picker);
        try
        {
            var box = picker.GetVisualDescendants().OfType<TextBox>().Single();
            box.Text = "02/10/2024"; Flush(window); box.Focus(); box.SelectionStart = 3; box.SelectionEnd = 5;
            Assert.Contains("A10", MaterialTextField.GetSupportingText(box)?.ToString());
            picker.DateFormatter = new Formatter("B"); Flush(window);
            Assert.Same(box, picker.GetVisualDescendants().OfType<TextBox>().Single());
            Assert.Equal("02/10/2024", box.Text); Assert.True(box.IsFocused);
            Assert.Equal(3, box.SelectionStart); Assert.Equal(5, box.SelectionEnd);
            Assert.Contains("B10", MaterialTextField.GetSupportingText(box)?.ToString());
            Assert.Null(picker.SelectedDate);
        }
        finally { window.Close(); }
    }

    private sealed class Formatter(string? prefix) : IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? month, CultureInfo culture) => prefix is null || month is null ? null : $"{prefix}-month-{month.Value.Month}";
        public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false) => prefix is null || date is null ? null
            : forContentDescription ? $"{prefix}-description-{date.Value.Day}" : $"{prefix}{date.Value.Day}";
    }
    private sealed class EmptyFormatter : IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? month, CultureInfo culture) => "";
        public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false) => "";
    }
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 700, Height = 700 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
