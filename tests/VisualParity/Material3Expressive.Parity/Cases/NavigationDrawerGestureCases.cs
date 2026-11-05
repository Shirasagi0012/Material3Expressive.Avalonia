using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("navigation-drawer-gesture")]
public sealed class NavigationDrawerGestureCases
{
    public static IEnumerable<CaseData<NavigationDrawerConfig>> Configurations()
    {
        foreach (var kind in new[] { "modal", "dismissible" })
        foreach (var action in new[] { "state", "direction", "gestures", "child", "native-thumb", "child-press" })
        foreach (var rtl in new[] { false, true })
        {
            var id = $"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id.Replace('.', ' '), new(Kind: kind, Open: action is not ("child" or "native-thumb"), Rtl: rtl, Action: action));
        }
    }

    [ParityScenario("interruption", Data = nameof(Configurations))]
    public async Task Interruption(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NavigationDrawerConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, 320);
        var scene = new NavigationDrawerCases.Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        void Input(long time, string action, Point point)
        {
            Advance(time); scene.Input(action, point);
            writer.Record(time, "input", $"{action} ({point.X},{point.Y})",
                new { action, x = point.X, y = point.Y, unit = "dip", space = "scene", pointerType = "mouse",
                    button = action == "move" ? null : "primary" });
            host.Draw();
        }
        var sign = config.Rtl ? -1 : 1;
        if (config.Action is "child" or "native-thumb") sign = -sign;
        var origin = new Point(config.Rtl ? 240 : 280, 250);
        if (config.Action == "child-press")
        {
            origin = scene.Item.TranslatePoint(new Point(80, 28), host.Window)!.Value;
            sign = 0;
        }
        Sample("initial");
        Input(1000, "move", origin); Input(2000, "down", origin);
        Input(18000, "move", origin - new Point(40 * sign, 0)); Sample("drag-start");
        Advance(33000);
        switch (config.Action)
        {
            case "state": scene.ReplaceState(); break;
            case "direction": scene.Root.FlowDirection = config.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft; break;
            case "gestures": case "child-press": ((DismissibleNavigationDrawer)scene.Drawer).GesturesEnabled = false; break;
        }
        writer.Record(33000, "mutation", config.Action); host.Draw(); Sample("changed");
        Advance(48000); host.Draw(); Sample("committed");
        Input(65000, "move", origin - new Point(80 * sign, 0)); Sample("move-after-change");
        Input(90000, "up", origin - new Point(80 * sign, 0)); Sample("release-after-change");
        foreach (var time in new long[] { 96000, 160000, 400000, 800000 })
        { Advance(time); host.Draw(); Sample($"time-{time}"); }
    }
}
