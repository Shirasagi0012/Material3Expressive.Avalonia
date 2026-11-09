using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ProgressStopIndicatorConfig(string Mode = "standalone", string Cap = "round", bool Rtl = false,
    double Width = 120, double Height = 20, double Stop = 8, bool Bypass = false, bool Dynamic = false);

[ParitySuite("progress-stop-indicator")]
public sealed class ProgressStopIndicatorCases
{
    public static IEnumerable<CaseData<ProgressStopIndicatorConfig>> Cases()
    {
        foreach (var cap in new[] { "round", "flat", "square" })
        foreach (var rtl in new[] { false, true })
        {
            var suffix = $"{cap}.{(rtl ? "rtl" : "ltr")}";
            yield return new($"static.standalone.{suffix}", "Standalone default stop", new(Cap: cap, Rtl: rtl));
            yield return new($"static.tall.{suffix}", "Trailing padding limited to six DIP", new(Cap: cap, Rtl: rtl, Height: 40));
            yield return new($"static.clamped.{suffix}", "Stop clamped to actual height", new(Cap: cap, Rtl: rtl, Height: 4));
            yield return new($"static.narrow.{suffix}", "Actual drawing may overflow a narrow owner", new(Cap: cap, Rtl: rtl, Width: 4, Height: 12));
            yield return new($"static.zero.{suffix}", "Zero stop size", new(Cap: cap, Rtl: rtl, Stop: 0));
            yield return new($"static.callback.{suffix}", "Compose two stop markers after the real progress", new("callback", cap, rtl));
        }
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            yield return new($"static.bypass.{direction}", "Owner bypasses Avalonia automatic mirroring", new(Rtl: rtl, Bypass: true));
            foreach (var mode in new[] { "standalone", "callback" })
                yield return new($"dynamic.{mode}.{direction}", "Live size, color, alpha, direction, drawing replacement and reattachment",
                    new(mode, Rtl: rtl, Dynamic: true));
        }
    }

    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<ProgressStopIndicatorConfig>();
        await using var host = new AvaloniaHost(writer, 240, 120);
        var stop = c.Stop; var color = Colors.Black; var cap = ParseCap(c.Cap);
        var standalone = new StopHost(c.Bypass) { StopSize = stop, Color = color, Cap = cap };
        var indicator = new LinearProgressIndicator { Color = Colors.Blue, TrackColor = Color.Parse("#CCCCCC"),
            Progress = .75, StopSize = stop, StrokeCap = cap };
        Control target = c.Mode == "standalone" ? standalone : indicator;
        target.Width = c.Width; target.Height = c.Height;
        var root = (Grid)CaseDefaults.Center(target); root.Background = Brushes.White;
        RenderOptions.SetRequiresFullOpacityHandling(root, true); // Same whole-owner alpha as Compose graphicsLayer.
        root.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        void SetCallback() => indicator.DrawStopIndicator = (context, _) =>
        {
            ProgressIndicatorDefaults.DrawStopIndicator(context, indicator, stop, color, cap);
            ProgressIndicatorDefaults.DrawStopIndicator(context, indicator, stop / 2, Colors.Red, PenLineCap.Round);
        };
        SetCallback(); host.Mount(root); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("owner", "owner", null, AvaloniaMeasurements.Geometry(target, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action)
        { host.AdvanceTo(time); action(); target.InvalidateVisual(); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "clamped-size", () => { target.Height = 6; stop = 14; standalone.StopSize = stop; });
        Change(17000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
        Change(33000, "color-opacity", () => { color = Color.Parse("#804488CC"); standalone.Color = color; target.Opacity = .5; });
        Change(49000, "shape-size", () => { target.Height = 28; target.Width = 80; stop = 10; cap = PenLineCap.Square;
            standalone.StopSize = stop; standalone.Cap = cap; indicator.StrokeCap = cap; });
        Change(65000, "detach-reattach", () => { root.Children.Remove(target); host.Draw(); root.Children.Add(target); });
        if (c.Mode == "callback")
        {
            Change(81000, "no-stop", () => indicator.DrawStopIndicator = (_, _) => { });
            Change(97000, "default-stop", () => { indicator.StopSize = 4; indicator.DrawStopIndicator = null; });
            Change(113000, "restore-composite", SetCallback);
        }
    }
    private static PenLineCap ParseCap(string cap) => cap switch { "flat" => PenLineCap.Flat, "square" => PenLineCap.Square, _ => PenLineCap.Round };
    private sealed class StopHost(bool bypass) : Control
    {
        protected override bool BypassFlowDirectionPolicies => bypass;
        public double StopSize;
        public Color Color;
        public PenLineCap Cap;
        public override void Render(DrawingContext context) => ProgressIndicatorDefaults.DrawStopIndicator(context, this, StopSize, Color, Cap);
    }
}
