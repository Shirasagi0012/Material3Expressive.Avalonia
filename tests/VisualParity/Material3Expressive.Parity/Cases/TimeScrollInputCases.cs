using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TimeScrollInputConfig(string Action = "touch", bool Minute = false, bool Clock24 = true, bool Rtl = false);

[ParitySuite("time-scroll-input")]
public sealed class TimeScrollInputCases
{
    public static IEnumerable<CaseData<TimeScrollInputConfig>> Configurations()
    {
        foreach (var action in new[] { "touch", "touch-slow", "touch-external", "wheel", "key", "click" })
        foreach (var minute in new[] { false, true })
            yield return new($"{action}.{(minute ? "minute" : "hour")}", "real pager input and time synchronization", new(action, minute));
        yield return new("touch.12h.rtl", "12-hour wrap preserves period", new(Clock24: false, Rtl: true));
        yield return new("key.12h.rtl", "12-hour keyboard wrap", new("key", Clock24: false, Rtl: true));
    }
    [ParityScenario("input", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimeScrollInputConfig>();
        await using var host = new AvaloniaHost(writer, 420, 240, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.Outline] = Color.Parse("#79747E");
        host.Window.Resources[Sys.Color.SurfaceContainerLowest] = Colors.White;
        var picker = new TimeScroll { Is24Hour = c.Clock24, SelectedTime = new(23, 59), Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        host.Mount(picker); host.Frame(0);
        var field = picker.GetVisualDescendants().OfType<ScrollField>().Single(p => p.Name == (c.Minute ? "PART_MinuteScroll" : "PART_HourScroll"));
        var center = field.TranslatePoint(new Point(field.Bounds.Width / 2, field.Bounds.Height / 2), host.Window)!.Value;
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
        var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        var mouse = (IMouseDevice)impl.GetType().GetProperty("MouseDevice")!.GetValue(impl)!;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [
            new("picker", "time-scroll", null, [
                Measurement.Numeric("hour", picker.SelectedTime.Hour, "hour", definition: "time-picker.selected-hour"),
                Measurement.Numeric("minute", picker.SelectedTime.Minute, "minute", definition: "time-picker.selected-minute")]),
            new("field", "scroll-field", "picker", AvaloniaMeasurements.Geometry(field, host.Window).Concat([
                Measurement.Numeric("targetPage", field.State!.TargetPage, "index", definition: "scroll-field.target-page")]).ToArray())]));
        Sample("initial");
        for (long time = 16000; time <= 1280000; time += 16000)
        {
            host.AdvanceTo(time);
            if (c.Action.StartsWith("touch"))
            {
                if (time == 64000 && c.Action == "touch-external")
                    picker.SelectedTime = c.Minute ? new(23, 45) : new(15, 59);
                var distance = time switch { 16000 => 0d, 32000 => 10, 48000 => 36, 64000 => 60, 80000 => 88, _ => -1 };
                var release = c.Action == "touch-slow" ? 160000 : 96000;
                if (distance >= 0 || time == release)
                {
                    if (time == release) distance = 88;
                    var type = time == 16000 ? RawPointerEventType.TouchBegin : time == release ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                    input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, (ulong)(1000 + time / 1000), root,
                        type, new Point(center.X, center.Y - distance), RawInputModifiers.None, 1L)!);
                    writer.Record(time, "input", $"touch {type}");
                }
            }
            if (c.Action == "wheel" && time is 16000 or 32000)
                input((RawInputEventArgs)Activator.CreateInstance(typeof(RawMouseWheelEventArgs), mouse, (ulong)(1000 + time / 1000), root,
                    center, new Vector(0, -3), RawInputModifiers.None)!);
            if (c.Action == "key" && time == 16000)
            {
                field.Focus();
                host.Window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
                host.Window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            }
            var neighbour = new Point(center.X, center.Y + 50);
            if (c.Action == "click")
            {
                if (time == 16000) host.Input("move", neighbour);
                if (time == 32000) host.Input("down", neighbour);
                if (time == 80000) host.Input("up", neighbour);
            }
            host.Frame(time);
            if (time <= 160000 || time % 160000 == 0) Sample($"time-{time}");
        }
    }
}
