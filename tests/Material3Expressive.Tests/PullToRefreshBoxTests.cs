using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class PullToRefreshBoxTests
{
    [AvaloniaTheory]
    [InlineData(100, 0)]
    [InlineData(230, 1)]
    public void TouchCancellationSettlesThePullWithZeroVelocity(double endY, int expectedRequests)
    {
        using var clock = new MotionTestClockScope();
        var (window, box, viewer) = Show();
        using var touch = new TouchDevice();
        var requests = 0;
        box.RefreshRequested += (_, _) => requests++;
        try
        {
            clock.SendFrame(TimeSpan.Zero);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 30);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, endY); Render(window);
            Assert.True(box.ActualState.DistanceFraction > 0);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, endY); Render(window);
            Assert.Equal(expectedRequests, requests);
            Frames(clock, window, 16, 1008);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void FirstTouchMovementConsumesOnlyCoreSlopAndDisconnectRestoresPresenterPolicy(double density)
    {
        using var clock = new MotionTestClockScope();
        var (window, box, viewer) = Show(density);
        using var touch = new TouchDevice();
        try
        {
            var presenter = Assert.IsType<SmoothScroll.Avalonia.Controls.ScrollViewerPresenter>(viewer.Presenter);
            Assert.Equal(18d, presenter.VerticalTouchSlop);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 30);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 100); Render(window);
            // Core vertical touch slop: (70 - 18) * dragMultiplier(.5) / threshold(80).
            Assert.Equal(.325f, box.ActualState.DistanceFraction);
            Assert.Equal(0, viewer.Offset.Y);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, 100); Render(window);
            box.Content = null; Render(window);
            Assert.False(box.IsScrollConnected);
            Assert.Null(presenter.VerticalTouchSlop);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void RealTouchPullAtStartRequestsRefreshAndCompletes(double density)
    {
        using var clock = new MotionTestClockScope();
        var (window, box, viewer) = Show(density);
        using var touch = new TouchDevice();
        var requests = 0;
        box.RefreshRequested += (_, _) => { requests++; box.IsRefreshing = true; };
        try
        {
            Assert.True(box.IsScrollConnected, $"template={box.Template}, presenter={viewer.Presenter}, visuals={string.Join(';', box.GetVisualDescendants().Select(v => v.GetType().Name))}");
            clock.SendFrame(TimeSpan.Zero);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 30);
            Send(window, touch, 108, RawPointerEventType.TouchUpdate, 40);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 100);
            Send(window, touch, 132, RawPointerEventType.TouchUpdate, 170);
            Send(window, touch, 148, RawPointerEventType.TouchUpdate, 230);
            Render(window);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.True(box.ActualState.DistanceFraction > 1);
            Assert.Equal(0, requests);
            Send(window, touch, 164, RawPointerEventType.TouchEnd, 230);
            Render(window);
            Assert.Equal(1, requests);
            Frames(clock, window, 16, 1008);
            Assert.Equal(1, box.ActualState.DistanceFraction);
            box.IsRefreshing = false;
            Frames(clock, window, 1024, 2016);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void BodyConsumesBeforePullAndUpwardPreScrollRetractsBeforeBody()
    {
        using var clock = new MotionTestClockScope();
        var (window, box, viewer) = Show();
        using var touch = new TouchDevice();
        try
        {
            viewer.Offset = new Vector(0, 60); Render(window);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 40);
            Send(window, touch, 108, RawPointerEventType.TouchUpdate, 52);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 80); Render(window);
            Assert.True(viewer.Offset.Y < 60);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Send(window, touch, 132, RawPointerEventType.TouchUpdate, 200); Render(window);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.True(box.ActualState.DistanceFraction > 0);
            Send(window, touch, 148, RawPointerEventType.TouchUpdate, 160); Render(window);
            Assert.Equal(0, viewer.Offset.Y);
            Send(window, touch, 164, RawPointerEventType.TouchUpdate, 40); Render(window);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Assert.True(viewer.Offset.Y > 0);
            Send(window, touch, 180, RawPointerEventType.TouchCancel, 40); Render(window);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RefreshEnabledGatesPullWithoutDisablingContentAndReplacementReleasesOldState()
    {
        using var clock = new MotionTestClockScope();
        var (window, box, viewer) = Show();
        using var touch = new TouchDevice();
        try
        {
            box.RefreshEnabled = false;
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 30);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 230); Render(window);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Assert.True(viewer.IsEffectivelyEnabled);
            Send(window, touch, 132, RawPointerEventType.TouchEnd, 230); Render(window);
            box.RefreshEnabled = true;
            var previous = box.ActualState;
            box.State = new PullToRefreshState(); Render(window);
            Assert.NotSame(previous, box.ActualState);
            Assert.Same(box.ActualState, Assert.IsType<PullToRefreshIndicator>(box.ActualIndicator).State);
            window.Content = null; Render(window);
            Assert.False(box.IsScrollConnected);
            window.Content = box; Render(window);
            Assert.True(box.IsScrollConnected);
            Assert.Equal(0, box.ActualState.DistanceFraction);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AmbiguousViewportsDisconnectAndSingleReplacementReconnects()
    {
        using var clock = new MotionTestClockScope();
        var (window, box, first) = Show();
        try
        {
            box.Content = null;
            var second = Viewer();
            var grid = new Grid { Children = { first, second } };
            box.Content = grid; Render(window);
            Assert.False(box.IsScrollConnected);
            grid.Children.Remove(first); Render(window);
            Assert.True(box.IsScrollConnected);
            second.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; Render(window);
            Assert.False(box.IsScrollConnected);
            second.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden; Render(window);
            Assert.True(box.IsScrollConnected);
            second.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto; Render(window);
            Assert.False(box.IsScrollConnected);
            second.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; Render(window);
            Assert.True(box.IsScrollConnected);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ScaffoldAppBarAndPullShareThePhysicalViewport()
    {
        using var clock = new MotionTestClockScope();
        var viewer = Viewer();
        var box = new PullToRefreshBox { Content = viewer };
        var topState = new TopAppBarState();
        var bar = new TopAppBar { ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways)
            { State = topState, SnapAnimationSpec = null, FlingAnimationSpec = null } };
        var scaffold = new Scaffold { TopBar = bar };
        var body = new Border { Child = box };
        body.Bind(Border.WidthProperty, new Avalonia.Data.Binding("Bounds.Width") { Source = scaffold });
        body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = scaffold });
        body.Bind(Border.PaddingProperty, scaffold.GetObservable(Scaffold.ContentPaddingProperty));
        scaffold.Content = body;
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add(new MaterialTheme()); window.Content = scaffold; window.Show(); Render(window);
        using var touch = new TouchDevice();
        try
        {
            Assert.True(box.IsScrollConnected);
            var start = viewer.TranslatePoint(new Point(60, 160), window)!.Value.Y;
            Send(window, touch, 100, RawPointerEventType.TouchBegin, start);
            Send(window, touch, 108, RawPointerEventType.TouchUpdate, start - 12);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, start - 60);
            Send(window, touch, 132, RawPointerEventType.TouchUpdate, start - 120); Render(window);
            Assert.True(topState.HeightOffset < 0);
            Assert.True(viewer.Offset.Y > 0);
            Assert.Equal(0, box.ActualState.DistanceFraction);
            Send(window, touch, 148, RawPointerEventType.TouchCancel, start - 120); Render(window);
        }
        finally { window.Close(); }
    }

    private static ScrollViewer Viewer() => new()
    {
        Content = new Border { Height = 1200, Background = Brushes.LightBlue },
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        IsScrollInertiaEnabled = false
    };

    private static (MotionTestWindow Window, PullToRefreshBox Box, ScrollViewer Viewer) Show(double density = 1)
    {
        var viewer = Viewer();
        var box = new PullToRefreshBox { Content = viewer };
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add(new MaterialTheme()); window.Content = box; window.Show(); window.SetRenderScaling(density); Render(window);
        return (window, box, viewer);
    }

    private static void Send(Window window, TouchDevice device, ulong time, RawPointerEventType type, double y) =>
        window.PlatformImpl!.Input!(new RawTouchEventArgs(device, time,
            (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!,
            type, new Point(60, y), RawInputModifiers.None, 1));

    private static void Render(Window window)
    {
        for (var turn = 0; turn < 4; turn++)
        {
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            using (window.CaptureRenderedFrame()) { }
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    {
        for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Render(window); }
    }
}
