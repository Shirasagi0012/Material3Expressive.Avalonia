using Avalonia;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

[ParitySuite("segmented-button-source")]
public sealed class SegmentedButtonSourceCases
{
    public static IEnumerable<CaseData<SegmentedButtonConfig>> Configurations()
    {
        foreach (var multi in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "count", "replace", "hover" })
        {
            var id = $"{(multi ? "multi" : "single")}.{(selected ? "selected" : "unselected")}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id.Replace('.', ' '), new(Multi: multi, Selected: selected, Rtl: rtl,
                CustomColors: true, Action: "source-" + action));
        }
    }
    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SegmentedButtonConfig>();
        await using var host = new AvaloniaHost(writer, 520, 208);
        var scene = new SegmentedButtonCases.Scene(host, config);
        var source = new MaterialInteractionSource(); scene.Items[0].InteractionSource = source;
        host.Mount(CaseDefaults.Center(scene.Row)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        Sample("initial"); long time = 0, next = 16000;
        void Change(string id, Action action)
        {
            time += 64000;
            while (next <= time) { host.Frame(next); next += 16000; }
            host.AdvanceTo(time); action(); writer.Record(time, "source", id); host.Draw(); host.Frame(time); Sample(id);
        }
        var center = new Rect(scene.Items[0].Bounds.Size).Center;
        var press = new MaterialInteractionEvent.Press(center); var second = new MaterialInteractionEvent.Press(center);
        var focus = new MaterialInteractionEvent.Focus(); var hover = new MaterialInteractionEvent.HoverEnter();
        void Emit(string id, MaterialInteractionEvent value) => Change(id, () => source.Emit(value));
        switch (config.Action)
        {
            case "source-count":
                Emit("press", press); Emit("second-press", second); Emit("focus", focus);
                Emit("first-release", new MaterialInteractionEvent.Release(press));
                Emit("cancel", new MaterialInteractionEvent.CancelPress(second));
                Emit("unfocus", new MaterialInteractionEvent.Unfocus(focus));
                Emit("unmatched-release", new MaterialInteractionEvent.Release(press)); break;
            case "source-replace":
                Emit("press", press); var old = source;
                Change("replace", () => scene.Items[0].InteractionSource = source = new());
                Change("old-release", () => old.Emit(new MaterialInteractionEvent.Release(press)));
                Emit("new-release", new MaterialInteractionEvent.Release(press));
                Emit("new-press", second); Emit("cancel", new MaterialInteractionEvent.CancelPress(second)); break;
            case "source-hover":
                Emit("hover", hover); var drag = new MaterialInteractionEvent.DragStart(); Emit("drag", drag);
                Emit("hover-exit", new MaterialInteractionEvent.HoverExit(hover));
                Emit("drag-stop", new MaterialInteractionEvent.DragStop(drag)); break;
        }
        Change("late", () => { });
    }
}
