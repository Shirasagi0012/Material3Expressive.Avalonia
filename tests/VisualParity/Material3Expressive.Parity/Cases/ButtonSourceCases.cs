using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record ButtonSourceConfig(string Kind, string Style, string Action, bool Checked = false, bool Rtl = false);

[ParitySuite("button-source")]
public sealed class ButtonSourceCases
{
    public static IEnumerable<CaseData<ButtonSourceConfig>> Configurations()
    {
        // Shape has four distinct owners; colors/elevation vary by style. Existing suites cover sizes.
        foreach (var kind in new[] { "button", "toggle", "leading", "trailing", "trailing-click" })
        foreach (var style in new[] { "filled", "tonal", "elevated", "outlined" })
        foreach (var action in new[] { "overlap", "replace", "disable" })
            yield return new($"dynamic.{kind}.{style}.{action}", $"{kind} {style} {action}", new(kind, style, action));
        foreach (var action in new[] { "overlap", "replace", "disable" })
            yield return new($"dynamic.button.text.{action}", $"button text {action}", new("button", "text", action));
        foreach (var kind in new[] { "button", "toggle", "leading", "trailing" })
        {
            yield return new($"dynamic.{kind}.native-replace", $"{kind} native source replacement", new(kind, "filled", "native-replace"));
            yield return new($"dynamic.{kind}.hover-replace", $"{kind} hover source replacement", new(kind, "elevated", "hover-replace"));
        }
        foreach (var kind in new[] { "toggle", "trailing" })
            yield return new($"dynamic.{kind}.checked-rtl", $"{kind} selected RTL source", new(kind, "filled", "overlap", true, true));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<ButtonSourceConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        Button button; Control root; Func<ProbePoint[]> measure; Action configure = () => { };
        if (c.Kind == "button")
        {
            var scene = new ButtonCases.ButtonScene(host, new(Style: c.Style, Width: null, Rtl: c.Rtl, Action: "source"), writer);
            button = scene.Button; root = button; measure = scene.Measure; configure = () => scene.ConfigureContent();
        }
        else if (c.Kind == "toggle")
        {
            var scene = new ToggleButtonCases.Scene(host, new(Style: c.Style, Checked: c.Checked, Rtl: c.Rtl));
            button = scene.Button; root = button; measure = scene.Measure;
        }
        else
        {
            var scene = new SplitButtonCases.Scene(host, new(Style: c.Style, Checked: c.Checked, Toggle: c.Kind != "trailing-click", Rtl: c.Rtl));
            button = c.Kind == "leading" ? scene.Leading : scene.Trailing; root = scene.Split; measure = scene.Measure;
        }
        var source = new MaterialInteractionSource(); MaterialButton.SetInteractionSource(button, source);
        host.Mount(CaseDefaults.Center(root)); configure(); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, measure()));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "source", id); host.Draw(); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Sample("initial");
        var center = new Rect(button.Bounds.Size).Center;
        var first = new MaterialInteractionEvent.Press(center); var second = new MaterialInteractionEvent.Press(center);
        var hover = new MaterialInteractionEvent.HoverEnter();
        if (c.Action == "native-replace")
        {
            var pointer = button.TranslatePoint(center, host.Window)!.Value;
            Change(1000, "native-press", () => { host.Input("move", pointer); host.Input("down", pointer); }); At(16000); At(64000);
            Change(65000, "replace", () => MaterialButton.SetInteractionSource(button, new())); At(80000); At(128000);
            Change(129000, "native-release", () => host.Input("up", pointer)); At(160000); At(400000);
            Change(401000, "new-press", () => host.Input("down", pointer)); At(480000);
            Change(481000, "new-release", () => host.Input("up", pointer)); At(800000); return;
        }
        if (c.Action == "hover-replace")
        {
            Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000);
            Change(65000, "replace", () => MaterialButton.SetInteractionSource(button, source = new())); At(80000); At(160000);
            Change(193000, "old-end-on-new-source", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); At(208000); At(400000); return;
        }
        Change(1000, "press", () => source.Emit(first)); At(16000); At(64000);
        switch (c.Action)
        {
            case "overlap":
                Change(65000, "second-press", () => source.Emit(second)); At(80000); At(128000);
                Change(129000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "last-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); break;
            case "replace":
                var old = source;
                Change(65000, "replace", () => MaterialButton.SetInteractionSource(button, source = new())); At(80000); At(128000);
                Change(129000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "new-hover", () => source.Emit(hover)); break;
            case "disable":
                Change(65000, "disable", () => button.IsEnabled = false); At(80000); At(128000);
                Change(129000, "external-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "enable", () => button.IsEnabled = true); break;
        }
        At(208000); At(240000); At(400000); At(800000);
    }
}
