// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// PullToRefresh.kt, PullToRefreshBoxTest.kt, and PullToRefreshStateImplTest.kt. Pull multiplier,
// strict threshold, overshoot, nested ordering, state restoration, and refresh ownership are source
// parity. Keyboard and automation assertions cover the explicit D003 desktop adaptation.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialPullToRefreshBoxTests
{
    [AvaloniaFact]
    public void StateIsPerInstanceNonNullReplaceableAndResetCreatesFreshHiddenState()
    {
        var first = new MaterialPullToRefreshBox();
        var second = new MaterialPullToRefreshBox();
        Assert.NotSame(first.State, second.State);

        var restored = new PullToRefreshState(.5);
        first.State = restored;
        Assert.Same(restored, first.State);
        Assert.Equal(.5, first.State.DistanceFraction, precision: 6);

        first.ResetState();
        Assert.NotSame(restored, first.State);
        Assert.Equal(0, first.State.DistanceFraction, precision: 6);

        first.State = null!;
        Assert.NotNull(first.State);
    }

    [AvaloniaFact]
    public void RawPullUsesHalfDistanceAndNonlinearOvershootCappedAtTwo()
    {
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);

        Assert.True(box.BeginPull());
        Assert.Equal(80, box.PullBy(80), precision: 6);
        Assert.Equal(.5, box.State.DistanceFraction, precision: 6);

        box.PullBy(160);
        Assert.Equal(1.4375, box.State.DistanceFraction, precision: 6);

        box.PullBy(800);
        Assert.Equal(2, box.State.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void ReleaseRefreshesOnlyStrictlyBeyondAdjustedThreshold()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);
        var refreshes = 0;
        box.RefreshRequested += (_, _) => refreshes++;

        box.BeginPull();
        box.PullBy(159);
        box.EndPull();
        Assert.Equal(0, refreshes);
        clock.SettleAll();

        box.BeginPull();
        box.PullBy(160);
        box.EndPull();
        Assert.Equal(0, refreshes);
        clock.SettleAll();

        box.BeginPull();
        box.PullBy(161);
        box.EndPull();
        Assert.Equal(1, refreshes);
        clock.SettleAll();
        Assert.Equal(0, box.State.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void ApplicationOwnedRefreshingHoldsAtThresholdThenHidesOnCompletion()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);
        var refreshes = 0;
        box.RefreshRequested += (_, _) =>
        {
            refreshes++;
            box.IsRefreshing = true;
        };

        box.BeginPull();
        box.PullBy(200);
        box.EndPull();
        Assert.Equal(1, refreshes);
        Assert.True(box.IsRefreshing);
        Assert.Equal(1, box.MotionTargetFraction, precision: 6);
        clock.SettleAll();
        Assert.Equal(1, box.State.DistanceFraction, precision: 6);

        Assert.False(box.BeginPull());
        Assert.Equal(0, box.PullBy(80), precision: 6);

        box.IsRefreshing = false;
        Assert.Equal(0, box.MotionTargetFraction, precision: 6);
        clock.SettleAll();
        Assert.Equal(0, box.State.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void UpwardNestedScrollRetractsBeforeChildAndDownwardRequiresChildAtStart()
    {
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);

        Assert.Equal(0, box.ConsumeNestedScrollDelta(40, childAtStart: false));
        Assert.Equal(40, box.ConsumeNestedScrollDelta(40, childAtStart: true), precision: 6);
        Assert.Equal(.25, box.State.DistanceFraction, precision: 6);

        Assert.Equal(-20, box.ConsumeNestedScrollDelta(-20, childAtStart: false), precision: 6);
        Assert.Equal(.125, box.State.DistanceFraction, precision: 6);

        Assert.Equal(-20, box.ConsumeNestedScrollDelta(-80, childAtStart: false), precision: 6);
        Assert.Equal(0, box.State.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void ReleaseConsumesOnlyDownwardVelocityWhileIndicatorIsVisible()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox();
        ThemeTestHelper.ShowWindow(box);

        box.BeginPull();
        box.PullBy(40);
        Assert.Equal(320, box.EndPull(320), precision: 6);
        clock.SettleAll();

        box.BeginPull();
        box.PullBy(40);
        Assert.Equal(0, box.EndPull(-320), precision: 6);
        clock.SettleAll();

        box.BeginPull();
        Assert.Equal(0, box.EndPull(320), precision: 6);
    }

    [AvaloniaFact]
    public void PullDisabledBlocksGesturesButKeepsDesktopRefreshAction()
    {
        var box = CreateBox();
        box.PullEnabled = false;
        ThemeTestHelper.ShowWindow(box);
        var refreshes = 0;
        box.RefreshRequested += (_, _) => refreshes++;

        Assert.False(box.BeginPull());
        Assert.Equal(0, box.PullBy(200), precision: 6);

        RaiseKey(box, Key.F5);
        RaiseKey(box, Key.R, KeyModifiers.Control);
        Assert.Equal(2, refreshes);
    }

    [AvaloniaFact]
    public void ProgrammaticTransitionsRetargetFromCurrentSpringValue()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox(new PullToRefreshState(.75));
        ThemeTestHelper.ShowWindow(box);

        box.State.AnimateToHidden();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(25));
        var interrupted = box.State.DistanceFraction;
        Assert.InRange(interrupted, .001, .749);

        box.State.AnimateToThreshold();
        Assert.Equal(interrupted, box.State.DistanceFraction, precision: 6);
        Assert.True(box.State.IsAnimating);
        Assert.Equal(1, box.MotionTargetFraction, precision: 6);
        clock.SettleAll();
        Assert.Equal(1, box.State.DistanceFraction, precision: 6);
        Assert.False(box.State.IsAnimating);
    }

    [AvaloniaFact]
    public void StateReplacementStopsUpdatingOldOwnerAndRestoresReplacementFraction()
    {
        using var clock = new MotionTestClockScope();
        var old = new PullToRefreshState(.75);
        var box = CreateBox(old);
        ThemeTestHelper.ShowWindow(box);
        old.AnimateToHidden();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(20));

        var replacement = new PullToRefreshState(.5);
        box.State = replacement;
        Assert.Equal(.5, replacement.DistanceFraction, precision: 6);
        Assert.Equal(0, box.IndicatorOffset, precision: 6);

        old.AnimateToThreshold();
        clock.SettleAll();
        Assert.Same(replacement, box.State);
        Assert.Equal(.5, replacement.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void EscapeHidesPendingPullButDoesNotCancelApplicationRefresh()
    {
        using var clock = new MotionTestClockScope();
        var box = CreateBox(new PullToRefreshState(.75));
        ThemeTestHelper.ShowWindow(box);

        RaiseKey(box, Key.Escape);
        Assert.Equal(0, box.MotionTargetFraction, precision: 6);
        clock.SettleAll();

        box.IsRefreshing = true;
        clock.SettleAll();
        RaiseKey(box, Key.Escape);
        Assert.True(box.IsRefreshing);
        Assert.Equal(1, box.State.DistanceFraction, precision: 6);
    }

    [AvaloniaFact]
    public void AutomationExposesRefreshInvokeAndReadOnlyClampedProgress()
    {
        var box = CreateBox(new PullToRefreshState(1.5));
        AutomationProperties.SetName(box, "Refresh messages");
        ThemeTestHelper.ShowWindow(box);
        var refreshes = 0;
        box.RefreshRequested += (_, _) => refreshes++;

        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        var invoke = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetProvider<IInvokeProvider>());
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(
            peer.GetProvider<IRangeValueProvider>());
        Assert.True(range.IsReadOnly);
        Assert.Equal(0, range.Minimum);
        Assert.Equal(1, range.Maximum);
        Assert.Equal(1, range.Value);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(.5));

        invoke.Invoke();
        Assert.Equal(1, refreshes);
        box.IsRefreshing = true;
        Assert.Null(peer.GetProvider<IInvokeProvider>());
    }

    [AvaloniaFact]
    public void TemplateOverlaysNonInteractiveIndicatorWithoutMovingContent()
    {
        var content = new Border { Background = Brushes.Blue };
        var box = CreateBox(new PullToRefreshState(.5));
        box.Content = content;
        ThemeTestHelper.ShowWindow(box);

        var panel = Assert.Single(
            box.GetVisualDescendants().OfType<MaterialPullToRefreshBoxPanel>());
        var presenters = box.GetVisualDescendants().OfType<ContentPresenter>().ToArray();
        var indicator = Assert.Single(
            box.GetVisualDescendants().OfType<MaterialPullToRefreshIndicator>());

        Assert.Equal(new Rect(box.Bounds.Size), content.Bounds);
        Assert.Equal(0, box.IndicatorOffset, precision: 6);
        Assert.Equal(0, indicator.Bounds.Y, precision: 6);
        Assert.False(indicator.IsHitTestVisible);
        Assert.Equal(3, indicator.ParityShadowParameters.Elevation);
        Assert.False(box.ClipToBounds);
        Assert.False(panel.ClipToBounds);
        Assert.Equal(2, presenters.Length);
    }

    private static MaterialPullToRefreshBox CreateBox(PullToRefreshState? state = null) =>
        new()
        {
            State = state ?? new PullToRefreshState(),
            Width = 240,
            Height = 160,
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
