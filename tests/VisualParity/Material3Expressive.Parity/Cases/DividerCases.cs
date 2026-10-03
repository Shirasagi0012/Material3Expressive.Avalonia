using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DividerConfig(string Orientation = "horizontal", double? Thickness = null, string? Color = null,
    bool Dark = false, bool Rtl = false, double Width = 160, double Height = 120,
    double? Length = null, double? CrossSize = null, bool Inset = false, bool Unbounded = false, string Action = "none");

[ParitySuite("divider")]
public sealed class DividerCases
{
    public static IEnumerable<CaseData<DividerConfig>> StaticConfigurations()
    {
        foreach (var orientation in new[] { "horizontal", "vertical" })
        {
            foreach (var dark in new[] { false, true })
                yield return Data($"static.default.{orientation}.{(dark ? "dark" : "light")}", new(orientation, Dark: dark));
            foreach (var (id, thickness) in new[] { ("hairline", 0d), ("subpixel", 0.1), ("half-pixel", 0.125),
                ("fractional", 0.3), ("round-up", 0.375), ("round-up-large", 0.625), ("fractional-wide", 1.3), ("thick", 4d), ("negative", -1d) })
                yield return Data($"static.thickness.{orientation}.{id}", new(orientation, thickness));
            foreach (var (id, color) in new[] { ("opaque", "#126E60"), ("alpha", "#70336699"), ("transparent", "#00336699") })
                yield return Data($"static.color.{orientation}.{id}", new(orientation, 2, color));
            yield return Data($"static.layout.{orientation}.short", new(orientation, Length: 37));
            yield return Data($"static.layout.{orientation}.zero-length", new(orientation, Length: 0));
            yield return Data($"static.layout.{orientation}.explicit-cross", new(orientation, CrossSize: 20));
            yield return Data($"static.layout.{orientation}.constrained", new(orientation, 4,
                Width: orientation == "vertical" ? 0.5 : 160, Height: orientation == "horizontal" ? 0.5 : 120));
            yield return Data($"static.layout.{orientation}.zero-cross", new(orientation, 4,
                Width: orientation == "vertical" ? 0 : 160, Height: orientation == "horizontal" ? 0 : 120));
            yield return Data($"static.layout.{orientation}.inset", new(orientation, Inset: true));
            yield return Data($"static.layout.{orientation}.unbounded", new(orientation, Unbounded: true));
            // No state/input branches exist upstream; RTL is selected on both layout paths.
            yield return Data($"static.rtl.{orientation}", new(orientation, 1.3, Rtl: true, Length: 37, CrossSize: 20, Inset: true));
        }
    }

    public static IEnumerable<CaseData<DividerConfig>> DynamicConfigurations()
    {
        foreach (var orientation in new[] { "horizontal", "vertical" })
        foreach (var action in new[] { "theme", "thickness", "color", "resize", "orientation" })
            yield return Data($"dynamic.{action}.{orientation}", new(orientation, Action: action));
    }

    static CaseData<DividerConfig> Data(string id, DividerConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 200, 160);
        var scene = new DividerScene(host, writer.Metadata.ReadConfig<DividerConfig>());
        host.Mount(scene.Root); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<DividerConfig>();
        await using var host = new AvaloniaHost(writer, 200, 160);
        var scene = new DividerScene(host, config);
        host.Mount(scene.Root); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        void Change(long time, string id, Action change)
        {
            host.AdvanceTo(time); change(); writer.Record(time, "mutation", id);
            host.Draw(); host.Frame(time); Sample(id);
        }
        Sample("initial");
        switch (config.Action)
        {
            case "theme":
                Change(1000, "dark", () => CaseDefaults.SetThemeColors(host, true));
                Change(2000, "light", () => CaseDefaults.SetThemeColors(host, false)); break;
            case "thickness":
                Change(1000, "hairline", () => MaterialDivider.SetThickness(scene.Divider, 0));
                Change(2000, "thick", () => MaterialDivider.SetThickness(scene.Divider, 4));
                Change(3000, "fractional", () => MaterialDivider.SetThickness(scene.Divider, 0.3));
                Change(4000, "default", () => scene.Divider.ClearValue(MaterialDivider.ThicknessProperty)); break;
            case "color":
                Change(1000, "override", () => MaterialDivider.SetColor(scene.Divider, Color.Parse("#70336699")));
                Change(2000, "theme-under-override", () => CaseDefaults.SetThemeColors(host, true));
                Change(3000, "clear-override", () => scene.Divider.ClearValue(MaterialDivider.ColorProperty)); break;
            case "resize":
                Change(1000, "smaller", () => { scene.Container.Width = 80; scene.Container.Height = 64; });
                Change(2000, "restore", () => { scene.Container.Width = 160; scene.Container.Height = 120; }); break;
            case "orientation":
                Change(1000, "switched", () => MaterialDivider.SetOrientation(scene.Divider,
                    config.Orientation == "horizontal" ? Orientation.Vertical : Orientation.Horizontal));
                Change(2000, "restored", () => MaterialDivider.SetOrientation(scene.Divider,
                    config.Orientation == "horizontal" ? Orientation.Horizontal : Orientation.Vertical)); break;
        }
        // No animations: advancing event time alone must preserve the final layout. Self-test
        // also adds a passive image here, checking that extra captures do not alter later facts.
        host.AdvanceTo(5000); Sample("idle-layout", image: false);
    }

    sealed class DividerScene
    {
        readonly AvaloniaHost host;
        public Separator Divider { get; } = new();
        public Grid Container { get; }
        public Control Root { get; }

        public DividerScene(AvaloniaHost host, DividerConfig config)
        {
            this.host = host;
            CaseDefaults.SetThemeColors(host, config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            var vertical = config.Orientation == "vertical";
            MaterialDivider.SetOrientation(Divider, vertical ? Orientation.Vertical : Orientation.Horizontal);
            if (config.Thickness is { } thickness) MaterialDivider.SetThickness(Divider, thickness);
            if (config.Color is { } color) MaterialDivider.SetColor(Divider, Color.Parse(color));
            if (config.Length is { } length) { if (vertical) Divider.Height = length; else Divider.Width = length; }
            if (config.CrossSize is { } cross) { if (vertical) Divider.Width = cross; else Divider.Height = cross; }
            if (config.Inset) Divider.Margin = new Thickness(12, 8, 20, 16);
            Container = new Grid { Width = config.Width, Height = config.Height,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16, 16, 0, 0) };
            if (config.Unbounded)
                Container.Children.Add(new StackPanel { Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal, Children = { Divider } });
            else Container.Children.Add(Divider);
            Root = new Grid { Children = { Container } };
        }

        // Geometry observes the product Separator, including automatic thickness and fill constraints.
        // Raster verifies the real stroke/color; configured inputs are not submitted as observed paint.
        public ProbePoint[] Measure() => [new("divider", "divider", null,
            AvaloniaMeasurements.Geometry(Divider, host.Window).Where(m => m.Name is "box" or "size").ToArray())];
    }
}
