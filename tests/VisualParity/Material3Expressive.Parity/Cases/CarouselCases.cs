using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CarouselConfig(string Variant = "MultiBrowse", bool Rtl = false, int Count = 8,
    int Item = 0, double Width = 320, double ItemWidth = 160, double Spacing = 8,
    double Before = 0, double After = 0, double Radius = 16, string Action = "none", double Border = 0,
    string Mode = "Default", double Velocity = 0, double Offset = 0, bool ObserveRealization = false, bool AutoHeight = false);

[ParitySuite("carousel")]
public sealed class CarouselCases
{
    public static IEnumerable<CaseData<CarouselConfig>> StaticConfigurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var item in new[] { 0, 3, 7 })
            yield return Data($"static.{variant}.{(rtl ? "rtl" : "ltr")}.{item}", new(variant, rtl, Item: item));
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        {
            foreach (var count in new[] { 0, 1, 2, 3 })
                yield return Data($"static.{variant}.count-{count}", new(variant, Count: count));
            yield return Data($"static.{variant}.padding-start", new(variant, Before: 24, After: 16));
            yield return Data($"static.{variant}.padding-end", new(variant, Item: 7, Before: 24, After: 16));
            yield return Data($"static.{variant}.narrow", new(variant, Width: 72, ItemWidth: 120));
            yield return Data($"static.{variant}.fractional", new(variant, Width: 319, ItemWidth: 159.3, Spacing: 7.5));
            yield return Data($"static.{variant}.auto-width", new(variant, ItemWidth: 0));
            yield return Data($"static.{variant}.border", new(variant, Border: 2));
        }
    }
    public static IEnumerable<CaseData<CarouselConfig>> DynamicConfigurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "jump", "animate", "far", "reverse", "shape" })
            yield return Data($"dynamic.{variant}.{(rtl ? "rtl" : "ltr")}.{action}", new(variant, rtl, Action: action));
    }
    private static CaseData<CarouselConfig> Data(string id, CarouselConfig config) => new(id.ToLowerInvariant(), id, config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))] public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))] public Task Dynamic(CaptureWriter writer) => Capture(writer);

    internal static async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<CarouselConfig>();
        var resize = c.Action.StartsWith("focus-shrink") || c.Action == "focus-grow";
        await using var host = new AvaloniaHost(writer, resize ? 480 : 400, 180, new ManualTimeProvider());
        var carousel = new HorizontalCarousel { Width = c.Width, Height = c.AutoHeight ? double.NaN : 120, ItemWidth = c.ItemWidth == 0 ? double.NaN : c.ItemWidth,
            Variant = Enum.Parse<CarouselVariant>(c.Variant), ItemSpacing = c.Spacing,
            FlingMode = Enum.Parse<CarouselFlingMode>(c.Mode),
            UserScrollEnabled = c.Action is not ("semantics-disabled" or "wheel-disabled" or "key-disabled" or "focus-disabled" or "focus-shrink-disabled"),
            ContentPadding = new(c.Before, 0, c.After, 0), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var items = Enumerable.Range(0, c.ObserveRealization ? 0 : c.Count).Select(i => new CarouselItem { CornerRadius = new(c.Radius), Content = new Marker(i) { Focusable = c.Action.StartsWith("key-") || c.Action.StartsWith("focus-") },
            BorderBrush = Brushes.Black, BorderThickness = new(c.Border) }).ToArray();
        if (c.ObserveRealization)
        {
            carousel.ItemsSource = Enumerable.Range(0, c.Count).ToArray();
            carousel.ItemTemplate = new FuncDataTemplate<int>((index, _) => new Marker(index)
            { Tag = index, Height = c.AutoHeight ? index < 10 ? 40 : 90 : double.NaN, Focusable = c.Action.StartsWith("nav-") && (c.Action != "nav-sparse" || index % 3 == 0) });
        }
        else foreach (var item in items) carousel.Items.Add(item);
        host.Mount(new Grid { Margin = new Thickness(24), Children = { carousel } }); host.Frame(0);
        carousel.ScrollToItem(c.Item); host.Draw();
        if (c.Offset != 0) { carousel.ScrollOffset += c.Offset; host.Draw(); }
        var focusAccepted = false;
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("viewport", "viewport", null, [
                Measurement.Numeric("currentItem", carousel.CurrentItem, "index", definition: "carousel.currentItem"),
                Measurement.Numeric("scrollOffset", Math.Clamp(carousel.ScrollOffset, 0, carousel.MaximumScrollOffset), "dip", definition: "carousel.scrollOffset")]) };
            if (c.AutoHeight) points.Add(new("height", "viewport", "viewport", [
                Measurement.Numeric("height", carousel.Bounds.Height, "dip", definition: "carousel.viewportHeight")]));
            if (c.ObserveRealization) points.Add(new("pages", "lifecycle", "viewport", [
                Measurement.Text("realized", string.Join(",", carousel.GetRealizedContainers().Select(carousel.IndexFromContainer).Order()),
                    definition: "carousel.measuredPageIndices")]));
            if (c.Action.StartsWith("nav-")) points.Add(new("focus", "input", "viewport", [
                Measurement.Numeric("focusedItem", (host.Window.FocusManager!.GetFocusedElement() as Control)?.Tag is int index ? index : -1,
                    "index", definition: "carousel.focusedItem")]));
            if (c.Action.StartsWith("semantics-")) points.Add(new("actions", "semantics", "viewport", [
                Measurement.Flag("pagingAvailable", ControlAutomationPeer.CreatePeerForElement(carousel)!.GetProvider<IScrollProvider>() is not null,
                    definition: "carousel.pagingAvailable")]));
            if (c.Action.StartsWith("focus-")) points.Add(new("focus", "input", "viewport", [
                Measurement.Flag("accepted", focusAccepted, definition: "carousel.focusRequestAccepted")]));
            if (c.Action.StartsWith("focus-shrink") || c.Action == "focus-grow") points.Add(new("size", "viewport", "viewport", [
                Measurement.Numeric("width", carousel.Bounds.Width, "dip", definition: "carousel.viewportWidth")]));
            if (c.Count > 0)
            {
                var info = ((CarouselItem)carousel.ContainerFromIndex(carousel.CurrentItem)!).DrawInfo;
                points.Add(new("current", "item", "viewport", [
                    Measurement.Numeric("size", info.Size, "dip", definition: "carousel.mask.size"),
                    Measurement.Numeric("minSize", info.MinSize, "dip", definition: "carousel.mask.minimum"),
                    Measurement.Numeric("maxSize", info.MaxSize, "dip", definition: "carousel.mask.maximum") ]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action.StartsWith("nav-"))
        {
            carousel.ContainerFromIndex(c.Item)!.GetVisualDescendants().OfType<Control>().Single(control => control.Tag is int).Focus();
            host.Draw();
            for (long time = 16000; time <= 1280000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time is 16000 or 336000 or 656000 or 976000)
                {
                    var backwards = c.Action == "nav-reverse" && time >= 656000;
                    var key = c.Action != "nav-next" ? Key.Tab : c.Rtl ? Key.Left : Key.Right;
                    var modifiers = backwards ? RawInputModifiers.Shift : RawInputModifiers.None;
                    host.Window.KeyPress(key, modifiers, PhysicalKey.None, null);
                    host.Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
                    writer.Record(time, "input", $"focus navigation {key}");
                }
                host.Frame(time);
                if (time <= 96000 || time % 80000 == 0 || time is 336000 or 656000 or 976000) Sample($"time-{time}");
            }
            return;
        }
        if (c.Action.StartsWith("focus-"))
        {
            for (long time = 16000; time <= 640000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 16000)
                {
                    var index = c.Action == "focus-previous" ? c.Item - 1 : c.Action is "focus-current" or "focus-disabled" ? c.Item : c.Item + 1;
                    focusAccepted = ((Control)items[index].Content!).Focus(); host.Draw();
                    writer.Record(time, "input", $"focus item {index}");
                }
                if (time == 80000 && c.Action == "focus-reverse")
                {
                    focusAccepted = ((Control)items[c.Item - 1].Content!).Focus(); host.Draw();
                    writer.Record(time, "input", $"focus item {c.Item - 1}");
                }
                if (time == 80000 && c.Action == "focus-navigation") { carousel.ScrollToItem(0); host.Draw(); }
                if (time == 80000 && (c.Action.StartsWith("focus-shrink") || c.Action == "focus-grow"))
                {
                    // Compose applies this state change during the next frame's recomposition.
                    // Let Frame draw the invalidated layout after its animation waiter batch.
                    carousel.Width = c.Action == "focus-grow" ? 400 : 240;
                    writer.Record(time, "input", $"viewport width {carousel.Width}");
                }
                host.Frame(time);
                if (time <= 160000 || time % 80000 == 0) Sample($"time-{time}");
            }
            return;
        }
        if (c.Action.StartsWith("key-"))
        {
            ((Control)items[c.Item].Content!).Focus(); host.Draw();
            for (long time = 16000; time <= 320000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 16000 || c.Action == "key-repeat" && time is 32000 or 48000)
                {
                    var key = c.Action == "key-up" ? Key.PageUp : Key.PageDown;
                    host.Window.KeyPress(key, RawInputModifiers.None, key == Key.PageUp ? PhysicalKey.PageUp : PhysicalKey.PageDown, null); host.Window.KeyRelease(key, RawInputModifiers.None, key == Key.PageUp ? PhysicalKey.PageUp : PhysicalKey.PageDown, null);
                    writer.Record(time, "input", $"key {key}");
                }
                host.Frame(time);
                if (time <= 80000 || time % 80000 == 0) Sample($"time-{time}");
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
            for (long time = 16000; time <= 1600000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 16000 || c.Action == "wheel-burst" && time is 32000 or 48000 || c.Action == "wheel-reverse" && time == 48000)
                {
                    var delta = c.Action == "wheel-small" ? -.1 : c.Action == "wheel-large" ? -12 : -3;
                    if (c.Action == "wheel-reverse" && time == 48000) delta = 6;
                    if (c.Action == "wheel-start-bound") delta = 3;
                    if (c.Rtl) delta = -delta;
                    var vector = c.Action == "wheel-vertical" ? new Vector(0, -3) : new Vector(delta, 0);
                    input((RawInputEventArgs)Activator.CreateInstance(typeof(RawMouseWheelEventArgs), mouse, (ulong)(1000 + time / 1000), root,
                        new Point(184, 84), vector, RawInputModifiers.None)!);
                    writer.Record(time, "input", $"wheel {vector}");
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
                var distance = time switch { 16000 => 0, 32000 => 10, 48000 => 36, 64000 => 80, 80000 => 120, _ => -1 };
                var release = c.Action == "touch-slow" ? 160000 : 96000;
                var disable = time == 80000 && c.Action == "touch-disable";
                if (disable) { carousel.UserScrollEnabled = false; host.Frame(time); }
                if (distance >= 0 || time == release)
                {
                    if (time == release) distance = 120;
                    var point = new Point(24 + 200 + (c.Rtl ? distance : -distance), 84);
                    var type = time == 16000 ? RawPointerEventType.TouchBegin : time == release ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                    input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, (ulong)(1000 + time / 1000), root,
                        type, point, RawInputModifiers.None, 1L)!);
                    writer.Record(time, "input", $"touch {type} ({point.X},{point.Y})");
                }
                if (disable) host.Draw(); else host.Frame(time);
                if (time <= 240000 || time % 80000 == 0) Sample($"time-{time}");
            }
            return;
        }
        Task? animation = null;
        host.AdvanceTo(16000);
        if (c.Action == "jump") carousel.ScrollToItem(3);
        else if (c.Action == "shape") foreach (var item in items) item.CornerRadius = new(40);
        else if (c.Action == "fling") animation = carousel.FlingAsync(c.Velocity);
        else if (c.Action.StartsWith("semantics-"))
            ControlAutomationPeer.CreatePeerForElement(carousel)!.GetProvider<IScrollProvider>()?.Scroll(
                c.Action == "semantics-previous" ? ScrollAmount.LargeDecrement : ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
        else animation = carousel.AnimateScrollToItemAsync(c.Action == "far" ? c.ObserveRealization ? c.Count - 1 : 7 : 1);
        var duration = c.Action == "fling" ? 1600000 : 640000;
        for (long time = 16000; time <= duration; time += 16000)
        {
            if (time == 128000 && c.Action == "reverse")
            {
                host.AdvanceTo(time);
                animation = carousel.AnimateScrollToItemAsync(0);
                // Drain cancellation continuations before sending the next source frame,
                // matching ComposeHost.frame's scheduler.runCurrent before performFrame.
                host.Draw();
            }
            host.Frame(time);
            if (time <= 160000 || time % 80000 == 0) Sample($"time-{time}");
        }
        if (animation is not null) await animation;
    }
    private sealed class Marker(int index) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(new[] { Brushes.CornflowerBlue, Brushes.Coral, Brushes.DarkSeaGreen }[index % 3], new Rect(Bounds.Size));
            context.FillRectangle(Brushes.Navy, new Rect(0, 0, Bounds.Width / 3, Bounds.Height / 2));
            context.FillRectangle(Brushes.Gold, new Rect(Bounds.Width * .6, Bounds.Height * .5, Bounds.Width * .2, Bounds.Height * .4));
        }
    }
}

