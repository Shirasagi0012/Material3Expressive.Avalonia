using System.Globalization;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class DateRangeNavigationTests
{
    [AvaloniaFact]
    public void MonthActionsNavigateFromFirstVisibleMonthAndPreserveTheCalendarAndSelection()
    {
        using var clock = new MotionTestClockScope();
        var picker = Create(); var window = Show(picker);
        try
        {
            var list = picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single();
            Assert.Equal(new DateOnly(2024, 2, 1), picker.DisplayedMonth);
            var heading = picker.GetVisualDescendants().OfType<DateRangeMonthHeading>().First();
            var peer = ControlAutomationPeer.CreatePeerForElement(heading)!;
            var actions = peer.GetChildren()!;
            Assert.Equal(2, actions.Count);
            Assert.Equal("Scroll to show the previous month", actions[0].GetName());
            Assert.Equal("Scroll to show the next month", actions[1].GetName());
            Assert.All(actions, action => Assert.False(action.IsKeyboardFocusable()));
            ((IInvokeProvider)actions[1]).Invoke(); Flush(window);
            Assert.Equal(new DateOnly(2024, 3, 1), picker.DisplayedMonth);
            Assert.Same(list, picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single());
            Assert.Equal(new DateRange(new(2024, 2, 10), new(2024, 2, 20)), picker.SelectedRange);
            Assert.True(picker.TryScrollToPreviousMonth()); Flush(window);
            Assert.Equal(new DateOnly(2024, 2, 1), picker.DisplayedMonth);
            picker.DisplayedMonth = new(2024, 7, 1); Flush(window);
            Assert.Same(list, picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single());
            Assert.Equal(new DateOnly(2024, 7, 1), picker.DisplayedMonth);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NavigationRejectsCalendarBoundsDisabledInputAndDetachedStates()
    {
        using var clock = new MotionTestClockScope();
        var picker = Create(); picker.DisplayedMonth = new(2024, 1, 1);
        Assert.False(picker.TryScrollToNextMonth());
        var window = Show(picker);
        try
        {
            Assert.False(picker.TryScrollToPreviousMonth());
            picker.IsEnabled = false;
            Assert.False(picker.TryScrollToNextMonth());
            picker.IsEnabled = true;
            picker.DisplayedMonth = new(2024, 12, 1); Flush(window);
            Assert.False(picker.TryScrollToNextMonth());
            Assert.True(picker.TryScrollToPreviousMonth()); Flush(window);
            picker.DisplayMode = DatePickerDisplayMode.Input; Flush(window);
            Assert.False(picker.TryScrollToNextMonth());
        }
        finally { window.Close(); }
        Assert.False(picker.TryScrollToPreviousMonth());
    }

    [AvaloniaTheory]
    [InlineData("de-DE", "Zum Ansehen des nächsten Monats scrollen")]
    [InlineData("zh-CN", "滚动即可显示下个月")]
    public void HeadingActionNamesUsePickerCulture(string culture, string next)
    {
        using var clock = new MotionTestClockScope();
        var picker = Create(); picker.Culture = CultureInfo.GetCultureInfo(culture);
        var window = Show(picker);
        try
        {
            var heading = picker.GetVisualDescendants().OfType<DateRangeMonthHeading>().First();
            Assert.Equal(next, ControlAutomationPeer.CreatePeerForElement(heading)!.GetChildren()![1].GetName());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void QueuedNavigationCannotChangeTheReplacementCalendarOrInputMode()
    {
        using var clock = new MotionTestClockScope();
        var picker = Create(); var window = Show(picker);
        try
        {
            Assert.True(picker.TryScrollToNextMonth());
            picker.DisplayMode = DatePickerDisplayMode.Input; Flush(window);
            Assert.Equal(new DateOnly(2024, 2, 1), picker.DisplayedMonth);
            picker.DisplayMode = DatePickerDisplayMode.Picker; Flush(window);
            Assert.True(picker.TryScrollToNextMonth());
            picker.Culture = CultureInfo.GetCultureInfo("de-DE"); Flush(window);
            Assert.Equal(new DateOnly(2024, 2, 1), picker.DisplayedMonth);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DialogSuppliesTheRemainingFiniteViewportAcrossResizeAndModeChanges()
    {
        using var clock = new MotionTestClockScope();
        var picker = Create();
        picker.ClearValue(Control.WidthProperty);
        var confirm = new Button { Content = "Confirm", Height = 40 };
        var dialog = new DatePickerDialog { Content = picker, ConfirmButton = confirm,
            IsOpen = true, AnimateTransition = false };
        var window = Show(new Grid { Children = { dialog } });
        try
        {
            var list = picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single();
            var originalHeight = list.Bounds.Height;
            Assert.True(originalHeight > 0);
            Assert.True(list.Bounds.Height < picker.Bounds.Height);
            window.Height = 450; Flush(window);
            Assert.InRange(list.Bounds.Height, 1, originalHeight - 1);
            Assert.Equal(40, confirm.Bounds.Height);
            Assert.True(picker.TryScrollToNextMonth()); Flush(window);
            Assert.Equal(new DateOnly(2024, 3, 1), picker.DisplayedMonth);
            picker.DisplayMode = DatePickerDisplayMode.Input; Flush(window);
            picker.DisplayMode = DatePickerDisplayMode.Picker; Flush(window);
            Assert.True(picker.GetVisualDescendants().OfType<DateRangeMonthList>().Last().Bounds.Height > 0);
        }
        finally { window.Close(); }
    }

    private static DateRangePicker Create() => new()
    {
        Width = 420, MinYear = 2024, MaxYear = 2024, DisplayedMonth = new(2024, 2, 1),
        Culture = CultureInfo.GetCultureInfo("en-US"), SelectedRange = new(new(2024, 2, 10), new(2024, 2, 20)),
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
    };
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 700, Height = 700 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window)
    {
        for (var i = 0; i < 3; i++) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    }
}
