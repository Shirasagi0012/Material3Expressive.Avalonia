using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Parity;

public sealed record DateHeaderConfig(bool Range = false, bool Input = false, string Selection = "both",
    string Culture = "en-US", bool Rtl = false, int Width = 360, bool Dynamic = false, bool DefaultFormatter = false);

[ParitySuite("date-header")]
public sealed class DateHeaderCases
{
    public static IEnumerable<CaseData<DateHeaderConfig>> Configurations()
    {
        foreach (var range in new[] { false, true })
        foreach (var input in new[] { false, true })
            yield return new($"static.default.{range}.{input}".ToLowerInvariant(), "Native default date formatting", new(range, input, DefaultFormatter: true));
        foreach (var range in new[] { false, true })
        foreach (var input in new[] { false, true })
        foreach (var selection in new[] { "none", "start", "both", "null", "empty" })
            yield return new($"static.{range}.{input}.{selection}".ToLowerInvariant(), "Public default title and headline content", new(range, input, selection));
        foreach (var range in new[] { false, true })
        {
            yield return new($"static.{range}.de-rtl".ToLowerInvariant(), "Localized RTL content", new(range, Culture: "de-DE", Rtl: true));
            yield return new($"static.{range}.narrow".ToLowerInvariant(), "Limited custom-header width", new(range, Width: 130));
            yield return new($"dynamic.{range}.replace".ToLowerInvariant(), "Replace dates, mode, locale and formatter", new(range, Dynamic: true));
        }
    }

    [ParityScenario("capture", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DateHeaderConfig>();
        await using var host = new AvaloniaHost(writer, 400, 200);
        CaseDefaults.SetThemeColors(host, false);
        var title = c.Range ? new DateRangePickerTitle() : new DatePickerTitle();
        Control headline = c.Range ? new DateRangePickerHeadline() : new DatePickerHeadline();
        MaterialText.SetStyle(title, new TextStyle { FontFamily = new("fonts:Roboto#Roboto"), FontSize = 14, FontWeight = FontWeight.Medium, LineHeight = 20, LetterSpacing = .1 });
        MaterialText.SetStyle(headline, new TextStyle { FontFamily = new("fonts:Roboto#Roboto"), FontSize = c.Range ? 22 : 28, FontWeight = FontWeight.Normal, LineHeight = c.Range ? 28 : 36, LetterSpacing = 0 });
        var root = new StackPanel { Spacing = 8, Width = c.Width, Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children = { title, headline } };
        void Configure(bool input, string selection, string culture, string prefix)
        {
            title.DisplayMode = input ? DatePickerDisplayMode.Input : DatePickerDisplayMode.Picker;
            title.Culture = CultureInfo.GetCultureInfo(culture);
            headline.SetValue(DatePicker.DisplayModeProperty, title.DisplayMode);
            headline.SetValue(DatePicker.CultureProperty, title.Culture);
            headline.SetValue(DatePicker.DateFormatterProperty, c.DefaultFormatter ? null : new Formatter(prefix, selection));
            DateOnly? start = selection == "none" ? null : new(2024, 2, 10);
            DateOnly? end = selection is "none" or "start" ? null : new(2024, 2, 20);
            if (headline is DateRangePickerHeadline pair) pair.SelectedRange = new(start, end);
            else ((DatePickerHeadline)headline).SelectedDate = start;
        }
        Configure(c.Input, c.Selection, c.Culture, "F");
        host.Mount(root); host.Frame(0);
        void Sample(string id)
        {
            var parts = headline is TextBlock text ? new[] { text.Text ?? "" }
                : headline.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
            writer.Submit(host.Snapshot(id, id, [new("header", "date-header", null,
                [Measurement.Text("title", title.Text ?? ""), Measurement.Text("headline", string.Join("|", parts)),
                 Measurement.Text("description", AutomationProperties.GetName(headline)!),
                 Measurement.Flag("polite", AutomationProperties.GetLiveSetting(headline) == AutomationLiveSetting.Polite),
                 Measurement.Numeric("width", root.Bounds.Width, "dip"), Measurement.Numeric("height", root.Bounds.Height, "dip")])]));
        }
        Sample("initial");
        if (!c.Dynamic) return;
        foreach (var (time, mode, selection, culture, prefix) in new[] {
            (16000L, true, "none", "en-US", "F"), (32000L, false, "start", "de-DE", "G"),
            (48000L, true, "empty", "de-DE", "G"), (64000L, false, "both", "en-US", "F") })
        {
            host.AdvanceTo(time); Configure(mode, selection, culture, prefix); host.Frame(time); Sample($"time-{time}");
        }
    }

    private sealed class Formatter(string prefix, string selection) : IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? date, CultureInfo culture) => date?.ToString("Y", culture);
        public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false) =>
            date is null || selection == "null" ? null : selection == "empty" ? "" : $"{prefix}{(forContentDescription ? "D" : "V")}{date.Value.Day}";
    }
}
