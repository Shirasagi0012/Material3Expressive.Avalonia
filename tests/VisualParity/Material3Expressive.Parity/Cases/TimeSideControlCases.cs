using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Parity;

public sealed record TimeSideControlConfig(string Kind = "input", bool Clock24 = false,
    bool Rtl = false, bool Dark = false, bool TemplateOnly = false, string Action = "none");

[ParitySuite("time-picker-side-control")]
public sealed class TimeSideControlCases
{
    public static IEnumerable<CaseData<TimeSideControlConfig>> Configurations()
    {
        foreach (var kind in new[] { "input", "scroll" })
        {
            foreach (var clock24 in new[] { false, true })
            foreach (var rtl in new[] { false, true })
            foreach (var dark in new[] { false, true })
                yield return new($"static.{kind}.{(clock24 ? "24h" : "12h")}.{(rtl ? "rtl" : "ltr")}.{(dark ? "dark" : "light")}",
                    "uncontained time side controls", new(kind, clock24, rtl, dark));
            yield return new($"static.{kind}.template-only", "template-only mode toggle", new(kind, TemplateOnly: true));
            foreach (var action in new[] { "press", "edge", "touch-press", "touch-edge", "remove" })
            foreach (var rtl in new[] { false, true })
                yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}",
                    "side control interaction and lifecycle", new(kind, Rtl: rtl, Action: action));
        }
    }

    [ParityScenario("side-control", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimeSideControlConfig>();
        await using var host = new AvaloniaHost(writer, 420, 260);
        // Fixed Avalonia 12.1.2 marks its raw-input constructors PrivateApi. Keep this
        // probe local instead of enabling private framework APIs for the whole runner.
        using var touch = Activator.CreateInstance<TouchDevice>();
        CaseDefaults.SetThemeColors(host, c.Dark);
        TemplatedControl picker = c.Kind == "input" ? new TimeInput() : new TimeScroll();
        picker.SetValue(TimePicker.SelectedTimeProperty, new TimeOnly(2, 30));
        picker.SetValue(TimePicker.Is24HourProperty, c.Clock24);
        picker.SetValue(TimePicker.VariantProperty, TimePickerVariant.Vibrant);
        picker.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Control Toggle() => new Border { Width = 24, Height = 24, Background = Brushes.Magenta };
        if (c.TemplateOnly)
            picker.SetValue(TimePicker.ToggleTemplateProperty, new FuncDataTemplate<object?>((_, _) => Toggle()));
        else picker.SetValue(TimePicker.ToggleProperty, Toggle());
        picker.Margin = new Thickness(16);
        picker.HorizontalAlignment = HorizontalAlignment.Left; picker.VerticalAlignment = VerticalAlignment.Top;
        host.Mount(new Grid { Children = { picker } }); host.Frame(0);

        void Sample(string id)
        {
            var time = picker.GetValue(TimePicker.SelectedTimeProperty);
            var points = new List<ProbePoint> { new("picker", "time-picker", null, [
                Measurement.Numeric("hour", time.Hour, "hour", definition: "time-picker.selected-hour"),
                Measurement.Numeric("minute", time.Minute, "minute", definition: "time-picker.selected-minute")]) };
            var side = picker.GetVisualDescendants().OfType<TimeSideControlLayout>().SingleOrDefault();
            if (side is not null)
            {
                // Report the actual mode content, not an inferred position from the side-column algorithm.
                var toggle = side.GetVisualDescendants().OfType<Border>().Single(b => b.Background == Brushes.Magenta);
                points.Add(new("mode", "mode-toggle", "picker", AvaloniaMeasurements.Geometry(toggle, host.Window)));
            }
            var buttons = picker.GetVisualDescendants().OfType<TimePeriodButton>().ToArray();
            for (var i = 0; i < buttons.Length; i++)
            {
                var surface = buttons[i].GetVisualDescendants().OfType<MaterialSurface>().Single();
                points.Add(new(i == 0 ? "am" : "pm", "period", "picker",
                    AvaloniaMeasurements.Geometry(surface, host.Window).Concat([
                        Measurement.Flag("selected", buttons[i].IsChecked == true, definition: "time-picker.period.selected")]).ToArray()));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Input(long time, string action)
        {
            Advance(time);
            var button = picker.GetVisualDescendants().OfType<TimePeriodButton>().Last();
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var point = surface.TranslatePoint(new Point(c.Action.EndsWith("edge") ? -3 : 20, 20), host.Window)!.Value;
            if (c.Action.StartsWith("touch-"))
            {
                var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
                var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
                var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
                input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, (ulong)(1000 + time / 1000), root,
                    action == "down" ? RawPointerEventType.TouchBegin : RawPointerEventType.TouchEnd, point, RawInputModifiers.None, 1L)!);
                writer.Record(time, "input", $"touch {action} ({point.X},{point.Y})");
            }
            else host.Input(action, point);
            host.Draw(); Sample($"{action}-{time}");
        }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action == "remove")
        {
            Advance(33000); picker.SetValue(TimePicker.ToggleProperty, null); host.Draw(); Sample("removed");
            Times(64000);
            Advance(97000); picker.SetValue(TimePicker.ToggleProperty, Toggle()); host.Draw(); Sample("restored");
            Times(112000, 320000); return;
        }
        Input(33000, "down"); Times(64000); Input(97000, "up"); Times(112000, 160000, 320000, 640000);
        Input(657000, "down"); Input(689000, "up"); Times(800000, 1024000);
    }
}
