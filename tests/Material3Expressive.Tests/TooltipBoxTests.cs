using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipBoxTests
{
    [AvaloniaTheory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void RtlAnchor_UsesPhysicalOriginWithLocalSize(bool parentRtl, double scale)
    {
        using var clock = new MotionTestClockScope();
        var tooltip = new PlainTooltip { Content = new Border { Width = 64, Height = 8 } };
        var box = Box(tooltip, new(initialIsVisible: true, mutatorMutex: new()));
        box.FlowDirection = FlowDirection.RightToLeft;
        box.RenderTransform = new ScaleTransform(scale, scale);
        var window = DialogTests.Show(new Grid
        {
            FlowDirection = parentRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children = { box }
        });
        // Avalonia mirrors RTL coordinates; Core's positionInWindow starts at
        // the physical left of the layout node, including its explicit scale.
        var anchorOrigin = box.TranslatePoint(new Point(box.Bounds.Width, 0), window)!.Value;
        var first = tooltip.TranslatePoint(default, window)!.Value;
        var last = tooltip.TranslatePoint(new Point(tooltip.Bounds.Width, 0), window)!.Value;
        var tooltipOrigin = new Point(Math.Min(first.X, last.X), first.Y);
        Assert.Equal(new Point(anchorOrigin.X - 28, anchorOrigin.Y - 28), tooltipOrigin);
        Assert.Equal(new Size(24, 24), box.Bounds.Size);
    }

    [AvaloniaFact]
    public void TransformedAnchor_UsesWindowOriginAndLocalSizeLikeCorePopup()
    {
        using var clock = new MotionTestClockScope();
        var tooltip = new PlainTooltip { Content = new Border { Width = 64, Height = 8 } };
        var box = Box(tooltip, new(initialIsVisible: true, mutatorMutex: new()));
        box.RenderTransform = new ScaleTransform(2, 2);
        var window = DialogTests.Show(new Grid { Children = { box } });
        var anchor = box.TranslatePoint(default, window)!.Value;
        Assert.Equal(new Point(anchor.X - 28, anchor.Y - 28), tooltip.TranslatePoint(default, window));
        Assert.Equal(new Size(24, 24), box.Bounds.Size);
    }

    [AvaloniaFact]
    public void AnchorMeasuresAlone_AndInitialPopupUsesWindowPixelPosition()
    {
        using var clock = new MotionTestClockScope();
        var tooltip = new PlainTooltip { Content = new Border { Width = 64, Height = 8 } };
        var box = Box(tooltip, new(initialIsVisible: true, mutatorMutex: new()));
        var window = DialogTests.Show(new Grid { Children = { box } });
        Assert.Equal(new Size(24, 24), box.Bounds.Size);
        Assert.Equal(new Size(80, 24), tooltip.Bounds.Size);
        var anchor = box.TranslatePoint(default, window)!.Value;
        var origin = tooltip.TranslatePoint(default, window)!.Value;
        Assert.Equal(new Point(anchor.X - 28, anchor.Y - 28), origin);
        Assert.True(box.IsPresented);
        Assert.False(tooltip.GetVisualAncestors().OfType<OverlayPopupHost>().Single().ClipToBounds);
    }

    [AvaloniaFact]
    public void DismissRetainsPopupUntilExit_AndDetachCancelsFrameWaiters()
    {
        using var clock = new MotionTestClockScope();
        var state = new TooltipState(initialIsVisible: true, isPersistent: false, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = "Tooltip" }, state);
        var root = new Grid { Children = { box } };
        var window = DialogTests.Show(root);
        state.Dismiss(); Flush(window);
        Assert.True(box.IsPresented); Assert.True(state.IsVisible);
        Advance(clock, window, 50);
        Assert.False(box.IsPresented); Assert.False(state.IsVisible);
        var show = state.ShowAsync(MutatePriority.UserInput); Flush(window);
        Assert.True(box.IsPresented);
        window.Content = null; Flush(window);
        Assert.False(box.IsPresented); Assert.Equal(0, clock.PendingFrameCount);
        Assert.True(show.IsCanceled);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void HoverExit_UsesPersistence_AndDisabledInputDoesNotShow(bool persistent)
    {
        using var clock = new MotionTestClockScope();
        var state = new TooltipState(isPersistent: persistent, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = "Tooltip" }, state);
        var window = DialogTests.Show(new Grid { Children = { box } });
        var center = box.TranslatePoint(new Point(12, 12), window)!.Value;
        window.MouseMove(center); Flush(window); Assert.True(state.Transition.TargetState);
        window.MouseMove(new Point(500, 280)); Flush(window); Assert.Equal(persistent, state.Transition.TargetState);
        state.Dismiss(); box.EnableUserInput = false;
        window.MouseMove(center); Flush(window); Assert.False(state.Transition.TargetState);
    }

    [AvaloniaFact]
    public void FocusEscapeAndHasActionTab_PreserveCoreDesktopBehavior()
    {
        using var clock = new MotionTestClockScope();
        var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        var anchor = new Button { Content = "Anchor", Width = 80, Height = 40 };
        var box = Box(new PlainTooltip { Content = "Tooltip" }, state); box.Content = anchor; box.HasAction = true;
        var window = DialogTests.Show(new Grid { Children = { box } });
        anchor.Focus(); Flush(window); Assert.True(state.Transition.TargetState);
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "Tab"); Flush(window);
        Assert.Same(anchor, window.FocusManager!.GetFocusedElement());
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Flush(window);
        Assert.False(state.Transition.TargetState); Assert.Same(anchor, window.FocusManager.GetFocusedElement());
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutsidePress_PassesThrough_AndCallbackOwnsDismissal(bool callback)
    {
        using var clock = new MotionTestClockScope();
        var state = new TooltipState(initialIsVisible: true, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = "Tooltip" }, state); box.EnableUserInput = false;
        int requests = 0, presses = 0;
        if (callback) box.DismissRequested += (_, _) => requests++;
        var root = new Grid { Background = Avalonia.Media.Brushes.White, Children = { box } };
        root.PointerPressed += (_, _) => presses++;
        var window = DialogTests.Show(root);
        window.MouseDown(new Point(10, 10), MouseButton.Left); Flush(window);
        Assert.Equal(1, presses); Assert.Equal(callback ? 1 : 0, requests);
        Assert.Equal(callback, state.Transition.TargetState);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
    }

    [AvaloniaFact]
    public void StateReplacementAndReattach_CleanOldOperationAndKeepLogicalTooltip()
    {
        using var clock = new MotionTestClockScope();
        var old = new TooltipState(mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = "old" }, old);
        var root = new Grid { Children = { box } }; var window = DialogTests.Show(root);
        var show = old.ShowAsync(); Flush(window);
        box.State = new(initialIsVisible: true, mutatorMutex: new()); Flush(window);
        Assert.True(show.IsCanceled); Assert.False(old.Transition.TargetState);
        var oldTooltip = (PlainTooltip)box.Tooltip!;
        var next = new PlainTooltip { Content = "new" }; box.Tooltip = next; Flush(window);
        Assert.Null(oldTooltip.Parent); Assert.Same(box, next.Parent);
        window.Content = null; Flush(window); window.Content = root; Flush(window);
        Assert.True(box.IsPresented); Assert.Equal("new", next.Content);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void Automation_ExposesTooltipLiveRegionAndAnchorShowAction()
    {
        using var clock = new MotionTestClockScope();
        var box = Box(new PlainTooltip { Content = "Tooltip" }, new(mutatorMutex: new()));
        var window = DialogTests.Show(new Grid { Children = { box } });
        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        ((IInvokeProvider)peer).Invoke(); Flush(window);
        Assert.True(box.State.Transition.TargetState);
        var presenter = box.Tooltip is Control tooltip ? tooltip.GetVisualAncestors().OfType<TooltipPresenter>().Single() : throw new Exception();
        var popupPeer = ControlAutomationPeer.CreatePeerForElement(presenter);
        Assert.Equal(AutomationControlType.ToolTip, popupPeer.GetAutomationControlType());
        Assert.Equal(AutomationLiveSetting.Assertive, popupPeer.GetLiveSetting());
    }

    [AvaloniaFact]
    public void TooltipTemplate_UpdatesDataAndInheritsTheDeclarationResources()
    {
        using var clock = new MotionTestClockScope();
        var box = new TooltipBox { Content = new Border { Width = 24, Height = 24 }, Tooltip = "first",
            State = new(initialIsVisible: true, mutatorMutex: new()),
            TooltipTemplate = new FuncDataTemplate<string>((value, _) => new PlainTooltip { Content = value }) };
        var window = DialogTests.Show(new Grid { Children = { box } });
        var tooltip = window.GetVisualDescendants().OfType<PlainTooltip>().Single();
        Assert.Equal("first", tooltip.Content);
        box.Tooltip = "second"; Flush(window);
        tooltip = window.GetVisualDescendants().OfType<PlainTooltip>().Single();
        Assert.Equal("second", tooltip.Content);
        box.Resources[Tokens.MaterialTokens.Sys.Color.InverseOnSurface] = Colors.Yellow;
        Assert.Equal(Colors.Yellow, ((ISolidColorBrush)tooltip.Foreground!).Color);
        Assert.True(box.IsPresented);
    }

    [AvaloniaTheory]
    [InlineData(PointerType.Touch)]
    [InlineData(PointerType.Pen)]
    public async Task LongPress_ConsumesRelease_AndDismissesAfterTimeoutAndRelease(PointerType type)
    {
        using var clock = new MotionTestClockScope();
        var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = "Tooltip" }, state);
        var window = DialogTests.Show(new Grid { Children = { box } });
        var anchor = (Control)box.Content!;
        var point = anchor.TranslatePoint(new Point(12, 12), window)!.Value;
        using var pointer = new Pointer(Pointer.GetNextFreeId(), type, true);
        var down = new PointerPressedEventArgs(anchor, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None);
        anchor.RaiseEvent(down);
        await Task.Delay((box.GetPlatformSettings()?.HoldWaitDuration ?? TimeSpan.FromMilliseconds(500)) + TimeSpan.FromMilliseconds(40), TestContext.Current.CancellationToken);
        Flush(window); Assert.True(state.Transition.TargetState);
        var up = new PointerReleasedEventArgs(anchor, pointer, window, point, 1000,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left);
        anchor.RaiseEvent(up); Assert.True(up.Handled);
        // The baseline awaits show() before observing release: a short long-press
        // remains visible until the 1500ms show timeout, even after the pointer is up.
        Assert.True(state.Transition.TargetState);
        await Task.Delay(TooltipDefaults.TooltipDuration + TimeSpan.FromMilliseconds(40), TestContext.Current.CancellationToken);
        Flush(window); Assert.False(state.Transition.TargetState);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public async Task ShortTouchPress_CancelsPendingLongPress()
    {
        using var clock = new MotionTestClockScope();
        var box = Box(new PlainTooltip { Content = "Tooltip" }, new(isPersistent: false, mutatorMutex: new()));
        var window = DialogTests.Show(new Grid { Children = { box } });
        var anchor = (Control)box.Content!; var point = anchor.TranslatePoint(new Point(12, 12), window)!.Value;
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        anchor.RaiseEvent(new PointerPressedEventArgs(anchor, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        var up = new PointerReleasedEventArgs(anchor, pointer, window, point, 1,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left);
        anchor.RaiseEvent(up); Assert.False(up.Handled);
        await Task.Delay((box.GetPlatformSettings()?.HoldWaitDuration ?? TimeSpan.FromMilliseconds(500)) + TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        Assert.False(box.State.IsVisible); Assert.Null(window.MotionRoot.LastError);
    }

    static TooltipBox Box(PlainTooltip tooltip, TooltipState state) => new() { Content = new Border { Width = 24, Height = 24 }, Tooltip = tooltip, State = state,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
