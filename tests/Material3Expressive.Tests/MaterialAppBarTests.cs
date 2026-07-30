// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// AppBar.kt:518-968, 1693-2243, 2249-2375, 3493-3761 and AppBar*Tokens.kt v14_0_0.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialAppBarTests
{
    [AvaloniaTheory]
    [InlineData(AppBarSize.Small, 64)]
    [InlineData(AppBarSize.Medium, 112)]
    [InlineData(AppBarSize.Large, 152)]
    public void StandardSizesUseAlpha25TokenHeights(AppBarSize size, double height)
    {
        var appBar = new MaterialAppBar { Size = size, Title = "Inbox" };
        ThemeTestHelper.ShowWindow(appBar);

        Assert.Equal(height, appBar.CurrentHeight);
    }

    [AvaloniaTheory]
    [InlineData(AppBarSize.Medium, false, 112, 28)]
    [InlineData(AppBarSize.Medium, true, 136, 28)]
    [InlineData(AppBarSize.Large, false, 120, 36)]
    [InlineData(AppBarSize.Large, true, 152, 36)]
    public void FlexibleSizesAndTypographyUseAlpha25Tokens(
        AppBarSize size,
        bool subtitle,
        double height,
        double titleSize)
    {
        var appBar = new MaterialAppBar
        {
            Size = size,
            Variant = AppBarVariant.Flexible,
            Title = "Inbox",
            Subtitle = subtitle ? "12 messages" : null
        };
        ThemeTestHelper.ShowWindow(appBar);

        Assert.Equal(height, appBar.CurrentHeight);
        Assert.Equal(titleSize, appBar.FontSize);
    }

    [AvaloniaFact]
    public void StateGetterIsNonNullReplacementSnapsAndResetCreatesFreshExpandedState()
    {
        var appBar = new MaterialAppBar { Size = AppBarSize.Medium };
        var initial = appBar.State;
        var collapsed = new AppBarScrollState(1, 48);

        appBar.State = collapsed;
        ThemeTestHelper.ShowWindow(appBar);

        Assert.Same(collapsed, appBar.State);
        Assert.Equal(64, appBar.CurrentHeight);

        appBar.ResetState();

        Assert.NotSame(initial, appBar.State);
        Assert.NotSame(collapsed, appBar.State);
        Assert.Equal(0, appBar.State.CollapsedFraction);
        Assert.Equal(112, appBar.CurrentHeight);

        appBar.State = null!;
        Assert.NotNull(appBar.State);
        Assert.Equal(0, appBar.State.CollapsedFraction);
    }

    [AvaloniaFact]
    public void EnterAlwaysConsumesBothDirectionsAcrossTheCollapseRange()
    {
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            ScrollBehavior = AppBarScrollBehavior.EnterAlways
        };
        ThemeTestHelper.ShowWindow(appBar);

        Assert.Equal(24, appBar.ApplyScrollDelta(24));
        Assert.Equal(0.5, appBar.State.CollapsedFraction, precision: 6);
        Assert.Equal(88, appBar.CurrentHeight, precision: 6);

        Assert.Equal(-24, appBar.ApplyScrollDelta(-24));
        Assert.Equal(0, appBar.State.CollapsedFraction);
        Assert.Equal(112, appBar.CurrentHeight);
    }

    [AvaloniaFact]
    public void ExitUntilCollapsedExpandsOnlyAfterContentReachesItsStart()
    {
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            ScrollBehavior = AppBarScrollBehavior.ExitUntilCollapsed
        };
        ThemeTestHelper.ShowWindow(appBar);

        appBar.ApplyScrollDelta(48);
        Assert.Equal(1, appBar.State.CollapsedFraction);

        Assert.Equal(0, appBar.ApplyScrollDelta(-24, contentAtStart: false));
        Assert.Equal(1, appBar.State.CollapsedFraction);

        Assert.Equal(-24, appBar.ApplyScrollDelta(-24, contentAtStart: true));
        Assert.Equal(0.5, appBar.State.CollapsedFraction, precision: 6);
    }

    [AvaloniaFact]
    public void PinnedTracksOverlapWithoutChangingHeight()
    {
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Large,
            ScrollBehavior = AppBarScrollBehavior.Pinned
        };
        ThemeTestHelper.ShowWindow(appBar);

        Assert.Equal(0, appBar.ApplyScrollDelta(80));
        Assert.Equal(80, appBar.State.ContentOffset);
        Assert.Equal(0, appBar.State.CollapsedFraction);
        Assert.Equal(152, appBar.CurrentHeight);
    }

    [AvaloniaFact]
    public void ScrollViewerOffsetDrivesTheConnectedState()
    {
        var viewer = new ScrollViewer
        {
            Width = 240,
            Height = 100,
            Content = new Border { Height = 500 }
        };
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            ScrollBehavior = AppBarScrollBehavior.EnterAlways,
            ScrollTarget = viewer
        };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { appBar, viewer } });

        viewer.Offset = new Vector(0, 48);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, appBar.State.CollapsedFraction);
        Assert.Equal(48, appBar.State.ContentOffset);
        Assert.Equal(64, appBar.CurrentHeight);
    }

    [AvaloniaFact]
    public void AutomationExposesCollapseStateAndTracksReplacement()
    {
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            Title = "Inbox"
        };
        ThemeTestHelper.ShowWindow(appBar);

        var peer = ControlAutomationPeer.CreatePeerForElement(appBar);
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());
        Assert.Equal("Inbox", peer.GetName());
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        provider.Collapse();
        Assert.Equal(1, appBar.State.CollapsedFraction);
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);

        appBar.State = new AppBarScrollState(0.5);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, provider.ExpandCollapseState);

        provider.Expand();
        Assert.Equal(0, appBar.State.CollapsedFraction);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
    }

    [AvaloniaFact]
    public void ControlValuedTitleMovesBetweenPresentersDuringControlledCollapse()
    {
        using var clock = new MotionTestClockScope();
        var title = new TextBlock { Text = "Inbox" };
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            Title = title
        };
        ThemeTestHelper.ShowWindow(appBar);
        clock.SettleAll();

        Assert.Single(appBar.GetVisualDescendants(), item => ReferenceEquals(item, title));
        Assert.Equal("PART_StackedTitlePresenter", title.GetVisualParent()?.Name);

        appBar.Size = AppBarSize.Small;
        Assert.Single(appBar.GetVisualDescendants(), item => ReferenceEquals(item, title));
        Assert.Equal("PART_InlineTitlePresenter", title.GetVisualParent()?.Name);

        appBar.Size = AppBarSize.Medium;
        Assert.Single(appBar.GetVisualDescendants(), item => ReferenceEquals(item, title));
        Assert.Equal("PART_StackedTitlePresenter", title.GetVisualParent()?.Name);

        appBar.State.Collapse();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        Assert.Single(appBar.GetVisualDescendants(), item => ReferenceEquals(item, title));

        clock.SettleAll();
        Assert.Equal(64, appBar.CurrentHeight, precision: 6);
        Assert.Single(appBar.GetVisualDescendants(), item => ReferenceEquals(item, title));
    }

    [AvaloniaFact]
    public void ReplacingStateInterruptsAnInFlightCollapseAndDetachesTheOldOwner()
    {
        using var clock = new MotionTestClockScope();
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            Title = "Inbox"
        };
        ThemeTestHelper.ShowWindow(appBar);
        clock.SettleAll();

        var oldState = appBar.State;
        oldState.Collapse();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.InRange(appBar.CurrentHeight, 64.000001, 111.999999);

        var replacement = new AppBarScrollState();
        appBar.State = replacement;
        Assert.Equal(112, appBar.CurrentHeight);

        oldState.Collapse();
        clock.SettleAll();
        Assert.Same(replacement, appBar.State);
        Assert.Equal(0, appBar.State.CollapsedFraction);
        Assert.Equal(112, appBar.CurrentHeight);
    }
}
