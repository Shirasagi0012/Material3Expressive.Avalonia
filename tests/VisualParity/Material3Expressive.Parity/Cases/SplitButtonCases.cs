using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Split = Material3Expressive.Components.SplitButton;

namespace Material3Expressive.Parity;

public sealed record SplitButtonConfig(string Style = "filled", string Size = "small", bool Checked = false,
    bool Toggle = true, bool Enabled = true, bool Dark = false, bool Rtl = false,
    double? Width = null, double? Height = null, double Spacing = 2, double ContentHeight = 16,
    string Action = "none", string Part = "trailing");

[ParitySuite("split-button")]
public sealed class SplitButtonCases
{
    static readonly string[] Styles = ["filled", "tonal", "elevated", "outlined"];
    static readonly string[] Sizes = ["xsmall", "small", "medium", "large", "xlarge"];
    public static IEnumerable<CaseData<SplitButtonConfig>> StaticConfigurations()
    {
        foreach (var style in Styles)
        foreach (var size in Sizes)
        foreach (var check in new[] { false, true })
            yield return Data($"static.appearance.{style}.{size}.{(check ? "on" : "off")}", new(style, size, check));
        foreach (var style in Styles)
        {
            yield return Data($"static.plain.{style}", new(style, Toggle: false));
            foreach (var check in new[] { false, true })
            {
                yield return Data($"static.disabled.{style}.{check}", new(style, Checked: check, Enabled: false));
                yield return Data($"static.dark.{style}.{check}", new(style, Checked: check, Dark: true));
            }
        }
        foreach (var size in Sizes) yield return Data($"static.rtl.{size}", new(Size: size, Rtl: true));
        foreach (var width in new[] { 24d, 48d, 80d, 160d }) yield return Data($"static.constraint.width-{width}", new(Width: width));
        yield return Data("static.constraint.short", new(Width: 80, Height: 16));
        yield return Data("static.layout.shared-height", new(ContentHeight: 72));
        yield return Data("static.layout.spacing", new(Spacing: 7));
    }
    public static IEnumerable<CaseData<SplitButtonConfig>> DynamicConfigurations()
    {
        foreach (var style in Styles)
        {
            yield return Data($"dynamic.selection.{style}", new(style, Action: "selection"));
            foreach (var part in new[] { "leading", "trailing" })
                yield return Data($"dynamic.press.{style}.{part}", new(style, Action: "press", Part: part));
        }
        foreach (var action in new[] { "reverse", "shape", "quick", "cancel", "disable", "disabled-input", "space", "enter", "theme", "hover" })
            yield return Data($"dynamic.{action}", new(Enabled: action != "disabled-input", Action: action));
        yield return Data("dynamic.press.checked", new(Checked: true, Action: "press"));
        yield return Data("dynamic.press.plain", new(Toggle: false, Action: "press"));
        yield return Data("dynamic.space.leading", new(Action: "space", Part: "leading"));
        yield return Data("dynamic.enter.leading", new(Action: "enter", Part: "leading"));
        yield return Data("dynamic.hover.elevated", new("elevated", Action: "hover", Part: "leading"));
    }
    static CaseData<SplitButtonConfig> Data(string id, SplitButtonConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SplitButtonConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        var scene = new Scene(host, config);
        host.Mount(CaseDefaults.Center(scene.Split)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SplitButtonConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        var scene = new Scene(host, config);
        host.Mount(CaseDefaults.Center(scene.Split)); host.Frame(-1000); host.Frame(0);
        var button = config.Part == "leading" ? scene.Leading : scene.Trailing;
        var nextFrame = 16000L;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void Input(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); }
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(415, 203); Sample("initial");
        switch (config.Action)
        {
            case "selection": case "reverse": case "shape":
                Mutate(1000, "selected", () => ((ToggleButton)scene.Trailing).IsChecked = true); Times(16000, 32000, 64000);
                if (config.Action == "reverse") Mutate(65000, "reverse", () => ((ToggleButton)scene.Trailing).IsChecked = false);
                if (config.Action == "shape") Mutate(65000, "shape-change", () => scene.Trailing.CornerRadius = new CornerRadius(8));
                Times(80000, 160000, 400000); break;
            case "theme": Mutate(1000, "dark", () => CaseDefaults.SetThemeColors(host, true)); Times(16000, 400000); break;
            case "space": case "enter":
                Advance(1000); button.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); Sample("key-up"); Times(176000, 400000); break;
            case "hover":
                Input(1000, "move", center); Sample("hover"); Times(16000, 64000, 160000); Input(161000, "move", outside); Sample("exit"); Times(176000, 400000); break;
            default:
                Input(1000, "move", center); Input(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input") { Input(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000); break; }
                Times(16000, 32000, 64000);
                if (config.Action == "cancel") Input(65000, "move", outside);
                if (config.Action == "disable") Mutate(65000, "disabled", () => button.IsEnabled = false);
                Times(80000, 160000, 240000); Input(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 400000, 800000); break;
        }
    }
    sealed class Scene
    {
        readonly AvaloniaHost host;
        readonly SplitButtonConfig config;
        readonly Marker leadingContent, trailingContent;
        int leadingClicks, trailingClicks;
        public Split Split { get; }
        public Button Leading { get; }
        public Button Trailing { get; }
        public Scene(AvaloniaHost host, SplitButtonConfig config)
        {
            this.host = host; this.config = config; CaseDefaults.SetThemeColors(host, config.Dark);
            Leading = new Button(); Trailing = config.Toggle ? new ToggleButton { IsChecked = config.Checked } : new Button();
            var style = char.ToUpperInvariant(config.Style[0]) + config.Style[1..];
            Leading.Theme = (ControlTheme)host.Window.FindResource($"Material{style}SplitButtonLeadingTheme")!;
            Trailing.Theme = (ControlTheme)host.Window.FindResource($"Material{style}SplitButtonTrailingTheme")!;
            Leading.IsEnabled = Trailing.IsEnabled = config.Enabled;
            if (config.Size != "small") { Leading.Classes.Add(config.Size); Trailing.Classes.Add(config.Size); }
            Leading.Content = leadingContent = new Marker { PreferredSize = new(32, config.ContentHeight) };
            var iconSize = config.Size switch { "xsmall" or "small" => 22, "medium" => 26, "large" => 38, "xlarge" => 50, _ => throw new InvalidOperationException() };
            Trailing.Content = trailingContent = new Marker { PreferredSize = new(iconSize, iconSize) };
            Leading.Click += (_, _) => leadingClicks++; Trailing.Click += (_, _) => trailingClicks++;
            Split = new Split { LeadingButton = Leading, TrailingButton = Trailing, Width = config.Width ?? double.NaN, Height = config.Height ?? double.NaN,
                Spacing = config.Spacing, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        }
        public ProbePoint[] Measure()
        {
            ProbePoint ButtonPoint(Button button, string id, int clicks) => new(id, "button", "split", [.. Layout(button),
                Measurement.Flag("checked", button is ToggleButton { IsChecked: true }, definition: "split-button.checked"),
                Measurement.Flag("enabled", button.IsEffectivelyEnabled, definition: "split-button.enabled"),
                Measurement.Numeric("clicks", clicks, "count", definition: "split-button.click-count")]);
            ProbePoint SurfacePoint(Button button, string id)
            {
                var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
                var corners = (CornerRadius)typeof(MaterialSurface).GetProperty("EffectiveCornerRadius", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)!;
                var radii = config.Rtl ? new[] { corners.TopRight, corners.TopLeft, corners.BottomLeft, corners.BottomRight }
                    : new[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft };
                // Full is a sentinel on Avalonia; a degenerate painted box normalizes it to zero.
                var maximum = Math.Min(surface.Bounds.Width, surface.Bounds.Height) / 2;
                return new(id + "-container", "container", id, [.. Layout(surface), Measurement.Vector("radii", "rect", radii.Select(r => Math.Clamp(r, 0, maximum)).ToArray(), "dip", "container", "motion", "split-button.visual-corner-radii", components: ["topLeft", "topRight", "bottomRight", "bottomLeft"]), Measurement.Numeric("shadowElevation", surface.ShadowElevation, category: "motion", definition: "button.shadow-elevation")]);
            }
            return [new("split", "split-button", null, Layout(Split)), ButtonPoint(Leading, "leading", leadingClicks), ButtonPoint(Trailing, "trailing", trailingClicks),
                SurfacePoint(Leading, "leading"), SurfacePoint(Trailing, "trailing"),
                new("leading-content", "content", "leading-container", Layout(leadingContent)), new("trailing-content", "content", "trailing-container", Layout(trailingContent))];
        }
        Measurement[] Layout(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
    }
    sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public Size PreferredSize { get; init; }
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(availableSize.Width, PreferredSize.Width), Math.Min(availableSize.Height, PreferredSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
