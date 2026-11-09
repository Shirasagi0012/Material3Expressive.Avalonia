using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DateRangeKeyboardConfig(bool Rtl = false, bool Toggle = true);

[ParitySuite("date-range-keyboard")]
public sealed class DateRangeKeyboardCases
{
    public static IEnumerable<CaseData<DateRangeKeyboardConfig>> Configurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var toggle in new[] { false, true })
            yield return new($"dynamic.{(rtl ? "rtl" : "ltr")}.{(toggle ? "toggle" : "no-toggle")}", "Tab exits the range calendar", new(rtl, toggle));
    }
    [ParityScenario("tab", Data = nameof(Configurations))]
    public async Task Tab(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DateRangeKeyboardConfig>();
        await using var host = new AvaloniaHost(writer, 440, 780);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var before = new Border { Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Blue, Focusable = true };
        var after = new Border { Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Red, Focusable = true };
        var picker = new DateRangePicker { Width = 400, Height = 680, MinYear = 2024, MaxYear = 2024,
            DisplayedMonth = new(2024, 2, 1), ShowModeToggle = c.Toggle, Culture = CultureInfo.GetCultureInfo("en-US") };
        host.Mount(new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { before, picker, after } }); host.Frame(0);
        for (long t = 16000; t <= 512000; t += 16000) host.Frame(t);
        void FocusDate(long time)
        {
            host.AdvanceTo(time);
            var day = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, new DateOnly(2024, 2, 15)));
            if (!day.Focus()) throw new InvalidOperationException("Date could not receive focus.");
            writer.Record(time, "input", "focus February 15"); host.Draw(); host.Frame(time);
        }
        void Sample(string id)
        {
            var focused = host.Window.FocusManager!.GetFocusedElement();
            var calendar = focused is Button { Tag: DateOnly };
            var header = focused is Control control && picker.IsVisualAncestorOf(control) && !calendar;
            writer.Submit(host.Snapshot(id, id, [new ProbePoint("focus", "keyboard-focus", null, [
                Measurement.Flag("before", before.IsFocused, definition: "range-keyboard.before"),
                Measurement.Flag("after", after.IsFocused, definition: "range-keyboard.after"),
                Measurement.Flag("calendar", calendar, definition: "range-keyboard.calendar"),
                Measurement.Flag("header", header, definition: "range-keyboard.header")]) ]));
        }
        FocusDate(600000); Sample("date-focused");
        Key(700000, false); Sample("tab-exited");
        FocusDate(800000); Key(900000, true); Sample("shift-tab-exited");
        void Key(long time, bool shift)
        {
            host.AdvanceTo(time); var modifiers = shift ? RawInputModifiers.Shift : RawInputModifiers.None;
            host.Window.KeyPress(Avalonia.Input.Key.Tab, modifiers, PhysicalKey.None, null); host.Window.KeyRelease(Avalonia.Input.Key.Tab, modifiers, PhysicalKey.None, null);
            host.Draw(); host.Frame(time);
        }
    }
}
