using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ScrollFieldConfig(int Count = 7, int Index = 3, double Height = 200, double Width = 120,
    bool Rtl = false, bool Dark = false, string Action = "none");

[ParitySuite("scroll-field")]
public sealed class ScrollFieldCases
{
    public static IEnumerable<CaseData<ScrollFieldConfig>> StaticConfigurations()
    {
        foreach (var height in new[] { 120d, 200, 201.25 })
        foreach (var rtl in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.h{height}.{(rtl ? "rtl" : "ltr")}.{(dark ? "dark" : "light")}", new(Height: height, Rtl: rtl, Dark: dark));
        foreach (var count in new[] { 1, 2, 60 }) yield return Data($"static.count-{count}", new(Count: count, Index: count - 1));
        yield return Data("static.narrow", new(Width: 16));
    }
    public static IEnumerable<CaseData<ScrollFieldConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "jump", "animate", "far", "reverse", "wrap", "click-wrap", "cancel", "replace" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}", new(Count: action == "far" ? 60 : 7, Index: action.Contains("wrap") ? 6 : 3, Rtl: rtl, Action: action));
    }
    private static CaseData<ScrollFieldConfig> Data(string id, ScrollFieldConfig c) => new(id, id, c);
    [ParityScenario("static", Data = nameof(StaticConfigurations))] public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))] public Task Dynamic(CaptureWriter writer) => Capture(writer);
    internal static async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<ScrollFieldConfig>();
        await using var host = new AvaloniaHost(writer, 200, 280, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, c.Dark);
        // The same fixed Compose palette input, rather than the app's generated MCU palette.
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.SurfaceContainerLowest] = Color.Parse(c.Dark ? "#0F0D13" : "#FFFFFF");
        var state = new ScrollFieldState(c.Count, c.Index);
        var field = new ScrollField { State = state, Width = c.Width, Height = c.Height,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            ItemTemplate = new FuncDataTemplate<ScrollFieldItem>((item, _) => new Marker(item!)) };
        host.Mount(new Grid { Margin = new Thickness(24), Children = { field } }); host.Frame(0);
        void Sample(string id)
        {
            var panel = field.GetVisualDescendants().OfType<ScrollFieldPanel>().Single();
            var markers = panel.GetVisualDescendants().OfType<Marker>().ToArray();
            var selected = markers.SingleOrDefault(m => m.Item.IsSelected);
            var points = new List<ProbePoint> { new("state", "state", null, [
                Measurement.Numeric("selected", state.SelectedOption, "index", definition: "scrollField.selected"),
                Measurement.Numeric("target", state.TargetOption, "index", definition: "scrollField.target"),
                Measurement.Flag("scrolling", state.IsScrollInProgress, definition: "scrollField.scrolling")]) };
            if (selected is not null) points.Add(new("selected", "item", null, [
                Measurement.Numeric("y", selected.TranslatePoint(default, field)!.Value.Y, "dip", definition: "scrollField.selected.y") ]));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action.StartsWith("key-") || c.Action.StartsWith("mouse-"))
        {
            for (long time = 16000; time <= 960000; time += 16000)
            {
                host.AdvanceTo(time);
                if (c.Action.StartsWith("key-") && (time == 16000 || c.Action == "key-repeat" && time is 32000 or 48000))
                {
                    field.Focus();
                    var key = c.Action == "key-up" ? Key.Up : Key.Down;
                    host.Window.KeyPress(key, RawInputModifiers.None, key == Key.Up ? PhysicalKey.ArrowUp : PhysicalKey.ArrowDown, null);
                    host.Window.KeyRelease(key, RawInputModifiers.None, key == Key.Up ? PhysicalKey.ArrowUp : PhysicalKey.ArrowDown, null);
                    writer.Record(time, "input", $"key {key}");
                }
                if (c.Action.StartsWith("mouse-"))
                {
                    var y = c.Action == "mouse-current" ? 124 : 190;
                    if (time == 16000) host.Input("move", new(84, y));
                    if (time == 32000) host.Input("down", new(84, y));
                    if (time == 80000) host.Input("up", new(84, y));
                }
                host.Frame(time);
                if (time <= 160000 || time % 80000 == 0) Sample($"time-{time}");
            }
            return;
        }
        if (c.Action.StartsWith("wheel-"))
        {
            var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
            var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
            var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
            var mouse = (IMouseDevice)impl.GetType().GetProperty("MouseDevice")!.GetValue(impl)!;
            for (long time = 16000; time <= 960000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 16000 || c.Action == "wheel-burst" && time is 32000 or 48000 || c.Action == "wheel-reverse" && time == 48000)
                {
                    var delta = c.Action == "wheel-small" ? -.1 : c.Action == "wheel-large" ? -12 : -3;
                    if (c.Action == "wheel-reverse" && time == 48000) delta = 6;
                    input((RawInputEventArgs)Activator.CreateInstance(typeof(RawMouseWheelEventArgs), mouse, (ulong)(1000 + time / 1000), root,
                        new Point(84, 124), new Vector(0, delta), RawInputModifiers.None)!);
                    writer.Record(time, "input", $"wheel {delta}");
                }
                host.Frame(time);
                if (time <= 240000 || time % 80000 == 0) Sample($"time-{time}");
            }
            return;
        }
        if (c.Action.StartsWith("touch-"))
        {
            using var touch = Activator.CreateInstance<TouchDevice>();
            var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
            var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
            var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
            for (long time = 16000; time <= 1280000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 64000 && c.Action == "touch-navigation")
                {
                    _ = state.AnimateScrollToOptionAsync(0);
                    writer.Record(time, "mutation", "default-priority navigation during user drag");
                }
                if (time == 64000 && c.Action == "touch-request")
                {
                    state.SelectedOption = 0;
                    writer.Record(time, "mutation", "selection request during user drag");
                }
                var distance = time switch { 16000 => 0d, 32000 => 10, 48000 => 36, 64000 => 60, 80000 => 88, _ => -1 };
                var release = c.Action == "touch-slow" ? 160000 : 96000;
                if (distance >= 0 || time == release)
                {
                    if (time == release) distance = 88;
                    if (c.Action == "touch-small") distance *= .4;
                    if (c.Action == "touch-backward") distance = -distance;
                    var point = new Point(84, 124 - distance);
                    var type = time == 16000 ? RawPointerEventType.TouchBegin : time == release ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                    input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, (ulong)(1000 + time / 1000), root,
                        type, point, RawInputModifiers.None, 1L)!);
                    writer.Record(time, "input", $"touch {type} ({point.X},{point.Y})");
                }
                host.Frame(time);
                if (time <= 240000 || time % 80000 == 0) Sample($"time-{time}");
            }
            return;
        }
        host.AdvanceTo(16000);
        Task? run = null;
        if (c.Action == "jump") state.SelectedOption = 4;
        else if (c.Action == "wrap") run = state.AnimateToPageAsync(state.TargetPage + 1);
        else if (c.Action == "click-wrap") field.AnimatePage(state.CurrentPage - 6);
        else run = state.AnimateScrollToOptionAsync(c.Action == "far" ? 50 : 4);
        host.Draw(); Sample("requested");
        for (long time = 16000; time <= 960000; time += 16000)
        {
            if (time == 80000)
            {
                host.AdvanceTo(time);
                if (c.Action == "reverse") run = state.AnimateScrollToOptionAsync(2);
                if (c.Action == "cancel") state.SelectedOption = 1;
                if (c.Action == "replace") field.State = state = new(5, 2);
            }
            host.Frame(time);
            if (time <= 160000 || time % 160000 == 0) Sample($"time-{time}");
        }
        if (run is { IsFaulted: true }) await run;
    }
    private sealed class Marker : Control
    {
        // Compose Canvas coordinates are physical even in RTL; do not mirror the test drawing.
        protected override bool BypassFlowDirectionPolicies => true;
        internal ScrollFieldItem Item { get; }
        internal Marker(ScrollFieldItem item)
        {
            Item = item;
            item.PropertyChanged += (_, _) => InvalidateVisual();
        }
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(32, availableSize.Width), Math.Min(24, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Item.IsSelected ? Brushes.Navy : Brushes.CornflowerBlue, new Rect(Bounds.Size));
            context.FillRectangle(Brushes.Gold, new Rect(0, 0, 4 + Item.Index % 7 * 4, 8));
        }
    }
}

