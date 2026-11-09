using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record AppBarOverflowIndicatorConfig(bool Rtl = false, bool Enabled = true,
    bool Cut = false, bool CustomColors = false, bool Dark = false, string Action = "none", string Culture = "en-US", string Font = "Roboto");

[ParitySuite("appbar-overflow-indicator")]
public sealed class AppBarOverflowIndicatorCases
{
    public static IEnumerable<CaseData<AppBarOverflowIndicatorConfig>> StaticConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var cut in new[] { false, true })
        foreach (var custom in new[] { false, true })
            yield return new($"static.{(rtl ? "rtl" : "ltr")}.{(enabled ? "enabled" : "disabled")}.{(cut ? "cut" : "round")}.{(custom ? "custom" : "ambient")}",
                "Independent overflow indicator geometry, shape and four color channels", new(rtl, enabled, cut, custom));
        yield return new("static.dark.enabled", "Dark ambient overflow indicator", new(Dark: true));
        yield return new("static.dark.disabled", "Dark disabled overflow indicator", new(Enabled: false, Dark: true));
    }

    public static IEnumerable<CaseData<AppBarOverflowIndicatorConfig>> DynamicConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var cut in new[] { false, true })
        foreach (var action in new[] { "click", "disable", "disabled-hover", "source", "state-replace", "source-replace" })
            yield return new($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}.{(cut ? "cut" : "round")}",
                "Overflow state and interaction feedback through real public inputs", new(rtl, Enabled: action != "disabled-hover", Cut: cut, CustomColors: true, Action: action));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<AppBarOverflowIndicatorConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        CaseDefaults.SetThemeColors(host, config.Dark);
        // Tooltip content is part of this public entry point. Both ends use the same English label and bundled Roboto.
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseSurface] = Color.Parse(config.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseOnSurface] = Color.Parse(config.Dark ? "#322F35" : "#F5EFF7");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.TypeScale.BodySmall.Font] = new FontFamily($"fonts:{config.Font}#{config.Font}");
        var initialState = new AppBarMenuState();
        var source = new MaterialInteractionSource();
        var indicator = new AppBarOverflowIndicator
        {
            MenuState = initialState, IsEnabled = config.Enabled, InteractionSource = source, Description = "More options",
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Foreground = new SolidColorBrush(Color.Parse(config.Dark ? "#A0D0BCFF" : "#40208070")),
            Shape = config.Cut ? new CutCornerShape { TopStart = new(2, RelativeUnit.Absolute), TopEnd = new(8, RelativeUnit.Absolute),
                BottomEnd = new(12, RelativeUnit.Absolute), BottomStart = new(4, RelativeUnit.Absolute) } : null
        };
        if (config.CustomColors)
        {
            indicator.ContainerColor = Color.Parse("#E8DEF8"); indicator.ContentColor = Color.Parse("#7F173D8F");
            indicator.DisabledContainerColor = Color.Parse("#FFAACC"); indicator.DisabledContentColor = Color.Parse("#551122EE");
        }
        var root = new Canvas { Background = Brushes.White, Children = { indicator } };
        Canvas.SetLeft(indicator, 96); Canvas.SetTop(indicator, 56);
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        var button = indicator.GetVisualDescendants().OfType<IconButton>().Single();
        var pointer = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        var press = new MaterialInteractionEvent.Press(new Rect(button.Bounds.Size).Center);
        var hover = new MaterialInteractionEvent.HoverEnter();
        var focus = new MaterialInteractionEvent.Focus();
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new("indicator", "container", null, AvaloniaMeasurements.Geometry(indicator, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
         new("state", "state", null, [Measurement.Flag("showing", indicator.MenuState!.IsShowing, definition: "appbar.menu.showing"),
             Measurement.Flag("originalShowing", initialState.IsShowing, definition: "appbar.menu.originalShowing"),
             Measurement.Flag("enabled", indicator.IsEnabled, definition: "appbar.indicator.enabled")])]));
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "input", id); host.Draw(); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Click() { host.Input("move", pointer); host.Input("down", pointer); host.Input("up", pointer); host.Input("move", new(8, 8)); }
        Sample("initial"); if (!dynamic) return;
        switch (config.Action)
        {
            case "disabled-hover":
                Change(1000, "enter", () => host.Input("move", pointer)); Times(16000, 64000, 224000, 400000);
                Change(401000, "disabled-click", () => { host.Input("down", pointer); host.Input("up", pointer); }); Times(416000);
                Change(417000, "exit", () => host.Input("move", new(8, 8))); Times(432000, 512000, 800000); break;
            case "click":
                Change(1000, "press", () => { host.Input("move", pointer); host.Input("down", pointer); }); Times(16000, 64000);
                Change(65000, "release", () => { host.Input("up", pointer); host.Input("move", new(8, 8)); }); Times(80000, 160000, 400000);
                Change(401000, "close", Click); Times(416000, 480000, 800000); break;
            case "disable":
                Change(1000, "press", () => { host.Input("move", pointer); host.Input("down", pointer); }); Times(16000, 64000);
                Change(65000, "disable", () => indicator.IsEnabled = false); Times(80000, 128000);
                Change(129000, "release", () => { host.Input("up", pointer); host.Input("move", new(8, 8)); }); Times(160000, 400000);
                Change(401000, "enable", () => indicator.IsEnabled = true); Times(416000, 800000); break;
            case "state-replace":
                Change(1000, "open-original", Click); Times(16000, 64000);
                Change(65000, "replace", () => indicator.MenuState = new AppBarMenuState()); Times(80000);
                Change(81000, "open-current", Click); Times(96000, 160000);
                Change(161000, "close-current", Click); Times(176000, 400000); break;
            case "source-replace":
                Change(1000, "press", () => source.Emit(press)); Times(16000, 64000);
                var old = source;
                Change(65000, "replace", () => indicator.InteractionSource = source = new()); Times(80000, 128000);
                Change(129000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(press))); Times(160000);
                Change(161000, "new-hover", () => source.Emit(hover)); Times(176000, 240000);
                Change(241000, "new-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); Times(256000, 400000); break;
            case "source":
                Change(1000, "hover", () => source.Emit(hover)); Times(16000, 64000);
                Change(65000, "focus", () => source.Emit(focus)); Times(80000, 128000);
                Change(129000, "press", () => source.Emit(press)); Times(144000, 192000);
                Change(193000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press))); Times(208000, 240000);
                Change(241000, "unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(focus))); Times(256000, 320000);
                Change(321000, "exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); Times(336000, 400000, 800000); break;
        }
    }
}
