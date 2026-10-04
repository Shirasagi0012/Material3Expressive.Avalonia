using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using MaterialShapes;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record LoadingIndicatorConfig(bool Contained = false, double Progress = .5, bool Indeterminate = false,
    bool Dark = false, bool Rtl = false, string Polygons = "default", string Shape = "full",
    double? Width = null, double? Height = null, double? ConstraintWidth = null, double? ConstraintHeight = null,
    string? Color = null, string? ContainerColor = null, string Action = "none");

[ParitySuite("loading-indicator")]
public sealed class LoadingIndicatorCases
{
    public static IEnumerable<CaseData<LoadingIndicatorConfig>> StaticConfigurations()
    {
        foreach (var contained in new[] { false, true })
        {
            var kind = contained ? "contained" : "plain";
            foreach (var progress in new[] { -.5, 0, .15, .5, .85, 1, 1.5 })
                yield return Data($"static.progress.{kind}.{progress.ToString(System.Globalization.CultureInfo.InvariantCulture)}", new(contained, progress));
            yield return Data($"static.theme.{kind}.dark", new(contained, Dark: true));
            yield return Data($"static.direction.{kind}.rtl", new(contained, Rtl: true));
            yield return Data($"static.color.{kind}.custom", new(contained, Color: "#806750A4", ContainerColor: "#FF00CCAA"));
            foreach (var (id, width, height) in new[] { ("wide", 80d, 24d), ("tall", 24d, 80d),
                ("large", 90d, 90d), ("fractional", 37.25, 24.5), ("zero", 0d, 0d) })
                yield return Data($"static.size.{kind}.{id}", new(contained, Width: width, Height: height));
            foreach (var (id, width, height) in new[] { ("small", 20d, 12d), ("wide", 80d, 20d), ("tall", 12d, 80d) })
                yield return Data($"static.constraint.{kind}.{id}", new(contained, ConstraintWidth: width, ConstraintHeight: height));
            // Three shapes exercise both morph intervals, their shared boundary and final endpoint.
            foreach (var progress in new[] { 0, .25, .5, .75, 1 })
                yield return Data($"static.polygons.{kind}.{progress.ToString(System.Globalization.CultureInfo.InvariantCulture)}", new(contained, progress, Polygons: "three"));
        }
        // ContainerShape is public on the contained upstream overload. Non-square outlines also
        // exercise the square active shape's alignment and clipping without fixture compensation.
        foreach (var shape in new[] { "round", "asymmetric", "triangle", "ellipse" })
            foreach (var rtl in new[] { false, true })
                yield return Data($"static.shape.contained.{shape}.{(rtl ? "rtl" : "ltr")}", new(true, .75, Rtl: rtl, Shape: shape, Width: 80, Height: 24));
    }

