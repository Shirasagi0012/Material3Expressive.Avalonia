using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Parity;

public sealed record DateFormatterConfig(bool Range = false, bool Input = false, bool Selected = true,
    bool NullResult = false, string Culture = "en-US", bool Dynamic = false);

[ParitySuite("date-formatter")]
public sealed class DateFormatterCases
{
    public static IEnumerable<CaseData<DateFormatterConfig>> Cases()
    {
        foreach (var range in new[] { false, true })
        foreach (var input in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var empty in new[] { false, true })
        foreach (var culture in new[] { "en-US", "de-DE" })
            yield return new($"static.{range}.{input}.{selected}.{empty}.{culture}".ToLowerInvariant(), "Custom date presentation and null fallback",
                new(range, input, selected, empty, culture));
        foreach (var range in new[] { false, true })
        foreach (var input in new[] { false, true })
            yield return new($"dynamic.{range}.{input}".ToLowerInvariant(), "Replace formatter without changing selection or input mode", new(range, input, Dynamic: true));
    }
    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DateFormatterConfig>();
        await using var host = new AvaloniaHost(writer, 440, 680);
        CaseDefaults.SetThemeColors(host, false);
        TemplatedControl picker = c.Range ? new DateRangePicker() : new DatePicker();
        picker.SetValue(DatePicker.CultureProperty, CultureInfo.GetCultureInfo(c.Culture));
        picker.SetValue(DatePicker.MinYearProperty, 2024); picker.SetValue(DatePicker.MaxYearProperty, 2024);
        picker.SetValue(DatePicker.DisplayedMonthProperty, new DateOnly(2024, 2, 1));
        picker.SetValue(DatePicker.DisplayModeProperty, c.Input ? DatePickerDisplayMode.Input : DatePickerDisplayMode.Picker);
        picker.SetValue(DatePicker.DateFormatterProperty, new Formatter("F", c.NullResult));
        if (c.Selected)
        {
            if (picker is DateRangePicker range) range.SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20));
            else ((DatePicker)picker).SelectedDate = new(2024, 2, 10);
        }
        picker.Width = 400; picker.HorizontalAlignment = HorizontalAlignment.Left; picker.VerticalAlignment = VerticalAlignment.Top;
        host.Mount(new Grid { Background = Brushes.White, Margin = new Thickness(16), Children = { picker } });
        host.Frame(0);
        void Sample(string id)
        {
            var headline = picker.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Headline");
            var selected = picker is DateRangePicker range ? range.SelectedRange : new DateRange(((DatePicker)picker).SelectedDate, null);
            writer.Submit(host.Snapshot(id, id, [new("picker", "picker", null,
            [Measurement.Text("headlineDescription", AutomationProperties.GetName(headline) ?? throw new InvalidOperationException("Missing default headline description."), definition: "date-picker.headline-description"),
                Measurement.Numeric("start", selected.Start?.DayNumber ?? -1, "day", definition: "date-picker.selected-start-day-number"),
                Measurement.Numeric("end", selected.End?.DayNumber ?? -1, "day", definition: "date-picker.selected-end-day-number")])]));
        }
        Sample("initial");
        for (long time = 16000; time <= 512000; time += 16000) host.Frame(time);
        Sample("settled");
        if (!c.Dynamic) return;
        void Change(long time, string id, IDatePickerFormatter formatter)
        { host.AdvanceTo(time); picker.SetValue(DatePicker.DateFormatterProperty, formatter); host.Frame(time); Sample(id); }
        Change(528000, "replacement", new Formatter("G", false));
        Change(544000, "null-results", new Formatter("G", true));
        Change(560000, "restored", new Formatter("F", false));
    }
    private sealed class Formatter(string prefix, bool empty) : IDatePickerFormatter
    {
        public string? FormatMonthYear(DateOnly? month, CultureInfo culture) => empty ? null : month?.ToString($"'{prefix} M:'yyyy-MM", CultureInfo.InvariantCulture);
        public string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false) => empty ? null
            : date?.ToString($"'{prefix} {(forContentDescription ? "D" : "V") }:'yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
