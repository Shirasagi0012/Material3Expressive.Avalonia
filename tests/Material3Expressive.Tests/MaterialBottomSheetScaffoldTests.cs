// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// BottomSheetScaffold.kt:116-340 and SheetDefaults.kt:90-160 (VERSION 29.0.0).
// Compose's suspend commands are represented by synchronous vetoable commands because Avalonia
// controls expose event-loop animations rather than coroutine jobs.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialBottomSheetScaffoldTests
{
    [AvaloniaFact]
    public void DefaultsUseTheStandardScaffoldContract()
    {
        var scaffold = new MaterialBottomSheetScaffold
        {
            Content = new Border { Height = 320 },
            SheetContent = new Border { Height = 180 }
        };

        ThemeTestHelper.ShowWindow(scaffold);

        Assert.Equal(56, scaffold.SheetPeekHeight);
        Assert.Equal(640, scaffold.SheetMaxWidth);
        Assert.True(scaffold.SheetSwipeEnabled);
        Assert.Equal(BottomSheetValue.PartiallyExpanded, scaffold.EffectiveState.CurrentValue);
        Assert.Equal(new Thickness(0, 0, 0, 56), scaffold.ContentPadding);
    }

    [AvaloniaFact]
    public void StateGetterIsAlwaysNonNullAndResetCreatesAFreshDefault()
    {
        var scaffold = new MaterialBottomSheetScaffold();
        var initial = scaffold.State;

        Assert.NotNull(initial);
        Assert.Equal(BottomSheetValue.PartiallyExpanded, initial.CurrentValue);

        scaffold.ResetState();
        var reset = scaffold.State;
        Assert.NotSame(initial, reset);
        Assert.Equal(BottomSheetValue.PartiallyExpanded, reset.CurrentValue);

        // A binding can still deliver null at runtime; it is treated as an immediate reset.
        scaffold.State = null!;
        Assert.NotNull(scaffold.State);
        Assert.NotSame(reset, scaffold.State);
    }

    [AvaloniaFact]
    public void OwnedStateIsConnectedToTheTemplatedSheetAndReplacementStateFollows()
    {
        var scaffold = new MaterialBottomSheetScaffold
        {
            Content = new Border { Height = 320 },
            SheetContent = new Border { Height = 180 }
        };

        ThemeTestHelper.ShowWindow(scaffold);

        var sheet = Assert.Single(
            scaffold.GetVisualDescendants().OfType<MaterialBottomSheetScaffoldSheet>());
        Assert.Same(scaffold.EffectiveState, sheet.State);
        Assert.True(scaffold.EffectiveState.Expand());
        Assert.Equal(0, sheet.SheetTargetFraction, precision: 6);

        var replacement = new BottomSheetState(BottomSheetValue.Expanded);
        scaffold.State = replacement;

        Assert.Same(replacement, scaffold.EffectiveState);
        Assert.Same(replacement, sheet.State);
        Assert.Equal(0, sheet.SheetFraction, precision: 6);
    }

    [AvaloniaFact]
    public void NestedScrollConsumesSheetTravelBeforeChildAndOnlyCollapsesAfterChildBoundary()
    {
        var state = new BottomSheetState();
        var scaffold = new MaterialBottomSheetScaffold
        {
            State = state,
            Width = 360,
            Height = 320,
            Content = new Border(),
            SheetContent = new Border { Height = 180 }
        };
        ThemeTestHelper.ShowWindow(scaffold);
        var sheet = Assert.Single(
            scaffold.GetVisualDescendants().OfType<MaterialBottomSheetScaffoldSheet>());

        var expandConsumed = sheet.ConsumeNestedScrollDelta(40, childConsumed: true);
        Assert.Equal(40, expandConsumed, precision: 6);
        Assert.InRange(sheet.SheetFraction, 0, 1);
        var expandedFraction = sheet.SheetFraction;

        Assert.Equal(0, sheet.ConsumeNestedScrollDelta(-20, childConsumed: true));
        Assert.Equal(expandedFraction, sheet.SheetFraction, precision: 6);

        var collapseConsumed = sheet.ConsumeNestedScrollDelta(-20, childConsumed: false);
        Assert.Equal(-20, collapseConsumed, precision: 6);
        Assert.True(sheet.SheetFraction > expandedFraction);

        sheet.SwipeEnabled = false;
        Assert.Equal(0, sheet.ConsumeNestedScrollDelta(20, childConsumed: false));
    }

    [AvaloniaFact]
    public void GestureVetoRestoresTheExistingLogicalAnchor()
    {
        var state = new BottomSheetState(
            BottomSheetValue.Expanded,
            value => value != BottomSheetValue.PartiallyExpanded);
        var scaffold = new MaterialBottomSheetScaffold
        {
            State = state,
            Width = 360,
            Height = 320,
            Content = new Border(),
            SheetContent = new Border { Height = 180 }
        };
        ThemeTestHelper.ShowWindow(scaffold);
        var sheet = Assert.Single(
            scaffold.GetVisualDescendants().OfType<MaterialBottomSheetScaffoldSheet>());

        Assert.True(sheet.ConsumeNestedScrollDelta(-100, childConsumed: false) < 0);
        Assert.True(sheet.SheetFraction > 0.5);
        sheet.EndNestedScrollGesture();

        Assert.Equal(BottomSheetValue.Expanded, state.CurrentValue);
        Assert.Equal(0, sheet.SheetTargetFraction, precision: 6);
    }

    [AvaloniaFact]
    public void SnackbarPlacementTracksTheSettledSheetState()
    {
        static (MaterialBottomSheetScaffold Scaffold, Border Snackbar) Create(
            BottomSheetValue value)
        {
            var snackbar = new Border { Width = 80, Height = 20 };
            var scaffold = new MaterialBottomSheetScaffold
            {
                State = new BottomSheetState(value),
                Width = 360,
                Height = 320,
                Content = new Border(),
                SheetContent = new Border { Height = 180 },
                SnackbarHost = snackbar
            };
            ThemeTestHelper.ShowWindow(scaffold);
            return (scaffold, snackbar);
        }

        var partial = Create(BottomSheetValue.PartiallyExpanded);
        var expanded = Create(BottomSheetValue.Expanded);

        Assert.Equal(244, partial.Snackbar.TranslatePoint(default, partial.Scaffold)!.Value.Y);
        Assert.Equal(300, expanded.Snackbar.TranslatePoint(default, expanded.Scaffold)!.Value.Y);
    }

    [AvaloniaFact]
    public void StateCommandsAreVetoableAndTrackTarget()
    {
        var accepted = new BottomSheetState();
        Assert.True(accepted.Expand());
        Assert.Equal(BottomSheetValue.Expanded, accepted.TargetValue);
        Assert.Equal(BottomSheetValue.Expanded, accepted.CurrentValue);

        var rejected = new BottomSheetState(
            confirmValueChange: value => value != BottomSheetValue.Expanded);
        Assert.False(rejected.Expand());
        Assert.Equal(BottomSheetValue.PartiallyExpanded, rejected.CurrentValue);
        Assert.Equal(BottomSheetValue.PartiallyExpanded, rejected.TargetValue);

        Assert.True(accepted.Hide());
        Assert.Equal(BottomSheetValue.Hidden, accepted.CurrentValue);
    }

    [AvaloniaFact]
    public void ExplicitStateIsRetainedWhenTheScaffoldIsTemplated()
    {
        var state = new BottomSheetState(BottomSheetValue.Expanded);
        var scaffold = new MaterialBottomSheetScaffold
        {
            State = state,
            Content = new TextBlock { Text = "Body" },
            SheetContent = new TextBlock { Text = "Sheet" },
            Width = 420,
            Height = 300
        };

        ThemeTestHelper.ShowWindow(scaffold);

        Assert.Same(state, scaffold.State);
        Assert.Equal(BottomSheetValue.Expanded, scaffold.EffectiveState.CurrentValue);
        Assert.Contains(
            scaffold.GetVisualDescendants().OfType<Control>(),
            item => item is not MaterialBottomSheetScaffold);
    }

    [AvaloniaFact]
    public void AutomationExposesTheThreeStateExpandCollapseContract()
    {
        var state = new BottomSheetState();
        var scaffold = new MaterialBottomSheetScaffold
        {
            State = state,
            Content = new TextBlock { Text = "Body" },
            SheetContent = new TextBlock { Text = "Sheet" }
        };
        ThemeTestHelper.ShowWindow(scaffold);

        var peer = ControlAutomationPeer.CreatePeerForElement(scaffold);
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());
        Assert.False(provider.ShowsMenu);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, provider.ExpandCollapseState);

        AutomationPropertyChangedEventArgs? changed = null;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                changed = e;
        };

        provider.Expand();
        Assert.Equal(BottomSheetValue.Expanded, state.CurrentValue);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
        Assert.NotNull(changed);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, changed!.OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, changed.NewValue);

        provider.Collapse();
        Assert.Equal(BottomSheetValue.PartiallyExpanded, state.CurrentValue);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, provider.ExpandCollapseState);

        state.Hide();
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
    }

    [AvaloniaFact]
    public void AutomationTracksReplacementStateAndHonorsVeto()
    {
        var rejected = new BottomSheetState(
            confirmValueChange: value => value != BottomSheetValue.Expanded);
        var scaffold = new MaterialBottomSheetScaffold
        {
            State = rejected,
            Content = new TextBlock { Text = "Body" },
            SheetContent = new TextBlock { Text = "Sheet" }
        };
        ThemeTestHelper.ShowWindow(scaffold);

        var peer = ControlAutomationPeer.CreatePeerForElement(scaffold);
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());

        provider.Expand();
        Assert.Equal(BottomSheetValue.PartiallyExpanded, rejected.CurrentValue);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, provider.ExpandCollapseState);

        scaffold.State = new BottomSheetState(BottomSheetValue.Expanded);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
    }
}
