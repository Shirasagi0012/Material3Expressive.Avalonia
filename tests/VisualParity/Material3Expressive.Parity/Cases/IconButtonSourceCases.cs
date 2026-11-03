using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

[ParitySuite("icon-button-source")]
public sealed class IconButtonSourceCases
{
    public static IEnumerable<CaseData<IconButtonConfig>> Configurations()
    {
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var morph in new[] { false, true })
        foreach (var action in new[] { "overlap", "replace", "native-replace", "hover", "disable" })
        {
            var id = $"{style}.{(morph ? "morph" : "fixed")}.{action}";
            yield return new(id, id.Replace('.', ' '), new(Style: style, PressedRadius: morph ? 8 : null, Action: "source-" + action));
        }
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var rtl in new[] { false, true })
            yield return new($"{style}.shape-cycle.{(rtl ? "rtl" : "ltr")}", $"{style} shape collector restart",
                new(Style: style, Rtl: rtl, PressedRadius: 8, Action: "shape-cycle"));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<IconButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new IconButtonCases.IconScene(host, config);
        var source = new MaterialInteractionSource(); scene.Button.InteractionSource = source;
        host.Mount(CaseDefaults.Center(scene.Button)); scene.ConfigureVector();
        host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        Sample("initial");
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "source", id); host.Draw(); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        // The local source uses owner-local DIP. Core uses clickable-local pixels; both emit at
        // the measured visual center, without rewriting any resulting geometry or ripple.
        var center = new Rect(scene.Button.Bounds.Size).Center;
        var first = new MaterialInteractionEvent.Press(center);
        var second = new MaterialInteractionEvent.Press(center);
        var hover = new MaterialInteractionEvent.HoverEnter();
        if (config.Action == "source-native-replace")
        {
            var pointer = scene.Button.TranslatePoint(center, host.Window)!.Value;
            Change(1000, "native-press", () => { host.Input("move", pointer); host.Input("down", pointer); }); At(16000); At(64000);
            Change(65000, "replace", () => scene.Button.InteractionSource = new()); At(80000); At(128000);
            Change(129000, "native-release", () => host.Input("up", pointer)); At(160000); At(400000);
            Change(401000, "new-native-press", () => host.Input("down", pointer)); At(480000);
            Change(481000, "new-native-release", () => host.Input("up", pointer)); At(800000);
            return;
        }
        if (config.Action == "source-hover")
        {
            var other = new MaterialInteractionEvent.HoverEnter();
            Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000);
            Change(65000, "second-hover", () => source.Emit(other)); At(80000);
            Change(81000, "first-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); At(112000);
            Change(113000, "last-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(other))); At(160000); At(400000);
            return;
        }
        Change(1000, "press", () => source.Emit(first)); At(16000); At(64000);
        switch (config.Action)
        {
            case "shape-cycle":
                Change(65000, "static-shape", () => MaterialButton.SetPressedCornerRadius(scene.Button, null)); At(80000); At(128000);
                Change(129000, "enable-morph", () => MaterialButton.SetPressedCornerRadius(scene.Button, new(8))); At(160000);
                Change(193000, "new-press", () => source.Emit(second)); break;
            case "source-overlap":
                Change(65000, "second-press", () => source.Emit(second)); At(80000); At(128000);
                Change(129000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "last-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); break;
            case "source-replace":
                var old = source;
                Change(65000, "replace", () => scene.Button.InteractionSource = source = new()); At(80000); At(128000);
                Change(129000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "new-hover", () => source.Emit(hover)); break;
            case "source-disable":
                Change(65000, "disable", () => scene.Button.IsEnabled = false); At(80000); At(128000);
                Change(129000, "external-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                Change(193000, "enable", () => scene.Button.IsEnabled = true); break;
        }
        At(208000); At(240000); At(400000); At(800000);
    }
}
