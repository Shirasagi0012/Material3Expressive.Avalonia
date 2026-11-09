using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record TabIndicatorDefaultsConfig(bool Primary = true, double IndicatorWidth = 24, double IndicatorHeight = 3,
    double? OuterWidth = null, double? OuterHeight = null, bool UnspecifiedWidth = false,
    string Shape = "default", string? Color = null, bool Rtl = false, bool Dark = false, string Action = "none");

[ParitySuite("tab-indicator-defaults")]
public sealed class TabIndicatorDefaultsCases
{
    public static IEnumerable<CaseData<TabIndicatorDefaultsConfig>> StaticConfigurations()
    {
        foreach (var primary in new[] { true, false })
        foreach (var rtl in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.default.{primary}.{rtl}.{dark}".ToLowerInvariant(), new(Primary: primary, Rtl: rtl, Dark: dark));
        foreach (var primary in new[] { true, false })
        {
            yield return Data($"static.constrained.{primary}".ToLowerInvariant(), new(Primary: primary, OuterWidth: 1, OuterHeight: 1));
            yield return Data($"static.expanded.{primary}".ToLowerInvariant(), new(Primary: primary, OuterWidth: 80, OuterHeight: 12));
            yield return Data($"static.fractional.{primary}".ToLowerInvariant(), new(Primary: primary, IndicatorWidth: 27.375, IndicatorHeight: 3.375));
            yield return Data($"static.zero.{primary}".ToLowerInvariant(), new(Primary: primary, IndicatorWidth: 0, IndicatorHeight: 0));
            yield return Data($"static.color.{primary}".ToLowerInvariant(), new(Primary: primary, Color: "#70336699"));
        }
        yield return Data("static.unspecified", new(OuterWidth: 80, UnspecifiedWidth: true));
        foreach (var shape in new[] { "cut", "percent", "path" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.shape.{shape}.{rtl}".ToLowerInvariant(), new(IndicatorWidth: 64, IndicatorHeight: 14, Shape: shape, Rtl: rtl));
    }
    public static IEnumerable<CaseData<TabIndicatorDefaultsConfig>> DynamicConfigurations()
    {
        foreach (var primary in new[] { true, false })
        foreach (var action in new[] { "size", "theme", "color" })
            yield return Data($"dynamic.{action}.{primary}".ToLowerInvariant(), new(Primary: primary, Action: action));
        foreach (var rtl in new[] { false, true })
            yield return Data($"dynamic.shape.{rtl}".ToLowerInvariant(), new(IndicatorWidth: 64, IndicatorHeight: 14, Rtl: rtl, Action: "shape"));
    }
    private static CaseData<TabIndicatorDefaultsConfig> Data(string id, TabIndicatorDefaultsConfig config) => new(id, id, config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public Task Changes(CaptureWriter writer) => Capture(writer, true);
    private static async Task Capture(CaptureWriter writer, bool changes)
    {
        var config = writer.Metadata.ReadConfig<TabIndicatorDefaultsConfig>();
        await using var host = new AvaloniaHost(writer, 240, 120);
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        CaseDefaults.SetThemeColors(host, config.Dark);
        TemplatedControl indicator = config.Primary ? new PrimaryTabIndicator
        {
            IndicatorWidth = config.UnspecifiedWidth ? double.NaN : config.IndicatorWidth,
            IndicatorHeight = config.IndicatorHeight, Shape = Shape(config.Shape)
        } : new SecondaryTabIndicator { IndicatorHeight = config.IndicatorHeight };
        if (config.OuterWidth is { } w) indicator.Width = w;
        if (config.OuterHeight is { } h) indicator.Height = h;
        if (config.Color is { } color) indicator.Background = new SolidColorBrush(Color.Parse(color));
        host.Mount(new Grid { Margin = new Thickness(16), Children = { indicator } }); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id,
            [new("indicator", "indicator", null, AvaloniaMeasurements.Geometry(indicator, host.Window).Where(x => x.Name is "box" or "size").ToArray())], image));
        void Change(long time, string id, Action action) { host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial"); if (!changes) return;
        switch (config.Action)
        {
            case "size":
                Change(1000, "grow", () => { indicator.SetValue(PrimaryTabIndicator.IndicatorHeightProperty, 12d); if (indicator is PrimaryTabIndicator p) p.IndicatorWidth = 60; });
                Change(2000, "constrain", () => { indicator.Width = 9; indicator.Height = 2; });
                Change(3000, "restore", () => { indicator.ClearValue(Control.WidthProperty); indicator.ClearValue(Control.HeightProperty); indicator.SetValue(PrimaryTabIndicator.IndicatorHeightProperty, 3d); if (indicator is PrimaryTabIndicator p) p.IndicatorWidth = 24; });
                break;
            case "shape":
                Change(1000, "cut", () => ((PrimaryTabIndicator)indicator).Shape = Shape("cut"));
                Change(2000, "path", () => ((PrimaryTabIndicator)indicator).Shape = Shape("path"));
                Change(3000, "restore", () => ((PrimaryTabIndicator)indicator).Shape = null); break;
            case "color":
                Change(1000, "alpha", () => indicator.Background = new SolidColorBrush(Color.Parse("#70336699")));
                Change(2000, "transparent", () => indicator.Background = Brushes.Transparent);
                Change(3000, "restore", () => indicator.ClearValue(TemplatedControl.BackgroundProperty)); break;
            case "theme":
                Change(1000, "dark", () => CaseDefaults.SetThemeColors(host, true));
                Change(2000, "light", () => CaseDefaults.SetThemeColors(host, false)); break;
        }
        host.AdvanceTo(5000); host.Draw(); Sample("passive", false);
    }
    private static MaterialShape? Shape(string name) => name switch
    {
        "cut" => new CutCornerShape { TopStart = new(8, RelativeUnit.Absolute), TopEnd = new(1, RelativeUnit.Absolute), BottomEnd = new(4, RelativeUnit.Absolute) },
        "percent" => new RoundedCornerShape { TopStart = new(.8, RelativeUnit.Relative), BottomEnd = new(.4, RelativeUnit.Relative) },
        "path" => new IndicatorPath(), _ => null
    };
    private sealed class IndicatorPath : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            var points = new[] { new Point(0, size.Height), new Point(size.Width * .2, 0), new Point(size.Width, 0), new Point(size.Width * .8, size.Height) };
            if (layoutDirection == FlowDirection.RightToLeft) points = points.Select(p => new Point(size.Width - p.X, p.Y)).ToArray();
            return new PathGeometry { Figures = new PathFigures { new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = true,
                Segments = new PathSegments(points.Skip(1).Select(p => new LineSegment { Point = p })) } } };
        }
    }
}
