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
using ComposeAnimation.TestSupport;
using Avalonia.Markup.Xaml;
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
        // BasicTooltipBox's outer Box excludes the WrappedAnchor modifier and uses
        // the physical layout origin even when the anchor content is mirrored.
        var anchorOrigin = TooltipAnchor.BoundsInWindow(box, window).Position;
        var first = tooltip.TranslatePoint(default, window)!.Value;
        var last = tooltip.TranslatePoint(new Point(tooltip.Bounds.Width, 0), window)!.Value;
        var tooltipOrigin = new Point(Math.Min(first.X, last.X), first.Y);
        Assert.Equal(new Point(anchorOrigin.X - 28, anchorOrigin.Y - 28), tooltipOrigin);
        Assert.Equal(new Size(24, 24), box.Bounds.Size);
    }

    [AvaloniaFact]
    public void TransformedAnchor_UsesUnmodifiedOuterBoxLikeCorePopup()
    {
        using var clock = new MotionTestClockScope();
        var tooltip = new PlainTooltip { Content = new Border { Width = 64, Height = 8 } };
        var box = Box(tooltip, new(initialIsVisible: true, mutatorMutex: new()));
        box.RenderTransform = new ScaleTransform(2, 2);
        var window = DialogTests.Show(new Grid { Children = { box } });
        var anchor = box.GetVisualParent()!.TranslatePoint(box.Bounds.Position, window)!.Value;
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
        Assert.True(show.IsCompletedSuccessfully);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
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

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DisablingInputCancelsHover_WithoutCancellingApplicationRequest(bool persistent, bool applicationRequest)
    {
        using var clock = new MotionTestClockScope();
        using var state = new TooltipState(isPersistent: persistent, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = new Border { Width = 40, Height = 8 } }, state);
        var window = DialogTests.Show(new Grid { Children = { box } });
        try
        {
            var center = box.TranslatePoint(new Point(12, 12), window)!.Value;
            window.MouseMove(center); Flush(window); Assert.True(state.Transition.TargetState);
            var application = applicationRequest ? state.ShowAsync(MutatePriority.UserInput) : null;
            Flush(window);
            box.EnableUserInput = false; Flush(window);
            Assert.Equal(applicationRequest, state.Transition.TargetState);
            if (application is not null)
            {
                Assert.False(application.IsCompleted);
                state.OnDispose(); await application;
        Assert.True(application.IsCompletedSuccessfully);
            }
            box.EnableUserInput = true;
            window.MouseMove(new Point(500, 280)); window.MouseMove(center); Flush(window);
            Assert.True(state.Transition.TargetState);
            box.EnableUserInput = false; Flush(window); Assert.False(state.Transition.TargetState);
        }
        finally { window.Close(); Advance(clock, window, 1); }
        Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
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
        Assert.True(show.IsCompletedSuccessfully); Assert.False(old.Transition.TargetState);
        var oldTooltip = (PlainTooltip)box.Tooltip!;
        var next = new PlainTooltip { Content = "new" }; box.Tooltip = next; Flush(window);
        Assert.Null(oldTooltip.Parent); Assert.Same(box, next.Parent);
        window.Content = null; Flush(window); window.Content = root; Flush(window);
        Assert.True(box.IsPresented); Assert.Equal("new", next.Content);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
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
        await Task.Delay(box.LongPressDuration + TimeSpan.FromMilliseconds(40), TestContext.Current.CancellationToken);
        Flush(window); Assert.True(state.Transition.TargetState);
        var up = new PointerReleasedEventArgs(anchor, pointer, window, point, 1000,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left);
        anchor.RaiseEvent(up); Assert.True(up.Handled);
        // The baseline awaits show() before observing release: a short long-press
        // remains visible until the 1500ms show timeout, even after the pointer is up.
        Assert.True(state.Transition.TargetState);
        await Task.Delay(TooltipDefaults.TooltipDuration + TimeSpan.FromMilliseconds(40), TestContext.Current.CancellationToken);
        Flush(window); Assert.False(state.Transition.TargetState);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
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
        await Task.Delay(box.LongPressDuration + TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        Assert.False(box.State.IsVisible); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(PointerType.Touch, "hold")]
    [InlineData(PointerType.Pen, "hold")]
    [InlineData(PointerType.Touch, "custom-duration")]
    [InlineData(PointerType.Touch, "change-duration")]
    [InlineData(PointerType.Touch, "release")]
    [InlineData(PointerType.Touch, "disable")]
    [InlineData(PointerType.Touch, "detach")]
    [InlineData(PointerType.Touch, "state")]
    [InlineData(PointerType.Touch, "capture")]
    public void LongPressUsesScopeTime_AndCancelsPendingGesture(PointerType type, string operation)
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        var state = new TooltipState(isPersistent: true, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = new Border { Width = 40, Height = 8 } }, state);
        var root = new Grid { Children = { box } };
        var window = new AnimationTestWindow(frames, time) { Width = 240, Height = 160, Content = root };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.AnimationRoot.DurationScale = 0; // Ordinary hold time must still elapse.
        using var pointer = new Pointer(Pointer.GetNextFreeId(), type, true);
        try
        {
            window.Show(); Flush(window);
            var anchor = (Control)box.Content!;
            var point = anchor.TranslatePoint(new Point(12, 12), window)!.Value;
            if (operation == "custom-duration") box.LongPressDuration = TimeSpan.FromMilliseconds(750);
            anchor.RaiseEvent(new PointerPressedEventArgs(anchor, pointer, window, point, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            var delay = box.LongPressDuration;
            if (operation != "custom-duration") Assert.Equal(TimeSpan.FromMilliseconds(500), delay);
            time.AdvanceBy(delay - TimeSpan.FromMilliseconds(1)); Flush(window);
            Assert.False(state.Transition.TargetState);
            var up = new PointerReleasedEventArgs(anchor, pointer, window, point, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left);
            switch (operation)
            {
                case "change-duration": box.LongPressDuration = TimeSpan.FromSeconds(5); break;
                case "release": anchor.RaiseEvent(up); Assert.False(up.Handled); break;
                case "disable": box.EnableUserInput = false; break;
                case "detach": root.Children.Remove(box); break;
                case "state": box.State = new(isPersistent: true, mutatorMutex: new()); break;
                case "capture": pointer.Capture(anchor); pointer.Capture(null); break;
            }
            Flush(window); time.AdvanceBy(TimeSpan.FromMilliseconds(1)); Flush(window);
            Assert.Equal(operation is "hold" or "custom-duration" or "change-duration", state.Transition.TargetState);
            if (operation is "hold" or "custom-duration" or "change-duration") { anchor.RaiseEvent(up); Assert.True(up.Handled); }
            else { time.AdvanceBy(delay); Flush(window); Assert.False(box.State.Transition.TargetState); }
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); state.Dispose(); }
        Assert.Equal(0, time.ActiveTimerCount); Assert.Equal(0, frames.PendingFrameCount);
        Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(MutatePriority.Default, false)]
    [InlineData(MutatePriority.PreventUserInput, false)]
    [InlineData(MutatePriority.UserInput, false)]
    [InlineData(MutatePriority.Default, true)]
    public async Task MountedStateTimeoutUsesScopeTime_AndPreservesPriorityCleanup(MutatePriority priority, bool persistent)
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        using var state = new TooltipState(isPersistent: persistent, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = new Border { Width = 40, Height = 8 } }, state);
        var window = new AnimationTestWindow(frames, time) { Width = 240, Height = 160, Content = box };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.AnimationRoot.DurationScale = 0;
        try
        {
            window.Show(); Flush(window);
            var show = state.ShowAsync(priority, TestContext.Current.CancellationToken);
            time.AdvanceBy(TimeSpan.FromMilliseconds(1499)); Flush(window);
            Assert.False(show.IsCompleted); Assert.True(state.Transition.TargetState);
            time.AdvanceBy(TimeSpan.FromMilliseconds(1)); Flush(window);
            var timed = !persistent && priority != MutatePriority.UserInput;
            if (timed)
            {
                Assert.True(show.IsCompletedSuccessfully);
                await show;
        Assert.True(show.IsCompletedSuccessfully);
                Assert.Equal(priority == MutatePriority.PreventUserInput, state.Transition.TargetState);
            }
            else
            {
                Assert.False(show.IsCompleted);
                window.Content = null; Flush(window);
                await show;
        Assert.True(show.IsCompletedSuccessfully);
            }
            Assert.Equal(0, time.ActiveTimerCount);
            if (timed)
            {
                // A second request must not retain the old timer or fail to acquire the mutex.
                var next = state.ShowAsync(MutatePriority.Default, TestContext.Current.CancellationToken);
                time.AdvanceBy(TimeSpan.FromMilliseconds(500)); Flush(window);
                box.State = new(isPersistent: true, mutatorMutex: new()); Flush(window);
                await next;
        Assert.True(next.IsCompletedSuccessfully);
                Assert.Equal(0, time.ActiveTimerCount);
            }
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
        Assert.Null(state.DelayScope);
        Assert.Equal(0, frames.PendingFrameCount); Assert.Equal(0, time.ActiveTimerCount);
        Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongPressDismissalWaitsForBothTimeoutAndRelease(bool releaseBeforeTimeout)
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        using var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = new Border { Width = 40, Height = 8 } }, state);
        var window = new AnimationTestWindow(frames, time) { Width = 240, Height = 160, Content = box };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        try
        {
            window.Show(); Flush(window);
            var anchor = (Control)box.Content!;
            var point = anchor.TranslatePoint(new Point(12, 12), window)!.Value;
            anchor.RaiseEvent(new PointerPressedEventArgs(anchor, pointer, window, point, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            time.AdvanceBy(TimeSpan.FromMilliseconds(500)); Flush(window);
            Assert.True(state.Transition.TargetState);
            var up = new PointerReleasedEventArgs(anchor, pointer, window, point, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left);
            if (releaseBeforeTimeout) { anchor.RaiseEvent(up); Assert.True(up.Handled); }
            time.AdvanceBy(TimeSpan.FromMilliseconds(1499)); Flush(window);
            Assert.True(state.Transition.TargetState);
            time.AdvanceBy(TimeSpan.FromMilliseconds(1)); Flush(window);
            Assert.Equal(!releaseBeforeTimeout, state.Transition.TargetState);
            if (!releaseBeforeTimeout) { anchor.RaiseEvent(up); Flush(window); Assert.True(up.Handled); }
            Assert.False(state.Transition.TargetState);
            Assert.Equal(0, time.ActiveTimerCount);
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
        Assert.Equal(0, frames.PendingFrameCount); Assert.Equal(0, time.ActiveTimerCount);
        Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisablingLongPressReleasesPriority_WithoutInventingDismissal(bool persistent)
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        using var state = new TooltipState(isPersistent: persistent, mutatorMutex: new());
        var box = Box(new PlainTooltip { Content = new Border { Width = 40, Height = 8 } }, state);
        var window = new AnimationTestWindow(frames, time) { Width = 240, Height = 160, Content = box };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        try
        {
            window.Show(); Flush(window);
            var anchor = (Control)box.Content!;
            var point = anchor.TranslatePoint(new Point(12, 12), window)!.Value;
            anchor.RaiseEvent(new PointerPressedEventArgs(anchor, pointer, window, point, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            time.AdvanceBy(TimeSpan.FromMilliseconds(500)); Flush(window);
            Assert.True(state.Transition.TargetState);
            box.EnableUserInput = false; Flush(window);
            Assert.True(state.Transition.TargetState); // Core cancels the collector, not visibility.
            Assert.Equal(0, time.ActiveTimerCount);
            var application = state.ShowAsync(MutatePriority.Default); Flush(window);
            Assert.False(application.IsCompleted); // Old PreventUserInput must no longer reject it.
            state.OnDispose(); await application;
        Assert.True(application.IsCompletedSuccessfully);
            Assert.False(state.Transition.TargetState);
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
        Assert.Equal(0, time.ActiveTimerCount); Assert.Equal(0, frames.PendingFrameCount);
        Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    static TooltipBox Box(PlainTooltip tooltip, TooltipState state) => new() { Content = new Border { Width = 24, Height = 24 }, Tooltip = tooltip, State = state,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
