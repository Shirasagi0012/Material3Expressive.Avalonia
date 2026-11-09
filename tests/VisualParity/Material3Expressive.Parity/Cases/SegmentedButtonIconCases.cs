using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record SegmentedButtonIconConfig(string Content = "check", bool Active = false, bool Rtl = false,
    double? Constraint = null, string Action = "none");

[ParitySuite("segmented-button-icon")]
public sealed class SegmentedButtonIconCases
{
    public static IEnumerable<CaseData<SegmentedButtonIconConfig>> StaticConfigurations()
    {
        foreach (var content in new[] { "check", "custom", "swap" })
        foreach (var active in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.{content}.{(active ? "active" : "inactive")}.{(rtl ? "rtl" : "ltr")}", new(content, active, rtl));
        foreach (var content in new[] { "check", "custom", "swap" })
            yield return Data($"static.constrained.{content}", new(content, true, Constraint: 12));
    }
    public static IEnumerable<CaseData<SegmentedButtonIconConfig>> DynamicConfigurations()
    {
        foreach (var content in new[] { "check", "custom", "swap" })
        foreach (var action in new[] { "selection", "reverse", "content", "reattach" })
            yield return Data($"dynamic.{action}.{content}", new(content, Action: action));
        yield return Data("dynamic.reverse.swap-rtl", new("swap", Rtl: true, Action: "reverse"));
        yield return Data("dynamic.selection.check-rtl", new("check", Rtl: true, Action: "selection"));
        yield return Data("dynamic.selection.custom-rtl", new("custom", Rtl: true, Action: "selection"));
        yield return Data("dynamic.reverse.custom-rtl", new("custom", Rtl: true, Action: "reverse"));
    }
    private static CaseData<SegmentedButtonIconConfig> Data(string id, SegmentedButtonIconConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 180, 100);
        var scene = new Scene(host, writer.Metadata.ReadConfig<SegmentedButtonIconConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SegmentedButtonIconConfig>();
        await using var host = new AvaloniaHost(writer, 180, 100);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        Change(1000, "active", () => scene.Icon.IsActive = true);
        Times(16000, 32000, 64000);
        if (config.Action == "reverse")
        {
            Change(65000, "reverse", () => scene.Icon.IsActive = false); Times(80000, 144000);
            Change(145000, "active-again", () => scene.Icon.IsActive = true);
        }
        else if (config.Action == "content")
            Change(65000, "replace-active", () => scene.Icon.ActiveContent = new Marker(new Size(32, 20)));
        else if (config.Action == "reattach")
        {
            Change(65000, "detach", () => scene.Container.Children.Remove(scene.Icon));
            Change(81000, "reattach", () => scene.Container.Children.Add(scene.Icon));
        }
        Times(160000, 400000);
        Change(401000, "inactive", () => scene.Icon.IsActive = false);
        Times(416000, 480000, 640000, 800000);
    }
    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        public SegmentedButtonIcon Icon { get; }
        public Grid Container { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, SegmentedButtonIconConfig config)
        {
            _host = host; CaseDefaults.SetThemeColors(host, false);
            Icon = new SegmentedButtonIcon { IsActive = config.Active, Foreground = Brush.Parse("#245A80"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            if (config.Content != "check") Icon.ActiveContent = new Marker(new Size(24, 14));
            if (config.Content == "swap") Icon.InactiveContent = new Marker(new Size(10, 22));
            Container = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Children = { Icon } };
            if (config.Constraint is { } constraint) { Container.Width = constraint; Container.Height = constraint; }
            Root = new Grid { Children = { Container } };
        }
        public ProbePoint[] Measure() => Icon.Parent is null ? [] : [new("icon", "selection-icon", null,
            AvaloniaMeasurements.Geometry(Icon, _host.Window).Where(m => m.Name is "box" or "size").ToArray())];
    }
    private sealed class Marker(Size preferred) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        static Marker() => AffectsRender<Marker>(Avalonia.Controls.Documents.TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(preferred.Width, availableSize.Width), Math.Min(preferred.Height, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(Avalonia.Controls.Documents.TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
