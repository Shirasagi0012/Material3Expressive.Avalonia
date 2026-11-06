using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SelectionDisplayConfig(string Kind = "checkbox", string State = "off", bool Enabled = true,
    bool Dark = false, bool Rtl = false, double? Width = null, double? Height = null,
    string Action = "none", string Target = "on", bool Icon = false);

[ParitySuite("selection-display")]
public sealed class SelectionDisplayCases
{
    public static IEnumerable<CaseData<SelectionDisplayConfig>> Configurations()
    {
        foreach (var kind in new[] { "checkbox", "radio", "switch" })
        {
            var states = kind == "checkbox" ? new[] { "off", "on", "indeterminate" } : new[] { "off", "on" };
            foreach (var state in states)
            foreach (var enabled in new[] { false, true })
            foreach (var dark in new[] { false, true })
            foreach (var rtl in new[] { false, true })
                yield return Data($"static.{kind}.{state}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}",
                    new(kind, state, enabled, dark, rtl));
            foreach (var size in new[] { 12d, 16, 24, 64 })
                yield return Data($"layout.{kind}.{size}", new(kind, "on", Width: size, Height: size));
            foreach (var state in states)
            foreach (var target in states.Where(target => target != state))
            {
                yield return Data($"transition.{kind}.{state}-{target}", new(kind, state, Action: "transition", Target: target));
                yield return Data($"reverse.{kind}.{state}-{target}", new(kind, state, Action: "reverse", Target: target));
            }
            yield return Data($"disable.{kind}", new(kind, "on", Action: "disable"));
            yield return Data($"input.{kind}", new(kind, Action: "input"));
            if (kind == "switch")
                foreach (var state in states)
                foreach (var rtl in new[] { false, true })
                {
                    yield return Data($"icon.switch.{state}.{(rtl ? "rtl" : "ltr")}", new(kind, state, Rtl: rtl, Icon: true));
                    yield return Data($"source.switch.{state}.{(rtl ? "rtl" : "ltr")}", new(kind, state, Rtl: rtl, Action: "source"));
                }
        }
    }
    private static CaseData<SelectionDisplayConfig> Data(string id, SelectionDisplayConfig config) => new(id, id.Replace('.', ' '), config);
    private static bool? Value(string value) => value switch { "on" => true, "off" => false, _ => null };

    [ParityScenario("display", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SelectionDisplayConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        CaseDefaults.SetThemeColors(host, config.Dark);
        host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(config.Dark ? "#CAC4D0" : "#49454F");
        if (config.Kind == "switch")
        {
            // Match the fixed Compose colorScheme inputs, as in SwitchScene.SetTheme.
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(config.Dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(config.Dark ? "#938F99" : "#79747E");
            host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse(config.Dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(config.Dark ? "#EADDFF" : "#21005D");
        }
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var source = new MaterialInteractionSource();
        TemplatedControl control = config.Kind switch
        {
            "checkbox" => new CheckBoxDisplay { IsChecked = Value(config.State) },
            "radio" => new RadioButtonDisplay { IsChecked = Value(config.State) == true },
            _ => new SwitchDisplay { IsChecked = Value(config.State) == true, InteractionSource = source,
                ThumbIcon = config.Icon ? new Border { Width = 8, Height = 8, Background = Brushes.Red } : null },
        };
        control.IsEnabled = config.Enabled;
        if (config.Width is { } width) control.Width = width;
        if (config.Height is { } height) control.Height = height;
        host.Mount(CaseDefaults.Center(control)); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            bool? state = control switch { CheckBoxDisplay checkbox => checkbox.IsChecked, RadioButtonDisplay radio => radio.IsChecked, SwitchDisplay toggle => toggle.IsChecked, _ => false };
            writer.Submit(host.Snapshot(id, id, [new("indicator", "indicator", null,
                [.. AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size"),
                    Measurement.Text("state", state switch { true => "on", false => "off", _ => "indeterminate" }, definition: "selection.display.state")])]));
        }
        void SetState(string value)
        {
            if (control is CheckBoxDisplay checkbox) checkbox.IsChecked = Value(value);
            else if (control is RadioButtonDisplay radio) radio.IsChecked = Value(value) == true;
            else ((SwitchDisplay)control).IsChecked = Value(value) == true;
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (config.Action == "none") return;
        if (config.Action == "source")
        {
            var press = new MaterialInteractionEvent.Press(new Point(26, 16));
            Change(1000, "press", () => source.Emit(press)); Times(16000, 64000);
            Change(65000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
            Times(80000, 112000, 160000, 240000, 400000, 800000); return;
        }
        if (config.Action == "input")
        {
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;
            Change(1000, "press", () => { host.Input("move", center); host.Input("down", center); });
            Times(16000, 64000);
            Change(65000, "release", () => host.Input("up", center)); Times(80000, 400000); return;
        }
        Change(1000, "change", () => { if (config.Action == "disable") control.IsEnabled = false; else SetState(config.Target); });
        Times(16000, 32000, 64000);
        if (config.Action == "reverse") Change(65000, "reverse", () => SetState(config.State));
        if (config.Action == "disable") Change(65000, "enable", () => control.IsEnabled = true);
        Times(80000, 112000, 160000, 240000, 400000, 800000);
    }
}
