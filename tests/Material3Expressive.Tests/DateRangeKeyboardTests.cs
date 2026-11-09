using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class DateRangeKeyboardTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void TabLeavesMonthListFromAnyDateAndShiftTabReturnsToHeaderOrPreviousControl(bool rtl, bool toggle)
    {
        using var clock = new MotionTestClockScope();
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var picker = new DateRangePicker { MinYear = 2024, MaxYear = 2024, DisplayedMonth = new(2024, 2, 1),
            Width = 420, Height = 680, ShowModeToggle = toggle, Culture = CultureInfo.GetCultureInfo("en-US") };
        var window = new MotionTestWindow { Width = 700, Height = 900,
            FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = new StackPanel { Children = { before, picker, after } }; window.Show(); Flush(window);
        try
        {
            var day = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, new DateOnly(2024, 2, 15)));
            Assert.True(day.Focus());
            Press(Key.Tab); Assert.True(after.IsFocused);
            Assert.True(day.Focus());
            Press(Key.Tab, RawInputModifiers.Shift);
            if (toggle) Assert.True(picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ModeToggle").IsFocused);
            else Assert.True(before.IsFocused);
            Assert.True(day.Focus());
            Press(Key.Right);
            Assert.Equal(new DateOnly(2024, 2, rtl ? 14 : 16), (window.FocusManager!.GetFocusedElement() as Button)?.Tag);
            void Press(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
            {
                window.KeyPress(key, modifiers, PhysicalKey.None, null); window.KeyRelease(key, modifiers, PhysicalKey.None, null); Flush(window);
            }
        }
        finally { window.Close(); }
    }
    private static void Flush(Window window)
    {
        for (var i = 0; i < 3; i++) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    }
}
