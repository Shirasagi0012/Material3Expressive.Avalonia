using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Parity;

public sealed record TimePickerKeyboardConfig(bool Clock24 = false, bool Rtl = false);

[ParitySuite("time-picker-keyboard")]
public sealed class TimePickerKeyboardCases
{
    public static IEnumerable<CaseData<TimePickerKeyboardConfig>> Configurations()
    {
        foreach (var clock24 in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"{(clock24 ? "24h" : "12h")}.{(rtl ? "rtl" : "ltr")}", "clock keyboard navigation", new(clock24, rtl));
    }

    [ParityScenario("keyboard", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimePickerKeyboardConfig>();
        await using var host = new AvaloniaHost(writer, 360, 480);
        CaseDefaults.SetThemeColors(host, false);
        var picker = new TimePicker { Is24Hour = c.Clock24, SelectedTime = new(11, 55),
            Layout = TimePickerLayout.Vertical, Margin = new Thickness(16),
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        host.Mount(new StackPanel { Background = Brushes.White, Children = { picker, new Button { Content = "After" } } }); host.Frame(0);
        var hour = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HourSelector");
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        void Sample(string id)
        {
            var index = Array.FindIndex(dial.GetVisualChildren().OfType<Button>().ToArray(), b => b.IsFocused);
            writer.Submit(host.Snapshot(id, id, [new("picker", "time-picker", null, [
                Measurement.Numeric("hour", picker.SelectedTime.Hour, "hour", definition: "time-picker.selected-hour"),
                Measurement.Numeric("minute", picker.SelectedTime.Minute, "minute", definition: "time-picker.selected-minute"),
                Measurement.Numeric("focusedTick", index + 1, "index", definition: "time-picker.focused-tick.one-based-or-zero"),
                Measurement.Text("selection", picker.Selection.ToString().ToLowerInvariant(), definition: "time-picker.selection")]) ]));
        }
        long time = 0;
        void Key(string id, Key key, bool shift = false)
        {
            time += 16000; host.AdvanceTo(time);
            var mods = shift ? Avalonia.Input.RawInputModifiers.Shift : 0;
            host.Window.KeyPress(key, mods, PhysicalKey.None, null); host.Window.KeyRelease(key, mods, PhysicalKey.None, null);
            host.Draw(); host.Frame(time); Sample(id);
        }
        Sample("initial"); hour.Focus(NavigationMethod.Tab); host.Draw();
        Key("activate-hour", Avalonia.Input.Key.Enter);
        Key("next-ring", Avalonia.Input.Key.Right);
        Key("previous-ring", Avalonia.Input.Key.Up);
        Key("next-again", Avalonia.Input.Key.Down);
        Key("commit-hour", Avalonia.Input.Key.Space);
        Key("leave-hour", Avalonia.Input.Key.Tab);
        Key("activate-minute", Avalonia.Input.Key.Enter);
        Key("minute-wrap", Avalonia.Input.Key.Right);
        Key("commit-minute", Avalonia.Input.Key.Enter);
        Key("back-to-minute", Avalonia.Input.Key.Tab, true);
        Key("reactivate-minute", Avalonia.Input.Key.Enter);
        Key("leave-minute", Avalonia.Input.Key.Tab);
        for (int i = 0; i < 32; i++) { time += 16000; host.Frame(time); }
        Sample("settled");
        picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MinuteSelector").Focus(NavigationMethod.Tab);
        host.Draw();
        Key("activate-settled", Avalonia.Input.Key.Enter);
        Key("wrap-settled", Avalonia.Input.Key.Right);
        Key("commit-settled", Avalonia.Input.Key.Enter);
    }
}
