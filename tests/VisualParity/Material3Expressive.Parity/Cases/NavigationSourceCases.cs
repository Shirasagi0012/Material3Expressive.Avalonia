using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record NavigationSourceConfig(string Kind, string Action, string Labels = "always", bool Rtl = false, double IconWidth = 24);

[ParitySuite("navigation-source")]
public sealed class NavigationSourceCases
{
    public static IEnumerable<CaseData<NavigationSourceConfig>> Configurations()
    {
        foreach (var kind in new[] { "bar", "rail", "short-top", "short-start" })
        {
            foreach (var action in new[] { "overlap", "replace", "disable", "hover-focus", "native-replace" })
                yield return new($"dynamic.{kind}.{action}", $"{kind} {action}", new(kind, action));
            yield return new($"dynamic.{kind}.no-label-rtl", $"{kind} icon-only RTL mapping", new(kind, "overlap", "none", true, 32));
        }
        foreach (var kind in new[] { "bar", "rail" })
            yield return new($"dynamic.{kind}.selected-label", $"{kind} selected-only label mapping", new(kind, "overlap", "selected", true));
        foreach (var kind in new[] { "short-top", "short-start" })
            yield return new($"dynamic.{kind}.position", $"{kind} native press during position change", new(kind, "position"));
        foreach (var action in new[] { "overlap", "replace", "hover-focus", "native-replace" })
            yield return new($"dynamic.drawer.{action}", $"drawer {action}", new("drawer", action));
        yield return new("dynamic.drawer.plain-rtl", "drawer plain RTL overlap", new("drawer", "overlap", "none", true));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<NavigationSourceConfig>();
        await using var host = new AvaloniaHost(writer, 420, 340);
        Control item, root; Func<ProbePoint[]> measure; Action<MaterialInteractionSource> setSource;
        Action changePosition = () => { };
        switch (c.Kind)
        {
            case "drawer":
                var drawer = new NavigationDrawerCases.Scene(host, new(Kind: "permanent", Selected: false, Icon: c.Labels != "none", Badge: c.Labels != "none", Rtl: c.Rtl, HostWidth: 420));
                item = drawer.Item; root = drawer.Root; measure = drawer.Measure; setSource = source => drawer.Item.InteractionSource = source; break;
            case "bar":
                var bar = new NavigationBarCases.BarScene(host, new(Labels: c.Labels, Rtl: c.Rtl, IconWidth: c.IconWidth));
                item = bar.Item; root = bar.Root; measure = bar.Measure; setSource = source => bar.Item.InteractionSource = source; break;
            case "rail":
                var rail = new NavigationRailCases.RailScene(host, new(Labels: c.Labels, Rtl: c.Rtl, IconWidth: c.IconWidth));
                item = rail.Item; root = rail.Root; measure = rail.Measure; setSource = source => rail.Item.InteractionSource = source; break;
            default:
                var shortBar = new ShortNavigationBarCases.BarScene(host, new(Labels: c.Labels, Position: c.Kind == "short-start" ? "start" : "top", Rtl: c.Rtl, IconWidth: c.IconWidth));
                item = shortBar.Item; root = shortBar.Root; measure = shortBar.Measure; setSource = source => shortBar.Item.InteractionSource = source;
                changePosition = () => shortBar.Item.IconPosition = c.Kind == "short-start" ? NavigationItemIconPosition.Top : NavigationItemIconPosition.Start; break;
        }
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
        if (c.Action is "native-replace" or "position")
        {
            var pointer = item.TranslatePoint(new Rect(item.Bounds.Size).Center, host.Window)!.Value;
            Change(1000, "native-press", () => { host.Input("move", pointer); host.Input("down", pointer); }); At(16000); At(64000);
            Change(65000, c.Action, c.Action == "position" ? changePosition : () => setSource(new())); At(80000); At(128000);
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
