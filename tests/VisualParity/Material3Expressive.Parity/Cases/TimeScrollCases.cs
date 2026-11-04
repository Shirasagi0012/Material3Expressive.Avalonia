using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TimeScrollConfig(bool Clock24 = true, bool Toggle = false, bool Dark = false,
    bool Rtl = false, string Action = "none");

[ParitySuite("time-scroll")]
public sealed class TimeScrollCases
{
    public static IEnumerable<CaseData<TimeScrollConfig>> Configurations()
    {
        foreach (var clock24 in new[] { false, true })
        foreach (var toggle in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{(clock24 ? "24h" : "12h")}.{(toggle ? "toggle" : "contained")}.{(dark ? "dark" : "light")}",
                "scroll field typography, page height and palette", new(clock24, toggle, dark));
        yield return new("static.rtl", "RTL time scroll placement", new(Rtl: true));
        foreach (var action in new[] { "selection", "reverse", "theme", "detach" })
            yield return new($"dynamic.{action}", "per-page animation and lifecycle", new(Action: action));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimeScrollConfig>();
        await using var host = new AvaloniaHost(writer, 420, 240);
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[Sys.Color.SurfaceContainerLowest] = Color.Parse(dark ? "#0F0D13" : "#FFFFFF");
        }
        Theme(c.Dark);
        var picker = new TimeScroll { SelectedTime = new(8, 30), Is24Hour = c.Clock24,
            Margin = new Thickness(16), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Toggle) picker.Toggle = new Border { Width = 24, Height = 24, Background = Brushes.Magenta };
        var root = new Grid { Children = { picker } };
        host.Mount(root); host.Frame(0);
        void Sample(string id)
        {
            var attached = picker.IsAttachedToVisualTree();
            var points = new List<ProbePoint> { new("picker", "time-scroll", null, [
                Measurement.Flag("attached", attached, definition: "time-scroll.attached"),
                Measurement.Numeric("hour", picker.SelectedTime.Hour, "hour", definition: "time-picker.selected-hour"),
                Measurement.Numeric("minute", picker.SelectedTime.Minute, "minute", definition: "time-picker.selected-minute")]) };
            if (attached)
            {
                points.Add(new("layout", "time-scroll-layout", "picker", AvaloniaMeasurements.Geometry(picker, host.Window).ToArray()));
                var panel = picker.GetVisualDescendants().OfType<TimeScrollPanel>().Single(p => p.Name == "PART_HourScroll");
                foreach (var item in panel.Children.Cast<ListBoxItem>().Where(item => item.Tag is 8 or 9))
                {
                    var color = ((ISolidColorBrush)item.Foreground!).Color;
                    points.Add(new($"hour-{item.Tag}", "scroll-item", "picker", [
                        Measurement.Numeric("fontSize", item.FontSize, "sp", category: "text", definition: "time-scroll.item.font-size"),
                        Measurement.Numeric("fontWeight", (int)item.FontWeight, "weight", category: "text", definition: "time-scroll.item.font-weight"),
                        Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "rgba8", "none", "properties", definition: "time-scroll.item.color")
                    ]));
                }
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "none") return;
        Change(1000, "select-nine", () => picker.SelectedTime = new(9, 30));
        At(16000, 32000, 64000);
        if (c.Action == "reverse") Change(65000, "reverse-eight", () => picker.SelectedTime = new(8, 30));
        if (c.Action == "theme") Change(65000, "dark-theme", () => Theme(true));
        if (c.Action == "detach")
        {
            Change(65000, "detach", () => root.Children.Clear());
            At(80000);
            Change(81000, "reattach", () => root.Children.Add(picker));
        }
        At(96000, 160000, 320000, 640000);
    }
}
