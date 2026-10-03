using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record RadioButtonConfig(bool Selected = false, bool Enabled = true, bool Dark = false,
    bool Rtl = false, bool CustomColors = false, double? Width = null, double? Height = null,
    string Action = "none", bool StandardMotion = false);

[ParitySuite("radio-button")]
public sealed class RadioButtonCases
{
    public static IEnumerable<CaseData<RadioButtonConfig>> StaticConfigurations()
    {
        // One public upstream API: selection × enabled × theme covers every default color branch.
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}", new(selected, enabled, dark));
        foreach (var selected in new[] { false, true })
        {
            foreach (var enabled in new[] { false, true })
                yield return Data($"static.colors.{selected}.{enabled}".ToLowerInvariant(), new(selected, enabled, CustomColors: true));
            yield return Data($"static.rtl.{(selected ? "selected" : "unselected")}", new(selected, Rtl: true));
        }
        // Auto size above; constraints exercise minimumInteractiveComponentSize and requiredSize.
        foreach (var width in new[] { 16d, 24d, 40d, 64d })
            yield return Data($"static.layout.width-{width}", new(true, Width: width));
        yield return Data("static.layout.short", new(true, Width: 64, Height: 16));
        yield return Data("static.layout.tall", new(false, Width: 24, Height: 80));
    }

    public static IEnumerable<CaseData<RadioButtonConfig>> DynamicConfigurations()
    {
        foreach (var selected in new[] { false, true })
        foreach (var action in new[] { "transition", "reverse", "enable", "theme", "colors", "disable-transition" })
            yield return Data($"dynamic.{action}.{(selected ? "selected" : "unselected")}", new(selected, Action: action));
        yield return Data("dynamic.transition.standard-motion", new(Action: "transition", StandardMotion: true));
        yield return Data("dynamic.transition.deselection-tail", new(true, Action: "deselection-tail"));
        yield return Data("dynamic.transition.deselection-tail-standard", new(true, Action: "deselection-tail", StandardMotion: true));
        yield return Data("dynamic.transition.disabled", new(Enabled: false, Action: "transition"));
        yield return Data("dynamic.transition.custom-colors", new(CustomColors: true, Action: "transition"));
        foreach (var action in new[] { "press", "quick", "cancel", "disable-press", "disabled-input", "hover", "space", "enter", "request-only", "edge", "group" })
            yield return Data($"dynamic.input.{action}", new(Enabled: action != "disabled-input", Action: action));
        yield return Data("dynamic.input.selected", new(true, Action: "press"));
        yield return Data("dynamic.input.rtl", new(Rtl: true, Action: "press"));
    }
    static CaseData<RadioButtonConfig> Data(string id, RadioButtonConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<RadioButtonConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new RadioScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<RadioButtonConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        var scene = new RadioScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: config.Action == "deselection-tail" || id is not ("time-32000" or "time-112000")));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var radio = scene.Radio;
        Point Center(RadioButton r) => r.TranslatePoint(new Point(r.Bounds.Width / 2, r.Bounds.Height / 2), host.Window)!.Value;
        var center = Center(radio);
        var outside = new Point(155, 123);
        Sample("initial");
        switch (config.Action)
        {
            case "deselection-tail":
                Mutate(1000, "selection-change", () => radio.IsChecked = false);
                // Observe every existing grid frame through disappearance and the final rebound.
                // Sampling does not insert extra frames or change the fixed Compose reference.
                Times(Enumerable.Range(1, 50).Select(frame => (long)frame * 16000).ToArray()); break;
            case "transition": case "reverse": case "disable-transition":
                Mutate(1000, "selection-change", () => radio.IsChecked = !config.Selected);
                Times(16000, 32000, 64000);
                if (config.Action == "reverse") Mutate(65000, "selection-reverse", () => radio.IsChecked = config.Selected);
                if (config.Action == "disable-transition") Mutate(65000, "disabled", () => radio.IsEnabled = false);
                Times(96000, 112000, 160000, 240000, 400000, 800000); break;
            case "enable":
                Mutate(1000, "disabled", () => radio.IsEnabled = false); Times(16000, 64000, 160000);
                Mutate(161000, "enabled", () => radio.IsEnabled = true); Times(176000, 240000, 400000); break;
            case "theme":
                Mutate(1000, "dark", () => CaseDefaults.SetThemeColors(host, true)); Times(16000, 64000, 160000, 400000);
                Mutate(401000, "light", () => CaseDefaults.SetThemeColors(host, false)); Times(416000, 480000, 800000); break;
            case "colors":
                Mutate(1000, "custom-colors", () => RadioButtonAppearance.SetColors(radio, RadioScene.CustomPalette)); Times(16000, 64000, 160000, 400000);
                Mutate(401000, "theme-colors", () => RadioButtonAppearance.SetColors(radio, null)); Times(416000, 480000, 800000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            case "space": case "enter":
                Advance(1000); radio.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000, 800000); break;
            case "group":
                var second = Center(scene.Second!);
                Event(1000, "move", second); Event(2000, "down", second); Event(5000, "up", second); Sample("second-selected"); Times(16000, 64000, 400000);
                Event(401000, "move", center); Event(402000, "down", center); Event(405000, "up", center); Sample("first-selected"); Times(416000, 480000, 800000); break;
            default:
                if (config.Action == "edge") center = radio.TranslatePoint(new Point(2, 2), host.Window)!.Value;
                Event(1000, "move", center); Event(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input" or "request-only") { Event(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000, 800000); break; }
                Times(16000, 64000);
                if (config.Action == "disable-press") Mutate(65000, "disabled", () => radio.IsEnabled = false);
                if (config.Action == "cancel") Event(65000, "move", outside);
                Times(80000, 160000, 240000);
                Event(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 320000, 400000, 800000); break;
        }
    }

    sealed class RadioScene
    {
        readonly AvaloniaHost host;
        int clicks;
        public RadioButton Radio { get; }
        public RadioButton? Second { get; }
        public Control Root { get; }
        public static RadioButtonColors CustomPalette => new(Color.Parse("#126E60"), Color.Parse("#A0895210"), Color.Parse("#70336699"), Color.Parse("#50556677"));
        public RadioScene(AvaloniaHost host, RadioButtonConfig config)
        {
            this.host = host;
            CaseDefaults.SetThemeColors(host, config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            if (config.StandardMotion)
            {
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialFastDamping] = 0.9d;
                host.Window.Resources[Tokens.MaterialTokens.Sys.Motion.Spring.SpatialFastStiffness] = 1400d;
            }
            Radio = new RadioButton { IsChecked = config.Action == "group" || config.Selected, IsEnabled = config.Enabled };
            if (config.Width is { } width) Radio.Width = width;
            if (config.Height is { } height) Radio.Height = height;
            Radio.Click += (_, _) => clicks++;
            if (config.CustomColors) RadioButtonAppearance.SetColors(Radio, CustomPalette);
            if (config.Action == "group")
            {
                Radio.GroupName = "parity";
                Second = new RadioButton { GroupName = "parity" };
                Second.Click += (_, _) => clicks++;
                Root = CaseDefaults.Center(new StackPanel { Orientation = Orientation.Horizontal, Children = { Radio, Second } });
            }
            else Root = CaseDefaults.Center(Radio);
        }
        public ProbePoint[] Measure() => [Point(Radio, "radio"), .. Second is null ? Array.Empty<ProbePoint>() : new[] { Point(Second, "second") }];
        ProbePoint Point(RadioButton radio, string id) => new(id, "radio-button", null,
            [.. AvaloniaMeasurements.Geometry(radio, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Flag("selected", radio.IsChecked == true, definition: "radio-button.selected", source: "native RadioButton.IsChecked"),
                Measurement.Flag("enabled", radio.IsEffectivelyEnabled, definition: "radio-button.enabled"),
                Measurement.Numeric("clicks", clicks, "count", definition: "radio-button.click-callback-count")]);
    }
}
