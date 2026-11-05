using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record FabSourceConfig(string Kind, string Size, string Action, bool Rtl = false);

[ParitySuite("fab-source")]
public sealed class FabSourceCases
{
    public static IEnumerable<CaseData<FabSourceConfig>> Configurations()
    {
        foreach (var kind in new[] { "icon", "extended", "content" })
        foreach (var action in new[] { "overlap", "replace", "replace-before-frame", "replace-revert", "hover-replace", "native-replace", "configuration", "configuration-interrupt", "configuration-coalesce" })
            yield return new($"dynamic.{kind}.{action}", $"{kind} {action}", new(kind, "default", action));
        foreach (var size in new[] { "small", "medium", "large" })
        foreach (var kind in new[] { "icon", "extended" })
            yield return new($"dynamic.{kind}.{size}.rtl", $"{kind} {size} RTL overlap", new(kind, size, "overlap", true));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<FabSourceConfig>();
        await using var host = new AvaloniaHost(writer, 420, 240);
        var scene = new FloatingActionButtonCases.FabScene(host, new(Kind: c.Kind, Size: c.Size, Rtl: c.Rtl, Elevation: "custom"));
        var fab = scene.Fab!;
        var source = new MaterialInteractionSource(); fab.InteractionSource = source;
        host.Mount(scene.Root); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            var points = scene.Measure();
            var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
            points[0] = points[0] with { Measurements = [..points[0].Measurements,
                Measurement.Numeric("shadowElevation", surface.ShadowElevation, category: "motion",
                    definition: "fab.shadow-elevation", source: "MaterialSurface.ShadowElevation consumed by DrawShadow")] };
            writer.Submit(host.Snapshot(id, id, points));
        }
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "source", id); host.Draw(); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Sample("initial");
        var center = new Rect(fab.Bounds.Size).Center;
        var first = new MaterialInteractionEvent.Press(center); var second = new MaterialInteractionEvent.Press(center);
        var hover = new MaterialInteractionEvent.HoverEnter(); var focus = new MaterialInteractionEvent.Focus();
        if (c.Action == "native-replace")
        {
            var pointer = fab.TranslatePoint(center, host.Window)!.Value;
            Change(1000, "native-press", () => { host.Input("move", pointer); host.Input("down", pointer); }); At(16000); At(64000);
            Change(65000, "replace", () => fab.InteractionSource = new()); At(80000); At(128000);
            Change(129000, "native-release", () => host.Input("up", pointer)); At(160000); At(400000); At(800000); return;
        }
        if (c.Action == "hover-replace")
        {
            Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000); At(160000);
            Change(193000, "replace", () => fab.InteractionSource = source = new()); At(208000); At(240000); At(400000);
            Change(401000, "new-focus", () => source.Emit(focus)); At(480000); At(640000);
            Change(641000, "unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(focus))); At(800000); return;
        }
        Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000);
        if (c.Action.StartsWith("configuration", StringComparison.Ordinal))
        {
            Change(65000, "unused-height", () => fab.Elevation = new(2, 40, 60, 9)); At(80000); At(128000);
            Change(129000, "press", () => source.Emit(first)); At(160000); At(208000);
            Change(209000, "active-height", () => fab.Elevation = new(2, 5, 60, 9));
            if (c.Action == "configuration-coalesce")
                Change(210000, "latest-height", () => fab.Elevation = new(2, 7, 60, 9));
            At(224000);
            if (c.Action == "configuration-interrupt")
            {
                Change(225000, "cancel-after-effect", () => source.Emit(new MaterialInteractionEvent.CancelPress(first)));
                At(240000); At(256000); At(320000); At(400000); At(800000); return;
            }
            At(240000);
            Change(241000, "cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(first)));
            At(320000); At(400000); At(800000); return;
        }
        Change(65000, "press", () => source.Emit(first)); At(80000); At(128000);
        if (c.Action is "replace-before-frame" or "replace-revert")
        {
            var original = source;
            var skipped = new MaterialInteractionSource();
            Change(129000, "replace-first", () => fab.InteractionSource = source = skipped);
            if (c.Action == "replace-revert")
            {
                Change(130000, "restore-original", () => fab.InteractionSource = source = original);
                Change(131000, "unobserved-focus", () => skipped.Emit(focus)); At(144000);
                Change(145000, "original-cancel", () => original.Emit(new MaterialInteractionEvent.CancelPress(first)));
            }
            else
            {
                Change(130000, "replace-latest", () => fab.InteractionSource = source = new());
                Change(131000, "skipped-press", () => skipped.Emit(second));
                Change(132000, "unobserved-focus", () => source.Emit(focus));
                Change(133000, "original-focus", () => original.Emit(focus)); At(144000);
                Change(145000, "skipped-hover", () => skipped.Emit(hover));
                Change(146000, "latest-hover", () => source.Emit(new MaterialInteractionEvent.HoverEnter()));
            }
            At(160000); At(208000); At(400000); At(800000); return;
        }
        if (c.Action == "overlap")
        {
            Change(129000, "second-press", () => source.Emit(second)); At(160000);
            Change(193000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(first))); At(208000); At(240000);
            Change(241000, "last-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); At(320000);
            Change(321000, "hover-end", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover)));
        }
        else
        {
            var old = source;
            Change(129000, "replace", () => fab.InteractionSource = source = new()); At(160000);
            Change(193000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(first))); At(208000); At(240000);
            Change(241000, "new-focus", () => source.Emit(focus)); At(320000);
            Change(321000, "unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(focus)));
        }
        At(400000); At(800000);
    }
}
