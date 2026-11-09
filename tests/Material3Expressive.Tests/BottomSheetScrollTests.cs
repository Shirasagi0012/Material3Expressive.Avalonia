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
using SmoothScroll.Avalonia.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class BottomSheetScrollTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void TouchExpandsBeforeBodyAndCollapsesAfterBodyReachesStart(double density)
    {
        using var clock = new MotionTestClockScope();
        var (window, sheet, viewer) = Show(density);
        using var touch = new TouchDevice();
        try
        {
            Assert.True(sheet.IsScrollConnected);
            Assert.Equal(240, sheet.State.CurrentOffset);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 350);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 280); Render(window);
            Assert.Equal(188, sheet.State.CurrentOffset);
            Assert.Equal(0, viewer.Offset.Y);
            Send(window, touch, 132, RawPointerEventType.TouchUpdate, 100); Render(window);
            Assert.Equal(72, sheet.State.CurrentOffset);
            Assert.Equal(64, viewer.Offset.Y);
            Send(window, touch, 148, RawPointerEventType.TouchUpdate, 200); Render(window);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.Equal(108, sheet.State.CurrentOffset);
            Send(window, touch, 164, RawPointerEventType.TouchCancel, 200); Render(window);
            Frames(clock, window);
            Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
            Assert.Equal(72, sheet.State.CurrentOffset);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ContentReceivesFiniteRemainingHeightWithoutAnExplicitViewportHeight()
    {
        using var clock = new MotionTestClockScope();
        var (window, sheet, viewer) = Show();
        try
        {
            viewer.ClearValue(Control.HeightProperty); Render(window);
            Assert.Equal(432, viewer.Bounds.Height);
            Assert.Equal(0, sheet.State.MinimumAnchor);
            Assert.True(viewer.Extent.Height > viewer.Viewport.Height);
            Assert.True(sheet.IsScrollConnected);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void ReleaseAfterAStationaryPauseSettlesToTheNearestAnchor(double density)
    {
        using var clock = new MotionTestClockScope();
        var (window, sheet, viewer) = Show(density);
        using var touch = new TouchDevice();
        try
        {
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 350);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 310); Render(window);
            Assert.Equal(218, sheet.State.CurrentOffset);
            Send(window, touch, 250, RawPointerEventType.TouchEnd, 310); Render(window);
            Frames(clock, window);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue);
            Assert.Equal(240, sheet.State.CurrentOffset);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DisabledGesturesKeepBodyScrollableAndManualHostCanOptOut()
    {
        using var clock = new MotionTestClockScope();
        var (window, sheet, viewer) = Show();
        using var touch = new TouchDevice();
        try
        {
            sheet.GesturesEnabled = false;
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 350);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 280); Render(window);
            Assert.Equal(240, sheet.State.CurrentOffset);
            Assert.Equal(52, viewer.Offset.Y);
            Send(window, touch, 132, RawPointerEventType.TouchCancel, 280); Render(window);
            var presenter = Assert.IsType<SmoothScroll.Avalonia.Controls.ScrollViewerPresenter>(viewer.Presenter);
            sheet.AutoConnectScroll = false; Render(window);
            Assert.False(sheet.IsScrollConnected);
            Assert.Null(presenter.VerticalTouchSlop);
            sheet.AutoConnectScroll = true; Render(window);
            Assert.True(sheet.IsScrollConnected);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AmbiguousSourcesAndStateReplacementReleaseOldConnection()
    {
        using var clock = new MotionTestClockScope();
        var (window, sheet, first) = Show();
        try
        {
            sheet.Content = null;
            var second = Viewer(); var grid = new Grid { Children = { first, second } };
            sheet.Content = grid; Render(window);
            Assert.False(sheet.IsScrollConnected);
            grid.Children.Remove(first); Render(window);
            Assert.True(sheet.IsScrollConnected);
            var old = sheet.State;
            sheet.State = new SheetState(SheetValue.Expanded); Render(window);
            Assert.True(sheet.IsScrollConnected);
            Assert.Equal(72, sheet.State.CurrentOffset);
            window.Content = null; Render(window);
            Assert.False(sheet.IsScrollConnected);
            window.Content = sheet; Render(window);
            Assert.True(sheet.IsScrollConnected);
            Assert.Equal(240, old.CurrentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SheetAndRefreshShareTheViewportInNestedOrder()
    {
        using var clock = new MotionTestClockScope();
        var viewer = Viewer();
        var refresh = new PullToRefreshBox { Content = viewer };
        var sheet = new BottomSheet { Content = refresh, State = new SheetState(SheetValue.PartiallyExpanded) };
        var window = new MotionTestWindow { Width = 240, Height = 480 };
        window.Styles.Add(new MaterialTheme()); window.Content = sheet; window.Show(); Render(window);
        using var touch = new TouchDevice();
        try
        {
            clock.SendFrame(TimeSpan.Zero); Render(window);
            Assert.True(sheet.IsScrollConnected); Assert.True(refresh.IsScrollConnected);
            Send(window, touch, 100, RawPointerEventType.TouchBegin, 350);
            Send(window, touch, 116, RawPointerEventType.TouchUpdate, 400); Render(window);
            Assert.Equal(240, sheet.State.CurrentOffset); Assert.Equal(0, viewer.Offset.Y);
            Assert.Equal(.2f, refresh.ActualState.DistanceFraction);
            // Outer sheet pre-scroll precedes the inner refresh's retraction.
            Send(window, touch, 132, RawPointerEventType.TouchUpdate, 300); Render(window);
            Assert.Equal(140, sheet.State.CurrentOffset); Assert.Equal(.2f, refresh.ActualState.DistanceFraction);
            Send(window, touch, 148, RawPointerEventType.TouchCancel, 300); Render(window);
            for (var ms = 16; ms <= 2400; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Render(window); }
            Assert.Equal(0, refresh.ActualState.DistanceFraction);
            Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScaffoldAndModalForwardAutomaticConnectionPolicy(bool modal)
    {
        using var clock = new MotionTestClockScope();
        var viewer = Viewer();
        var state = new SheetState(SheetValue.PartiallyExpanded);
        Control owner = modal ? new ModalBottomSheet { Content = viewer, State = state, IsOpen = true }
            : new BottomSheetScaffold { SheetContent = viewer, State = new BottomSheetScaffoldState(state, new SnackbarHostState()) };
        var window = new MotionTestWindow { Width = 240, Height = 480 };
        window.Styles.Add(new MaterialTheme()); window.Content = owner; window.Show(); Render(window);
        try
        {
            var sheet = modal ? ((ModalBottomSheet)owner).PresentedSheet! : owner.GetVisualDescendants().OfType<BottomSheet>().Single();
            Assert.True(sheet.IsScrollConnected);
            owner.SetValue(BottomSheet.AutoConnectScrollProperty, false); Render(window);
            Assert.False(sheet.IsScrollConnected);
            owner.SetValue(BottomSheet.AutoConnectScrollProperty, true); Render(window);
            Assert.True(sheet.IsScrollConnected);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [Fact]
    public void ParticipantOnlyConsumesUserDeltasAndKeepsFloatClampAcrossTransactions()
    {
        SheetScrollSnapshot last = default;
        var participant = new BottomSheetScrollParticipant(new(2, 144, 960, true), 480, 1, snapshot => last = snapshot);
        var up = new ScrollMovementContext(500, 0, 0, 0, 1000, 0, ScrollMovementSource.Direct);
        Assert.Equal(168, participant.PreScroll(in up));
        var down = up with { Delta = -600 };
        Assert.Equal(0, participant.PreScroll(in down));
        Assert.Equal(-408, participant.PostScroll(in down, 0));
        var inertia = up with { Source = ScrollMovementSource.Inertia };
        Assert.Equal(0, participant.PreScroll(in inertia));
        participant.OnScrollCompleted(default);
        Assert.Equal(960, last.Offset);
        participant.SetOffset(300, 2);
        participant.Configure(new(2, 144, 960, false));
        Assert.Equal(0, participant.PreScroll(in up));
        participant.OnScrollCompleted(default);
        Assert.Equal(2, last.Generation); Assert.Equal(300, last.Offset);
    }

    private static ScrollViewer Viewer() => new()
    {
        Height = 360, Content = new Border { Height = 1200, Background = Brushes.LightBlue },
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        IsScrollInertiaEnabled = true
    };
    private static (MotionTestWindow Window, BottomSheet Sheet, ScrollViewer Viewer) Show(double density = 1)
    {
        var viewer = Viewer();
        var sheet = new BottomSheet { State = new SheetState(SheetValue.PartiallyExpanded), Content = viewer };
        var window = new MotionTestWindow { Width = 240, Height = 480 };
        window.Styles.Add(new MaterialTheme()); window.Content = sheet; window.Show(); window.SetRenderScaling(density); Render(window);
        return (window, sheet, viewer);
    }
    private static void Send(Window window, TouchDevice device, ulong time, RawPointerEventType type, double y) =>
        window.PlatformImpl!.Input!(new RawTouchEventArgs(device, time,
            (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!,
            type, new Point(60, y), RawInputModifiers.None, 1));
    private static void Render(Window window)
    {
        for (var turn = 0; turn < 4; turn++) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); using (window.CaptureRenderedFrame()) { } }
        Dispatcher.UIThread.RunJobs();
    }
    private static void Frames(MotionTestClockScope clock, Window window)
    {
        for (var ms = 0; ms <= 1600; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Render(window); }
    }
}
