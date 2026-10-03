using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CheckBoxConfig(string Api = "tri", string State = "off", bool Enabled = true,
    bool Dark = false, bool Rtl = false, bool CustomColors = false, string Stroke = "default",
    double? Width = null, double? Height = null, string Action = "none", string Target = "on", bool StandardMotion = false);

[ParitySuite("checkbox")]
public sealed class CheckBoxCases
{
    public static IEnumerable<CaseData<CheckBoxConfig>> StaticConfigurations()
    {
        // Both real upstream entry points: boolean has two states, tri-state has three.
        foreach (var api in new[] { "boolean", "tri" })
        foreach (var state in api == "boolean" ? new[] { "off", "on" } : new[] { "off", "on", "indeterminate" })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{api}.{state}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}",
                new(api, state, enabled, dark));
        foreach (var state in new[] { "off", "on", "indeterminate" })
        {
            foreach (var enabled in new[] { false, true })
                yield return Data($"static.colors.{state}.{(enabled ? "enabled" : "disabled")}", new(State: state, Enabled: enabled, CustomColors: true));
            foreach (var stroke in new[] { "round", "dash", "thick" })
                yield return Data($"static.stroke.{stroke}.{state}", new(State: state, Stroke: stroke));
            yield return Data($"static.rtl.{state}", new(State: state, Rtl: true));
        }
        // Default size is automatic above. These constrain or expand the actual layout.
        foreach (var width in new[] { 16d, 24d, 40d, 64d })
            yield return Data($"static.layout.width-{width}", new(State: "on", Width: width));
        yield return Data("static.layout.short", new(State: "indeterminate", Width: 64, Height: 16));
        yield return Data("static.layout.tall", new(State: "off", Width: 24, Height: 80));
    }

    public static IEnumerable<CaseData<CheckBoxConfig>> DynamicConfigurations()
    {
        foreach (var state in new[] { "off", "on", "indeterminate" })
        {
            foreach (var target in new[] { "off", "on", "indeterminate" }.Where(target => target != state))
            {
                yield return Data($"dynamic.transition.{state}-to-{target}", new(State: state, Target: target, Action: "transition"));
                yield return Data($"dynamic.reverse.{state}-to-{target}", new(State: state, Target: target, Action: "reverse"));
            }
            yield return Data($"dynamic.disable.{state}", new(State: state, Action: "enable"));
            yield return Data($"dynamic.theme.{state}", new(State: state, Action: "theme"));
        }
        yield return Data("dynamic.transition.boolean", new(Api: "boolean", Action: "transition"));
        yield return Data("dynamic.transition.standard-motion", new(Action: "transition", StandardMotion: true));
        yield return Data("dynamic.transition.disabled", new(Enabled: false, Action: "transition"));
        yield return Data("dynamic.transition.custom-colors", new(CustomColors: true, Action: "transition"));
        yield return Data("dynamic.transition.third-target", new(Action: "third"));
        foreach (var action in new[] { "press", "quick", "cancel", "disable-press", "disabled-input", "hover", "space", "enter", "tri-cycle" })
            yield return Data($"dynamic.input.{action}", new(Api: action == "tri-cycle" ? "tri" : "boolean",
                State: action == "tri-cycle" ? "on" : "off", Enabled: action != "disabled-input", Action: action));
    }
    static CaseData<CheckBoxConfig> Data(string id, CheckBoxConfig config) => new(id, id, config);
    static bool? Value(string state) => state switch { "off" => false, "on" => true, "indeterminate" => null, _ => throw new ArgumentException(state) };

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<CheckBoxConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new CheckScene(host, config);
        host.Mount(CaseDefaults.Center(scene.CheckBox)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<CheckBoxConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new CheckScene(host, config);
        host.Mount(CaseDefaults.Center(scene.CheckBox)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var box = scene.CheckBox;
        var center = box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(155, 123);
        Sample("initial");
        switch (config.Action)
        {
            case "transition": case "reverse": case "third":
                Mutate(1000, "state-change", () => box.IsChecked = Value(config.Target));
                Times(16000, 32000, 64000);
                if (config.Action == "reverse") Mutate(65000, "state-reverse", () => box.IsChecked = Value(config.State));
                if (config.Action == "third") Mutate(65000, "third-state", () => box.IsChecked = null);
                Times(96000, 112000, 160000, 240000, 400000, 800000); break;
            case "enable":
                Mutate(1000, "disabled", () => box.IsEnabled = false); Times(16000, 64000, 160000);
                Mutate(161000, "enabled", () => box.IsEnabled = true); Times(176000, 240000, 400000); break;
            case "theme":
                Mutate(1000, "dark", () => scene.SetTheme(true)); Times(16000, 64000, 160000, 400000);
                Mutate(401000, "light", () => scene.SetTheme(false)); Times(416000, 480000, 800000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            case "space": case "enter":
                Advance(1000); box.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000, 800000); break;
            default:
                Event(1000, "move", center); Event(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input") { Event(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000, 800000); break; }
                Times(16000, 64000);
                if (config.Action == "disable-press") Mutate(65000, "disabled", () => box.IsEnabled = false);
                if (config.Action == "cancel") Event(65000, "move", outside);
                Times(80000, 160000, 240000);
                Event(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 320000, 400000, 800000); break;
        }
    }

    sealed class CheckScene
    {
        readonly AvaloniaHost host;
        int clicks;
        public CheckBox CheckBox { get; }
        public CheckScene(AvaloniaHost host, CheckBoxConfig config)
        {
            this.host = host;
            SetTheme(config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            if (config.StandardMotion)
            {
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialDefaultDamping] = 0.9d;
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialDefaultStiffness] = 700d;
            }
            CheckBox = new CheckBox { IsChecked = Value(config.State), IsThreeState = config.Api == "tri", IsEnabled = config.Enabled };
            if (config.Width is { } width) CheckBox.Width = width;
            if (config.Height is { } height) CheckBox.Height = height;
            CheckBox.Click += (_, _) => clicks++;
            if (config.CustomColors) CheckBoxAppearance.SetColors(CheckBox, new(
                Color.Parse("#F0E080"), Color.Parse("#40AABBCC"), Color.Parse("#126E60"), Color.Parse("#40FF0000"),
                Color.Parse("#70336699"), Color.Parse("#20223344"), Color.Parse("#80553677"), Color.Parse("#335588"),
                Color.Parse("#895210"), Color.Parse("#90556677"), Color.Parse("#50365A80"), Color.Parse("#A0775533"), Color.Parse("#008800")));
            if (config.Stroke != "default")
            {
                var stroke = new CheckBoxStroke { Width = config.Stroke == "thick" ? 5 : config.Stroke == "round" ? 3 : 2,
                    Cap = config.Stroke == "round" ? PenLineCap.Round : PenLineCap.Square,
                    Join = config.Stroke == "round" ? PenLineJoin.Round : PenLineJoin.Miter,
                    Dashes = config.Stroke == "dash" ? [3, 2] : null };
                CheckBoxAppearance.SetCheckmarkStroke(CheckBox, stroke);
                CheckBoxAppearance.SetOutlineStroke(CheckBox, stroke);
            }
        }
        public void SetTheme(bool dark)
        {
            host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            // Both ends use stock Compose scheme inputs; MCU seed generation is outside this suite.
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.Primary] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnPrimary] = Color.Parse(dark ? "#381E72" : "#FFFFFF");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
            host.Window.Resources[Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
        }
        public ProbePoint[] Measure() => [new("checkbox", "checkbox", null,
            [.. AvaloniaMeasurements.Geometry(CheckBox, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("state", CheckBox.IsChecked switch { false => 0, true => 1, null => 2 }, "enum",
                    definition: "checkbox.toggle-state", source: "native CheckBox.IsChecked: Off=0, On=1, Indeterminate=2"),
                Measurement.Flag("enabled", CheckBox.IsEffectivelyEnabled, definition: "checkbox.enabled"),
                Measurement.Numeric("clicks", clicks, "count", definition: "checkbox.click-callback-count")])];
    }
}
