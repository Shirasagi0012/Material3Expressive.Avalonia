using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using TimePicker = Material3Expressive.Components.TimePicker;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TimePickerPointerConfig(string Action = "tap", bool Clock24 = true, bool Rtl = false, bool Expressive = true, bool FixedInputs = true, bool Vibrant = false, bool Horizontal = false);

[ParitySuite("time-picker-pointer")]
public sealed class TimePickerPointerCases
{
    public static IEnumerable<CaseData<TimePickerPointerConfig>> Configurations()
    {
        foreach (var action in new[] { "tap", "drag", "minute-tap", "minute-drag", "rings", "interrupt", "reject", "field-transition", "release-jump", "selection-reverse" })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}", $"clock pointer {action}", new(action, true, rtl));
        foreach (var vibrant in new[] { false, true })
        foreach (var horizontal in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"period.{(vibrant ? "vibrant" : "standard")}.{(horizontal ? "horizontal" : "vertical")}.{(rtl ? "rtl" : "ltr")}",
                "period toggle press, checked shape, repeated activation", new("period", false, rtl, Vibrant: vibrant, Horizontal: horizontal));
        yield return new("dynamic.tap.12h", "12-hour clock tap", new("tap", false));
        yield return new("dynamic.drag.12h", "12-hour clock drag", new("drag", false));
        yield return new("dynamic.drag.default-platform-theme", "platform default typography and remaining color roles", new("drag", FixedInputs: false));
        yield return new("dynamic.drag.standard-motion", "standard motion clock drag", new("drag", Expressive: false));
    }

    [ParityScenario("pointer", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimePickerPointerConfig>();
        await using var host = new AvaloniaHost(writer, c.Horizontal ? 700 : 360, 480, new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess));
        CaseDefaults.SetThemeColors(host, false);
        if (c.FixedInputs)
        {
            // Explicit matching inputs for this component; the default-platform-theme case
            // retains the separate baseline-font/color comparison rather than hiding it.
            host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse("#EADDFF");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse("#21005D");
            host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse("#E6E0E9");
            host.Window.Resources[Sys.Color.SurfaceContainerLowest] = Colors.White;
        }
        if (!c.Expressive)
        {
            host.Window.Resources[Sys.Motion.Spring.SpatialDefaultDamping] = .9d;
            host.Window.Resources[Sys.Motion.Spring.SpatialDefaultStiffness] = 700d;
        }
        var picker = new TimePicker { Is24Hour = c.Clock24, SelectedTime = new(0, c.Action == "field-transition" ? 30 : 0),
            Selection = c.Action.StartsWith("minute") ? TimeSelection.Minute : TimeSelection.Hour,
            Variant = c.Vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard,
            Layout = c.Horizontal ? TimePickerLayout.Horizontal : TimePickerLayout.Vertical, Margin = new Thickness(16),
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        host.Mount(new StackPanel { Background = Brushes.White, Children = { picker } }); host.Frame(0);
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        void Sample(string id)
        {
            var labels = dial.GetVisualDescendants().OfType<Button>().ToArray();
            var selected = labels.Where(label => ControlAutomationPeer.CreatePeerForElement(label) is ISelectionItemProvider { IsSelected: true })
                .Select(label => ((Panel)label.GetVisualParent()!).Children.IndexOf(label) + 1).Order().ToArray();
            writer.Submit(host.Snapshot(id, id, [new("picker", "time-picker", null, [
            Measurement.Numeric("hour", picker.SelectedTime.Hour, "hour", definition: "time-picker.selected-hour"),
            Measurement.Numeric("minute", picker.SelectedTime.Minute, "minute", definition: "time-picker.selected-minute"),
            Measurement.Text("selection", picker.Selection.ToString().ToLowerInvariant(), definition: "time-picker.selection"),
            Measurement.Numeric("angle", dial.CurrentAngle, "radian", category: "motion", definition: "time-picker.selector.current-angle"),
            Measurement.Numeric("labelCount", labels.Length, "count", definition: "time-picker.composed-label-count"),
            Measurement.Text("selectedTicks", string.Join(",", selected), definition: "time-picker.selector.selected-ticks")]) ]));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Input(long time, string action, double x, double y)
        {
            Advance(time); host.Input(action, dial.TranslatePoint(new Point(x, y), host.Window)!.Value);
            host.Draw(); Sample($"{action}-{time}");
        }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "period")
        {
            var pm = picker.GetVisualDescendants().OfType<TimePeriodButton>().Last();
            void PeriodInput(long time, string action)
            {
                Advance(time); host.Input(action, pm.TranslatePoint(new Point(pm.Bounds.Width / 2, pm.Bounds.Height / 2), host.Window)!.Value);
                host.Draw(); Sample($"{action}-{time}");
            }
            PeriodInput(33000, "down"); Times(64000); PeriodInput(97000, "up");
            Times(112000, 160000, 208000, 320000, 640000);
            PeriodInput(657000, "down"); PeriodInput(689000, "up"); Times(800000, 1024000);
            return;
        }
        if (c.Action is "reject" or "field-transition")
        {
            Advance(1000); picker.SelectedTime = new(0, 30); picker.Selection = TimeSelection.Minute; host.Draw();
            Times(16000, 32000);
        }
        Input(33000, "down", 229, 128);
        if (c.Action is "drag" or "minute-drag" or "rings" or "release-jump")
        {
            Input(49000, "move", c.Action == "rings" ? 197 : 228, c.Action == "rings" ? 128 : 140);
            Times(64000);
            if (c.Action == "rings") Input(81000, "move", 229, 128);
            else Input(81000, "move", 200, 200);
        }
        var stationaryRelease = c.Action is "drag" or "minute-drag";
        Input(97000, "up", stationaryRelease ? 200 : 229, stationaryRelease ? 200 : 128);
        Times(112000, 160000);
        if (c.Action == "interrupt")
        {
            Input(177000, "down", 128, 229); Input(193000, "up", 128, 229);
        }
        if (c.Action == "selection-reverse")
        {
            Advance(177000); picker.Selection = TimeSelection.Minute; host.Draw(); Sample("select-minute");
            Times(208000);
            Advance(225000); picker.Selection = TimeSelection.Hour; host.Draw(); Sample("select-hour");
        }
        else Times(208000);
        Times(320000, 480000, 640000, 800000, 1024000);
    }
}