[ParitySuite("scroll-field-touch")]
public sealed class ScrollFieldTouchCases
{
    public static IEnumerable<CaseData<ScrollFieldConfig>> Configurations()
    {
        foreach (var action in new[] { "touch-fast", "touch-slow", "touch-small", "touch-backward", "touch-navigation", "touch-request" })
        foreach (var rtl in new[] { false, true })
        {
            var id = $"dynamic.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Rtl: rtl, Action: action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => ScrollFieldCases.Capture(writer);
}

[ParitySuite("scroll-field-wheel")]
public sealed class ScrollFieldWheelCases
{
    public static IEnumerable<CaseData<ScrollFieldConfig>> Configurations()
    {
        foreach (var action in new[] { "wheel-single", "wheel-small", "wheel-large", "wheel-burst", "wheel-reverse" })
        foreach (var rtl in new[] { false, true })
        {
            var id = $"dynamic.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Rtl: rtl, Action: action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => ScrollFieldCases.Capture(writer);
}

[ParitySuite("scroll-field-navigation")]
public sealed class ScrollFieldNavigationCases
{
    public static IEnumerable<CaseData<ScrollFieldConfig>> Configurations()
    {
        foreach (var action in new[] { "key-up", "key-down", "key-repeat", "mouse-current", "mouse-next", "mouse-wrap" })
        foreach (var rtl in new[] { false, true })
        {
            var id = $"dynamic.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Index: action == "mouse-wrap" ? 6 : 3, Rtl: rtl, Action: action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => ScrollFieldCases.Capture(writer);
}
