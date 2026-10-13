using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ModalWideNavigationRailTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpeningContent_PreservesMotionOnlyForRetainedRail(bool hide)
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(hide); var item = (WideNavigationRailItem)rail.Items[0]!;
        var window = Show(rail);
        try
        {
            for (var opening = 0; opening < 2; opening++)
            {
                // Changing selection while hidden must also initialize the next content instance,
                // rather than carry a selection animation from an invisible collapsed presenter.
                if (opening == 1) item.IsSelected = true;
                _ = rail.State.ExpandAsync();
                for (var frame = 0; !rail.IsModalVisible && frame < 12; frame++) Advance(clock, window, 1);
                Assert.True(rail.IsModalVisible);
                var layout = Part<WideNavigationRailItemLayout>(item, "PART_Layout");
                if (hide)
                {
                    Assert.Equal(1f, layout.IconPositionProgress);
                    Assert.Equal(opening == 1 ? 1f : 0f, layout.SelectionProgress);
                    if (opening == 1) Assert.Equal(-220f, rail.ModalOffset);
                }
                else Assert.InRange(layout.IconPositionProgress, 0f, .99f);

                Advance(clock, window);
                Assert.Equal(1f, layout.IconPositionProgress);
                _ = rail.State.CollapseAsync(); Advance(clock, window);
                Assert.False(rail.IsModalVisible);
            }
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HiddenContent_NewStateDoesNotReuseCollapsedAnchors()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true); var window = Show(rail);
        try
        {
            _ = rail.State.ExpandAsync(); Advance(clock, window);
            _ = rail.State.CollapseAsync(); Advance(clock, window);
            Assert.False(rail.IsModalVisible);
            Assert.Equal(-220f, rail.ModalOffset);
            rail.State = new(WideNavigationRailValue.Expanded);
            for (var frame = 0; !rail.IsModalVisible && frame < 12; frame++) Advance(clock, window, 1);
            Assert.True(rail.IsModalVisible);
            Assert.Equal(0f, rail.ModalOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false, 96, false)]
    [InlineData(true, false, 0, false)]
    [InlineData(false, true, 96, true)]
    [InlineData(true, true, 0, true)]
    public void InitialState_UsesInlineReservationAndFullWindowOverlay(bool hide, bool expanded, double width, bool modal)
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(hide, expanded); var item = (WideNavigationRailItem)rail.Items[0]!;
        var window = Show(rail);
        try
        {
            Assert.Equal(width, rail.DesiredSize.Width);
            Assert.Equal(modal, rail.IsModalVisible);
            Assert.Equal(expanded, item.RailExpanded);
            if (modal)
            {
                Assert.Equal(new Size(520, 240), Part<Panel>(window, "PART_Overlay").Bounds.Size);
                Assert.Equal(new Size(220, 240), Part<Control>(window, "PART_ModalSurface").Bounds.Size);
                Assert.Equal(44, item.TranslatePoint(default, window)!.Value.Y);
            }
            else if (!hide) Assert.Equal(44, item.TranslatePoint(default, rail)!.Value.Y);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExpansionTransfer_PreservesItemsAndSelection_ThenRestoresFocus(bool hide)
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(hide); var item = (WideNavigationRailItem)rail.Items[0]!; item.IsSelected = true;
        var button = new Button { Content = "Page", HorizontalAlignment = HorizontalAlignment.Right };
        var window = Show(rail, button);
        try
        {
            button.Focus();
            _ = rail.State.ExpandAsync(); Advance(clock, window);
            Assert.True(rail.IsModalVisible); Assert.Equal(220, Part<Control>(window, "PART_ModalSurface").Bounds.Width);
            Assert.Same(item, rail.NavigationItems.Single()); Assert.True(item.IsSelected); Assert.Equal(56, item.Bounds.Height);
            var selection = Assert.IsAssignableFrom<ISelectionProvider>(ControlAutomationPeer.CreatePeerForElement(rail));
            Assert.Single(selection.GetSelection());
            button.Focus(); Assert.False(button.IsFocused);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t"); Assert.True(item.IsFocused);
            _ = rail.State.CollapseAsync(); Advance(clock, window);
            Assert.False(rail.IsModalVisible); Assert.True(button.IsFocused);
            Assert.Same(item, rail.NavigationItems.Single()); Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExpandedHeader_UsesSeparateControlAndPadding_WithoutReparentingInlineHeader()
    {
        using var clock = new MotionTestClockScope();
        var collapsed = new Border { Width = 48, Height = 40 }; var expanded = new Border { Width = 64, Height = 40 };
        var rail = Rail(false, true); rail.Header = collapsed; rail.ExpandedHeader = expanded; rail.ExpandedHeaderTopPadding = 8;
        var window = Show(rail);
        try
        {
            Assert.True(collapsed.IsAttachedToVisualTree()); Assert.True(expanded.IsAttachedToVisualTree());
            Assert.Equal(52, expanded.TranslatePoint(default, window)!.Value.Y);
            Assert.Equal(132, rail.NavigationItems.Single().TranslatePoint(default, window)!.Value.Y);
            Assert.NotSame(collapsed.Parent, expanded.Parent);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void HeaderTemplate_InstantiatesBothSlotsAndRetainsOwnerDataContext()
    {
        using var clock = new MotionTestClockScope();
        var created = 0;
        var rail = Rail(false, true); var data = new object(); rail.DataContext = data; rail.Header = "Menu";
        rail.HeaderTemplate = new FuncDataTemplate<string>((_, _) => { created++; return new Border { Width = 48, Height = 40 }; });
        var window = Show(rail);
        try
        {
            Assert.Equal(2, created);
            Assert.Same(data, Part<Panel>(window, "PART_Overlay").DataContext);
            Assert.Equal(Color.Parse("#FFF2ECF5"), ((ISolidColorBrush)rail.ModalBackground!).Color);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void BackSetting_DoesNotDisableCommonEscapeKeyUp()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); rail.ShouldDismissOnBackPress = false;
        var window = Show(rail);
        try
        {
            window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
            Advance(clock, window);
            Assert.False(rail.IsModalVisible); Assert.Equal(WideNavigationRailValue.Collapsed, rail.State.TargetValue);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void NativeBack_DismissesButEscapeWaitsForKeyUp()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); var window = Show(rail);
        try
        {
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            var back = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            window.RaiseEvent(back); Assert.True(back.Handled);
            Advance(clock, window); Assert.False(rail.IsModalVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ScrimAutomationInvoke_ReopensAndReleasesOverlayAfterDetach()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); var window = Show(rail);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(Part<Border>(window, "PART_Scrim"))!;
            Assert.Equal("Close rail", peer.GetName());
            Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke(); Advance(clock, window);
            Assert.False(rail.IsModalVisible);
            _ = rail.State.ExpandAsync(); Advance(clock, window); Assert.True(rail.IsModalVisible);
            var root = window.Content;
            window.Content = null; Advance(clock, window, 2);
            Assert.False(rail.IsModalVisible);
            window.Content = root;
            window.UpdateLayout(); Advance(clock, window, 2);
            Assert.True(rail.IsModalVisible); Assert.Single(rail.NavigationItems);
            Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(Part<Border>(window, "PART_Scrim"))).Invoke();
            Advance(clock, window); Assert.False(rail.IsModalVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scrim_BlocksPageClickAndDismissesThroughCorrectPath(bool hide)
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(hide, true); var clicks = 0;
        var button = new Button { HorizontalAlignment = HorizontalAlignment.Right, Width = 80 }; button.Click += (_, _) => clicks++;
        var window = Show(rail, button);
        try
        {
            var point = new Point(480, 120); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            if (hide) Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            else Assert.Equal(WideNavigationRailValue.Collapsed, rail.State.TargetValue);
            Advance(clock, window); Assert.Null(window.MotionRoot.Diagnostics.LastError);
            Assert.False(rail.IsModalVisible, $"target={rail.State.TargetValue}, position={rail.PositionProgress}, offset={rail.ModalOffset}, alpha={rail.ScrimAlpha}"); Assert.Equal(0, clicks);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DetachDuringDismiss_CancelsAndReleasesOverlay()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); var window = Show(rail);
        try
        {
            rail.DismissFromScrim(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.Content = null;
            Assert.False(rail.IsModalVisible); Advance(clock, window);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [Fact]
    public void DesktopVelocity_UsesLeastSquaresAndStoppedPointerWindow()
    {
        var tracker = new Material3Expressive.Foundation.Input.VelocityTracker(); tracker.Add(0, 100); tracker.Add(16, 84); tracker.Add(32, 68);
        Assert.InRange(tracker.Calculate(32), -1000.1, -999.9);
        Assert.Equal(0, tracker.Calculate(73));
        tracker.Reset(); tracker.Add(0, 100); tracker.Add(16, 116);
        Assert.InRange(tracker.Calculate(16), 999.9, 1000.1);
    }
    [AvaloniaTheory]
    [InlineData(false, 160, false, true)]
    [InlineData(true, 160, false, true)]
    [InlineData(false, 60, false, false)]
    [InlineData(false, 60, true, true)]
    public void DragClose_UsesDistanceVelocityAndLogicalRtlDirection(bool rtl, double distance, bool fling, bool closes)
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); rail.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = Show(rail);
        try
        {
            Advance(clock, window, 2);
            var device = (IMouseDevice)window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(window.PlatformImpl)!;
            var input = (Action<RawInputEventArgs>)window.PlatformImpl.GetType().GetProperty("Input")!.GetValue(window.PlatformImpl)!;
            var origin = new Point(rtl ? 410 : 110, 220); var sign = rtl ? -1 : 1;
            var root = window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl);
            void Send(ulong time, RawPointerEventType type, Point point) => input((RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), device, time, root, type, point,
                type == RawPointerEventType.Move ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!);
            Send(100, RawPointerEventType.LeftButtonDown, origin);
            Send(116, RawPointerEventType.Move, origin + new Point(-20 * sign, 0));
            Send(132, RawPointerEventType.Move, origin + new Point(-distance * sign, 0));
            Assert.InRange(rail.ModalOffset, -(float)distance, -(float)distance + 1);
            Send(fling ? 133ul : 180ul, RawPointerEventType.LeftButtonUp, origin + new Point(-distance * sign, 0));
            Advance(clock, window);
            Assert.Equal(!closes, rail.IsModalVisible); Assert.Equal(closes ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded, rail.State.TargetValue);
            // The new anchored mutation cancels the fling's awaited outer animation.
            // Fixed Compose retains its raw value at 1 even after setting target 0.
            if (closes) Assert.Equal(WideNavigationRailValue.Expanded, rail.State.CurrentValue);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DragFromItem_CancelCaptureSettlesWithoutInvokingNavigation()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(true, true); var item = (WideNavigationRailItem)rail.Items[0]!;
        var clicks = 0; item.Click += (_, _) => clicks++;
        var window = Show(rail);
        try
        {
            Advance(clock, window, 2);
            var device = (IMouseDevice)window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(window.PlatformImpl)!;
            var input = (Action<RawInputEventArgs>)window.PlatformImpl.GetType().GetProperty("Input")!.GetValue(window.PlatformImpl)!;
            var root = window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl);
            var origin = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
            void Send(ulong time, RawPointerEventType type, Point point) => input((RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), device, time, root, type, point,
                type == RawPointerEventType.Move ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!);
            Send(100, RawPointerEventType.LeftButtonDown, origin);
            Send(116, RawPointerEventType.Move, origin - new Point(160, 0));
            Assert.True(rail.ModalOffset < -150);
            Send(132, RawPointerEventType.CancelCapture, origin - new Point(160, 0));
            Advance(clock, window);
            Assert.Equal(0, clicks); Assert.False(item.IsPressed); Assert.False(rail.IsModalVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    private static ModalWideNavigationRail Rail(bool hide = false, bool expanded = false) => new()
    {
        HideOnCollapse = hide, State = new(expanded ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed),
        Items = { new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 }, VerticalAlignment = VerticalAlignment.Top } }
    };
    private static MotionTestWindow Show(Control rail, Control? page = null)
    {
        var root = new Grid(); if (page is not null) root.Children.Add(page); root.Children.Add(rail);
        return DialogTests.Show(root, 240);
    }
    private static void Advance(MotionTestClockScope clock, Window window, int frames = 80)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
    }
    private static T Part<T>(Control root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
