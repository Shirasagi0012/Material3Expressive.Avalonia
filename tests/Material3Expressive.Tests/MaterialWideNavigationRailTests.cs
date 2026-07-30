// Upstream contract: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 WideNavigationRail.kt,
// WideNavigationRailState.kt, WideNavigationRailTest.kt, and NavigationRail*Tokens.kt.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialWideNavigationRailTests
{
    [AvaloniaFact]
    public void CollapsedRailUses96DipWidth64DipItemsAnd44DipTopPadding()
    {
        var rail = Rail(expanded: false);
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });

        Assert.Equal(96, rail.Bounds.Width, precision: 10);
        Assert.All(rail.Items, item => Assert.Equal(96, item.Bounds.Width, precision: 10));
        Assert.All(rail.Items, item => Assert.Equal(64, item.Bounds.Height, precision: 10));
        Assert.Equal(44, TopInRail(rail.Items[0], rail), precision: 10);
        Assert.Equal(112, TopInRail(rail.Items[1], rail), precision: 10);
        Assert.All(rail.Items, item => Assert.Equal(NavigationItemIconPosition.Top, item.IconPosition));
    }

    [AvaloniaFact]
    public void ExpandedRailUses220DipMinimumWidthAndLeading56DipIndicator()
    {
        var rail = Rail(expanded: true);
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });

        var indicator = IndicatorArea(rail.Items[0]);
        Assert.Equal(220, rail.Bounds.Width, precision: 10);
        Assert.Equal(56, rail.Items[0].Bounds.Height, precision: 10);
        Assert.Equal(20, indicator.X, precision: 10);
        Assert.Equal(56, indicator.Height, precision: 10);
        Assert.Equal(NavigationItemIconPosition.Start, rail.Items[0].IconPosition);
        Assert.Equal(14, rail.Items[0].FontSize, precision: 10);
    }

    [AvaloniaFact]
    public void ExpandedRailGrowsForContentButStopsAt360Dip()
    {
        var rail = Rail(expanded: true);
        rail.Items[0].Content = new Border { Width = 500, Height = 20 };
        ThemeTestHelper.ShowWindow(new Border { Width = 600, Height = 400, Child = rail });

        Assert.Equal(360, rail.Bounds.Width, precision: 10);
        Assert.True(rail.Items[0].Bounds.Width <= 340.0001);
    }

    [AvaloniaFact]
    public void ExpandedRailRespectsNarrowConstraintsAndRecoversItsMinimumWidth()
    {
        var rail = Rail(expanded: true);
        rail.SelectedIndex = 2;
        var selected = rail.Items[2];
        var host = new Border { Width = 180, Height = 400, Child = rail };
        ThemeTestHelper.ShowWindow(host);
        Assert.Equal(180, rail.Bounds.Width, precision: 10);

        host.Width = 300;
        host.InvalidateMeasure();
        host.Measure(new Size(300, 400));
        host.Arrange(new Rect(0, 0, 300, 400));

        Assert.Equal(220, rail.Bounds.Width, precision: 10);
        Assert.Same(selected, rail.Items[2]);
        Assert.Equal(2, rail.SelectedIndex);
    }

    [AvaloniaFact]
    public void ExpansionKeepsDestinationInstancesAndSelection()
    {
        var rail = Rail(expanded: false);
        rail.SelectedIndex = 1;
        var selected = rail.Items[1];
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });

        rail.IsExpanded = true;
        SetExpansion(rail, 1);
        rail.InvalidateMeasure();

        Assert.Same(selected, rail.Items[1]);
        Assert.Equal(1, rail.SelectedIndex);
        Assert.True(selected.IsChecked);
        Assert.Equal(NavigationItemIconPosition.Start, selected.IconPosition);
    }

    [AvaloniaFact]
    public void ItemCanKeepAnExplicitIconPositionAcrossExpansion()
    {
        var rail = Rail(expanded: false);
        rail.Items[0].FollowWideRailExpansion = false;
        rail.Items[0].IconPosition = NavigationItemIconPosition.Start;
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });

        rail.IsExpanded = true;
        SetExpansion(rail, 1);

        Assert.Equal(NavigationItemIconPosition.Start, rail.Items[0].IconPosition);
        rail.Items[0].IconPosition = NavigationItemIconPosition.Top;
        rail.IsExpanded = false;
        Assert.Equal(NavigationItemIconPosition.Top, rail.Items[0].IconPosition);
    }

    [AvaloniaFact]
    public void ExpansionUsesTheDefaultSpatialSpringAndCanReverseMidFlight()
    {
        using var clock = new MotionTestClockScope();
        var rail = Rail(expanded: false);
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });
        clock.SettleAll();

        rail.IsExpanded = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.InRange(rail.ExpansionProgress, 0.000001, 0.999999);

        var expanding = rail.ExpansionProgress;
        rail.IsExpanded = false;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.NotEqual(expanding, rail.ExpansionProgress);
        clock.SettleAll();
        Assert.Equal(0, rail.ExpansionProgress);
        Assert.Equal(0, rail.SelectedIndex);
        Assert.Same(rail.Items[0], rail.Items.Single(static item => item.IsChecked == true));
    }

    [AvaloniaFact]
    public void HeaderAndVerticalArrangementsFollowTheUpstreamGeometry()
    {
        var header = new Border { Width = 48, Height = 48 };
        var top = Rail(expanded: false);
        top.Header = header;
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = top });
        Assert.Equal(44, TopInRail(header, top), precision: 10);
        Assert.Equal(132, TopInRail(top.Items[0], top), precision: 10);

        var centered = Rail(expanded: false);
        centered.Arrangement = WideNavigationRailArrangement.Center;
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = centered });
        Assert.Equal(100, TopInRail(centered.Items[0], centered), precision: 10);

        var bottom = Rail(expanded: false);
        bottom.Arrangement = WideNavigationRailArrangement.Bottom;
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = bottom });
        Assert.Equal(156, TopInRail(bottom.Items[0], bottom), precision: 10);
    }

    [AvaloniaFact]
    public void ArrowKeysWrapSelectWhileTabRemainsOrdinaryTraversal()
    {
        var rail = Rail(expanded: false);
        var after = new Button { Content = "After" };
        var window = ThemeTestHelper.ShowWindow(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { rail, after }
            });
        rail.Items[1].Focus();

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.True(rail.Items[2].IsFocused);
        Assert.Equal(2, rail.SelectedIndex);

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.True(rail.Items[0].IsFocused);
        Assert.Equal(0, rail.SelectedIndex);

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Assert.False(rail.Items[0].IsFocused);
    }

    [AvaloniaFact]
    public void AutomationExposesSelectionAndExpansionWithoutReplacingItemPeers()
    {
        var rail = Rail(expanded: false);
        ThemeTestHelper.ShowWindow(rail);
        var peer = ControlAutomationPeer.CreatePeerForElement(rail);
        var selection = Assert.IsAssignableFrom<ISelectionProvider>(
            peer.GetProvider<ISelectionProvider>());
        var expansion = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());
        var children = peer.GetChildren();
        var second = Assert.IsAssignableFrom<ISelectionItemProvider>(
            children[1].GetProvider<ISelectionItemProvider>());

        Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());
        Assert.All(children, child => Assert.Equal(AutomationControlType.TabItem, child.GetAutomationControlType()));
        second.Select();
        Assert.Equal(1, rail.SelectedIndex);
        Assert.Single(selection.GetSelection());
        Assert.Same(selection, second.SelectionContainer);

        expansion.Expand();
        Assert.True(rail.IsExpanded);
        Assert.Equal(ExpandCollapseState.Expanded, expansion.ExpandCollapseState);
        Assert.Same(children[1], peer.GetChildren()[1]);

        rail.Items[1].IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(second.Select);
        rail.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(expansion.Collapse);
    }

    [AvaloniaFact]
    public void AutomationPlacesTheNativeHeaderPeerBeforeDestinationPeers()
    {
        var rail = Rail(expanded: false);
        rail.Header = new Button { Content = "Menu" };
        ThemeTestHelper.ShowWindow(rail);

        var children = ControlAutomationPeer.CreatePeerForElement(rail).GetChildren();
        Assert.Equal(4, children.Count);
        Assert.Equal(AutomationControlType.Button, children[0].GetAutomationControlType());
        Assert.All(
            children.Skip(1),
            child => Assert.Equal(AutomationControlType.TabItem, child.GetAutomationControlType()));
    }

    [AvaloniaFact]
    public void ExpandedRtlAlignsTheIndicatorToTheLogicalStart()
    {
        var rail = Rail(expanded: true);
        rail.FlowDirection = FlowDirection.RightToLeft;
        ThemeTestHelper.ShowWindow(new Border { Width = 500, Height = 400, Child = rail });

        var item = rail.Items[0];
        var indicator = IndicatorArea(item);
        Assert.Equal(item.Bounds.Width - 20 - indicator.Width, indicator.X, precision: 10);
    }

    private static MaterialWideNavigationRail Rail(bool expanded)
    {
        var rail = new MaterialWideNavigationRail
        {
            IsExpanded = expanded,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        for (var index = 0; index < 3; index++)
        {
            rail.Items.Add(
                new MaterialNavigationItem
                {
                    Content = $"Item {index + 1}",
                    Icon = new Border { Width = 24, Height = 24 }
                });
        }
        return rail;
    }

    private static Rect IndicatorArea(MaterialNavigationItem item) =>
        (Rect)(typeof(MaterialNavigationItem)
            .GetProperty(
                "IndicatorArea",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(item)
            ?? throw new InvalidOperationException("IndicatorArea was unavailable."));

    private static void SetExpansion(MaterialWideNavigationRail rail, double progress) =>
        typeof(MaterialWideNavigationRail)
            .GetMethod(
                "SetExpansionForTesting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(rail, [progress]);

    private static double TopInRail(Control control, MaterialWideNavigationRail rail) =>
        control.TranslatePoint(default, rail)?.Y
        ?? throw new InvalidOperationException("Control was not attached to the rail.");
}