[ParitySuite("carousel-virtualization")]
public sealed class CarouselVirtualizationCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "none", "jump", "far", "reverse" })
        {
            var id = $"{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Count: 1000, Item: action == "none" ? 500 : 0,
                Radius: 0, Action: action, ObserveRealization: true));
        }
    }
    [ParityScenario("pages", Data = nameof(Configurations))]
    public Task Pages(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-auto-height")]
public sealed class CarouselAutoHeightCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "far", "reverse" })
        {
            var id = $"{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Count: 100, Item: action == "reverse" ? 50 : 0,
                Radius: 0, Action: action, ObserveRealization: true, AutoHeight: true));
        }
    }
    [ParityScenario("resize", Data = nameof(Configurations))]
    public Task Resize(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-focus-search")]
public sealed class CarouselFocusSearchCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "nav-next", "nav-tab", "nav-reverse", "nav-sparse" })
        {
            var id = $"{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Count: 50, Radius: 0, Action: action, ObserveRealization: true));
        }
    }
    [ParityScenario("keys", Data = nameof(Configurations))]
    public Task Keys(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-semantics")]
public sealed class CarouselSemanticsCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "next", "previous", "offset", "disabled" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Item: action == "previous" ? 7 : action == "offset" ? 3 : 0,
                Action: "semantics-" + (action == "offset" ? "next" : action), Offset: action == "offset" ? 45 : 0));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-touch")]
