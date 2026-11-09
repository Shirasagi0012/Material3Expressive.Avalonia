using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class FloatingToolbarScrollTests
{
    [AvaloniaTheory]
    [InlineData(FloatingToolbarExitDirection.Start, 40, 0)]
    [InlineData(FloatingToolbarExitDirection.End, 40, 0)]
    [InlineData(FloatingToolbarExitDirection.Top, 80, -80)]
    [InlineData(FloatingToolbarExitDirection.Bottom, 80, -80)]
    public void RealContentDragObservesConsumptionAndZeroReleaseAutomaticallySnaps(
        FloatingToolbarExitDirection direction, double distance, float endpoint)
    {
        using var clock = new MotionTestClockScope();
        var (window, toolbar, viewer) = Show(direction);
        using var touch = new TouchDevice();
        try
        {
            Assert.True(toolbar.IsScrollConnected);
            var state = toolbar.ScrollBehavior!.State;
            Assert.Equal(-80, state.OffsetLimit);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 150);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 150 - distance); Render(window);
            Assert.Equal(distance - 18, viewer.Offset.Y);
            Assert.Equal(-(float)(distance - 18), state.Offset);
            Assert.Equal(state.Offset, state.ContentOffset);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, 150 - distance); Render(window);
            Frames(clock, window, 0, 1200);
            // The upstream limit tracks rounded placement minus the unrounded state.
            // Reaching it preserves a fractional state while the physical exit is 80 DIP.
            Assert.Equal(endpoint == 0 ? 0 : state.OffsetLimit, state.Offset);
            Assert.Equal(endpoint, MathF.Round(state.Offset));
            Assert.Equal(distance - 18, viewer.Offset.Y);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PointerReleaseAfterRestSnapsWithoutRevivingBodyInertia()
    {
        using var clock = new MotionTestClockScope();
        var (window, toolbar, viewer) = Show(FloatingToolbarExitDirection.Bottom);
        using var touch = new TouchDevice();
        try
        {
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 150);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 70); Render(window);
            Send(window, touch, 200, RawPointerEventType.TouchEnd, 70); Render(window);
            Frames(clock, window, 0, 1200);
            Assert.Equal(62, viewer.Offset.Y);
            Assert.Equal(toolbar.ScrollBehavior!.State.OffsetLimit, toolbar.ScrollBehavior.State.Offset);
            Assert.Equal(-80, MathF.Round(toolbar.ScrollBehavior.State.Offset));
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ProgrammaticBodyPositionDoesNotMasqueradeAsNestedConsumption()
    {
        using var clock = new MotionTestClockScope();
        var (window, toolbar, viewer) = Show(FloatingToolbarExitDirection.Bottom);
        try
        {
            viewer.Offset = new Vector(0, 150); Render(window);
            Assert.Equal(0, toolbar.ScrollBehavior!.State.Offset);
            Assert.Equal(0, toolbar.ScrollBehavior.State.ContentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("source")]
    [InlineData("behavior")]
    [InlineData("exploration")]
    [InlineData("detach")]
    public void DisconnectCancelsSettlingAndRestoresPresenterPolicy(string action)
    {
        using var clock = new MotionTestClockScope();
        var (window, toolbar, viewer) = Show(FloatingToolbarExitDirection.Bottom);
        using var touch = new TouchDevice();
        try
        {
            var previous = toolbar.ScrollBehavior!;
            var presenter = Assert.IsType<SmoothScroll.Avalonia.Controls.ScrollViewerPresenter>(viewer.Presenter);
            Assert.Equal(18, presenter.VerticalTouchSlop);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 150);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 70); Render(window);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, 70); Render(window);
            Frames(clock, window, 0, 64);
            switch (action)
            {
                case "source": toolbar.ScrollSource = null; break;
                case "behavior": toolbar.ScrollBehavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom); break;
                case "exploration": toolbar.TouchExplorationEnabled = true; break;
                case "detach": ((Grid)window.Content!).Children.Remove(toolbar); break;
            }
            Render(window);
            var stopped = previous.State.Offset;
            Frames(clock, window, 80, 1200);
            Assert.Equal(stopped, previous.State.Offset);
            Assert.Equal(action == "behavior", toolbar.IsScrollConnected);
            Assert.Equal(action == "behavior" ? 18d : null, presenter.VerticalTouchSlop);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NewContentGestureInterruptsPreviousPostFlingSnap()
    {
        using var clock = new MotionTestClockScope();
        var (window, toolbar, _) = Show(FloatingToolbarExitDirection.Bottom);
        using var touch = new TouchDevice();
        try
        {
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 150);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 70); Render(window);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, 70); Render(window);
            Frames(clock, window, 0, 64);
            Send(window, touch, 200, RawPointerEventType.TouchBegin, 100);
            Send(window, touch, 216, RawPointerEventType.TouchUpdate, 140); Render(window);
            var interrupted = toolbar.ScrollBehavior!.State.Offset;
            Frames(clock, window, 80, 800);
            Assert.Equal(interrupted, toolbar.ScrollBehavior.State.Offset);
            Send(window, touch, 1000, RawPointerEventType.TouchCancel, 140); Render(window);
            Frames(clock, window, 816, 1808);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static (MotionTestWindow Window, FloatingToolbar Toolbar, ScrollViewer Viewer) Show(FloatingToolbarExitDirection direction)
    {
        var viewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, IsScrollInertiaEnabled = true,
            Content = new Border { Height = 1200, Background = Brushes.LightBlue } };
        FloatingToolbar toolbar = direction is FloatingToolbarExitDirection.Start or FloatingToolbarExitDirection.End
            ? new VerticalFloatingToolbar() : new HorizontalFloatingToolbar();
        toolbar.Content = new Border { Width = 48, Height = 48 };
        toolbar.Margin = new Thickness(16);
        toolbar.HorizontalAlignment = direction == FloatingToolbarExitDirection.Start ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        toolbar.VerticalAlignment = direction == FloatingToolbarExitDirection.Top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        toolbar.ScrollBehavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(direction);
        toolbar.ScrollSource = viewer;
        var window = new MotionTestWindow { Width = 280, Height = 320 };
        window.Styles.Add(new MaterialTheme()); window.Content = new Grid { Children = { viewer, toolbar } }; window.Show(); Render(window);
        return (window, toolbar, viewer);
    }
    private static void Send(Window window, TouchDevice device, ulong time, RawPointerEventType type, double y) =>
        window.PlatformImpl!.Input!(new RawTouchEventArgs(device, time,
            (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!,
            type, new Point(120, y), RawInputModifiers.None, 1));
    private static void Render(Window window)
    {
        for (var turn = 0; turn < 4; turn++)
        { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); using var frame = window.CaptureRenderedFrame(); }
        Dispatcher.UIThread.RunJobs();
    }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    { for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Render(window); } }
}
