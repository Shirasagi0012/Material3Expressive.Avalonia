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
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ToggleButtonConfig(string Style = "filled", string Size = "small", bool Checked = false,
    bool Enabled = true, bool Dark = false, bool Rtl = false, bool Icon = false, bool Empty = false,
    double? Width = null, double? Height = null, bool Custom = false, string Action = "none");

[ParitySuite("toggle-button")]
public sealed class ToggleButtonCases
{
    static readonly string[] Styles = ["filled", "tonal", "elevated", "outlined"];
    static readonly string[] Sizes = ["xsmall", "small", "medium", "large", "xlarge"];
    public static IEnumerable<CaseData<ToggleButtonConfig>> StaticConfigurations()
    {
        foreach (var style in Styles)
        foreach (var size in Sizes)
        foreach (var check in new[] { false, true })
            yield return Data($"static.appearance.{style}.{size}.{(check ? "on" : "off")}", new(style, size, check));
        // Disabled/color branches depend on style and checked state, not on the shared size layout.
        foreach (var style in Styles)
        foreach (var check in new[] { false, true })
        {
            yield return Data($"static.disabled.{style}.{check}", new(style, Checked: check, Enabled: false));
            yield return Data($"static.dark.{style}.{check}", new(style, Checked: check, Dark: true));
            yield return Data($"static.colors.{style}.{check}", new(style, Checked: check, Custom: true));
        }
        foreach (var size in Sizes)
        {
            yield return Data($"static.icon.{size}", new(Size: size, Icon: true));
            yield return Data($"static.empty.{size}", new(Size: size, Empty: true));
        }
        yield return Data("static.rtl.icon", new(Rtl: true, Icon: true));
        yield return Data("static.rtl.checked", new(Rtl: true, Checked: true));
        foreach (var width in new[] { 24d, 48d, 160d }) yield return Data($"static.constraint.width-{width}", new(Width: width, Icon: true));
        yield return Data("static.constraint.short", new(Width: 48, Height: 16, Checked: true));
    }
    public static IEnumerable<CaseData<ToggleButtonConfig>> DynamicConfigurations()
    {
        foreach (var style in Styles)
        {
            yield return Data($"dynamic.selection.{style}", new(style, Action: "selection"));
            yield return Data($"dynamic.press.{style}", new(style, Action: "press"));
            yield return Data($"dynamic.hover.{style}", new(style, Action: "hover"));
        }
        foreach (var action in new[] { "reverse", "third", "shape", "quick", "cancel", "disable", "disabled-input", "space", "enter", "theme" })
            yield return Data($"dynamic.{action}", new(Enabled: action != "disabled-input", Action: action));
        yield return Data("dynamic.outlined.reverse", new("outlined", Action: "reverse"));
        yield return Data("dynamic.press.checked", new(Checked: true, Action: "press"));
    }
    static CaseData<ToggleButtonConfig> Data(string id, ToggleButtonConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ToggleButtonConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        var scene = new Scene(host, config);
        host.Mount(CaseDefaults.Center(scene.Button)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ToggleButtonConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        var scene = new Scene(host, config); var button = scene.Button;
        host.Mount(CaseDefaults.Center(button)); host.Frame(-1000); host.Frame(0);
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
            case "selection": case "reverse": case "third": case "shape":
                Mutate(1000, "selected", () => button.IsChecked = true); Times(16000, 32000, 64000);
                if (config.Action == "reverse") Mutate(65000, "reverse", () => button.IsChecked = false);
                if (config.Action == "third") { Input(65000, "move", center); Input(66000, "down", center); Sample("third-target"); }
                if (config.Action == "shape") Mutate(65000, "shape-change", () => ToggleButtonAppearance.SetCheckedCornerRadius(button, new CornerRadius(18)));
                Times(80000, 160000, 400000);
                if (config.Action == "third") { Input(401000, "up", center); Sample("release"); Times(416000, 800000); }
                break;
            case "theme":
                Mutate(1000, "dark", () => scene.SetTheme(true)); Times(16000, 400000); break;
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
        readonly ToggleButtonConfig config;
        readonly Marker? marker;
        readonly Marker? icon;
        int clicks;
        public ToggleButton Button { get; }
        public Scene(AvaloniaHost host, ToggleButtonConfig config)
        {
            this.host = host; this.config = config; SetTheme(config.Dark);
            Button = new ToggleButton { IsChecked = config.Checked, IsEnabled = config.Enabled, Width = config.Width ?? double.NaN,
                Height = config.Height ?? double.NaN, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            Button.Theme = (ControlTheme)host.Window.FindResource($"Material{char.ToUpperInvariant(config.Style[0])}{config.Style[1..]}ToggleButtonTheme")!;
            if (config.Size != "small") Button.Classes.Add(config.Size);
            if (!config.Empty) Button.Content = marker = new Marker { PreferredSize = new Size(32, 16) };
            if (config.Icon) { icon = new Marker { FillAvailable = true }; ToggleButtonAppearance.SetIcon(Button, icon); }
            if (config.Custom) ToggleButtonAppearance.SetColors(Button, new(Color.Parse("#126E60"), Color.Parse("#F0E080"), Color.Parse("#70336699"), Color.Parse("#50556677"), Color.Parse("#335588"), Color.Parse("#FFEEDD")));
            Button.Click += (_, _) => clicks++;
        }
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
            host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
            host.Window.Resources[Sys.Color.OnSecondary] = Color.Parse(dark ? "#332D41" : "#FFFFFF");
            host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(dark ? "#E6E0E9" : "#322F35");
            host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(dark ? "#322F35" : "#F5EFF7");
        }
        public ProbePoint[] Measure()
        {
            var surface = Button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var interaction = typeof(MaterialInteraction).GetMethod("GetOrCreate", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [Button])!;
            var stateAlpha = (double)interaction.GetType().GetProperty("StateLayerOpacity", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(interaction)!;
            var corners = (CornerRadius)typeof(MaterialSurface).GetProperty("EffectiveCornerRadius", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(surface)!;
            return [new("button", "toggle-button", null, [.. Layout(Button), Measurement.Flag("checked", Button.IsChecked == true, definition: "toggle-button.checked"), Measurement.Flag("enabled", Button.IsEffectivelyEnabled, definition: "toggle-button.enabled"), Measurement.Flag("focused", Button.IsFocused, definition: "button.focused"), Measurement.Numeric("clicks", clicks, "count", definition: "toggle-button.click-count")]),
                new("container", "container", "button", [.. Layout(surface), Measurement.Vector("corners", "rect", [corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft], "dip", "container", "motion", "toggle-button.visual-corner-radii", components: ["topLeft", "topRight", "bottomRight", "bottomLeft"]), Measurement.Numeric("shadowElevation", surface.ShadowElevation, category: "motion", definition: "button.shadow-elevation"), Measurement.Numeric("stateLayerAlpha", stateAlpha, "ratio", category: "motion", definition: "button.state-layer-alpha")]),
                .. marker is null ? Array.Empty<ProbePoint>() : new[] { new ProbePoint("content", "content", "container", Layout(marker)) },
                .. icon is null ? Array.Empty<ProbePoint>() : new[] { new ProbePoint("icon", "icon", "container", Layout(icon)) }];
        }
        Measurement[] Layout(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
    }
    sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        public Size PreferredSize { get; init; } = new(20, 20);
        public bool FillAvailable { get; init; }
        protected override Size MeasureOverride(Size availableSize) => new(
            FillAvailable && double.IsFinite(availableSize.Width) ? availableSize.Width : Math.Min(availableSize.Width, PreferredSize.Width),
            FillAvailable && double.IsFinite(availableSize.Height) ? availableSize.Height : Math.Min(availableSize.Height, PreferredSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
