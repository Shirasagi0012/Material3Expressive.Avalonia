using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record SelectionSourceConfig(string Kind = "checkbox", string Action = "press");

[ParitySuite("selection-source")]
public sealed class SelectionInteractionSourceCases
{
    public static IEnumerable<CaseData<SelectionSourceConfig>> Configurations()
    {
        foreach (var kind in new[] { "checkbox", "tri", "radio" })
        foreach (var action in new[] { "press", "overlap", "replace", "disable", "hover-drag", "reattach", "null-native" })
            yield return new($"{kind}.{action}", $"{kind} external source {action}", new(kind, action));
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SelectionSourceConfig>();
        await using var host = new AvaloniaHost(writer, 160, 128);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse("#49454F");
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurface] = Color.Parse("#1D1B20");
        ToggleButton control = c.Kind == "radio" ? new RadioButton() : new CheckBox { IsThreeState = c.Kind == "tri", IsChecked = c.Kind == "tri" ? null : false };
        var clicks = 0; control.Click += (_, _) => clicks++;
        var source = new MaterialInteractionSource();
        void SetSource(MaterialInteractionSource? value)
        {
            if (control is RadioButton radio) RadioButtonAppearance.SetInteractionSource(radio, value);
            else CheckBoxAppearance.SetInteractionSource((CheckBox)control, value);
        }
        SetSource(source);
        var content = CaseDefaults.Center(control);
        var root = new Border { Background = Brushes.White, Child = content };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long t) { while (next <= t) { host.Frame(next); next += 16000; } host.AdvanceTo(t); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("control", c.Kind, null,
            [.. AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("state", control.IsChecked switch { true => 1, null => 2, _ => 0 }, "enum", definition: "selection-source.state"),
                Measurement.Flag("enabled", control.IsEffectivelyEnabled, definition: "selection-source.enabled"),
                Measurement.Numeric("clicks", clicks, "count", definition: "selection-source.clicks")])]));
        void Times(params long[] ts) { foreach (var t in ts) { Advance(t); host.Draw(); Sample($"time-{t}"); } }
        void Mutate(long t, string id, Action action, bool sample = true)
        { Advance(t); action(); writer.Record(t, "mutation", id); host.Draw(); host.Frame(t); if (sample) Sample(id); }
        var press = new MaterialInteractionEvent.Press(new Point(24, 24));
        var second = new MaterialInteractionEvent.Press(new Point(24, 24));
        var hover = new MaterialInteractionEvent.HoverEnter();
        var drag = new MaterialInteractionEvent.DragStart();
        Sample("initial");
        if (c.Action == "hover-drag")
        {
            Mutate(1000, "hover", () => source.Emit(hover)); Times(16000, 64000);
            Mutate(65000, "drag", () => source.Emit(drag)); Times(80000, 160000);
            Mutate(161000, "drag-stop", () => source.Emit(new MaterialInteractionEvent.DragStop(drag))); Times(176000, 240000);
            Mutate(241000, "hover-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); Times(256000, 400000);
            return;
        }
        if (c.Action == "null-native")
        {
            Mutate(1000, "local-source", () => SetSource(null));
            var center = control.TranslatePoint(new Rect(control.Bounds.Size).Center, host.Window)!.Value;
            Mutate(17000, "native-down", () => { host.Input("move", center); host.Input("down", center); }); Times(32000, 64000);
            Mutate(65000, "native-up", () => host.Input("up", center)); Times(80000, 240000, 800000);
            return;
        }
        Mutate(1000, "press", () => source.Emit(press)); Times(16000, 64000);
        switch (c.Action)
        {
            case "overlap":
                Mutate(65000, "second-press", () => source.Emit(second));
                Mutate(66000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(press))); Times(80000, 160000, 240000);
                Mutate(241000, "second-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); break;
            case "replace":
                var old = source; source = new(); source.Emit(second);
                Mutate(65000, "replace", () => SetSource(source));
                Mutate(66000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(press))); Times(80000, 160000);
                Mutate(161000, "new-press", () => source.Emit(press)); Times(176000, 240000);
                Mutate(241000, "new-release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
            case "disable":
                Mutate(65000, "disable", () => control.IsEnabled = false); Times(80000, 160000);
                Mutate(161000, "external-release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
                Mutate(177000, "disabled-press", () => source.Emit(second)); Times(192000, 240000);
                Mutate(241000, "enable", () => control.IsEnabled = true);
                Mutate(242000, "release", () => source.Emit(new MaterialInteractionEvent.Release(second))); break;
            case "reattach":
                Mutate(65000, "detach", () => root.Child = null, false);
                Mutate(66000, "unmounted-press", () => source.Emit(second), false);
                Mutate(81000, "reattach", () => root.Child = content); Times(96000, 160000);
                Mutate(161000, "old-release", () => source.Emit(new MaterialInteractionEvent.Release(second)));
                Mutate(177000, "new-press", () => source.Emit(press)); Times(192000, 240000);
                Mutate(241000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
            default:
                Times(160000, 240000);
                Mutate(241000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
        }
        Times(256000, 320000, 400000, 800000);
    }
}
