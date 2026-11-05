using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record BadgeBaselineConfig(bool Tooltip = false, bool Rtl = false, bool Nested = false, double? Height = null);

[ParitySuite("badge-baseline")]
public sealed class BadgeBaselineCases
{
    public static IEnumerable<CaseData<BadgeBaselineConfig>> Configurations()
    {
        foreach (var tooltip in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var nested in new[] { false, true })
        foreach (var height in new double?[] { null, 64 })
        {
            var id = $"{(tooltip ? "tooltip" : "snackbar")}.{(rtl ? "rtl" : "ltr")}.{(nested ? "nested" : "single")}.{(height is null ? "natural" : "tall")}";
            yield return new(id, id, new(tooltip, rtl, nested, height));
        }
    }

    [ParityScenario("baseline", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<BadgeBaselineConfig>();
        await using var host = new AvaloniaHost(writer, 400, 240);
        CaseDefaults.SetThemeColors(host, false);
        // Match the fixed lightColorScheme inputs, as in SnackbarCases/RichTooltipCases;
        // CaseDefaults sets only the common button palette, not these container roles.
        host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse("#322F35");
        host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse("#F5EFF7");
        host.Window.Resources[Sys.Color.InversePrimary] = Color.Parse("#D0BCFF");
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse("#F3EDF7");
        host.Window.Resources[Sys.Color.Surface] = Color.Parse("#FEF7FF");
        host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse("#6750A4");
        host.Window.Resources[Sys.Color.Shadow] = Colors.Black;
        host.Window.Resources[Sys.Color.Error] = Color.Parse("#B3261E");
        host.Window.Resources[Sys.Color.OnError] = Colors.White;
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var anchor = new Marker(60, 32, Colors.Blue);
        var badgeMarker = new Marker(8, 16, Colors.Red);
        MaterialTextBaseline.SetFirst(anchor, 10); MaterialTextBaseline.SetLast(anchor, 26);
        MaterialTextBaseline.SetFirst(badgeMarker, 1); MaterialTextBaseline.SetLast(badgeMarker, 15);
        var box = new BadgedBox { Content = anchor, Badge = badgeMarker };
        if (config.Nested) box = new BadgedBox { Content = box, Badge = new Badge() };
        if (config.Height is { } height) box.Height = height;
        var other = new Marker(24, 24, Colors.Green);
        Control root = config.Tooltip
            ? new RichTooltip { Title = box, Content = other }
            : new Snackbar { Content = box, Action = other, ActionOnNewLine = true, Width = 320 };
        host.Mount(CaseDefaults.Center(root)); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray();
            var baselines = MaterialTextBaseline.Read(box, 4);
            writer.Submit(host.Snapshot(id, id,
            [
                new("host", "host", null, Geometry(root)),
                new("box", "box", "host", [.. Geometry(box),
                    Measurement.Numeric("first", baselines.First, "px", definition: "badge.anchor-first-baseline", source: "MaterialTextBaseline.Read(BadgedBox, density)"),
                    Measurement.Numeric("last", baselines.Last, "px", definition: "badge.anchor-last-baseline", source: "MaterialTextBaseline.Read(BadgedBox, density)")]),
                new("anchor", "anchor", "box", Geometry(anchor)),
                new("badge", "badge", "box", Geometry(badgeMarker)),
                new("other", "other", "host", Geometry(other))
            ]));
        }
        void Change(long time, string id, Action change)
        {
            host.AdvanceTo(time); change(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id);
        }
        Sample("initial");
        Change(1000, "first", () => MaterialTextBaseline.SetFirst(anchor, 6));
        Change(2000, "last", () => MaterialTextBaseline.SetLast(anchor, 20));
        Change(3000, "badge-only", () => MaterialTextBaseline.SetFirst(badgeMarker, 30));
        Change(4000, "no-anchor-lines", () => { MaterialTextBaseline.SetFirst(anchor, null); MaterialTextBaseline.SetLast(anchor, null); });
        Change(5000, "restore", () => { MaterialTextBaseline.SetFirst(anchor, 10); MaterialTextBaseline.SetLast(anchor, 26); });
    }

    private sealed class Marker(double width, double height, Color color) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(new SolidColorBrush(color), new Rect(Bounds.Size));
    }
}
