using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record BottomSheetConfig(string Initial = "Expanded", double ContentHeight = 320, bool Handle = true,
    bool Partial = true, bool Dark = false, bool Rtl = false, bool Insets = false, bool Gestures = true, bool Veto = false,
    double MaxWidth = 640, string Action = "none", bool Scaffold = false, double Peek = 56,
    bool Modal = false, bool Outside = true, bool Scrim = true);

[ParitySuite("bottom-sheet")]
public sealed class BottomSheetCases
{
    public static IEnumerable<CaseData<BottomSheetConfig>> StaticConfigurations()
    {
        foreach (var initial in new[] { "Expanded", "PartiallyExpanded" })
        foreach (var height in new[] { 80d, 320d })
        foreach (var handle in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.modal.{initial.ToLowerInvariant()}.{height}.{(handle ? "handle" : "plain")}.{(dark ? "dark" : "light")}", new(Initial: initial, ContentHeight: height, Handle: handle, Dark: dark, Modal: true));
        yield return Data("static.modal.insets", new(Insets: true, Modal: true));
        yield return Data("static.modal.rtl-insets", new(Insets: true, Rtl: true, Modal: true));
        yield return Data("static.modal.narrow", new(MaxWidth: 360, Modal: true));
        yield return Data("static.modal.no-scrim", new(Scrim: false, Modal: true));
        foreach (var initial in Enum.GetNames<SheetValue>())
        foreach (var height in new[] { 80d, 320d })
            yield return Data($"static.{initial.ToLowerInvariant()}.{height}", new(Initial: initial, ContentHeight: height));
        yield return Data("static.no-handle", new(Handle: false));
        yield return Data("static.no-partial", new(Partial: false));
        yield return Data("static.dark", new(Dark: true));
        yield return Data("static.insets", new(Insets: true));
        yield return Data("static.rtl-insets", new(Rtl: true, Insets: true));
        yield return Data("static.narrow", new(MaxWidth: 360));
        yield return Data("static.empty", new(ContentHeight: 0, Handle: false));
        foreach (var initial in new[] { "Expanded", "PartiallyExpanded" })
        foreach (var peek in new[] { 0d, 56d, 100d })
            yield return Data($"static.scaffold.{initial.ToLowerInvariant()}.peek-{peek}", new(Initial: initial, Scaffold: true, Peek: peek));
        yield return Data("static.scaffold.equal-peek", new(Initial: "PartiallyExpanded", ContentHeight: 8, Scaffold: true));
        yield return Data("static.scaffold.empty", new(Initial: "PartiallyExpanded", ContentHeight: 0, Handle: false, Scaffold: true));
        yield return Data("static.scaffold.rtl", new(Initial: "PartiallyExpanded", Rtl: true, Scaffold: true));
    }
    public static IEnumerable<CaseData<BottomSheetConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "auto-show", "expand", "hide", "reverse", "outside", "outside-veto", "outside-disabled", "escape-expanded", "escape-partial", "escape-veto", "escape-disabled", "drag-dismiss" })
            yield return Data($"dynamic.modal.{action}", new(Initial: action is "auto-show" or "reverse" ? "Hidden" : action == "escape-expanded" ? "Expanded" : "PartiallyExpanded",
                Veto: action.EndsWith("veto"), Outside: action != "outside-disabled", Modal: true, Action: action));
        yield return Data("dynamic.modal.auto-show-no-partial", new(Initial: "Hidden", Partial: false, Modal: true, Action: "auto-show"));
        foreach (var action in new[] { "show", "expand", "hide", "partial", "reverse", "resize", "grow", "veto", "drag-down", "drag-up", "drag-return", "drag-fast", "drag-disabled", "disable-during-drag", "handle" })
            yield return Data($"dynamic.{action}", new(Initial: action is "show" or "reverse" or "resize" ? "Hidden" : action is "expand" or "grow" or "drag-up" or "handle" ? "PartiallyExpanded" : "Expanded",
                ContentHeight: action == "grow" ? 80 : 320, Veto: action == "veto", Gestures: action != "drag-disabled", Action: action));
        foreach (var action in new[] { "drag-dismiss", "drag-floor-fast", "drag-veto", "handle-disabled", "escape-partial" })
            yield return Data($"dynamic.{action}", new(Initial: "PartiallyExpanded", Veto: action == "drag-veto", Gestures: action != "handle-disabled", Action: action));
        yield return Data("dynamic.escape-expanded", new(Action: "escape-expanded"));
        yield return Data("dynamic.escape-disabled", new(Action: "escape-disabled"));
        foreach (var action in new[] { "expand", "partial", "peek-change", "drag-down", "handle" })
            yield return Data($"dynamic.scaffold.{action}", new(Initial: action is "expand" or "handle" ? "PartiallyExpanded" : "Expanded", Scaffold: true, Action: action));
        yield return Data("dynamic.scaffold.zero-peek-hidden", new(Initial: "PartiallyExpanded", Scaffold: true, Peek: 0, Action: "zero-peek-hidden"));
    }
    private static CaseData<BottomSheetConfig> Data(string id, BottomSheetConfig c) => new(id, id.Replace('.', ' '), c);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<BottomSheetConfig>(); await using var host = new AvaloniaHost(writer, 720, 480);
        var scene = new Scene(host, c);
        var previousCulture = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US"); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0); }
        finally { CultureInfo.CurrentUICulture = previousCulture; }
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<BottomSheetConfig>(); await using var host = new AvaloniaHost(writer, 720, 480);
        var scene = new Scene(host, c);
        var previousCulture = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US"); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0); }
        finally { CultureInfo.CurrentUICulture = previousCulture; }
        long nextFrame = 16000;
        void Advance(long t) { while (nextFrame <= t) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(t); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        void Times(params long[] times) { foreach (var t in times) { Advance(t); host.Draw(); Sample($"time-{t}"); } }
        void Mutate(long t, string id, Action action, bool sample = true) { Advance(t); action(); writer.Record(t, "mutation", id); host.Draw(); if (sample) Sample(id); }
        void Input(long t, string action, Point p) { Advance(t); scene.Input(action, p); host.Draw(); }
        Sample("initial");
        if (c.Action == "auto-show") Times(16000, 64000, 96000, 192000, 416000, 800000);
        else if (c.Action.StartsWith("outside"))
        {
            Input(1000, "move", new Point(10, 10)); Input(2000, "down", new Point(10, 10));
            Input(18000, "up", new Point(10, 10)); Times(32000, 96000, 192000, 416000, 800000);
        }
        else if (c.Action == "zero-peek-hidden")
        {
            Mutate(1000, "settle-hidden", () => { _ = scene.Scaffold!.OnPostFlingAsync(0); }, false); Times(16000);
            Mutate(17000, "restore-peek", () => scene.Scaffold!.SheetPeekHeight = 56, false); Times(32000);
            Mutate(33000, "show", () => { _ = scene.Sheet.State.ShowAsync(); }); Times(48000, 96000, 192000, 416000, 800000);
        }
        else if (c.Action.StartsWith("drag") || c.Action == "disable-during-drag")
        {
            var origin = new Point(300, c.Initial == "Expanded" ? 200 : 320);
            Input(1000, "move", origin); Input(2000, "down", origin); Sample("drag-down");
            var sign = c.Action == "drag-up" ? -1 : 1;
            Input(18000, "move", origin + new Vector(0, 32 * sign)); Sample("drag-start");
            var end = origin + new Vector(0, (c.Action == "drag-return" ? 40 : 150) * sign);
            Input(34000, "move", end); Sample("drag-move");
            if (c.Action == "disable-during-drag") Mutate(35000, "disable", () => scene.Sheet.GesturesEnabled = false, false);
            Input(c.Action.EndsWith("fast") ? 35000 : 100000, "up", end); Sample("drag-up");
            Times(112000, 160000, 256000, 416000, 800000);
        }
        else if (c.Action.StartsWith("escape"))
        {
            Mutate(1000, "escape-down", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null));
            Mutate(2000, "escape-up", () => host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null));
            Times(16000, 64000, 192000, 416000, 800000);
        }
        else if (c.Action.StartsWith("handle"))
        {
            var handlePoint = new Point(360, c.Scaffold ? 480 - c.Peek + 24 : 264);
            Input(1000, "move", handlePoint); Input(2000, "down", handlePoint);
            Input(18000, "up", handlePoint); Times(32000, 96000, 192000, 416000, 800000);
        }
        else
        {
            Mutate(1000, "state-change", () =>
            {
                if (c.Action == "grow") scene.Content.Height = 320;
                else _ = c.Action switch { "hide" or "veto" => scene.Sheet.State.HideAsync(), "partial" or "peek-change" => scene.Sheet.State.PartialExpandAsync(), "expand" => scene.Sheet.State.ExpandAsync(), _ => scene.Sheet.State.ShowAsync() };
            }, c.Action != "grow");
            Times(16000, 64000, 96000);
            if (c.Action == "reverse") Mutate(97000, "reverse", () => { _ = scene.Sheet.State.HideAsync(); });
            if (c.Action == "resize") Mutate(97000, "resize", () => scene.Content.Height = 80, false);
            if (c.Action == "peek-change") Mutate(97000, "peek-change", () => scene.Scaffold!.SheetPeekHeight = 100, false);
            Times(192000, 320000, 416000, 800000);
        }
    }
    private sealed class Scene
    {
        private readonly AvaloniaHost _host; private readonly IMouseDevice _mouse; private bool _pressed; private int _dismissals;
        private readonly BottomSheet? _sheet;
        public BottomSheet Sheet => _sheet ?? Modal?.PresentedSheet ?? Scaffold!.GetVisualDescendants().OfType<BottomSheet>().Single();
        public ModalBottomSheet? Modal { get; }
        public BottomSheetScaffold? Scaffold { get; }
        private readonly Border _body = new() { Background = Brush.Parse("#F3EDF7") };
        private readonly Border _top = new() { Height = 56, Background = Brush.Parse("#E8DEF8") };
        private readonly Border _snack = new() { Width = 120, Height = 32, Background = Brush.Parse("#993344") };
        public Border Content { get; } public Control Root { get; }
        public Scene(AvaloniaHost host, BottomSheetConfig c)
        {
            _host = host; CaseDefaults.SetThemeColors(host, c.Dark);
            host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseSurface] = Color.Parse(c.Dark ? "#E6E0E9" : "#322F35");
            host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseOnSurface] = Color.Parse(c.Dark ? "#322F35" : "#F5EFF7");
            _mouse = (IMouseDevice)host.Window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(host.Window.PlatformImpl)!;
            // Compose size(24.dp) obeys the zero-height Box's maximum constraint. Avalonia's
            // explicit child Height can overflow its Border, so give the marker the same bound.
            Content = new Border { Height = c.ContentHeight, Background = Brush.Parse("#336699"), Child = new Border { Width = 24, Height = Math.Min(24, c.ContentHeight), Background = Brush.Parse("#FFD166"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } };
            if (c.Modal)
            {
                Modal = new ModalBottomSheet { IsOpen = true, State = new(Enum.Parse<SheetValue>(c.Initial), c.Partial ? null : [SheetValue.Hidden, SheetValue.Expanded], v => !c.Veto || v != SheetValue.Hidden),
                    Content = Content, SheetMaxWidth = c.MaxWidth, GesturesEnabled = c.Gestures,
                    ShouldDismissOnBackPress = c.Action != "escape-disabled", ShouldDismissOnClickOutside = c.Outside };
                if (!c.Handle) Modal.DragHandle = null;
                if (c.Insets) Modal.WindowInsets = new Thickness(13, 7, 5, 11);
                if (!c.Scrim) Modal.ScrimColor = null;
                Modal.DismissRequested += (_, _) => _dismissals++;
            }
            else if (c.Scaffold)
            {
                Scaffold = new BottomSheetScaffold { State = new(new SheetState(Enum.Parse<SheetValue>(c.Initial), [SheetValue.PartiallyExpanded, SheetValue.Expanded]), new SnackbarHostState()),
                    SheetContent = Content, SheetPeekHeight = c.Peek, SheetMaxWidth = c.MaxWidth, SheetSwipeEnabled = c.Gestures, TopBar = _top, Content = _body, SnackbarHost = _snack };
                if (!c.Handle) Scaffold.SheetDragHandle = null;
            }
            else
            {
                _sheet = new BottomSheet { State = new(Enum.Parse<SheetValue>(c.Initial), c.Partial ? null : [SheetValue.Hidden, SheetValue.Expanded], v => !c.Veto || v != SheetValue.Hidden), Content = Content, SheetMaxWidth = c.MaxWidth, GesturesEnabled = c.Gestures };
                Sheet.BackHandlerEnabled = c.Action != "escape-disabled";
                if (!c.Handle) Sheet.DragHandle = null;
                if (c.Insets) Sheet.WindowInsets = new Thickness(13, 7, 5, 11);
                Sheet.DismissRequested += (_, _) => _dismissals++;
            }
            Root = new Border { Background = Brushes.White, Child = (Control?)Modal ?? (Control?)Scaffold ?? Sheet, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
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
            var state = Sheet.State; var surface = Sheet.GetVisualDescendants().OfType<MaterialSurface>().First();
            var points = new List<ProbePoint> { new("state", "state", null, [
                Measurement.Text("currentValue", state.CurrentValue.ToString(), definition: "sheet.current-value"),
                Measurement.Text("targetValue", state.TargetValue.ToString(), definition: "sheet.target-value"),
                Measurement.Flag("visible", state.IsVisible, definition: "sheet.visible"),
                Measurement.Flag("animating", state.IsAnimationRunning, definition: "sheet.animating"),
                Measurement.Flag("hasPartial", state.HasPartiallyExpandedState, definition: "sheet.has-partial"),
                Measurement.Numeric("offset", state.CurrentOffset, "dip", "local", "motion", "sheet.offset", "SheetState.requireOffset / density"),
                Measurement.Numeric("dismissals", _dismissals, "count", definition: "sheet.dismissals")]),
                new("sheet", "sheet", null, Geometry(surface)), new("content", "content", "sheet", Geometry(Content)) };
            if (Scaffold is not null) points.AddRange([new("body", "body", null, Geometry(_body)), new("top", "top", null, Geometry(_top)), new("snackbar", "snackbar", null, Geometry(_snack)),
                new("padding", "padding", null, [Measurement.Numeric("bottom", Scaffold.ContentPadding.Bottom, "dip", definition: "sheet.body-bottom-padding")])]);
            return points.ToArray();
        }
    }
}
