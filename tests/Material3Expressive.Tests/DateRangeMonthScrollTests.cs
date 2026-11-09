using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class DateRangeMonthScrollTests
{
    [AvaloniaFact]
    public void VariableMeasuredMonths_CrossBoundaries_BackfillEnd_AndVirtualize()
    {
        var list = Create();
        var window = Show(list); using var close = new CloseWindow(window);
        Assert.Equal(1, list.FirstVisibleIndex);
        Assert.Equal(160, list.Consume(160)); Flush(window);
        Assert.Equal(2, list.FirstVisibleIndex); Assert.Equal(20, list.FirstVisibleOffset);
        Assert.Equal(220, list.Consume(10000)); Flush(window);
        Assert.Equal(4, list.FirstVisibleIndex); Assert.Equal(60, list.FirstVisibleOffset);
        Assert.False(list.CanScrollNext); Assert.Equal(0, list.Consume(1));
        Assert.Equal(-460, list.Consume(-10000)); Flush(window);
        Assert.Equal(0, list.FirstVisibleIndex); Assert.Equal(0, list.FirstVisibleOffset);
        Assert.False(list.CanScrollPrevious);
        Assert.InRange(list.Children.Count, 1, 3);
        list.ScrollToMonth(new(2024, 2, 1)); Flush(window);
        list.ContainerFromIndex(1)!.Height = 200; Flush(window);
        list.Consume(199); Flush(window);
        Assert.Equal(1, list.FirstVisibleIndex); Assert.Equal(199, list.FirstVisibleOffset);
        list.Consume(1); Flush(window);
        Assert.Equal(2, list.FirstVisibleIndex); Assert.Equal(0, list.FirstVisibleOffset);
    }

    [AvaloniaFact]
    public void FractionalPixels_AreAccumulated_WithoutLargeDateRangeCoordinates()
    {
        var list = new DateRangeMonthList(new(1, 1, 1), new(9999, 12, 1), new(9999, 1, 1),
            _ => new Border { Height = 100 }, _ => { }) { Width = 240, Height = 180 };
        var window = Show(list); using var close = new CloseWindow(window);
        Assert.Equal(.25f, list.Consume(.25f)); Assert.Equal(0, list.FirstVisibleOffset);
        Assert.Equal(.25f, list.Consume(.25f)); Assert.Equal(0, list.FirstVisibleOffset);
        Assert.Equal(.25f, list.Consume(.25f)); Assert.Equal(1, list.FirstVisibleOffset);
        Assert.Equal(new DateOnly(9999, 1, 1), list.FirstVisibleMonth);
        Assert.Equal(2, list.Children.Count);
        list.ScrollToMonth(new(1, 1, 1)); Flush(window);
        Assert.Equal(new DateOnly(1, 1, 1), list.FirstVisibleMonth);
        Assert.InRange(list.Children.Count, 1, 3);
    }

    [AvaloniaFact]
    public void FocusedMonth_IsRetainedOutsideViewport_UntilFocusMoves()
    {
        var list = Create();
        var window = Show(list); using var close = new CloseWindow(window);
        var focused = list.ContainerFromIndex(1)!;
        focused.Focusable = true; focused.Focus();
        Assert.True(focused.IsKeyboardFocusWithin);
        list.ScrollToMonth(new(2024, 5, 1)); Flush(window);
        Assert.Same(focused, list.ContainerFromIndex(1));
        var next = list.ContainerFromIndex(4)!; next.Focusable = true; next.Focus();
        list.Consume(-1); Flush(window);
        Assert.Null(list.ContainerFromIndex(1));
    }

    [AvaloniaFact]
    public void FocusingDayInPartiallyVisibleMonth_UsesNearestEdgeAndRetainsCorePlacementTiming()
    {
        using var clock = new MotionTestClockScope();
        var list = new DateRangeMonthList(new(2024, 1, 1), new(2024, 12, 1), new(2024, 2, 1),
            _ => new Grid { Height = 140, Children = { new Border { Focusable = true, Height = 20,
                Margin = new Thickness(0, 120, 0, 0), VerticalAlignment = VerticalAlignment.Top } } }, _ => { })
            { Width = 240, Height = 180 };
        var window = Show(list); using var close = new CloseWindow(window);
        var day = ((Grid)list.ContainerFromIndex(2)!).Children[0];
        var required = day.TranslatePoint(new Point(0, day.Bounds.Height), list)!.Value.Y - list.Bounds.Height;
        day.Focus();
        for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.True(day.IsFocused);
        Assert.Equal(1, list.FirstVisibleIndex);
        // The fixed upstream reads placement after the frame. Crossing the visibility
        // boundary may overshoot the nearest edge; the paired motion case checks that curve.
        Assert.True(list.FirstVisibleOffset >= required);
        Assert.True(day.TranslatePoint(default, list)!.Value.Y >= 0);
        Assert.True(day.TranslatePoint(new Point(0, day.Bounds.Height), list)!.Value.Y <= list.Bounds.Height);
        Assert.False(list.IsScrollInProgress);
    }

    [AvaloniaFact]
    public void UnboundedVerticalMeasurement_RequiresExplicitViewport()
    {
        var list = new DateRangeMonthList(new(2024, 1, 1), new(2024, 12, 1), new(2024, 2, 1),
            _ => new Border { Height = 100 }, _ => { });
        var error = Assert.Throws<InvalidOperationException>(() => list.Measure(new Size(240, double.PositiveInfinity)));
        Assert.Contains("bounded height", error.Message);
    }

    [AvaloniaFact]
    public void ViewportShrink_KeepsPreviouslyVisibleFocusedDay_WithoutReturningToScrolledOutFocus()
    {
        using var clock = new MotionTestClockScope();
        var list = new DateRangeMonthList(new(2024, 1, 1), new(2024, 12, 1), new(2024, 2, 1),
            _ => new Grid { Height = 140, Children = { new Border { Focusable = true, Height = 20,
                Margin = new Thickness(0, 120, 0, 0), VerticalAlignment = VerticalAlignment.Top } } }, _ => { })
            { Width = 240, Height = 180 };
        var window = Show(list); using var close = new CloseWindow(window);
        var day = ((Grid)list.ContainerFromIndex(1)!).Children[0];
        day.Focus(); Assert.Equal(0, list.FirstVisibleOffset);
        list.Height = 100; Flush(window);
        for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.True(list.FirstVisibleOffset >= 40);
        Assert.True(day.TranslatePoint(default, list)!.Value.Y >= 0);
        Assert.True(day.TranslatePoint(new Point(0, day.Bounds.Height), list)!.Value.Y <= list.Bounds.Height);
        Assert.True(day.IsFocused); Assert.False(list.IsScrollInProgress);
        list.Consume(100); Flush(window);
        var position = (list.FirstVisibleIndex, list.FirstVisibleOffset);
        list.Height = 80; Flush(window);
        for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.Equal(position, (list.FirstVisibleIndex, list.FirstVisibleOffset));
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(3)]
    public void SplineFling_IsContinuous_NoMonthSnap_AndIgnoresVisualDurationScale(double scale)
    {
        using var clock = new MotionTestClockScope();
        var list = new DateRangeMonthList(new(2024, 1, 1), new(2030, 12, 1), new(2024, 2, 1),
            _ => new Border { Height = 320 }, _ => { }) { Width = 240, Height = 180 };
        var window = Show(list); using var close = new CloseWindow(window);
        window.SetValue(AnimationScope.DurationScaleProperty, scale);
        list.Consume(20);
        var fling = list.FlingAsync(1500); clock.Track(fling);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        var initial = list.FirstVisibleOffset;
        for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.True(fling.IsCompletedSuccessfully); Assert.False(list.IsScrollInProgress);
        Assert.True(list.FirstVisibleIndex > 1 || list.FirstVisibleOffset > initial);
        Assert.NotEqual(0, list.FirstVisibleOffset); // Default range fling has no snap phase.
    }

    [AvaloniaFact]
    public void ExternalJump_AndDetach_CancelFling_ReattachedListRemainsUsable()
    {
        using var clock = new MotionTestClockScope();
        var list = Create(); var window = Show(list); using var close = new CloseWindow(window);
        clock.Track(list.FlingAsync(1500)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        list.ScrollToMonth(new(2024, 3, 1)); Flush(window);
        for (var i = 0; i < 20; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(2, list.FirstVisibleIndex); Assert.Equal(0, list.FirstVisibleOffset);
        Assert.False(list.IsScrollInProgress);
        clock.Track(list.FlingAsync(1500)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        window.Content = null; Flush(window);
        var position = (list.FirstVisibleIndex, list.FirstVisibleOffset);
        for (var i = 0; i < 20; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(position, (list.FirstVisibleIndex, list.FirstVisibleOffset));
        Assert.False(list.IsScrollInProgress);
        window.Content = list; Flush(window);
        list.ScrollToMonth(new(2024, 2, 1)); Flush(window);
        Assert.Equal(1, list.FirstVisibleIndex); Assert.Equal(0, list.FirstVisibleOffset);
        Assert.Equal(40, list.Consume(40));
    }

    [AvaloniaFact]
    public void Wheel_UsesSharedSmoothing_StopsWithoutFling_AndIgnoresMouseDragging()
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        var list = Create();
        var window = new AnimationTestWindow(frames, time) { Width = 500, Height = 700, Content = list };
        using var close = new CloseWindow(window); window.Show(); Flush(window);
        using (window.CaptureRenderedFrame()) { }
        frames.SendFrame(TimeSpan.Zero);
        window.MouseDown(new(80, 120), MouseButton.Left); window.MouseMove(new(80, 20));
        window.MouseUp(new(80, 20), MouseButton.Left);
        Assert.Equal(0, list.FirstVisibleOffset);
        window.MouseWheel(new(80, 100), new Vector(0, -3));
        Advance(40);
        var position = (list.FirstVisibleIndex, list.FirstVisibleOffset);
        Assert.Equal((1, 27d), position); // Windows fallback: 3 * viewport 180 / 20.
        Assert.False(list.IsScrollInProgress);
        Advance(30);
        Assert.Equal(position, (list.FirstVisibleIndex, list.FirstVisibleOffset));
        void Advance(int count)
        {
            for (var i = 0; i < count; i++)
            {
                time.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
                frames.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
            }
        }
    }

    [AvaloniaTheory]
    [InlineData(false, 1)]
    [InlineData(true, 4)]
    public void RealRangePicker_TouchOverDay_CancelsClick_AndExternalMonthInterrupts(bool rtl, double density)
    {
        using var clock = new MotionTestClockScope();
        var picker = new DateRangePicker
        {
            MinYear = 2024, MaxYear = 2025, DisplayedMonth = new(2024, 2, 1),
            Culture = CultureInfo.GetCultureInfo("en-US"), Width = 400, Height = 620
        };
        var window = Show(picker, rtl, density); using var close = new CloseWindow(window);
        for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        var list = picker.GetVisualDescendants().OfType<DateRangeMonthList>().Single();
        using (window.CaptureRenderedFrame()) { }
        using var touch = new TouchDevice();
        var day = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, new DateOnly(2024, 2, 21)));
        var origin = day.TranslatePoint(new Point(day.Bounds.Width / 2, day.Bounds.Height / 2), window)!.Value;
        Send(100, RawPointerEventType.TouchBegin, 0);
        Send(116, RawPointerEventType.TouchUpdate, 10);
        Assert.Equal(0, list.FirstVisibleOffset);
        Send(132, RawPointerEventType.TouchUpdate, 36);
        Send(148, RawPointerEventType.TouchUpdate, 80);
        Send(164, RawPointerEventType.TouchUpdate, 120);
        Assert.InRange(list.FirstVisibleOffset, 101.9, 102.1);
        Send(180, RawPointerEventType.TouchEnd, 120);
        Assert.Null(picker.SelectedRange.Start); Assert.Null(picker.SelectedRange.End);
        picker.DisplayedMonth = new(2024, 8, 1); Flush(window);
        for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.Equal(new DateOnly(2024, 8, 1), list.FirstVisibleMonth);
        Assert.Equal(0, list.FirstVisibleOffset); Assert.False(list.IsScrollInProgress);
        void Send(ulong time, RawPointerEventType type, double distance)
        {
            window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time,
                (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!,
                type, origin - new Vector(0, distance), RawInputModifiers.None, 1));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Flush(window);
        }
    }

    private static DateRangeMonthList Create() => new(new(2024, 1, 1), new(2024, 6, 1), new(2024, 2, 1),
        month => new Border { Height = (month.Month % 3) switch { 1 => 80, 2 => 140, _ => 100 } }, _ => { })
        { Width = 240, Height = 180, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

    private static MotionTestWindow Show(Control content, bool rtl = false, double density = 1)
    {
        var window = new MotionTestWindow { Width = 500, Height = 700, FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.SetRenderScaling(density); window.Show(); Flush(window); return window;
    }
    private sealed class CloseWindow(Window window) : IDisposable { public void Dispose() => window.Close(); }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
