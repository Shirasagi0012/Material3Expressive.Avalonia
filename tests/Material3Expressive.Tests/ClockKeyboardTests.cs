using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Tests;

public class ClockKeyboardTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ArrowKeysLoopBothRingsWithoutChangingTime_AndKeyUpCommits(bool clock24, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { Is24Hour = clock24, SelectedTime = new(11, 55),
            Layout = TimePickerLayout.Vertical, FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight };
        var window = DialogTests.Show(picker);
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        var labels = dial.Labels.ToArray();
        Assert.All(labels, b => Assert.False(b.Focusable));
        dial.Activate(); Flush(window);
        Assert.True(labels[11].IsFocused);
        Press(Key.Right); Assert.True(labels[clock24 ? 12 : 0].IsFocused);
        Assert.Equal(new TimeOnly(11, 55), picker.SelectedTime);
        Press(Key.Enter); Assert.Equal(new TimeOnly(11, 55), picker.SelectedTime);
        window.KeyRelease(Key.Enter, Avalonia.Input.RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
        Assert.Equal(new TimeOnly(clock24 ? 12 : 0, 55), picker.SelectedTime);
        Assert.Equal(TimeSelection.Hour, picker.Selection);
        for (int i = 0; i < 12; i++) Press(Key.Down);
        Assert.True(labels[0].IsFocused);
        Press(Key.Up); Assert.True(labels[^1].IsFocused);
        window.Close();
        void Press(Key key) { window.KeyPress(key, Avalonia.Input.RawInputModifiers.None, PhysicalKey.None, null); Flush(window); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void SelectorsActivateTheSelectedTick_AndTabEscapesTheDial(bool clock24)
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { Is24Hour = clock24, SelectedTime = new(23, 58), Layout = TimePickerLayout.Vertical };
        var after = new Button { Content = "After" };
        var window = DialogTests.Show(new StackPanel { Children = { picker, after } });
        var hour = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HourSelector");
        var minute = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MinuteSelector");
        hour.Focus(NavigationMethod.Tab); ClickKey();
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        Assert.True(dial.Labels.Single(b => Equals(b.Content, clock24 ? "23" : "11")).IsFocused);
        Tab(false); Assert.True(minute.IsFocused);
        ClickKey();
        Assert.Equal(TimeSelection.Minute, picker.Selection);
        Assert.True(dial.Labels.Single(b => Equals(b.Content, "0")).IsFocused);
        Tab(true); Assert.True(minute.IsFocused);
        ClickKey(); Tab(false);
        if (clock24) Assert.True(after.IsFocused);
        else Assert.True(picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, DateTimeStrings.Get("TimePickerAM"))).IsFocused);
        window.Close();
        void ClickKey() { window.KeyPress(Key.Enter, 0, PhysicalKey.None, null); window.KeyRelease(Key.Enter, 0, PhysicalKey.None, null); Flush(window); }
        void Tab(bool shift) { window.KeyPress(Key.Tab, shift ? Avalonia.Input.RawInputModifiers.Shift : 0, PhysicalKey.None, null); Flush(window); }
    }

    [AvaloniaFact]
    public void TouchSelectorDoesNotTakeDialFocus_AndLaterKeyboardActivationUsesCurrentValue()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { Is24Hour = true, SelectedTime = new(11, 55), Layout = TimePickerLayout.Vertical };
        var window = DialogTests.Show(picker);
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        dial.Activate();
        var minute = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MinuteSelector");
        // Button.OnPointerReleased checks the renderer's hit-test scene, even when events are
        // raised directly. Layout alone leaves this dependent on the wall-clock render timer.
        // Complete actual rendering before injecting touch; no motion frame is advanced.
        using (window.CaptureRenderedFrame()) { }
        Assert.NotEmpty(minute.GetVisualsAt(new Point(24, 24)));
        var point = minute.TranslatePoint(new Avalonia.Point(24, 24), window)!.Value;
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        minute.RaiseEvent(new PointerPressedEventArgs(minute, pointer, window, point, 0,
            new PointerPointProperties(Avalonia.Input.RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        minute.RaiseEvent(new PointerReleasedEventArgs(minute, pointer, window, point, 1,
            new PointerPointProperties(0, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        Flush(window);
        Assert.Equal(TimeSelection.Minute, picker.Selection);
        Assert.All(dial.Labels, b => Assert.False(b.IsFocused));
        minute.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Enter, 0, PhysicalKey.None, null); window.KeyRelease(Key.Enter, 0, PhysicalKey.None, null); Flush(window);
        Assert.True(dial.Labels.Single(b => Equals(b.Content, "55")).IsFocused);
        picker.SelectedTime = new(11, 10); Flush(window);
        Assert.True(dial.Labels.Single(b => Equals(b.Content, "10")).IsFocused);
        window.Close();
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
