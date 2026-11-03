using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record TabSourceConfig(bool Native, string Content, string Action, bool Rtl = false);

[ParitySuite("tab-source")]
public sealed class TabSourceCases
{
    public static IEnumerable<CaseData<TabSourceConfig>> Configurations()
    {
        foreach (var native in new[] { false, true })
        {
            var kind = native ? "native" : "controlled";
            foreach (var action in new[] { "overlap", "replace", "disable", "hover-focus", "native-replace" })
                yield return new($"dynamic.{kind}.{action}", $"{kind} {action}", new(native, "stacked", action));
            foreach (var content in new[] { "label", "icon", "leading", "custom" })
                yield return new($"dynamic.{kind}.{content}.rtl", $"{kind} {content} RTL overlap", new(native, content, "overlap", true));
        }
    }
    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TabSourceConfig>();
        await using var host = new AvaloniaHost(writer, 420, 208);
        var scene = new TabRowCases.TabScene(host, new(Native: c.Native, Content: c.Content, Rtl: c.Rtl));
        var item = scene.Items[1]; var root = scene.Root;
        Func<ProbePoint[]> measure = scene.Measure;
        Action<MaterialInteractionSource> setSource = source => TabAppearance.SetInteractionSource(item, source);
        var source = new MaterialInteractionSource(); setSource(source);
        host.Mount(root); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, measure()));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "source", id); host.Draw(); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Sample("initial");
        // Source coordinates belong to the selectable owner, before each component's mapper.
        var first = new MaterialInteractionEvent.Press(new Point(7, 9));
        var second = new MaterialInteractionEvent.Press(new Point(item.Bounds.Width - 9, 15));
        var hover = new MaterialInteractionEvent.HoverEnter(); var focus = new MaterialInteractionEvent.Focus();
        if (c.Action == "native-replace")
        {
            var pointer = item.TranslatePoint(new Rect(item.Bounds.Size).Center, host.Window)!.Value;
            Change(1000, "native-press", () => { host.Input("move", pointer); host.Input("down", pointer); }); At(16000); At(64000);
            Change(65000, "replace", () => setSource(new())); At(80000); At(128000);
            Change(129000, "native-release", () => host.Input("up", pointer)); At(160000); At(400000); At(800000); return;
        }
        if (c.Action == "hover-focus")
        {
            Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000);
            Change(65000, "focus", () => source.Emit(focus)); At(80000); At(128000);
            Change(129000, "unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(focus))); At(160000);
            Change(193000, "hover-end", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover)));
        }
        else
        {
            Change(1000, "press", () => source.Emit(first)); At(16000); At(64000);
            switch (c.Action)
            {
                case "overlap":
                    Change(65000, "second-press", () => source.Emit(second)); At(80000); At(128000);
                    Change(129000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                    Change(193000, "last-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); break;
                case "replace":
                    var old = source;
                    Change(65000, "replace", () => setSource(source = new())); At(80000); At(128000);
                    Change(129000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                    Change(193000, "new-press", () => source.Emit(second));
                    Change(241000, "new-release", () => source.Emit(new MaterialInteractionEvent.Release(second))); break;
                case "disable":
                    Change(65000, "disable", () => item.IsEnabled = false); At(80000); At(128000);
                    Change(129000, "external-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(160000);
                    Change(193000, "enable", () => item.IsEnabled = true); break;
            }
        }
        At(256000); At(400000); At(800000);
    }
}
