using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DateRangeMonthMotionConfig(string Action = "fling", bool RealPicker = false, bool Rtl = false);

[ParitySuite("date-range-month-motion")]
public sealed class DateRangeMonthMotionCases
{
    public static IEnumerable<CaseData<DateRangeMonthMotionConfig>> Configurations()
    {
        foreach (var action in new[] { "fling", "fling-back", "touch", "touch-slow", "wheel", "interrupt", "boundary", "focus", "focus-resize" })
            yield return new($"dynamic.geometry.{action}", $"Variable month list {action}", new(action));
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "touch", "wheel", "interrupt" })
            yield return new($"dynamic.picker.{action}.{(rtl ? "rtl" : "ltr")}", $"Range picker {action}", new(action, true, rtl));
    }

    [ParityScenario("motion", Data = nameof(Configurations))]
    public async Task Motion(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DateRangeMonthMotionConfig>();
        await using var host = new AvaloniaHost(writer, 440, 680, new ManualTimeProvider());
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        DateRangePicker? picker = null;
        DateRangeMonthList list;
        if (c.RealPicker)
        {
            picker = new DateRangePicker { Width = 400, Height = 620, Margin = new(16), MinYear = 2024, MaxYear = 2025,
                Culture = CultureInfo.GetCultureInfo("en-US"), DisplayedMonth = new(2024, 2, 1),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            host.Mount(new Grid { Children = { picker } }); host.Frame(0);
            list = picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single();
        }
        else
        {
            list = new DateRangeMonthList(new(2023, 1, 1), new(2025, 12, 1), new(2024, 2, 1),
                month =>
                {
                    var marker = new Grid { Height = (month.Month % 3) switch { 1 => 80, 2 => 140, _ => 100 },
                        Background = (month.Month % 3) switch { 1 => Brushes.Red, 2 => Brushes.Blue, _ => Brushes.Green } };
                    if (c.Action.StartsWith("focus")) marker.Children.Add(new Border { Width = 24, Height = 20,
                        Background = Brushes.Yellow, Focusable = true, HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom });
                    return marker;
                }, _ => { })
            { Width = 240, Height = 180, Margin = new(16), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            host.Mount(new Grid { Children = { list } }); host.Frame(0);
        }
        for (long time = 16000; time <= 512000; time += 16000) host.Frame(time);
        void Sample(string id)
        {
            List<Measurement> facts = picker is null ? [
                ..AvaloniaMeasurements.Geometry(list, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("index", list.FirstVisibleIndex, "item", definition: "date-range.first-visible-index"),
                Measurement.Numeric("offset", list.FirstVisibleOffset, "dip", definition: "date-range.first-visible-offset")]
                : [
                Measurement.Numeric("month", picker.DisplayedMonth!.Value.DayNumber, "day", definition: "date-picker.displayed-month-day-number"),
                Measurement.Flag("selected-start", picker.SelectedRange.Start.HasValue, definition: "date-range.has-selected-start"),
                Measurement.Flag("selected-end", picker.SelectedRange.End.HasValue, definition: "date-range.has-selected-end")];
            writer.Submit(host.Snapshot(id, id, [new ProbePoint("months", picker is null ? "date-range-month-list" : "date-range-calendar", null, facts.ToArray())], true));
        }
        Sample("initial");
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(host.Window)!;
        var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        var mouse = (IMouseDevice)impl.GetType().GetProperty("MouseDevice")!.GetValue(impl)!;
        var origin = c.RealPicker ? new Point(220, 450) : new Point(84, 124);
        Task? fling = null;
        for (long elapsed = 16000; elapsed <= 1280000; elapsed += 16000)
        {
            var time = 600000 + elapsed; host.AdvanceTo(time);
            if (elapsed == 16000 && c.Action.StartsWith("focus"))
            {
                ((Grid)list.ContainerFromIndex(c.Action == "focus" ? 14 : 13)!).Children[0].Focus();
                writer.Record(time, "action", "focus day marker");
            }
            if (elapsed == 80000 && c.Action == "focus-resize")
            {
                list.Height = 100; writer.Record(time, "mutation", "viewport height 180 to 100");
            }
            if (elapsed == 16000 && c.Action is "fling" or "fling-back" or "boundary")
            {
                if (c.Action == "boundary") list.ScrollToMonth(new(2025, 12, 1));
                list.Consume(20 * 4); fling = list.FlingAsync((c.Action == "fling-back" ? -1500 : 1500) * 4);
                writer.Record(time, "action", $"range spline fling {c.Action}");
            }
            if (c.Action is "touch" or "touch-slow" or "interrupt")
            {
                var distance = elapsed switch { 16000 => 0d, 32000 => 10, 48000 => 36, 64000 => 60, 80000 => 88, _ => -1 };
                var release = c.Action == "touch-slow" ? 160000 : 96000;
                if (distance >= 0 || elapsed == release)
                {
                    if (elapsed == release) distance = 88;
                    var point = origin - new Vector(0, distance);
                    var type = elapsed == 16000 ? RawPointerEventType.TouchBegin : elapsed == release ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                    input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, (ulong)(1000 + time / 1000), root, type, point, RawInputModifiers.None, 1L)!);
                    writer.Record(time, "input", $"touch {type} ({point.X},{point.Y})");
                }
                if (elapsed == 160000 && c.Action == "interrupt")
                {
                    if (picker is null) list.ScrollToMonth(new(2024, 7, 1)); else picker.DisplayedMonth = new(2024, 7, 1);
                    writer.Record(time, "mutation", "external July interrupts scrolling");
                }
            }
            if (c.Action == "wheel" && elapsed == 16000)
            {
                input((RawInputEventArgs)Activator.CreateInstance(typeof(RawMouseWheelEventArgs), mouse, (ulong)(1000 + time / 1000), root, origin, new Vector(0, -3), RawInputModifiers.None)!);
                writer.Record(time, "input", "vertical wheel -3");
            }
            host.Frame(time);
            if (elapsed <= 240000 || elapsed % 160000 == 0) Sample($"time-{elapsed}");
        }
        if (fling is { IsFaulted: true }) await fling;
    }
}
