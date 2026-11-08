using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record MenuGroupSourceConfig(string Action = "overlap", string Path = "none", bool Rtl = false);
[ParitySuite("menu-group-source")]
public sealed class MenuGroupSourceCases
{
    public static IEnumerable<CaseData<MenuGroupSourceConfig>> Configurations()
    {
        foreach (var action in new[] { "overlap", "replace", "transient", "reattach", "reverse", "native" })
            yield return new(action, action, new(action));
        foreach (var path in new[] { "cut", "mixed", "generic" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "overlap", "replace", "transient", "reattach", "reverse", "native", "resize", "configure" })
            yield return new($"path.{path}.{rtl}.{action}".ToLowerInvariant(), $"{path} {rtl} {action}", new(action, path, rtl));
    }
    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<MenuGroupSourceConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        MenuCases.SetColors(host, false);
        var source = new MaterialInteractionSource();
        var body = new Grid { Width = 180, Height = 64, Children = {
                new Border { Width = 32, Height = 16, Background = Brushes.Blue } } };
        var group = new DropdownMenuGroup { InteractionSource = source, ShadowElevation = 0,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Items = { body } };
        void SetPath(string path)
        {
            group.Shape = path == "none" ? null : new CutCornerShape { TopStart = new(24, RelativeUnit.Absolute),
                TopEnd = new(4, RelativeUnit.Absolute), BottomEnd = new(16, RelativeUnit.Absolute), BottomStart = new(2, RelativeUnit.Absolute) };
            group.InactiveShape = path == "cut" ? new CutCornerShape(new RelativeScalar(.15, RelativeUnit.Relative))
                : path == "generic" ? new NotchedShape() : null;
        }
        SetPath(c.Path);
        var content = CaseDefaults.Center(group);
        var root = new Border { Background = Brushes.White, Child = content };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long t) { while (next <= t) { host.Frame(next); next += 16000; } host.AdvanceTo(t); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("group", "group", null,
            [.. AvaloniaMeasurements.Geometry(group, host.Window).Where(m => m.Name is "box" or "size")])]));
        void Times(params long[] ts) { foreach (var t in ts) { Advance(t); host.Draw(); Sample($"time-{t}"); } }
        void Mutate(long t, string id, Action action, bool sample = true)
        { Advance(t); action(); writer.Record(t, "mutation", id); host.Draw(); host.Frame(t); if (sample) Sample(id); }
        var first = new MaterialInteractionEvent.HoverEnter(); var second = new MaterialInteractionEvent.HoverEnter();
        Sample("initial");
        if (c.Action == "native")
        {
            Mutate(1000, "enter", () => host.Input("move", new Point(120, 80))); Times(16000, 64000);
            Mutate(65000, "exit", () => host.Input("move", new Point(8, 8))); Times(80000, 160000, 400000);
            Mutate(401000, "enter-again", () => host.Input("move", new Point(120, 80))); Times(416000, 480000, 800000); return;
        }
        Mutate(1000, "hover", () => { source.Emit(first); if (c.Action == "transient") source.Emit(new MaterialInteractionEvent.HoverExit(first)); });
        Times(16000, 64000);
        switch (c.Action)
        {
            case "resize":
                Mutate(65000, "exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(first))); Times(80000, 96000);
                Mutate(97000, "grow", () => body.Height = 96); Times(112000, 160000);
                Mutate(161000, "shrink", () => body.Height = 40); break;
            case "configure":
                Mutate(65000, "exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(first))); Times(80000, 96000);
                Mutate(97000, "mixed", () => SetPath("mixed")); Times(112000);
                Mutate(113000, "generic", () => SetPath("generic")); Times(128000);
                Mutate(129000, "cut", () => SetPath("cut")); Times(144000);
                Mutate(145000, "clear", () => SetPath("none")); break;
            case "replace":
                var old = source; source = new(); source.Emit(second);
                Mutate(65000, "replace", () => group.InteractionSource = source); Times(80000, 160000);
                Mutate(161000, "old-exit", () => old.Emit(new MaterialInteractionEvent.HoverExit(first))); Times(176000, 240000);
                Mutate(241000, "new-nonhover", () => source.Emit(new MaterialInteractionEvent.DragStart())); break;
            case "reattach":
                Mutate(65000, "detach", () => root.Child = null, false);
                Mutate(66000, "unmounted-hover", () => source.Emit(second), false);
                Mutate(81000, "reattach", () => root.Child = content); Times(96000, 160000);
                Mutate(161000, "old-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(second))); break;
            case "overlap":
                Mutate(65000, "second", () => source.Emit(second));
                Mutate(66000, "first-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(first))); Times(80000, 160000);
                Mutate(161000, "second-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(second))); break;
            case "reverse":
                Mutate(65000, "exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(first))); Times(80000, 96000);
                Mutate(97000, "reverse", () => source.Emit(second)); Times(112000, 160000);
                Mutate(161000, "exit-again", () => source.Emit(new MaterialInteractionEvent.HoverExit(second))); break;
        }
        Times(256000, 320000, 400000, 800000);
    }
    private sealed class NotchedShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            var path = new PathGeometry(); using var context = path.Open();
            context.BeginFigure(default, true); context.LineTo(new(size.Width, 0));
            context.LineTo(new(size.Width - 12, size.Height / 2)); context.LineTo(new(size.Width, size.Height));
            context.LineTo(new(0, size.Height)); context.EndFigure(true); return path;
        }
    }
}
