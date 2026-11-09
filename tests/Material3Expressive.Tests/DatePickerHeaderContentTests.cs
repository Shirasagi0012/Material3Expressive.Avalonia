using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Tests;

public class DatePickerHeaderContentTests
{
    [AvaloniaFact]
    public void RangeRowClampsSpacingWhenAnEarlierSlotExhaustsTheWidth()
    {
        var row = new DateRangeHeadlinePanel { Children = {
            new Border { Width = 100, Height = 10 }, new Border { Width = 0, Height = 10 }, new Border { Width = 0, Height = 10 } } };
        row.Measure(new Size(20, 20));
        row.Arrange(new Rect(0, 0, 20, 20));
        Assert.Equal(20, row.Children[1].Bounds.X);
        Assert.Equal(20, row.Children[2].Bounds.X);
    }
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void StandaloneTitlesAndHeadlinesFollowModeCultureAndSelection(bool range, bool input)
    {
        using var clock = new MotionTestClockScope();
        var culture = CultureInfo.GetCultureInfo("en-US");
        var title = range ? new DateRangePickerTitle() : new DatePickerTitle();
        Control headline = range ? new DateRangePickerHeadline() : new DatePickerHeadline();
        var panel = new StackPanel { Children = { title, headline } };
        title.Culture = culture;
        title.DisplayMode = input ? DatePickerDisplayMode.Input : DatePickerDisplayMode.Picker;
        headline.SetValue(DatePicker.CultureProperty, culture);
        headline.SetValue(DatePicker.DisplayModeProperty, title.DisplayMode);
        var window = Show(panel);
        try
        {
            Assert.Equal(DateTimeStrings.Get(input ? range ? "DateRangeInputTitle" : "DateInputTitle" :
                range ? "DateRangePickerTitle" : "DatePickerTitle", culture), title.Text);
            Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(headline));
            Assert.Contains(DateTimeStrings.Get(input ? "DateInputNoInputDescription" : "DatePickerNoSelectionDescription", culture), AutomationProperties.GetName(headline));
            if (headline is DateRangePickerHeadline pair)
            {
                Assert.Equal(new[] { "Start date", "-", "End date" }, Texts(pair));
                pair.DateFormatter = new Formatter();
                pair.SelectedRange = new(null, new(2024, 2, 20));
                Flush(window);
                Assert.Equal(new[] { "Start date", "-", "V20" }, Texts(pair));
                Assert.Empty(ControlAutomationPeer.CreatePeerForElement(pair)!.GetChildren());
            }
            else
            {
                var single = (DatePickerHeadline)headline;
                Assert.Equal(1, single.MaxLines);
                single.DateFormatter = new Formatter(); single.SelectedDate = new(2024, 2, 10);
                Flush(window); Assert.Equal("V10", single.Text);
                Assert.Contains("D10", AutomationProperties.GetName(single));
            }
            title.Culture = CultureInfo.GetCultureInfo("de-DE");
            headline.SetValue(DatePicker.CultureProperty, title.Culture);
            Flush(window);
            Assert.Equal(DateTimeStrings.Get(input ? range ? "DateRangeInputTitle" : "DateInputTitle" :
                range ? "DateRangePickerTitle" : "DatePickerTitle", title.Culture), title.Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void RangeHeadlineUsesThreeSlotsWithFourDipSpacingAndMergedSemantics(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var headline = new DateRangePickerHeadline { Culture = CultureInfo.GetCultureInfo("en-US"),
            SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20)), DateFormatter = new Formatter(),
            Foreground = Brushes.Red, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(headline);
        try
        {
            var texts = headline.GetVisualDescendants().OfType<TextBlock>().ToArray();
            Assert.Equal(new[] { "V10", "-", "V20" }, texts.Select(t => t.Text));
            Assert.All(texts, text => Assert.Equal(Colors.Red, ((ISolidColorBrush)text.Foreground!).Color));
            var positions = texts.Select(t => t.TranslatePoint(default, window)!.Value.X).ToArray();
            Assert.Equal(4, rtl ? positions[0] - positions[1] - texts[1].Bounds.Width : positions[1] - positions[0] - texts[0].Bounds.Width);
            Assert.Equal("Start date: D10, End date: D20", AutomationProperties.GetName(headline));
            headline.DateFormatter = new Formatter(empty: true); Flush(window);
            Assert.Equal(new[] { "", "-", "" }, Texts(headline));
            headline.DateFormatter = new Formatter(nullResult: true); Flush(window);
            Assert.Equal(new[] { "Start date", "-", "End date" }, Texts(headline));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void DefaultPickerReusesPublicContentAndPreservesCustomTemplateData(bool range)
    {
        using var clock = new MotionTestClockScope();
        TemplatedControl picker = range ? new DateRangePicker { SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20)) }
            : new DatePicker { SelectedDate = new(2024, 2, 10) };
        picker.SetValue(DatePicker.CultureProperty, CultureInfo.GetCultureInfo("en-US"));
        picker.SetValue(DatePicker.DateFormatterProperty, new Formatter());
        var window = Show(picker);
        try
        {
            var title = picker.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Title");
            var headline = picker.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Headline");
            Assert.IsAssignableFrom<DatePickerTitle>(title.Content);
            var original = headline.Content;
            Assert.True(range ? original is DateRangePickerHeadline : original is DatePickerHeadline);
            picker.SetValue(DatePicker.HeadlineTemplateProperty, new FuncDataTemplate<object?>((value, _) => new TextBlock { Text = $"Template:{value}" }));
            Flush(window);
            Assert.Equal(range ? "V10 – V20" : "V10", headline.Content);
            Assert.Null(AutomationProperties.GetName(headline));
            picker.SetValue(DatePicker.HeadlineProperty, "Application headline"); Flush(window);
            Assert.Equal("Application headline", headline.Content);
            picker.ClearValue(DatePicker.HeadlineProperty);
            picker.ClearValue(DatePicker.HeadlineTemplateProperty); Flush(window);
            Assert.Same(original, headline.Content);
            Assert.NotNull(AutomationProperties.GetName(headline));
        }
        finally { window.Close(); }
    }

    private sealed class Formatter(bool empty = false, bool nullResult = false) : IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? date, CultureInfo culture) => date?.ToString("Y", culture);
        public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false) =>
            nullResult || date is null ? null : empty ? "" : $"{(forContentDescription ? "D" : "V")}{date.Value.Day}";
    }
    private static string?[] Texts(Control control) => control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 500, Height = 600 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
