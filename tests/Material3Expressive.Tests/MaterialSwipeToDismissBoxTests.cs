// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// SwipeToDismissBox.kt and SwipeToDismissTest.kt. Pointer anchors, RTL, restoration, direction
// gating, and the 56dp / 125dp-per-second thresholds are source parity. Keyboard and Invoke are
// the explicit D003 Avalonia desktop adaptation.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialSwipeToDismissBoxTests
{
    [AvaloniaFact]
    public void StateIsPerInstanceNonNullAndResetCreatesAFreshSettledOwner()
    {
        var first = new MaterialSwipeToDismissBox();
        var second = new MaterialSwipeToDismissBox();

        Assert.NotSame(first.State, second.State);
        Assert.Equal(SwipeToDismissBoxValue.Settled, first.State.CurrentValue);
        Assert.Throws<InvalidOperationException>(() => first.State.RequireOffset());

        var original = first.State;
        first.ResetState();
        Assert.NotSame(original, first.State);
        Assert.Equal(SwipeToDismissBoxValue.Settled, first.State.SettledValue);

        first.State = null!;
        Assert.NotNull(first.State);
    }

    [AvaloniaFact]
    public void RestoredLogicalAnchorUsesMeasuredWidthAndReversesItsPhysicalOffsetInRtl()
    {
        var ltr = CreateBox(new SwipeToDismissBoxState(SwipeToDismissBoxValue.StartToEnd));
        var rtl = CreateBox(new SwipeToDismissBoxState(SwipeToDismissBoxValue.StartToEnd));
        rtl.FlowDirection = FlowDirection.RightToLeft;

        ThemeTestHelper.ShowWindow(new StackPanel { Children = { ltr, rtl } });

        Assert.Equal(240, ltr.State.RequireOffset(), precision: 6);
        Assert.Equal(240, ltr.ContentOffset, precision: 6);
        Assert.Equal(240, rtl.State.RequireOffset(), precision: 6);
        Assert.Equal(-240, rtl.ContentOffset, precision: 6);
        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, rtl.State.DismissDirection);
    }

    [AvaloniaFact]
    public void PositionAndVelocityThresholdsSelectAnchorsDeterministically()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);

        Assert.True(box.BeginDrag());
        Assert.Equal(55, box.DragBy(55), precision: 6);
        box.EndDrag();
        Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.TargetValue);
        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
        Assert.Equal(0, box.State.RequireOffset(), precision: 6);

        Assert.True(box.BeginDrag());
        box.DragBy(-10);
        box.EndDrag(-SwipeToDismissBoxDefaults.VelocityThreshold);
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, box.State.TargetValue);
        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, box.State.SettledValue);
        Assert.Equal(-240, box.State.RequireOffset(), precision: 6);
    }

    [AvaloniaFact]
    public void ProgrammaticResetInterruptsDismissMotionFromItsCurrentOffset()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);
        var dismissed = new List<SwipeToDismissBoxValue>();
        box.Dismissed += (_, e) => dismissed.Add(e.Direction);

        box.State.Dismiss(SwipeToDismissBoxValue.StartToEnd);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(40));
        var interruptedOffset = box.State.RequireOffset();
        Assert.InRange(interruptedOffset, 0.001, 239.999);

        box.State.Reset();
        Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.TargetValue);
        Assert.Equal(interruptedOffset, box.State.RequireOffset(), precision: 6);

        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
        Assert.Equal(0, box.State.RequireOffset(), precision: 6);
        Assert.Empty(dismissed);

        box.State.Dismiss(SwipeToDismissBoxValue.StartToEnd);
        clock.SettleAll();
        Assert.Equal([SwipeToDismissBoxValue.StartToEnd], dismissed);
    }

    [AvaloniaFact]
    public void StateReplacementCancelsTheOldOwnerAndSnapsToTheReplacementAnchor()
    {
        using var clock = new MotionTestClockScope();
        var oldState = new SwipeToDismissBoxState();
        var box = CreateBox(oldState);
        ThemeTestHelper.ShowWindow(box);

        oldState.Dismiss(SwipeToDismissBoxValue.StartToEnd);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(30));

        var replacement = new SwipeToDismissBoxState(SwipeToDismissBoxValue.EndToStart);
        box.State = replacement;
        Assert.Equal(-240, replacement.RequireOffset(), precision: 6);
        Assert.Equal(-240, box.ContentOffset, precision: 6);

        oldState.Reset();
        Assert.Same(replacement, box.State);
        Assert.Equal(-240, box.ContentOffset, precision: 6);
    }

    [AvaloniaFact]
    public void RemovingAnActiveAnchorForcesSettledAndDisabledDirectionsDoNotDrag()
    {
        var state = new SwipeToDismissBoxState(SwipeToDismissBoxValue.StartToEnd);
        var box = CreateBox(state);
        ThemeTestHelper.ShowWindow(box);

        box.EnableDismissFromStartToEnd = false;
        Assert.Equal(SwipeToDismissBoxValue.Settled, state.SettledValue);
        Assert.Equal(0, state.RequireOffset(), precision: 6);

        Assert.True(box.BeginDrag());
        Assert.Equal(0, box.DragBy(80), precision: 6);
        Assert.Equal(-40, box.DragBy(-40), precision: 6);
    }

    [AvaloniaFact]
    public void PointerPathUsesLogicalRtlDirection()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        box.FlowDirection = FlowDirection.RightToLeft;
        box.EnableDismissFromEndToStart = false;
        var window = ThemeTestHelper.ShowWindow(box);

        var start = box.TranslatePoint(new Point(180, 30), window)!.Value;
        window.MouseMove(start);
        window.MouseDown(start, MouseButton.Left);
        var end = box.TranslatePoint(new Point(90, 30), window)!.Value;
        window.MouseMove(end);
        Assert.Equal(90, box.State.RequireOffset(), precision: 6);
        window.MouseUp(end, MouseButton.Left);

        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.TargetValue);
        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.SettledValue);
        Assert.Equal(-240, box.ContentOffset, precision: 6);
    }

    [AvaloniaFact]
    public void DeleteShiftDeleteAndEscapeProvideDesktopKeyboardActions()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);

        RaiseKey(box, Key.Delete);
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, box.State.TargetValue);
        clock.SettleAll();

        RaiseKey(box, Key.Escape);
        Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.TargetValue);
        clock.SettleAll();

        RaiseKey(box, Key.Delete, KeyModifiers.Shift);
        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.TargetValue);
        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.SettledValue);
    }

    [AvaloniaFact]
    public void AutomationInvokeUsesTheCommonDismissActionAndDisappearsWithoutAnchors()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        AutomationProperties.SetName(box, "Archived message");
        ThemeTestHelper.ShowWindow(box);

        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        var invoke = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetProvider<IInvokeProvider>());

        invoke.Invoke();
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, box.State.TargetValue);
        clock.SettleAll();
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, box.State.SettledValue);

        box.State.Reset();
        clock.SettleAll();
        box.EnableDismissFromStartToEnd = false;
        box.EnableDismissFromEndToStart = false;
        Assert.Null(peer.GetProvider<IInvokeProvider>());
    }

    [AvaloniaFact]
    public void TemplateStacksBackgroundAtTheForegroundSize()
    {
        var background = new Border();
        var foreground = new Border { Width = 180, Height = 64 };
        var box = new MaterialSwipeToDismissBox
        {
            BackgroundContent = background,
            Content = foreground,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        ThemeTestHelper.ShowWindow(box);

        var presenters = box.GetVisualDescendants().OfType<ContentPresenter>().ToArray();
        Assert.Equal(2, presenters.Length);
        Assert.Equal(foreground.Bounds.Size, background.Bounds.Size);
        Assert.Equal(new Size(180, 64), box.Bounds.Size);
    }

    [AvaloniaFact]
    public void CenteredControlKeepsItsExplicitSlotAndComposeOverflowPolicy()
    {
        var foreground = new Border { Background = Brushes.Purple };
        var box = CreateBox();
        box.Content = foreground;
        box.HorizontalAlignment = HorizontalAlignment.Center;
        box.VerticalAlignment = VerticalAlignment.Center;
        var root = new Border { Width = 360, Height = 120, Child = box };
        ThemeTestHelper.ShowWindow(root);

        var panel = Assert.Single(
            box.GetVisualDescendants().OfType<MaterialSwipeToDismissBoxPanel>());
        Assert.Equal(new Size(240, 64), box.Bounds.Size);
        Assert.Equal(box.Bounds.Size, panel.Bounds.Size);
        Assert.Equal(box.Bounds.Size, foreground.Bounds.Size);
        Assert.False(box.ClipToBounds);
        Assert.False(panel.ClipToBounds);
    }

    private static MaterialSwipeToDismissBox CreateBox(SwipeToDismissBoxState? state = null) =>
        new()
        {
            State = state ?? new SwipeToDismissBoxState(),
            Width = 240,
            Height = 64,
            BackgroundContent = new Border { Background = Brushes.Transparent },
            Content = new Border { Background = Brushes.Transparent }
        };

    private static void RaiseKey(
        InputElement target,
        Key key,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers
        });
    }
}
