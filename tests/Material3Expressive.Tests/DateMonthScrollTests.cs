using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Tests;

public class DateMonthScrollTests
{
    [AvaloniaTheory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 4)]
    [InlineData(true, false, 4)]
    [InlineData(false, true, 4)]
    [InlineData(true, true, 4)]
    public void TouchOverDayButtons_DragsContinuously_AndUsesReleaseVelocity(bool rtl, bool slow, double density)
    {
        using var clock = new MotionTestClockScope();
        var (window, picker, strip) = Show(rtl, density);
        try
        {
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.Equal(rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, strip.FlowDirection);
            using (window.CaptureRenderedFrame()) { }
            using var touch = new TouchDevice();
            var day = picker.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Tag, new DateOnly(2024, 2, 7)));
            var origin = day.TranslatePoint(new Point(day.Bounds.Width / 2, day.Bounds.Height / 2), window)!.Value;
            Send(100, RawPointerEventType.TouchBegin, 0);
            Send(116, RawPointerEventType.TouchUpdate, 10);
            Send(132, RawPointerEventType.TouchUpdate, 36);
            Send(148, RawPointerEventType.TouchUpdate, 80);
            Send(164, RawPointerEventType.TouchUpdate, 120);
            Assert.InRange(strip.ScrollOffset, 101.9, 102.1);
            Send(slow ? 244UL : 180UL, RawPointerEventType.TouchEnd, 120);
            for (var i = 0; i < 70; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.Equal(new DateOnly(2024, slow ? 2 : 3, 1), picker.DisplayedMonth);
            Assert.Equal(0, strip.ScrollOffset);
            Assert.Null(picker.SelectedDate); // Taking the drag cancels the pressed day's click.
            void Send(ulong time, RawPointerEventType type, double distance)
            {
                window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time,
                    (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!,
                    type, origin + new Vector(rtl ? distance : -distance, 0), RawInputModifiers.None, 1));
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Flush(window);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(.25, 0, false)]
    [InlineData(.5, 0, true)]
    [InlineData(.75, 0, true)]
    [InlineData(.25, 400, true)]
    [InlineData(.75, -400, false)]
    [InlineData(.25, 399, false)]
    public void Release_SnapsToVisibleMonth_WithVelocityThreshold(double fraction, float velocity, bool next)
    {
        using var clock = new MotionTestClockScope();
        var (window,picker,strip)=Show();
        try
        {
            var width=(float)strip.Bounds.Width;
            strip.Consume(width*(float)fraction); Flush(window);
            Assert.Equal(new DateOnly(2024,2,1),picker.DisplayedMonth);
            Assert.Equal(2,strip.Children.Count);
            var motion=strip.SettleAsync(velocity); clock.Track(motion);
            for(var i=0;i<70;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush(window);}
            Assert.True(motion.IsCompletedSuccessfully);
            Assert.Equal(new DateOnly(2024,next?3:2,1),picker.DisplayedMonth);
            Assert.Equal(0,strip.ScrollOffset);
            Assert.Single(strip.Children);
        }
        finally {window.Close();}
    }

    [AvaloniaFact]
    public void Animation_UpdatesFirstVisibleMonth_Interrupts_AndStopsAtYearBoundary()
    {
        using var clock = new MotionTestClockScope();
        var (window,picker,strip)=Show();
        try
        {
            clock.Track(strip.AnimateToAsync(new(2024,3,1)));
            for(var i=0;i<5;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush(window);}
            Assert.True(strip.IsScrollInProgress);
            Assert.True(strip.ScrollOffset>0);
            Assert.Equal(new DateOnly(2024,2,1),picker.DisplayedMonth);
            clock.Track(strip.AnimateToAsync(new(2024,1,1)));
            for(var i=0;i<70;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush(window);}
            Assert.Equal(new DateOnly(2024,1,1),picker.DisplayedMonth);
            Assert.Equal(0,strip.Consume(-100));
            Assert.False(strip.CanScrollPrevious);
            picker.DisplayedMonth=new(2024,12,1);Flush(window);
            Assert.Equal(0,strip.Consume(100));
            Assert.False(strip.CanScrollNext);
        }
        finally {window.Close();}
    }

    [AvaloniaFact]
    public void DesktopMouseDragAndVerticalWheel_DoNotTurnMonths_ExternalMonthStillJumps()
    {
        var (window,picker,strip)=Show();
        try
        {
            var origin=strip.TranslatePoint(new Point(220,100),window)!.Value;
            window.MouseDown(origin,MouseButton.Left);
            window.MouseMove(origin-new Vector(130,0));
            window.MouseUp(origin-new Vector(130,0),MouseButton.Left);
            window.MouseWheel(origin,new Vector(0,-3));Flush(window);
            Assert.Equal(new DateOnly(2024,2,1),picker.DisplayedMonth);
            Assert.Equal(0,strip.ScrollOffset);
            picker.DisplayedMonth=new(2024,8,1);Flush(window);
            Assert.Equal(new DateOnly(2024,8,1),strip.FirstVisibleMonth);
            Assert.Single(strip.Children);
        }
        finally {window.Close();}
    }

    [AvaloniaFact]
    public void Detach_CancelsMonthAnimation_AndReattachRemainsUsable()
    {
        using var clock = new MotionTestClockScope();
        var (window,picker,strip)=Show();
        clock.Track(strip.AnimateToAsync(new(2024,3,1)));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush(window);
        window.Content=null;Flush(window);
        var stopped=picker.DisplayedMonth;
        for(var i=0;i<20;i++)clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(stopped,picker.DisplayedMonth);
        Assert.False(strip.IsScrollInProgress);
        window.Content=picker;Flush(window);
        picker.DisplayedMonth=new(2024,7,1);Flush(window);
        Assert.Equal(new DateOnly(2024,7,1),strip.FirstVisibleMonth);
    }

    private static (MotionTestWindow,DatePicker,DateMonthStrip) Show(bool rtl = false, double density = 1)
    {
        var picker=new DatePicker { MinYear=2024,MaxYear=2024,DisplayedMonth=new(2024,2,1),Culture=CultureInfo.GetCultureInfo("en-US"),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top };
        var window=new MotionTestWindow { Width=500,Height=700, FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content=picker;window.SetRenderScaling(density);window.Show();Flush(window);
        return(window,picker,picker.GetVisualDescendants().OfType<DateMonthStrip>().Single());
    }
    private static void Flush(Window window){Dispatcher.UIThread.RunJobs();window.UpdateLayout();}
}