    public static IEnumerable<CaseData<LoadingIndicatorConfig>> DynamicConfigurations()
    {
        foreach (var contained in new[] { false, true })
        {
            var kind = contained ? "contained" : "plain";
            foreach (var action in new[] { "indeterminate", "progress", "mode", "polygons", "theme", "detach", "sparse" })
                yield return Data($"dynamic.{action}.{kind}", new(contained, Indeterminate: action is not "progress" and not "mode", Action: action));
            yield return Data($"dynamic.indeterminate.{kind}.custom", new(contained, Indeterminate: true,
                Polygons: "three", Rtl: true, Dark: true, Width: 90, Height: 60, Action: "indeterminate"));
        }
    }
    static CaseData<LoadingIndicatorConfig> Data(string id, LoadingIndicatorConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 320, 160, new ManualTimeProvider());
        var scene = new Scene(host, writer.Metadata.ReadConfig<LoadingIndicatorConfig>());
        host.Mount(scene.Root); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<LoadingIndicatorConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160, new ManualTimeProvider());
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(0);
        long frame = 0;
        void Advance(long time)
        {
            if (config.Action != "sparse")
                while (frame + 16000 <= time) { frame += 16000; host.Frame(frame); }
            host.AdvanceTo(time); host.Draw();
        }
        void Sample(long time, string id) { Advance(time); writer.Submit(host.Snapshot(id, id, scene.Measure())); }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", id); host.Draw();
            // Both hosts resolve recomposition and newly created animation waiters at the
            // same logical time. These are two batches, with no elapsed time between them.
            host.Frame(time); host.Frame(time);
            writer.Submit(host.Snapshot(id, id, scene.Measure()));
        }
        Sample(0, "initial");
        switch (config.Action)
        {
            case "progress":
                Change(200000, "progress-85", () => scene.Indicator.Progress = .85);
                Change(400000, "progress-10", () => scene.Indicator.Progress = .1);
                Change(600000, "progress-100", () => scene.Indicator.Progress = 1); break;
            case "mode":
                Change(200000, "indeterminate", () => scene.Indicator.IsIndeterminate = true);
                Sample(280000, "morphing"); Sample(900000, "second-morph");
                Change(1000000, "determinate", () => scene.Indicator.IsIndeterminate = false);
                Change(1200000, "indeterminate-again", () => scene.Indicator.IsIndeterminate = true);
                Sample(1280000, "fresh-morph"); break;
            case "polygons":
                Sample(80000, "before-change");
                Change(100000, "two-shapes-during-spring", () => scene.Indicator.Polygons = Scene.PolygonSequence("two"));
                Sample(180000, "retained-progress"); Sample(740000, "next-morph");
                Change(1100000, "three-shapes", () => scene.Indicator.Polygons = Scene.PolygonSequence("three"));
                Sample(1450000, "changed-sequence");
                Change(1600000, "equal-list", () => scene.Indicator.Polygons = scene.Indicator.Polygons!.ToArray());
                Change(1800000, "default-sequence", () => scene.Indicator.Polygons = null);
                Sample(1880000, "default-retained-progress"); Sample(2500000, "default-next-morph"); break;
            case "theme":
                Change(200000, "dark", () => scene.SetTheme(true));
                Change(400000, "override", () => scene.SetColor(Colors.Red));
                Change(600000, "light-under-override", () => scene.SetTheme(false));
                Change(800000, "clear-override", () => scene.SetColor(null)); break;
            case "detach":
                Sample(80000, "morphing"); Change(100000, "detached", () => scene.SetPresent(false));
                Sample(800000, "detached-late"); Change(1000000, "reattached", () => scene.SetPresent(true));
                Sample(1080000, "fresh-state"); Sample(1700000, "fresh-second-morph"); break;
            case "sparse":
                foreach (var time in new long[] { 16000, 1000000, 1100000, 2000000, 2100000, 3500000 })
                { host.Frame(time); writer.Submit(host.Snapshot($"frame-{time}", "sparse frame", scene.Measure())); }
                break;
            default:
                foreach (var time in new long[] { 80000, 160000, 256000, 320000, 640000, 650000, 656000, 736000,
                    1300000, 1384000, 2600000, 3900000, 4550000, 4640000, 4666000, 4672000, 5200000, 9100000 })
                    Sample(time, $"time-{time}"); break;
        }
    }

    sealed class Scene
    {
        readonly AvaloniaHost host;
        readonly Grid owner;
        bool present = true;
        public LoadingIndicatorBase Indicator { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, LoadingIndicatorConfig config)
        {
            this.host = host; SetTheme(config.Dark);
            Indicator = config.Contained ? new ContainedLoadingIndicator() : new LoadingIndicator();
            Indicator.Progress = config.Progress; Indicator.IsIndeterminate = config.Indeterminate;
            Indicator.Polygons = PolygonSequence(config.Polygons);
            if (config.Rtl) Indicator.FlowDirection = FlowDirection.RightToLeft;
            if (config.Width is { } width) Indicator.Width = width;
            if (config.Height is { } height) Indicator.Height = height;
            if (config.Color is { } color) SetColor(Color.Parse(color));
            if (Indicator is ContainedLoadingIndicator contained)
            {
                if (config.ContainerColor is { } background) contained.ContainerColor = Color.Parse(background);
                switch (config.Shape)
                {
                    case "round": contained.CornerRadius = new(12); break;
                    case "asymmetric": contained.CornerRadius = new(24, 4, 8, 0); break;
                    case "ellipse": contained.ContainerShape = (size, _) => new EllipseGeometry(new Rect(size)); break;
                    case "triangle": contained.ContainerShape = (size, flow) =>
                    {
                        var path = new StreamGeometry(); using var pen = path.Open();
                        var rtl = flow == FlowDirection.RightToLeft;
                        pen.BeginFigure(new(rtl ? size.Width : 0, 0), true);
                        pen.LineTo(new(size.Width, size.Height)); pen.LineTo(new(0, size.Height)); pen.EndFigure(true);
                        return path;
                    }; break;
                }
            }
            owner = new Grid { Children = { Indicator } };
            if (config.ConstraintWidth is { } cw && config.ConstraintHeight is { } ch)
            { owner.Width = cw; owner.Height = ch; Root = CaseDefaults.Center(owner); }
            else
            {
                Indicator.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                Indicator.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                Root = owner;
            }
        }
        public static IReadOnlyList<RoundedPolygon>? PolygonSequence(string kind) => kind switch
        {
            "two" => [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst],
            "three" => [MaterialShapeCatalog.Circle, MaterialShapeCatalog.Pentagon, MaterialShapeCatalog.Cookie4Sided],
            _ => null
        };
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse(dark ? "#4F378B" : "#EADDFF");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(dark ? "#EADDFF" : "#21005D");
        }
        public void SetColor(Color? color)
        {
            if (Indicator is ContainedLoadingIndicator contained)
            { if (color is { } value) contained.IndicatorColor = value; else contained.ClearValue(ContainedLoadingIndicator.IndicatorColorProperty); }
            else
            { if (color is { } value) ((LoadingIndicator)Indicator).Color = value; else Indicator.ClearValue(LoadingIndicator.ColorProperty); }
        }
        public void SetPresent(bool value)
        {
            present = value;
            if (value) owner.Children.Add(Indicator); else owner.Children.Remove(Indicator);
        }
        public ProbePoint[] Measure() => present ? [new("indicator", "indicator", null,
            AvaloniaMeasurements.Geometry(Indicator, host.Window).Where(m => m.Name is "box" or "size").ToArray())] : [];
    }
}