public sealed class CarouselTouchCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "touch-fast", "touch-slow", "touch-disable" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Item: 2, Action: action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-fling")]
public sealed class CarouselFlingCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var mode in new[] { "SingleAdvance", "MultiBrowse", "NoSnap" })
        foreach (var velocity in new[] { 0d, 200, -200, 1400, -1400, 2800 })
        {
            var id = $"dynamic.{mode.ToLowerInvariant()}.velocity-{velocity}";
            yield return new(id, id, new(Count: 12, Item: 3, Action: "fling", Mode: mode, Velocity: velocity, Offset: 45));
        }
        yield return new("dynamic.rtl", "RTL fling", new(Rtl: true, Count: 12, Item: 3, Action: "fling", Mode: "SingleAdvance", Velocity: -1400, Offset: -45));
        yield return new("dynamic.at-snap", "Release exactly at snap", new(Count: 12, Item: 3, Action: "fling", Mode: "SingleAdvance", Velocity: 1400));
        yield return new("dynamic.start-bound", "Start-bound velocity", new(Action: "fling", Mode: "NoSnap", Velocity: -2800));
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-wheel")]
public sealed class CarouselWheelCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "single", "small", "large", "burst", "reverse", "vertical", "disabled", "start-bound", "end-bound" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Item: action == "start-bound" ? 0 : action == "end-bound" ? 7 : 2,
                Action: "wheel-" + action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-keyboard")]
public sealed class CarouselKeyboardCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "down", "up", "repeat", "disabled" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Item: 3, Action: "key-" + action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-focus")]
public sealed class CarouselFocusCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "current", "next", "previous", "reverse", "navigation", "disabled" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Item: 3, Action: "focus-" + action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}

[ParitySuite("carousel-resize")]
public sealed class CarouselResizeCases
{
    public static IEnumerable<CaseData<CarouselConfig>> Configurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "shrink", "shrink-disabled", "grow" })
        {
            var id = $"dynamic.{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(variant, rtl, Width: action == "grow" ? 240 : 400, Action: "focus-" + action));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))] public Task Dynamic(CaptureWriter writer) => CarouselCases.Capture(writer);
}
