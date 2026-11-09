using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Parity;

public sealed record FloatingToolbarTransitionConfig(bool Horizontal = true, string Alignment = "start",
    bool Rtl = false, bool Visible = false, string Action = "cycle");

[ParitySuite("floating-toolbar-transition")]
public sealed class FloatingToolbarTransitionCases
{
    public static IEnumerable<CaseData<FloatingToolbarTransitionConfig>> StaticConfigurations()
    {
        foreach (var horizontal in new[] { true, false })
        foreach (var alignment in new[] { "start", "center", "end" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.{horizontal}.{alignment}.{rtl}", new(horizontal, alignment, rtl, true));
    }
    public static IEnumerable<CaseData<FloatingToolbarTransitionConfig>> DynamicConfigurations()
    {
        foreach (var horizontal in new[] { true, false })
        {
            foreach (var alignment in new[] { "start", "center", "end" })
            foreach (var rtl in new[] { false, true })
                yield return Data($"dynamic.cycle.{horizontal}.{alignment}.{rtl}", new(horizontal, alignment, rtl));
            foreach (var rtl in new[] { false, true })
                yield return Data($"dynamic.reverse.{horizontal}.{rtl}", new(horizontal, Rtl: rtl, Action: "reverse"));
            foreach (var action in new[] { "content", "theme", "reattach" })
                yield return Data($"dynamic.{action}.{horizontal}", new(horizontal, Action: action));
        }
    }
    private static CaseData<FloatingToolbarTransitionConfig> Data(string id, FloatingToolbarTransitionConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 180, 120);
        var scene = new Scene(host, writer.Metadata.ReadConfig<FloatingToolbarTransitionConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<FloatingToolbarTransitionConfig>();
        await using var host = new AvaloniaHost(writer, 180, 120);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        Change(1000, "visible", () => scene.Visibility.Visible = true); Times(16000, 32000, 64000);
        if (config.Action == "reverse")
        {
            Change(65000, "reverse", () => scene.Visibility.Visible = false); Times(80000, 144000);
            Change(145000, "visible-again", () => scene.Visibility.Visible = true);
        }
        else if (config.Action == "content") Change(65000, "replace", scene.Replace);
        else if (config.Action == "theme") Change(65000, "theme", () => scene.Configure(new MaterialSpring(1, 400)));
        else if (config.Action == "reattach")
        {
            Change(65000, "detach", () => scene.Root.Children.Remove(scene.Visibility));
            Change(81000, "reattach", () => scene.Root.Children.Add(scene.Visibility));
        }
        Times(160000, 400000);
        Change(401000, "hidden", () => scene.Visibility.Visible = false); Times(416000, 480000, 640000, 800000);
        Change(801000, "return", () => scene.Visibility.Visible = true); Times(816000, 880000, 1000000, 1400000);
    }

    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly FloatingToolbarTransitionConfig _config;
        public AnimatedVisibility Visibility { get; }
        public Grid Root { get; }
        public Scene(AvaloniaHost host, FloatingToolbarTransitionConfig config)
        {
            _host = host; _config = config;
            Visibility = new AnimatedVisibility { Visible = config.Visible, Content = Marker(80, 40), TargetCommitPhase = AnimationCommitPhase.AfterFrame,
                FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Configure(Material3Expressive.Foundation.Motion.MotionScheme.Expressive.FastSpatial);
            Root = new Grid { Children = { Visibility } };
        }
        public void Replace() => Visibility.Content = Marker(110, 28);
        private static Control Marker(double width, double height) => new DirectionMarker(new Size(width, height));
        public void Configure(MaterialSpring spring)
        {
            var horizontal = _config.Alignment switch { "start" => VisibilityHorizontalAlignment.Start, "end" => VisibilityHorizontalAlignment.End, _ => VisibilityHorizontalAlignment.Center };
            var vertical = _config.Alignment switch { "start" => VerticalAlignment.Top, "end" => VerticalAlignment.Bottom, _ => VerticalAlignment.Center };
            Visibility.Enter = _config.Horizontal ? FloatingToolbarDefaults.HorizontalEnterTransition(Visibility, horizontal, spring)
                : FloatingToolbarDefaults.VerticalEnterTransition(Visibility, vertical, spring);
            Visibility.Exit = _config.Horizontal ? FloatingToolbarDefaults.HorizontalExitTransition(Visibility, horizontal, spring)
                : FloatingToolbarDefaults.VerticalExitTransition(Visibility, vertical, spring);
        }
        public ProbePoint[] Measure() => Visibility.Parent is null ? [] : [new("visibility", "toolbar-transition", null,
            AvaloniaMeasurements.Geometry(Visibility, _host.Window).Where(m => m.Name is "box" or "size").ToArray())];
    }

    private sealed class DirectionMarker(Size preferred) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(preferred.Width, availableSize.Width), Math.Min(preferred.Height, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(Brush.Parse("#205080"), null, new Rect(Bounds.Size));
            context.DrawRectangle(Brush.Parse("#D06020"), null, new Rect(0, 0, 8, Bounds.Height));
            context.DrawRectangle(Brush.Parse("#30A080"), null, new Rect(0, Math.Max(0, Bounds.Height - 6), Bounds.Width, 6));
        }
    }
}
