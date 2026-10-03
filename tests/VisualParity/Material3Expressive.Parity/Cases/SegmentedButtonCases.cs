using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SegmentedButtonConfig(bool Multi = false, bool Selected = false, bool Enabled = true,
    string Icon = "check", bool Dark = false, bool Rtl = false, int Count = 3, bool Varied = false,
    double? Width = null, double? Height = null, double LabelHeight = 16, double IconWidth = 18,
    double? Padding = null, double Border = 1, double? Radius = null, bool CustomColors = false, string Action = "none");

[ParitySuite("segmented-button")]
public sealed class SegmentedButtonCases
{
    public static IEnumerable<CaseData<SegmentedButtonConfig>> StaticConfigurations()
    {
        foreach (var multi in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var icon in new[] { "check", "active", "swap", "none" })
            yield return Data($"static.appearance.{(multi ? "multi" : "single")}.{selected}.{enabled}.{icon}", new(multi, selected, enabled, icon));
        // Colors and the desktop theme do not change the single/multi layout; test all four color states once.
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        {
            yield return Data($"static.dark.{selected}.{enabled}", new(Selected: selected, Enabled: enabled, Dark: true));
            yield return Data($"static.colors.{selected}.{enabled}", new(Selected: selected, Enabled: enabled, CustomColors: true));
        }
        yield return Data("static.layout.one", new(Count: 1, Selected: true));
        yield return Data("static.layout.five", new(Count: 5));
        yield return Data("static.layout.varied", new(Varied: true));
        yield return Data("static.layout.width", new(Width: 257));
        yield return Data("static.layout.narrow", new(Width: 96));
        yield return Data("static.layout.short", new(Width: 96, Height: 24));
        yield return Data("static.layout.tall-label", new(LabelHeight: 48));
        yield return Data("static.layout.wide-icon", new(Icon: "swap", IconWidth: 36));
        yield return Data("static.layout.padding-border", new(Selected: true, Padding: 20, Border: 2));
        yield return Data("static.layout.radius", new(Radius: 8));
        yield return Data("static.rtl.check", new(Selected: true, Rtl: true, Varied: true));
        yield return Data("static.rtl.swap", new(Icon: "swap", Rtl: true, Varied: true));
    }
    public static IEnumerable<CaseData<SegmentedButtonConfig>> DynamicConfigurations()
    {
        foreach (var icon in new[] { "check", "active", "swap" })
        foreach (var action in new[] { "selection", "reverse" })
            yield return Data($"dynamic.{action}.{icon}", new(Icon: icon, Action: action));
        foreach (var multi in new[] { false, true })
            yield return Data($"dynamic.click.{(multi ? "multi" : "single")}", new(Multi: multi, Action: "click"));
        foreach (var action in new[] { "press", "quick", "cancel", "disable", "disabled-input", "hover", "space", "enter", "theme", "content", "shape", "icon" })
            yield return Data($"dynamic.{action}", new(Enabled: action != "disabled-input", Action: action));
        yield return Data("dynamic.rtl.selection", new(Rtl: true, Action: "selection"));
        foreach (var selected in new[] { false, true })
            yield return Data($"dynamic.content-color.{selected}", new(Selected: selected, Action: "press-content"));
    }
    private static CaseData<SegmentedButtonConfig> Data(string id, SegmentedButtonConfig config) => new(id.ToLowerInvariant(), id, config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SegmentedButtonConfig>();
        await using var host = new AvaloniaHost(writer, 520, 208); var scene = new Scene(host, c);
        host.Mount(CaseDefaults.Center(scene.Row)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SegmentedButtonConfig>();
        await using var host = new AvaloniaHost(writer, 520, 208); var s = new Scene(host, c);
        host.Mount(CaseDefaults.Center(s.Row)); host.Frame(-1000); host.Frame(0);
        var nextFrame = 16000L;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, s.Measure(), image: id != "time-32000"));
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        // Event frames commit Compose recomposition before reading state; both ends receive
        // these frames in addition to the unchanged continuous 16ms grid.
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Input(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var item = s.Items[0]; var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(515, 203); Sample("initial");
        switch (c.Action)
        {
            case "selection": case "reverse":
                Mutate(1000, "selected", () => item.IsSelected = true); Times(16000, 32000, 64000);
                if (c.Action == "reverse") Mutate(65000, "reverse", () => item.IsSelected = false);
                Times(80000, 160000, 400000, 800000); break;
            case "theme": Mutate(1000, "dark", () => s.SetTheme(true)); Times(16000, 400000); break;
            case "content":
                Mutate(1000, "grow", () => s.Labels[0].PreferredSize = new Size(80, 48)); Times(16000, 64000);
                Mutate(65000, "shrink", () => s.Labels[0].PreferredSize = new Size(32, 16)); Times(80000, 400000); break;
            case "shape": Mutate(1000, "shape", () => s.Row.BaseShape = new CornerRadius(8)); Times(16000, 400000); break;
            case "icon":
                Mutate(1000, "icon", () => item.InactiveIcon = new Marker { PreferredSize = new Size(24, 18) }); Times(16000, 400000);
                Mutate(401000, "remove-icon", () => item.InactiveIcon = null); Times(416000, 800000); break;
            case "space": case "enter":
                Advance(1000); item.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = c.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); Sample("key-up"); Times(176000, 400000); break;
            case "hover": Input(1000, "move", center); Sample("hover"); Times(16000, 64000, 160000); Input(161000, "move", outside); Sample("exit"); Times(176000, 400000); break;
            default:
                Input(1000, "move", center); Input(2000, "down", center); Sample("press");
                if (c.Action is "quick" or "click" or "disabled-input") { Input(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000, 800000); break; }
                Times(16000, 32000, 64000);
                if (c.Action == "cancel") Input(65000, "move", outside);
                if (c.Action == "disable") Mutate(65000, "disabled", () => item.IsEnabled = false);
                Times(80000, 160000, 240000); Input(241000, "up", c.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 400000, 800000); break;
        }
    }
    private sealed class Scene
    {
        readonly AvaloniaHost host; readonly SegmentedButtonConfig c; readonly int[] clicks;
        public SegmentedButtonRow Row { get; }
        public SegmentedButton[] Items { get; }
        public Marker[] Labels { get; }
        public Scene(AvaloniaHost host, SegmentedButtonConfig c)
        {
            this.host = host; this.c = c; SetTheme(c.Dark);
            Row = c.Multi ? new MultiChoiceSegmentedButtonRow() : new SingleChoiceSegmentedButtonRow();
            Row.Width = c.Width ?? double.NaN; Row.Height = c.Height ?? double.NaN; Row.Space = c.Border;
            Row.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            if (c.Radius is { } radius) Row.BaseShape = new CornerRadius(radius);
            clicks = new int[c.Count]; Items = new SegmentedButton[c.Count]; Labels = new Marker[c.Count];
            for (var i = 0; i < c.Count; i++)
            {
                var index = i; var item = Items[i] = new SegmentedButton { IsSelected = i == 0 && c.Selected, IsEnabled = c.Enabled, ShowSelectionIcon = c.Icon != "none" };
                item.Content = Labels[i] = new Marker { PreferredSize = new Size(32 + (c.Varied ? i * 8 : 0), c.LabelHeight) };
                if (c.Icon is "active" or "swap") item.ActiveIcon = new Marker { PreferredSize = new Size(c.IconWidth, 18) };
                if (c.Icon == "swap") item.InactiveIcon = new Marker { PreferredSize = new Size(c.IconWidth, 18) };
                item.BorderThickness = new Thickness(c.Border); if (c.Padding is { } pad) item.Padding = new Thickness(pad);
                if (c.CustomColors) item.Colors = new SegmentedButtonColors
                {
                    ActiveContainerColor = Color.Parse("#126E60"), ActiveContentColor = Color.Parse("#F0E080"), ActiveBorderColor = Colors.Red,
                    InactiveContainerColor = Color.Parse("#223366"), InactiveContentColor = Colors.Yellow, InactiveBorderColor = Colors.Blue,
                    DisabledActiveContainerColor = Color.Parse("#70336699"), DisabledActiveContentColor = Color.Parse("#00FF00"), DisabledActiveBorderColor = Colors.Magenta,
                    DisabledInactiveContainerColor = Color.Parse("#50556677"), DisabledInactiveContentColor = Colors.Cyan, DisabledInactiveBorderColor = Colors.Black,
                };
                if (c.Action == "press-content") item.Colors = new SegmentedButtonColors
                    { ActiveContentColor = Color.Parse("#00FF00"), InactiveContentColor = Colors.Yellow };
                item.Click += (_, _) => { clicks[index]++; if (c.Action == "click") { if (!c.Multi) foreach (var other in Items) other.IsSelected = false; item.IsSelected = c.Multi ? !item.IsSelected : true; } };
                Row.Items.Add(item);
            }
        }
        public void SetTheme(bool dark) { CaseDefaults.SetThemeColors(host, dark); host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E"); }
        public ProbePoint[] Measure() => [new("row", "row", null, Layout(Row)), .. Items.SelectMany((item, i) => new[]
        {
            new ProbePoint($"item-{i}", "button", "row", [.. Layout(item), Measurement.Flag("selected", item.IsSelected, definition: "segmented-button.selected"),
                Measurement.Flag("enabled", item.IsEffectivelyEnabled, definition: "segmented-button.enabled"), Measurement.Numeric("clicks", clicks[i], "count", definition: "segmented-button.clicks")]),
            new ProbePoint($"container-{i}", "container", $"item-{i}", Layout(item.GetVisualDescendants().OfType<MaterialSurface>().Single())),
            new ProbePoint($"label-{i}", "content", $"container-{i}", Layout(Labels[i])),
        }).ToArray()];
        Measurement[] Layout(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
    }
    private sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        private Size _size;
        public Size PreferredSize { get => _size; set { _size = value; InvalidateMeasure(); } }
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(_size.Width, availableSize.Width), Math.Min(_size.Height, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
