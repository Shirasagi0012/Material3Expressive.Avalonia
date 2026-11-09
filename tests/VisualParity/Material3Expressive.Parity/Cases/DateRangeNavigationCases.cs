using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DateRangeNavigationConfig(int Month = 2, bool Rtl = false, bool Dark = false, string Culture = "en-US");

[ParitySuite("date-range-navigation")]
public sealed class DateRangeNavigationCases
{
    public static IEnumerable<CaseData<DateRangeNavigationConfig>> Configurations()
    {
        foreach (var month in new[] { 1, 2, 12 })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.month-{month}.{(rtl ? "rtl" : "ltr")}", "Range calendar month actions", new(month, rtl));
        yield return new("dynamic.german-dark", "Localized calendar actions", new(Dark: true, Culture: "de-DE"));
        yield return new("dynamic.chinese", "Localized calendar actions", new(Culture: "zh-CN"));
    }

    [ParityScenario("navigate", Data = nameof(Configurations))]
    public async Task Navigate(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DateRangeNavigationConfig>();
        await using var host = new AvaloniaHost(writer, 440, 680);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var picker = new DateRangePicker
        {
            Width = 400, MinYear = 2024, MaxYear = 2024, DisplayedMonth = new(2024, c.Month, 1),
            Culture = CultureInfo.GetCultureInfo(c.Culture), SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20)),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new(16)
        };
        host.Mount(new Grid { Children = { picker } }); host.Frame(0);
        for (long t = 16000; t <= 512000; t += 16000) host.Frame(t);
        bool accepted = false;
        void Sample(string id)
        {
            var heading = picker.GetVisualDescendants().OfType<DateRangeMonthHeading>().First();
            var actions = ControlAutomationPeer.CreatePeerForElement(heading)!.GetChildren()!;
            writer.Submit(host.Snapshot(id, id, [new ProbePoint("calendar", "date-range-calendar", null, [
                Measurement.Numeric("month", picker.DisplayedMonth!.Value.DayNumber, "day", definition: "date-picker.displayed-month-day-number"),
                Measurement.Numeric("start", picker.SelectedRange.Start!.Value.DayNumber, "day", definition: "date-picker.selected-start-day-number"),
                Measurement.Numeric("end", picker.SelectedRange.End!.Value.DayNumber, "day", definition: "date-picker.selected-end-day-number"),
                Measurement.Text("previous-action", actions[0].GetName()!, definition: "date-range.previous-action-name"),
                Measurement.Text("next-action", actions[1].GetName()!, definition: "date-range.next-action-name"),
                Measurement.Flag("accepted", accepted, definition: "date-range.month-action-accepted")
            ])]));
        }
        Sample("initial");
        void Action(long time, bool next)
        {
            host.AdvanceTo(time);
            accepted = next ? picker.TryScrollToNextMonth() : picker.TryScrollToPreviousMonth();
            writer.Record(time, "action", next ? "next month" : "previous month");
            host.Draw(); host.Frame(time); host.Frame(time + 16000); Sample(next ? $"next-{time}" : $"previous-{time}");
        }
        Action(600000, false); Action(800000, true); Action(1000000, true);
        host.AdvanceTo(1200000); picker.DisplayedMonth = new(2024, 7, 1);
        writer.Record(1200000, "mutation", "displayed July without rebuilding calendar");
        host.Draw(); host.Frame(1200000); host.Frame(1216000); Sample("external-month");
        Action(1400000, false);
    }
}
