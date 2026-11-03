using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record SwipeToDismissConfig(string Initial = "Settled", bool StartToEnd = true, bool EndToStart = true,
    bool Rtl = false, bool Gestures = true, double Width = 240, bool AutoSize = false, string Action = "none");

[ParitySuite("swipe-to-dismiss")]
public sealed class SwipeToDismissBoxCases
{
    public static IEnumerable<CaseData<SwipeToDismissConfig>> StaticConfigurations()
    {
        foreach (var initial in Enum.GetNames<SwipeToDismissBoxValue>())
        foreach (var rtl in new[] { false, true })
        foreach (var directions in new[] { "both", "start", "end", "neither" })
            yield return Data($"static.{initial.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{directions}",
                new(initial, directions is "both" or "start", directions is "both" or "end", rtl));
        yield return Data("static.auto-size", new(AutoSize: true));
        yield return Data("static.constrained", new(Width: 72));
        yield return Data("static.zero-width", new(Width: 0));
    }
    public static IEnumerable<CaseData<SwipeToDismissConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "dismiss", "reset", "reverse", "resize", "remove-anchor", "snap", "drag-return", "drag-dismiss", "drag-fast", "drag-disabled", "drag-single-direction", "disable-during-drag", "drag-reverse", "drag-interrupt", "drag-dismissed" })
            yield return Data($"dynamic.{action}", new(Initial: action is "reset" or "drag-dismissed" ? "StartToEnd" : "Settled",
                StartToEnd: action != "drag-single-direction", Gestures: action != "drag-disabled", Action: action));
        yield return Data("dynamic.rtl-drag-dismiss", new(Rtl: true, Action: "drag-dismiss"));
        yield return Data("dynamic.end-to-start", new(Action: "dismiss-end"));
    }
    private static CaseData<SwipeToDismissConfig> Data(string id, SwipeToDismissConfig c) => new(id, id.Replace('.', ' '), c);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SwipeToDismissConfig>();
        await using var host = new AvaloniaHost(writer, 520, 160);
        var scene = new Scene(host, c); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SwipeToDismissConfig>();
        await using var host = new AvaloniaHost(writer, 520, 160);
        var scene = new Scene(host, c); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long t) { while (nextFrame <= t) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(t); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        void Times(params long[] times) { foreach (var t in times) { Advance(t); host.Draw(); Sample($"time-{t}"); } }
        void Mutate(long t, string id, Action action, bool sample = true) { Advance(t); action(); writer.Record(t, "mutation", id); host.Draw(); if (sample) Sample(id); }
        void Input(long t, string action, Point p) { Advance(t); scene.Input(action, p); host.Draw(); }
        Sample("initial");
        if (c.Action is "dismiss" or "dismiss-end" or "reset" or "reverse" or "resize" or "remove-anchor" or "drag-interrupt")
        {
            Mutate(1000, "state-change", () => { _ = c.Action == "reset" ? scene.Box.State.ResetAsync() : scene.Box.State.DismissAsync(c.Action == "dismiss-end" ? SwipeToDismissBoxValue.EndToStart : SwipeToDismissBoxValue.StartToEnd); });
            Times(16000, 64000, 96000);
            if (c.Action == "reverse") Mutate(97000, "reverse", () => { _ = scene.Box.State.DismissAsync(SwipeToDismissBoxValue.EndToStart); });
            if (c.Action == "resize") Mutate(97000, "resize", () => scene.Box.Width = 280, false);
            if (c.Action == "remove-anchor") Mutate(97000, "remove-anchor", () => scene.Box.EnableDismissFromStartToEnd = false, false);
            if (c.Action == "drag-interrupt")
            {
                Input(98000, "down", new Point(220, 64)); Sample("interrupt-down");
                Input(114000, "move", new Point(150, 64)); Sample("interrupt-move");
                Input(170000, "up", new Point(150, 64)); Sample("interrupt-up");
            }
            Times(192000, 320000, 416000, 640000);
        }
        else if (c.Action == "snap")
        {
            Mutate(1000, "snap-dismiss", () => { _ = scene.Box.State.SnapToAsync(SwipeToDismissBoxValue.EndToStart); }); Times(16000);
            Mutate(17000, "snap-reset", () => { _ = scene.Box.State.SnapToAsync(SwipeToDismissBoxValue.Settled); }); Times(32000);
        }
        else
        {
            var origin = new Point(c.Rtl ? 460 : 60, 64);
            var sign = c.Rtl ? -1 : 1;
            Input(1000, "move", origin); Input(2000, "down", origin); Sample("drag-down");
            Input(18000, "move", origin + new Vector(32 * sign, 0)); Sample("drag-start");
            var distance = c.Action == "drag-return" ? 70 : 170;
            var end = origin + new Vector(distance * sign, 0);
            Input(34000, "move", end); Sample("drag-move");
            if (c.Action == "disable-during-drag") Mutate(35000, "disable", () => scene.Box.GesturesEnabled = false, false);
            if (c.Action == "drag-reverse") { end = origin + new Vector(-35 * sign, 0); Input(50000, "move", end); Sample("drag-reverse"); }
            Input(c.Action == "drag-fast" ? 35000 : 100000, "up", end); Sample("drag-up");
            Times(112000, 160000, 256000, 416000, 640000);
        }
    }

    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly IMouseDevice _mouse;
        private bool _pressed;
        private int _dismissals;
        private string _lastDismissal = "none";
        public SwipeToDismissBox Box { get; }
        public Control Root { get; }
        private readonly Border _foreground;
        private readonly Border _background;
        public Scene(AvaloniaHost host, SwipeToDismissConfig c)
        {
            _host = host;
            _mouse = (IMouseDevice)host.Window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(host.Window.PlatformImpl)!;
            // Compose size(24.dp) obeys the parent's maximum width. Avalonia's explicit Width
            // can overflow a zero-width Border, so express that same fixture constraint here.
            _foreground = new Border { Background = Brush.Parse("#336699"), Child = new Border { Width = Math.Min(24, c.AutoSize ? 96 : c.Width), Height = 24, Background = Brush.Parse("#FFD166"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center } };
            _background = new Border { Background = Brush.Parse("#993344") };
            if (c.AutoSize) { _foreground.Width = 96; _foreground.Height = 40; }
            Box = new SwipeToDismissBox
            {
                Width = c.AutoSize ? double.NaN : c.Width, Height = c.AutoSize ? double.NaN : 64,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Content = _foreground, BackgroundContent = _background,
                State = new(Enum.Parse<SwipeToDismissBoxValue>(c.Initial)),
                EnableDismissFromStartToEnd = c.StartToEnd, EnableDismissFromEndToStart = c.EndToStart, GesturesEnabled = c.Gestures
            };
            Box.Dismissed += (_, e) => { _dismissals++; _lastDismissal = e.Direction.ToString(); };
            Root = new Border { Padding = new Thickness(32), Background = Brushes.White, Child = Box, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        }
        public void Input(string action, Point p)
        {
            var type = action switch { "down" => RawPointerEventType.LeftButtonDown, "up" => RawPointerEventType.LeftButtonUp, _ => RawPointerEventType.Move };
            var platform = _host.Window.PlatformImpl!;
            var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), _mouse,
                (ulong)(1000 + Math.Max(0, _host.TimeUs) / 1000), platform.GetType().GetProperty("InputRoot")!.GetValue(platform), type, p,
                action == "move" && _pressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!;
            ((Action<RawInputEventArgs>)platform.GetType().GetProperty("Input")!.GetValue(platform)!)(raw);
            if (action == "down") _pressed = true; if (action == "up") _pressed = false;
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, _host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var state = Box.State;
            return [
                new("container", "container", null, [.. Geometry(Box),
                    Measurement.Text("currentValue", state.CurrentValue.ToString(), definition: "swipe.current-value"),
                    Measurement.Text("targetValue", state.TargetValue.ToString(), definition: "swipe.target-value"),
                    Measurement.Text("settledValue", state.SettledValue.ToString(), definition: "swipe.settled-value"),
                    Measurement.Text("dismissDirection", state.DismissDirection.ToString(), definition: "swipe.dismiss-direction"),
                    Measurement.Numeric("offset", state.CurrentOffset, "dip", "local", "motion", "swipe.offset", "SwipeToDismissBoxState.CurrentOffset"),
                    Measurement.Numeric("progress", state.Progress, "fraction", definition: "swipe.progress"),
                    Measurement.Numeric("dismissals", _dismissals, "count", definition: "swipe.dismissals"),
                    Measurement.Text("lastDismissal", _lastDismissal, definition: "swipe.last-dismissal")]),
                new("foreground", "foreground", "container", Geometry(_foreground)),
                new("background", "background", "container", Geometry(_background))
            ];
        }
    }
}
