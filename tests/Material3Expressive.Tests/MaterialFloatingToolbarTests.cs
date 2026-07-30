// Upstream contract: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 FloatingToolbar.kt and
// FloatingToolbarTest.kt. Geometry is shared with the pinned alpha22 executable reference.

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
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialFloatingToolbarTests
{
    [AvaloniaTheory]
    [InlineData(FloatingToolbarOrientation.Horizontal, 160, 64)]
    [InlineData(FloatingToolbarOrientation.Vertical, 64, 160)]
    public void OrientationUsesA64DipContainerAnd48DipActionTargets(
        FloatingToolbarOrientation orientation,
        double width,
        double height)
    {
        var toolbar = Toolbar(orientation, 3);
        ThemeTestHelper.ShowWindow(toolbar);

        Assert.Equal(width, toolbar.Bounds.Width, precision: 6);
        Assert.Equal(height, toolbar.Bounds.Height, precision: 6);
        Assert.All(toolbar.Items, item =>
        {
            Assert.Equal(48, item.Bounds.Width, precision: 6);
            Assert.Equal(48, item.Bounds.Height, precision: 6);
        });
    }

    [AvaloniaFact]
    public void CollapsedToolbarKeepsMainActionsAndHidesAuxiliarySlots()
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        toolbar.LeadingContent = Action("Leading");
        toolbar.TrailingContent = Action("Trailing");
        toolbar.IsExpanded = false;
        ThemeTestHelper.ShowWindow(toolbar);

        Assert.Equal(112, toolbar.Bounds.Width, precision: 6);
        Assert.Equal(0, toolbar.ExpansionProgress);
        Assert.All(toolbar.Items, item => Assert.True(item.IsEffectivelyVisible));
    }

    [AvaloniaFact]
    public void FabFormRetainsItsExpandedFootprintAndGrowsFabWhenCollapsed()
    {
        var fab = new MaterialFab { Content = "+" };
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 3);
        toolbar.FloatingActionButton = fab;
        toolbar.IsExpanded = false;
        ThemeTestHelper.ShowWindow(toolbar);

        Assert.Equal(224, toolbar.Bounds.Width, precision: 6);
        Assert.Equal(80, toolbar.Bounds.Height, precision: 6);
        Assert.Equal(80, fab.Bounds.Width, precision: 6);
        Assert.Equal(80, fab.Bounds.Height, precision: 6);
        Assert.Equal(FabVariant.Primary, fab.Variant);
    }

    [AvaloniaTheory]
    [InlineData(320, 100, FloatingToolbarOrientation.Horizontal)]
    [InlineData(100, 320, FloatingToolbarOrientation.Vertical)]
    public void AutoOrientationFollowsAvailableAspectRatio(
        double width,
        double height,
        FloatingToolbarOrientation expected)
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Auto, 2);
        ThemeTestHelper.ShowWindow(new Border
        {
            Width = width,
            Height = height,
            Child = toolbar
        });

        Assert.Equal(expected, toolbar.ResolvedOrientation);
    }

    [AvaloniaFact]
    public void ExitAlwaysStateCoercesOffsetAndResetDetachesTheOldState()
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        toolbar.ScrollBehavior = FloatingToolbarScrollBehavior.ExitAlways;
        ThemeTestHelper.ShowWindow(toolbar);
        var first = toolbar.State;

        Assert.Equal(80, toolbar.ApplyScrollDelta(1000));
        Assert.Equal(-80, first.Offset);
        Assert.Equal(80, toolbar.ScrollOffsetY);

        toolbar.ResetState();
        Assert.NotSame(first, toolbar.State);
        Assert.Equal(0, toolbar.State.Offset);
        Assert.Equal(0, toolbar.ScrollOffsetY);

        first.Show();
        Assert.Equal(0, toolbar.State.Offset);
    }

    [AvaloniaFact]
    public void ConnectedScrollViewerHidesAndRevealsTheToolbar()
    {
        var viewer = new ScrollViewer
        {
            Width = 240,
            Height = 100,
            Content = new Border { Height = 500 }
        };
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        toolbar.ScrollBehavior = FloatingToolbarScrollBehavior.ExitAlways;
        toolbar.ScrollTarget = viewer;
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { toolbar, viewer } });

        viewer.Offset = new Vector(0, 40);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-40, toolbar.State.Offset);

        viewer.Offset = new Vector(0, 10);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-10, toolbar.State.Offset);
    }

    [AvaloniaFact]
    public void DirectionalKeysMoveFocusInVisualOrderAndTabRemainsUnclaimed()
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 3);
        var after = Action("After");
        var window = ThemeTestHelper.ShowWindow(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { toolbar, after }
        });
        toolbar.Items[0].Focus();

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.True(toolbar.Items[1].IsFocused);

        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Assert.True(toolbar.Items[2].IsFocused);

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Assert.True(after.IsFocused);
    }

    [AvaloniaFact]
    public void HiddenAndOffscreenContentLeavesTheTabSequence()
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        var leading = Action("Leading");
        var fab = new MaterialFab { Content = "+" };
        toolbar.LeadingContent = leading;
        ThemeTestHelper.ShowWindow(toolbar);

        Assert.True(KeyboardNavigation.GetIsTabStop(leading));
        toolbar.IsExpanded = false;
        Assert.False(KeyboardNavigation.GetIsTabStop(leading));
        Assert.True(KeyboardNavigation.GetIsTabStop(toolbar.Items[0]));

        toolbar.FloatingActionButton = fab;
        Assert.False(KeyboardNavigation.GetIsTabStop(toolbar.Items[0]));
        Assert.True(KeyboardNavigation.GetIsTabStop(fab));

        toolbar.ScrollBehavior = FloatingToolbarScrollBehavior.ExitAlways;
        toolbar.ApplyScrollDelta(1);
        Assert.False(KeyboardNavigation.GetIsTabStop(fab));

        toolbar.State.Show();
        Assert.True(KeyboardNavigation.GetIsTabStop(fab));
    }

    [AvaloniaFact]
    public void AutomationUsesToolbarAndExpandCollapseWithoutReplacingActionPeers()
    {
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        ThemeTestHelper.ShowWindow(toolbar);
        var peer = ControlAutomationPeer.CreatePeerForElement(toolbar);
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());

        Assert.Equal(AutomationControlType.ToolBar, peer.GetAutomationControlType());
        Assert.Equal("Floating toolbar", peer.GetName());
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        provider.Collapse();
        Assert.False(toolbar.IsExpanded);
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
        provider.Expand();
        Assert.True(toolbar.IsExpanded);

        toolbar.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(provider.Collapse);
    }

    [AvaloniaFact]
    public void ExpansionUsesTheFastSpatialSpringAndCanReverseMidFlight()
    {
        using var clock = new MotionTestClockScope();
        var toolbar = Toolbar(FloatingToolbarOrientation.Horizontal, 2);
        ThemeTestHelper.ShowWindow(toolbar);
        clock.SettleAll();

        toolbar.IsExpanded = false;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.InRange(toolbar.ExpansionProgress, 0.000001, 0.999999);

        var collapsing = toolbar.ExpansionProgress;
        toolbar.IsExpanded = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.NotEqual(collapsing, toolbar.ExpansionProgress);
        clock.SettleAll();
        Assert.Equal(1, toolbar.ExpansionProgress);
    }

    private static MaterialFloatingToolbar Toolbar(
        FloatingToolbarOrientation orientation,
        int count)
    {
        var toolbar = new MaterialFloatingToolbar
        {
            Orientation = orientation,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        for (var index = 0; index < count; index++)
            toolbar.Items.Add(Action($"Action {index + 1}"));
        return toolbar;
    }

    private static Button Action(string name) => new()
    {
        Width = 48,
        Height = 48,
        Content = name,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center
    };
}
