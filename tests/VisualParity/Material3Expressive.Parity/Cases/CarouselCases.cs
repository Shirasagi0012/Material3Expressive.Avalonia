using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CarouselConfig(string Variant = "MultiBrowse", bool Rtl = false, int Count = 8,
    int Item = 0, double Width = 320, double ItemWidth = 160, double Spacing = 8,
    double Before = 0, double After = 0, double Radius = 16, string Action = "none", double Border = 0,
    string Mode = "Default", double Velocity = 0, double Offset = 0);

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
        await using var host = new AvaloniaHost(writer, 400, 180, new ManualTimeProvider());
        var carousel = new HorizontalCarousel { Width = c.Width, Height = 120, ItemWidth = c.ItemWidth == 0 ? double.NaN : c.ItemWidth,
            Variant = Enum.Parse<CarouselVariant>(c.Variant), ItemSpacing = c.Spacing,
            FlingMode = Enum.Parse<CarouselFlingMode>(c.Mode),
            UserScrollEnabled = c.Action != "semantics-disabled",
            ContentPadding = new(c.Before, 0, c.After, 0), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var items = Enumerable.Range(0, c.Count).Select(i => new CarouselItem { CornerRadius = new(c.Radius), Content = new Marker(i),
            BorderBrush = Brushes.Black, BorderThickness = new(c.Border) }).ToArray();
        foreach (var item in items) carousel.Items.Add(item);
        host.Mount(new Grid { Margin = new Thickness(24), Children = { carousel } }); host.Frame(0);
        carousel.ScrollToItem(c.Item); host.Draw();
        if (c.Offset != 0) { carousel.ScrollOffset += c.Offset; host.Draw(); }
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("viewport", "viewport", null, [
                Measurement.Numeric("currentItem", carousel.CurrentItem, "index", definition: "carousel.currentItem"),
                Measurement.Numeric("scrollOffset", Math.Clamp(carousel.ScrollOffset, 0, carousel.MaximumScrollOffset), "dip", definition: "carousel.scrollOffset")]) };
            if (c.Action.StartsWith("semantics-")) points.Add(new("actions", "semantics", "viewport", [
                Measurement.Flag("pagingAvailable", ControlAutomationPeer.CreatePeerForElement(carousel)!.GetProvider<IScrollProvider>() is not null,
                    definition: "carousel.pagingAvailable")]));
            if (items.Length > 0)
            {
                var info = items[carousel.CurrentItem].DrawInfo;
                points.Add(new("current", "item", "viewport", [
                    Measurement.Numeric("size", info.Size, "dip", definition: "carousel.mask.size"),
                    Measurement.Numeric("minSize", info.MinSize, "dip", definition: "carousel.mask.minimum"),
                    Measurement.Numeric("maxSize", info.MaxSize, "dip", definition: "carousel.mask.maximum") ]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        if (c.Action == "none") return;
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
        else animation = carousel.AnimateScrollToItemAsync(c.Action == "far" ? 7 : 1);
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
