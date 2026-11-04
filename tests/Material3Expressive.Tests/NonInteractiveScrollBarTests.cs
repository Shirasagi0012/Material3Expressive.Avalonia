using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class NonInteractiveScrollBarTests
{
    [AvaloniaFact]
    public void TokensAndDefaults_AreDistinctFromNativeScrollBar()
    {
        TokenScan.AssertComplete(new NonInteractiveScrollBarTokenSource(), typeof(MaterialTokens.Comp.NonInteractiveScrollBar));
        var bar = new NonInteractiveScrollBar(); using var host = new Host(bar);
        Assert.Equal(4, bar.Thickness); Assert.Equal(24, bar.ThumbMinLength); Assert.Equal(.9, bar.ThumbMaxLengthFraction);
        Assert.Equal(TimeSpan.FromMilliseconds(250), bar.FadeDuration); Assert.Equal(TimeSpan.FromMilliseconds(400), bar.FadeDelay);
        Assert.Equal(2, bar.MainAxisTrackInset); Assert.Equal(0, bar.CrossAxisTrackInset);
        Assert.False(bar.Focusable); Assert.False(bar.IsHitTestVisible); Assert.Null(bar.PaintedThumb);
        Assert.Throws<ArgumentException>(() => bar.ThumbMaxLengthFraction = 1.1);
        Assert.Throws<ArgumentException>(() => bar.ThumbMaxLengthFraction = double.NaN);
    }

    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Geometry_UsesLogicalEndAndDoesNotClampOverscroll(bool horizontal, bool rtl)
    {
        var bar = new NonInteractiveScrollBar { ContentSize = 600, ViewportSize = 120, IsFadeEnabled = false,
            Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new Host(bar);
        var start = bar.PaintedThumb!.Value;
        Assert.Equal(horizontal ? 116 : rtl ? 0 : 156, horizontal ? start.Y : start.X);
        Assert.Equal(horizontal ? (double)(156f * .2f) : 24, horizontal ? start.Width : start.Height);
        bar.ScrollOffset = 480; host.Draw(); var end = bar.PaintedThumb!.Value;
        if (horizontal) Assert.True(rtl ? end.X < start.X : end.X > start.X);
        else Assert.True(end.Y > start.Y);
        bar.ScrollOffset = -100; host.Draw(); var outside = bar.PaintedThumb!.Value;
        Assert.True(horizontal ? rtl ? outside.X > start.X : outside.X < start.X : outside.Y < start.Y);
        bar.ContentSize = 120; host.Draw(); Assert.Null(bar.PaintedThumb);
        bar.ContentSize = 600; bar.ViewportSize = 0; host.Draw(); Assert.Null(bar.PaintedThumb);
    }

    [AvaloniaFact]
    public void Fade_DelayRestartsOnObservedOffsetsAndScrollInterruptsFade()
    {
        var bar = new NonInteractiveScrollBar { ContentSize = 600, ViewportSize = 120 };
        using var host = new Host(bar);
        host.Advance(32); bar.ScrollOffset = 40; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        host.Advance(160); bar.ScrollOffset = 80; host.Draw();
        host.Advance(544); Assert.Equal(1, bar.PaintedAlpha);
        host.Advance(640); Assert.InRange(bar.PaintedAlpha, .01f, .99f);
        bar.ScrollOffset = 120; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        host.Advance(1024); Assert.Equal(1, bar.PaintedAlpha);
        host.Advance(1328); Assert.Equal(0, bar.PaintedAlpha); Assert.Null(bar.PaintedThumb);
    }

    [AvaloniaFact]
    public void ConfigurationRecreatesNodeWhileStateUpdatesOnlyInvalidateDrawing()
    {
        var bar = new NonInteractiveScrollBar { ContentSize = 600, ViewportSize = 120 };
        using var host = new Host(bar);
        bar.ScrollOffset = 50; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        bar.ContentSize = 800; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        bar.ThumbColor = Colors.Red; host.Draw(); Assert.Equal(0, bar.PaintedAlpha); Assert.Equal(0, host.Time.ActiveTimerCount);
        bar.ScrollOffset = 70; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        bar.IsFadeEnabled = false; host.Draw(); Assert.Equal(1, bar.PaintedAlpha); Assert.Equal(0, host.Time.ActiveTimerCount);
        bar.IsFadeEnabled = true; host.Draw(); Assert.Equal(0, bar.PaintedAlpha);
    }

    [AvaloniaFact]
    public void ContentThatFits_DoesNotObserveOffsetUntilItBecomesScrollable()
    {
        var bar = new NonInteractiveScrollBar { ContentSize = 100, ViewportSize = 120 };
        using var host = new Host(bar);
        bar.ScrollOffset = 50; host.Draw(); Assert.Equal(0, bar.PaintedAlpha);
        bar.ContentSize = 600; host.Draw(); Assert.Equal(1, bar.PaintedAlpha);
        host.Root.Children.Remove(bar); host.Draw(); Assert.Equal(0, host.Time.ActiveTimerCount); Assert.Equal(0, host.Clock.PendingFrameCount);
        bar.ScrollOffset = 200; host.Root.Children.Add(bar); host.Draw();
        // Re-attachment baselines the current offset; it must not fabricate a new scroll event.
        Assert.Equal(0, host.Time.ActiveTimerCount);
        bar.ScrollOffset = 220; host.Draw(); Assert.Equal(1, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void ScrollViewerBindings_FollowRealOffsetAndViewportWithoutReservingSpace()
    {
        var viewer = new ScrollViewer { Content = new Border { Height = 600 }, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        var bar = new NonInteractiveScrollBar { IsFadeEnabled = false };
        bar.Bind(NonInteractiveScrollBar.ContentSizeProperty, new Binding("Extent.Height") { Source = viewer });
        bar.Bind(NonInteractiveScrollBar.ViewportSizeProperty, new Binding("Viewport.Height") { Source = viewer });
        bar.Bind(NonInteractiveScrollBar.ScrollOffsetProperty, new Binding("Offset.Y") { Source = viewer });
        using var host = new Host(new Grid { Children = { viewer, bar } });
        Assert.Equal(viewer.Bounds.Size, bar.Bounds.Size); Assert.Equal(600, bar.ContentSize); Assert.Equal(120, bar.ViewportSize);
        viewer.Offset = new Vector(0, 240); host.Draw(); Assert.Equal(240, bar.ScrollOffset);
        Assert.Equal(new Rect(156, 48, 4, 24), bar.PaintedThumb);
    }

    [AvaloniaFact]
    public void DefaultThumbAlpha_ReplacesRoleAlphaAndLocalColorWinsUntilCleared()
    {
        var bar = new NonInteractiveScrollBar { ContentSize = 600, ViewportSize = 120, IsFadeEnabled = false };
        using var host = new Host(bar);
        host.Window.Resources[MaterialTokens.Sys.Color.Outline] = Color.Parse("#20336699"); host.Draw();
        Assert.Equal(Color.FromArgb(179, 0x33, 0x66, 0x99), bar.ThumbColor);
        bar.ThumbColor = Color.Parse("#10112233");
        host.Window.Resources[MaterialTokens.Comp.NonInteractiveScrollBar.ThumbOpacity] = .5; host.Draw();
        Assert.Equal(Color.Parse("#10112233"), bar.ThumbColor);
        bar.ClearValue(NonInteractiveScrollBar.ThumbColorProperty); host.Draw();
        Assert.Equal(Color.FromArgb(128, 0x33, 0x66, 0x99), bar.ThumbColor);
    }

    private sealed class Host : IDisposable
    {
        public ManualFrameSource Clock { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public AnimationTestWindow Window { get; }
        public Grid Root { get; } = new();
        private int _frame;
        public Host(Control control)
        {
            Window = new(Clock, Time) { Width = 160, Height = 120 };
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Root.Children.Add(control); Window.Content = Root; Window.Show(); Draw();
        }
        public void Draw()
        {
            Window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        }
        public void Advance(int target)
        {
            while (_frame + 16 <= target)
            {
                _frame += 16; Time.AdvanceBy(TimeSpan.FromMilliseconds(_frame) - Time.Elapsed); Dispatcher.UIThread.RunJobs();
                Clock.SendFrame(TimeSpan.FromMilliseconds(1000 + _frame)); Draw();
            }
        }
        public void Dispose()
        {
            Window.Close(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, Clock.PendingFrameCount); Assert.Equal(0, Time.ActiveTimerCount);
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError); Clock.Dispose();
        }
    }
}
