using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record SwitchConfig(string State = "off", bool Enabled = true, bool Dark = false,
    bool Rtl = false, string Thumb = "none", bool CustomColors = false, double? Width = null,
    double? Height = null, string Action = "none", string Target = "on", bool StandardMotion = false);

[ParitySuite("switch")]
public sealed class SwitchCases
{
    public static IEnumerable<CaseData<SwitchConfig>> StaticConfigurations()
    {
        foreach (var state in new[] { "off", "on" })
        foreach (var thumb in new[] { "none", "icon" })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{state}.{thumb}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}",
                new(state, enabled, dark, Thumb: thumb));
        // RTL affects handle placement and icon orientation; custom colors cover all 16 slots.
        foreach (var state in new[] { "off", "on" })
        foreach (var thumb in new[] { "none", "icon" })
            yield return Data($"static.rtl.{state}.{thumb}", new(state, Rtl: true, Thumb: thumb));
        foreach (var state in new[] { "off", "on" })
        foreach (var enabled in new[] { false, true })
            yield return Data($"static.colors.{state}.{(enabled ? "enabled" : "disabled")}", new(state, enabled, Thumb: "icon", CustomColors: true));
        foreach (var width in new[] { 20d, 40d, 80d })
            yield return Data($"static.layout.width-{width}", new(Width: width));
        yield return Data("static.layout.short", new(State: "on", Width: 40, Height: 16));
        yield return Data("static.layout.tall", new(Width: 80, Height: 80, Thumb: "icon"));
        yield return Data("static.content.empty", new(Thumb: "empty"));
        yield return Data("static.content.zero-height", new(Thumb: "zero-height"));
        yield return Data("static.content.large", new(Thumb: "large"));
        yield return Data("static.content.required-large", new(Thumb: "required-large"));
    }
    public static IEnumerable<CaseData<SwitchConfig>> DynamicConfigurations()
    {
        foreach (var state in new[] { "off", "on" })
        foreach (var thumb in new[] { "none", "icon" })
        {
            foreach (var action in new[] { "transition", "reverse", "press" })
                yield return Data($"dynamic.{action}.{state}.{thumb}", new(state, Thumb: thumb, Action: action, Target: state == "off" ? "on" : "off"));
        }
        foreach (var state in new[] { "off", "on" })
        {
            yield return Data($"dynamic.disable.{state}", new(state, Thumb: "icon", Action: "enable"));
            yield return Data($"dynamic.theme.{state}", new(state, Thumb: "icon", Action: "theme"));
            yield return Data($"dynamic.drag.{state}", new(state, Action: "drag"));
        }
        foreach (var action in new[] { "quick", "cancel", "disable-press", "disabled-input", "hover", "space", "enter", "content" })
            yield return Data($"dynamic.input.{action}", new(Enabled: action != "disabled-input", Action: action));
        yield return Data("dynamic.transition.standard-motion", new(Action: "transition", StandardMotion: true));
        yield return Data("dynamic.transition.disabled", new(Enabled: false, Action: "transition"));
        yield return Data("dynamic.press.after-transition", new(Action: "warm-press"));
        yield return Data("dynamic.drag.rtl", new(Rtl: true, Action: "drag"));
    }
    static CaseData<SwitchConfig> Data(string id, SwitchConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SwitchConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new SwitchScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Switch)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SwitchConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new SwitchScene(host, config); var box = scene.Switch;
        host.Mount(CaseDefaults.Center(box)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        var nextFrame = 16000L;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var center = box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(155, 123);
        Sample("initial");
        switch (config.Action)
        {
            case "transition": case "reverse":
                Mutate(1000, "state-change", () => box.IsChecked = config.Target == "on"); Times(16000, 32000, 64000);
                if (config.Action == "reverse") Mutate(65000, "state-reverse", () => box.IsChecked = config.State == "on");
                Times(96000, 112000, 160000, 240000, 400000, 800000); break;
            case "enable":
                Mutate(1000, "disabled", () => box.IsEnabled = false); Times(16000, 64000, 160000);
                Mutate(161000, "enabled", () => box.IsEnabled = true); Times(176000, 240000, 400000); break;
            case "theme":
                Mutate(1000, "dark", () => scene.SetTheme(true)); Times(16000, 64000, 160000, 400000);
                Mutate(401000, "light", () => scene.SetTheme(false)); Times(416000, 480000, 800000); break;
            case "content":
                Mutate(1000, "icon", () => scene.SetThumb("icon")); Times(16000, 64000, 400000);
                Mutate(401000, "empty", () => scene.SetThumb("empty")); Times(416000, 480000, 800000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            case "space": case "enter":
                Advance(1000); box.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000, 800000); break;
            case "drag":
                var moving = box.GetVisualDescendants().Single(v => v.Name == "PART_MovingKnobs");
                var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), host.Window)!.Value;
                var direction = (config.State == "off" ? 1 : -1) * (config.Rtl ? -1 : 1);
                var end = start + new Vector(direction * 16, 0);
                Event(1000, "move", start); Event(2000, "down", start); Sample("press");
                Event(16000, "move", end); Sample("drag");
                Event(64000, "up", end); Sample("release"); Times(80000, 160000, 400000, 800000); break;
            default:
                var pressTime = 2000L;
                if (config.Action == "warm-press")
                {
                    Mutate(1000, "state-change", () => box.IsChecked = true); Times(16000, 400000);
                    pressTime = 402000;
                }
                Event(pressTime - 1000, "move", center); Event(pressTime, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input")
                {
                    Event(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000, 800000); break;
                }
                Times(pressTime + 14000, pressTime + 62000);
                if (config.Action == "disable-press") Mutate(65000, "disabled", () => box.IsEnabled = false);
                if (config.Action == "cancel") Event(65000, "move", outside);
                Times(pressTime + 78000, pressTime + 158000, pressTime + 238000);
                Event(pressTime + 239000, "up", config.Action == "cancel" ? outside : center); Sample("release");
                Times(pressTime + 254000, pressTime + 318000, pressTime + 398000, pressTime + 798000); break;
        }
    }

    sealed class SwitchScene
    {
        readonly AvaloniaHost host;
        int callbacks;
        public ToggleSwitch Switch { get; }
        public SwitchScene(AvaloniaHost host, SwitchConfig config)
        {
            this.host = host; SetTheme(config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            if (config.StandardMotion)
            {
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialFastDamping] = 0.9d;
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialFastStiffness] = 1400d;
            }
            Switch = new ToggleSwitch { IsChecked = config.State == "on", IsEnabled = config.Enabled };
            if (config.Width is { } width) Switch.Width = width;
            if (config.Height is { } height) Switch.Height = height;
            // Observe actual native activation. Drag can change state without raising Click.
            Switch.Click += (_, _) => callbacks++;
            SetThumb(config.Thumb);
            if (config.CustomColors) SwitchAppearance.SetColors(Switch, new(
                Color.Parse("#F0E080"), Color.Parse("#126E60"), Color.Parse("#80335588"), Color.Parse("#895210"),
                Color.Parse("#AABBCC"), Color.Parse("#40FF0000"), Color.Parse("#335588"), Color.Parse("#126E60"),
                Color.Parse("#70336699"), Color.Parse("#20223344"), Color.Parse("#80553677"), Color.Parse("#008800"),
                Color.Parse("#90556677"), Color.Parse("#50365A80"), Color.Parse("#A0775533"), Color.Parse("#F0E080")));
        }
        public void SetThumb(string thumb)
        {
            var size = new Size(thumb == "empty" ? 0 : thumb is "large" or "required-large" ? 40 : 16,
                thumb is "empty" or "zero-height" ? 0 : thumb is "large" or "required-large" ? 40 : 16);
            // Compose size is preferred under constraints; Avalonia explicit Width/Height
            // overflows like requiredSize. Capture both behaviors rather than equating them.
            var content = new SwitchMarker { RequestedSize = size };
            if (thumb == "required-large") { content.Width = size.Width; content.Height = size.Height; }
            SwitchAppearance.SetThumbContent(Switch, thumb == "none" ? null : content);
        }
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnPrimaryContainer] = Color.Parse(dark ? "#EADDFF" : "#21005D");
        }
        public ProbePoint[] Measure()
        {
            var thumb = (Control)Switch.GetVisualDescendants().Single(v => v.Name == "PART_MovingKnobs");
            return [new("switch", "switch", null,
                [.. AvaloniaMeasurements.Geometry(Switch, host.Window).Where(m => m.Name is "box" or "size"),
                    Measurement.Flag("checked", Switch.IsChecked == true, definition: "switch.checked"),
                    Measurement.Flag("enabled", Switch.IsEffectivelyEnabled, definition: "switch.enabled"),
                    Measurement.Numeric("callbacks", callbacks, "count", definition: "switch.click-callback-count")]),
                new("thumb", "thumb", "switch", AvaloniaMeasurements.Geometry(thumb, host.Window).Where(m => m.Name is "box" or "size").ToArray())];
        }
    }
    sealed class SwitchMarker : Control
    {
        public Size RequestedSize { get; init; }
        protected override Size MeasureOverride(Size availableSize) => RequestedSize.Constrain(availableSize);
        static SwitchMarker() => AffectsRender<SwitchMarker>(TextElement.ForegroundProperty);
        public override void Render(DrawingContext context) =>
            context.DrawRectangle(this.GetValue(TextElement.ForegroundProperty), null, new Rect(Bounds.Size));
    }
}
