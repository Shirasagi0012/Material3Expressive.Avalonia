using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ScrollBarTests
{
    [AvaloniaFact]
    public void Tokens_AreCompleteAndKeepRoleReferences() =>
        TokenScan.AssertComplete(new ScrollBarTokenSource(), typeof(MaterialTokens.Comp.ScrollBar));

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Template_KeepsNativeTrackAndDefaults(bool horizontal)
    {
        var bar = Bar(horizontal); var window = Show(bar);
        try
        {
            Assert.Equal(8, horizontal ? bar.Bounds.Height : bar.Bounds.Width);
            Assert.Equal(new CornerRadius(4), bar.CornerRadius);
            Assert.Equal(TimeSpan.FromMilliseconds(300), ScrollBarAppearance.GetHoverDuration(bar));
            var track = Assert.IsType<Track>(Part<Track>(bar, "PART_Track"));
            var thumb = Part<Thumb>(bar, "PART_Thumb");
            Assert.Equal(50, horizontal ? thumb.Bounds.Width : thumb.Bounds.Height);
            Assert.Equal(16, horizontal ? thumb.MinWidth : thumb.MinHeight);
            Assert.Equal(!horizontal, track.IsDirectionReversed);
            Assert.Equal(2, bar.GetVisualDescendants().OfType<RepeatButton>().Count());
            Assert.IsType<ScrollBarAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(bar));
            Assert.False(bar.Focusable);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void StockContextFlyout_IsReusedAndCallerOverrideSurvives(bool horizontal)
    {
        var bar = Bar(horizontal); var window = Show(bar, fluent: true);
        try
        {
            var key = horizontal ? "HorizontalScrollBarContextFlyout" : "VerticalScrollBarContextFlyout";
            Assert.Same(window.FindResource(key), bar.ContextFlyout);
            var menu = Assert.IsType<MenuFlyout>(bar.ContextFlyout);
            bar.LargeChange = 7;
            menu.ShowAt(bar);
            Dispatcher.UIThread.RunJobs();
            var page = menu.Items.OfType<MenuItem>().Single(item =>
                AutomationProperties.GetAutomationId(item) == (horizontal ? "PageRight" : "PageDown"));
            Assert.NotNull(page.Command);
            page.Command.Execute(page.CommandParameter);
            Assert.Equal(7, bar.Value);
            menu.Hide();
            var custom = new MenuFlyout { Items = { new MenuItem { Header = "Custom" } } };
            bar.ContextFlyout = custom;
            bar.Orientation = horizontal ? Orientation.Vertical : Orientation.Horizontal;
            window.UpdateLayout();
            Assert.Same(custom, bar.ContextFlyout);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NativePagingAndDrag_WriteThroughBinding(bool horizontal, bool rtl)
    {
        var source = new ScrollBar { Maximum = 75, Value = 0 };
        var bar = Bar(horizontal);
        bar.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        bar.LargeChange = 7;
        bar.Bind(RangeBase.ValueProperty, new Binding(nameof(ScrollBar.Value)) { Source = source, Mode = BindingMode.TwoWay });
        var window = Show(bar);
        try
        {
            var page = Part<RepeatButton>(bar, "PART_PageDownButton");
            var point = Center(page, window);
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal(7, bar.Value); Assert.Equal(7, source.Value);
            window.UpdateLayout();
            var thumb = Part<Thumb>(bar, "PART_Thumb");
            point = Center(thumb, window);
            window.MouseDown(point, MouseButton.Left);
            window.MouseMove(point + (horizontal ? new Vector(rtl ? -20 : 20, 0) : new Vector(0, 20)));
            window.MouseUp(point, MouseButton.Left);
            Assert.True(bar.Value > 7); Assert.Equal(bar.Value, source.Value);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WholeTrackHover_AnimatesWithoutExpansionDelayOrThicknessChange()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            var normal = ScrollBarAppearance.GetUnhoverColor(bar); var hover = ScrollBarAppearance.GetHoverColor(bar);
            Assert.Equal(normal, visual.PaintedColor);
            window.MouseMove(Center(Part<RepeatButton>(bar, "PART_PageDownButton"), window));
            Assert.True(bar.IsPointerOver); Assert.False(bar.IsExpanded);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
            Assert.InRange(visual.PaintedColor.A, (byte)(normal.A + 1), (byte)(hover.A - 1));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
            Assert.Equal(hover, visual.PaintedColor); Assert.Equal(8, bar.Bounds.Width);
            bar.AllowAutoHide = false; Assert.True(bar.IsExpanded);
            window.MouseMove(new Point(220, 220)); Settle(clock);
            Assert.Equal(normal, visual.PaintedColor); Assert.Equal(8, bar.Bounds.Width);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DragOutsideTrack_KeepsHighlightUntilNativeRelease()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            var point = Center(Part<Thumb>(bar, "PART_Thumb"), window);
            window.MouseDown(point, MouseButton.Left);
            var outside = new Point(120, 100);
            window.MouseMove(outside); window.UpdateLayout(); Settle(clock);
            Assert.False(bar.IsPointerOver); Assert.True(bar.Value > 0);
            Assert.Equal(ScrollBarAppearance.GetHoverColor(bar), visual.PaintedColor);
            window.MouseUp(outside, MouseButton.Left); Settle(clock);
            Assert.Equal(ScrollBarAppearance.GetUnhoverColor(bar), visual.PaintedColor);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NativeCaptureLoss_ClearsDragHighlight()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            var thumb = Part<Thumb>(bar, "PART_Thumb");
            IPointer? pointer = null;
            thumb.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer,
                handledEventsToo: true);
            window.MouseDown(Center(thumb, window), MouseButton.Left);
            Assert.NotNull(pointer); Assert.NotNull(pointer.Captured);
            window.MouseMove(new Point(120, 100)); Settle(clock);
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            Assert.Equal(ScrollBarAppearance.GetHoverColor(bar), visual.PaintedColor);
            pointer.Capture(null); Settle(clock);
            Assert.Equal(ScrollBarAppearance.GetUnhoverColor(bar), visual.PaintedColor);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void HoverExitDuringTween_RetargetsFromCurrentColor()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            var normal = ScrollBarAppearance.GetUnhoverColor(bar);
            window.MouseMove(Center(Part<RepeatButton>(bar, "PART_PageDownButton"), window));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            var halfway = visual.PaintedColor;
            Assert.True(halfway.A > normal.A);
            window.MouseMove(new Point(220, 220));
            Assert.Equal(halfway, visual.PaintedColor);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            Assert.InRange(visual.PaintedColor.A, (byte)(normal.A + 1), (byte)(halfway.A - 1));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
            Assert.Equal(normal, visual.PaintedColor); Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OrientationTemplateReplacement_DetachesOldFeedback()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            window.MouseMove(Center(Part<RepeatButton>(bar, "PART_PageDownButton"), window));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            var oldVisual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            bar.Orientation = Orientation.Horizontal; bar.Width = 200; bar.Height = double.NaN;
            window.UpdateLayout();
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            Assert.NotSame(oldVisual, visual);
            var detachedColor = oldVisual.PaintedColor;
            ScrollBarAppearance.SetUnhoverColor(bar, Colors.Red);
            window.MouseMove(new Point(220, 220)); Settle(clock);
            Assert.Equal(Colors.Red, visual.PaintedColor);
            Assert.Equal(detachedColor, oldVisual.PaintedColor);
            Assert.Equal(8, bar.Bounds.Height); Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DynamicOnSurfaceAndAppearanceOverrides_UpdateColorAndGeometry()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar);
        try
        {
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            var color = Color.Parse("#126E60");
            bar.Resources[MaterialTokens.Sys.Color.OnSurface] = color;
            Settle(clock);
            Assert.Equal(Color.FromArgb(31, color.R, color.G, color.B), visual.PaintedColor);
            Assert.Equal(Color.FromArgb(128, color.R, color.G, color.B), ScrollBarAppearance.GetHoverColor(bar));
            ScrollBarAppearance.SetUnhoverColor(bar, Colors.Red);
            ScrollBarAppearance.SetThickness(bar, 12);
            ScrollBarAppearance.SetThumbMinLength(bar, 80);
            bar.CornerRadius = new CornerRadius(2);
            window.UpdateLayout(); Settle(clock);
            Assert.Equal(Colors.Red, visual.PaintedColor); Assert.Equal(new CornerRadius(2), visual.CornerRadius);
            Assert.Equal(12, bar.Bounds.Width); Assert.Equal(80, Part<Thumb>(bar, "PART_Thumb").Bounds.Height);
            bar.Resources[MaterialTokens.Sys.Color.OnSurface] = Colors.Blue;
            Settle(clock); Assert.Equal(Colors.Red, visual.PaintedColor);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DetachDuringAnimation_CancelsAndReattachUsesCurrentState()
    {
        using var clock = new MotionTestClockScope();
        var bar = Bar(); var window = Show(bar); var root = (Grid)window.Content!;
        try
        {
            window.MouseMove(Center(Part<RepeatButton>(bar, "PART_PageDownButton"), window));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            var visual = Part<ScrollBarThumb>(bar, "PART_ThumbVisual");
            root.Children.Remove(bar); Dispatcher.UIThread.RunJobs();
            var oldColor = visual.PaintedColor;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(400));
            Assert.Equal(oldColor, visual.PaintedColor); Assert.Equal(0, clock.PendingFrameCount);
            window.MouseMove(new Point(220, 220)); root.Children.Add(bar); window.UpdateLayout();
            Assert.Equal(ScrollBarAppearance.GetUnhoverColor(bar), visual.PaintedColor);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NativeScrollViewer_KeepsBindingVisibilityAndDeferredScrolling()
    {
        var viewer = new ScrollViewer { Width = 160, Height = 140, AllowAutoHide = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsDeferredScrollingEnabled = true, Content = new Border { Width = 400, Height = 500 } };
        var window = Show(viewer, fluent: true);
        try
        {
            // SmoothScroll connects to its owner on Loaded, after the first layout pass.
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            var bar = Part<ScrollBar>(viewer, "PART_VerticalScrollBar");
            Assert.IsType<ScrollBarThumb>(Part<ScrollBarThumb>(bar, "PART_ThumbVisual"));
            Assert.Equal(viewer.Viewport.Height, bar.ViewportSize);
            Assert.Equal(viewer.ScrollBarMaximum.Y, bar.Maximum);
            Assert.True(Part<Track>(bar, "PART_Track").DeferThumbDrag);
            viewer.Offset = new Vector(20, 40); Assert.Equal(40, bar.Value);
            bar.Value = 60; Assert.Equal(60, viewer.Offset.Y);
            viewer.Content = new Border { Width = 40, Height = 40 }; window.UpdateLayout();
            Assert.False(bar.IsVisible); Assert.Equal(0, viewer.Offset.Y);
        }
        finally { window.Close(); }
    }

    private static ScrollBar Bar(bool horizontal = false) => new()
    {
        Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
        Maximum = 75, ViewportSize = 25,
        Width = horizontal ? 200 : double.NaN, Height = horizontal ? double.NaN : 200,
    };
    private static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
    private static void Settle(MotionTestClockScope clock)
    {
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(300));
    }
    private static MotionTestWindow Show(Control control, bool fluent = false)
    {
        control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 260, Height = 240 };
        if (fluent) window.Styles.Add(new FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}
