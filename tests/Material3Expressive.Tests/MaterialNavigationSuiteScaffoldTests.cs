// Upstream contract: AndroidX ff0a0a99 material3-adaptive-navigation-suite
// NavigationSuiteScaffold.kt, WindowSizeClassHelper.kt, NavigationSuiteScaffoldTest.kt, and
// NavigationSuiteScaffoldDeviceTest.kt. Focus retention and Pane automation are desktop adapters.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialNavigationSuiteScaffoldTests
{
    [AvaloniaFact]
    public void AutoUsesThe600By480WindowSizeContractAtExactBoundaries()
    {
        var suite = Suite(599, 600);
        var label = new TextBlock { Text = "One" };
        suite.Items[0].Content = label;
        ThemeTestHelper.ShowWindow(suite);
        Assert.Equal(
            NavigationSuiteLayoutType.ShortNavigationBarCompact,
            suite.ResolvedLayoutType);

        Resize(suite, 600, 479);
        Assert.Equal(
            NavigationSuiteLayoutType.ShortNavigationBarMedium,
            suite.ResolvedLayoutType);

        Resize(suite, 600, 480);
        Assert.Equal(
            NavigationSuiteLayoutType.WideNavigationRailCollapsed,
            suite.ResolvedLayoutType);
        Assert.False(Active<MaterialWideNavigationRail>(suite).IsExpanded);
        Assert.All(suite.Items, item => Assert.False(item.IsWideNavigationExpandedItem));
        Assert.All(suite.Items, item => Assert.Equal(12, item.FontSize, precision: 6));
        Assert.All(
            suite.Items,
            item => Assert.Equal(TypographyRole.LabelMedium, TypographyScheme.GetTextStyle(item)));
        Assert.Equal(TypographyRole.LabelMedium, TypographyScheme.GetTextStyle(label));
    }

    [AvaloniaFact]
    public void ExplicitLayoutTypesOverrideSizeAndMapTheUpstreamItemGeometry()
    {
        var suite = Suite(320, 300);
        suite.LayoutType = NavigationSuiteLayoutType.WideNavigationRailExpanded;
        ThemeTestHelper.ShowWindow(suite);

        Assert.Equal(
            NavigationSuiteLayoutType.WideNavigationRailExpanded,
            suite.ResolvedLayoutType);
        Assert.All(
            suite.Items,
            item => Assert.Equal(NavigationItemIconPosition.Start, item.IconPosition));
        Assert.True(Active<MaterialWideNavigationRail>(suite).IsExpanded);

        suite.LayoutType = NavigationSuiteLayoutType.ShortNavigationBarCompact;
        Dispatcher.UIThread.RunJobs();
        Assert.All(
            suite.Items,
            item => Assert.Equal(NavigationItemIconPosition.Top, item.IconPosition));
    }

    [AvaloniaFact]
    public void ResponsiveChangesRetainDestinationInstancesSelectionAndEnabledState()
    {
        var suite = Suite(500, 600);
        suite.SelectedIndex = 1;
        suite.Items[2].IsEnabled = false;
        var items = suite.Items.ToArray();
        ThemeTestHelper.ShowWindow(suite);

        Resize(suite, 800, 600);

        Assert.Equal(
            NavigationSuiteLayoutType.WideNavigationRailCollapsed,
            suite.ResolvedLayoutType);
        Assert.Equal(1, suite.SelectedIndex);
        Assert.Same(items[1], suite.Items[1]);
        Assert.True(items[1].IsChecked);
        Assert.False(items[2].IsEnabled);
        Assert.Equal(items, Active<MaterialWideNavigationRail>(suite).Items);
    }

    [AvaloniaFact]
    public void ShortBarUsesBottomEdgeAndWideRailUsesLogicalStart()
    {
        var body = Box("body");
        var suite = Suite(500, 600, body);
        ThemeTestHelper.ShowWindow(suite);

        var shortBar = Active<MaterialShortNavigationBar>(suite);
        AssertRect(new Rect(0, 520, 500, 80), BoundsIn(shortBar, suite));
        AssertRect(new Rect(0, 0, 500, 520), BoundsIn(body, suite));

        Resize(suite, 800, 600);
        var wideRail = Active<MaterialWideNavigationRail>(suite);
        AssertRect(new Rect(0, 0, 96, 600), BoundsIn(wideRail, suite));
        AssertRect(new Rect(96, 0, 704, 600), BoundsIn(body, suite));
    }

    [AvaloniaFact]
    public void WideRailAndContentMirrorAtTheLogicalStartInRtl()
    {
        var body = Box("body");
        var suite = Suite(800, 600, body);
        suite.FlowDirection = FlowDirection.RightToLeft;
        ThemeTestHelper.ShowWindow(suite);

        var wideRail = Active<MaterialWideNavigationRail>(suite);
        AssertRect(new Rect(704, 0, 96, 600), BoundsIn(wideRail, suite));
        AssertRect(new Rect(0, 0, 704, 600), BoundsIn(body, suite));
    }

    [AvaloniaFact]
    public void PrimaryActionMovesFromAboveTheBarIntoTheWideRailHeader()
    {
        var action = Box("action", width: 40, height: 40);
        var suite = Suite(500, 600);
        suite.PrimaryAction = action;
        ThemeTestHelper.ShowWindow(suite);

        AssertRect(new Rect(444, 464, 40, 40), BoundsIn(action, suite));

        Resize(suite, 800, 600);
        var wideRail = Active<MaterialWideNavigationRail>(suite);
        Assert.Same(action, wideRail.Header);
        Assert.Equal(44, BoundsIn(action, wideRail).Y, precision: 6);
    }

    [AvaloniaFact]
    public void NestedNavigationLabelsKeepTheirTokenLineHeightAndTracking()
    {
        var suite = Suite(800, 600);
        ThemeTestHelper.ShowWindow(suite);
        Dispatcher.UIThread.RunJobs();

        var labels = suite.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(static text => text.Text?.StartsWith("Item ", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.Equal(3, labels.Length);
        Assert.All(labels, label => Assert.Equal(16, label.LineHeight, precision: 6));
        Assert.All(labels, label => Assert.Equal(0.5, label.LetterSpacing, precision: 6));
    }

    [AvaloniaFact]
    public void ExplicitLabelLineHeightAndTrackingRemainCallerOwned()
    {
        var label = new TextBlock
        {
            Text = "Custom",
            LineHeight = 30,
            LetterSpacing = 2
        };
        var suite = Suite(800, 600);
        suite.Items[0].Content = label;
        ThemeTestHelper.ShowWindow(suite);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(30, label.LineHeight);
        Assert.Equal(2, label.LetterSpacing);
    }

    [AvaloniaFact]
    public void VisibilityUsesDefaultSpatialMotionAndCanReverseWithoutChangingSelection()
    {
        using var clock = new MotionTestClockScope();
        var suite = Suite(800, 600);
        suite.SelectedIndex = 2;
        ThemeTestHelper.ShowWindow(suite);
        clock.SettleAll();

        suite.IsNavigationVisible = false;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.InRange(suite.NavigationVisibilityProgress, 0.000001, 0.999999);
        var hiding = suite.NavigationVisibilityProgress;

        suite.IsNavigationVisible = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.NotEqual(hiding, suite.NavigationVisibilityProgress);
        clock.SettleAll();

        Assert.Equal(1, suite.NavigationVisibilityProgress);
        Assert.Equal(2, suite.SelectedIndex);
        Assert.True(suite.Items[2].IsChecked);
    }

    [AvaloniaFact]
    public void HiddenAndNoneLayoutsGiveTheBodyTheCompleteBounds()
    {
        var body = Box("body");
        var suite = Suite(800, 600, body);
        suite.IsNavigationVisible = false;
        ThemeTestHelper.ShowWindow(suite);
        AssertRect(new Rect(0, 0, 800, 600), BoundsIn(body, suite));
        Assert.False(Active<MaterialWideNavigationRail>(suite).IsVisible);

        suite.IsNavigationVisible = true;
        suite.LayoutType = NavigationSuiteLayoutType.None;
        Dispatcher.UIThread.RunJobs();
        AssertRect(new Rect(0, 0, 800, 600), BoundsIn(body, suite));
        Assert.DoesNotContain(
            suite.GetVisualDescendants(),
            static visual => visual is MaterialShortNavigationBar or MaterialWideNavigationRail);
    }

    [AvaloniaFact]
    public void ActiveNavigationSelectionFlowsBackToTheScaffold()
    {
        var suite = Suite(500, 600);
        ThemeTestHelper.ShowWindow(suite);
        var bar = Active<MaterialShortNavigationBar>(suite);

        bar.Items[2].IsChecked = true;

        Assert.Equal(2, bar.SelectedIndex);
        Assert.Equal(2, suite.SelectedIndex);
    }

    [AvaloniaFact]
    public void FocusedDestinationIsRestoredAfterAResponsiveComponentChange()
    {
        var suite = Suite(500, 600);
        ThemeTestHelper.ShowWindow(suite);
        suite.Items[1].Focus();
        Assert.True(suite.Items[1].IsFocused);

        Resize(suite, 800, 600);
        Dispatcher.UIThread.RunJobs();

        Assert.True(suite.Items[1].IsFocused);
        Assert.Same(suite.Items[1], Active<MaterialWideNavigationRail>(suite).Items[1]);
    }

    [AvaloniaFact]
    public void AutomationKeepsANeutralPaneAroundTheActiveTabSelectionTree()
    {
        var suite = Suite(800, 600);
        ThemeTestHelper.ShowWindow(suite);
        var suitePeer = ControlAutomationPeer.CreatePeerForElement(suite);
        var navigationPeer =
            ControlAutomationPeer.CreatePeerForElement(Active<MaterialWideNavigationRail>(suite));

        Assert.Equal(AutomationControlType.Pane, suitePeer.GetAutomationControlType());
        Assert.Equal(AutomationControlType.Tab, navigationPeer.GetAutomationControlType());
        Assert.Equal(3, navigationPeer.GetChildren().Count);

        Resize(suite, 500, 600);
        var shortPeer =
            ControlAutomationPeer.CreatePeerForElement(Active<MaterialShortNavigationBar>(suite));
        Assert.Equal(AutomationControlType.Tab, shortPeer.GetAutomationControlType());
        Assert.Same(
            ControlAutomationPeer.CreatePeerForElement(suite.Items[0]),
            shortPeer.GetChildren()[0]);
    }

    private static MaterialNavigationSuiteScaffold Suite(
        double width,
        double height,
        Control? body = null)
    {
        var suite = new MaterialNavigationSuiteScaffold
        {
            Width = width,
            Height = height,
            SelectedIndex = 0,
            Content = body ?? Box("body")
        };
        for (var index = 0; index < 3; index++)
        {
            suite.Items.Add(
                new MaterialNavigationItem
                {
                    Content = $"Item {index + 1}",
                    Icon = new Border { Width = 24, Height = 24 }
                });
        }
        return suite;
    }

    private static T Active<T>(MaterialNavigationSuiteScaffold suite) where T : Control =>
        suite.GetVisualDescendants().OfType<T>().Single();

    private static void Resize(
        MaterialNavigationSuiteScaffold suite,
        double width,
        double height)
    {
        suite.Width = width;
        suite.Height = height;
        suite.Measure(new Size(width, height));
        suite.Arrange(new Rect(0, 0, width, height));
        Dispatcher.UIThread.RunJobs();
    }

    private static Border Box(
        string name,
        double width = double.NaN,
        double height = double.NaN) =>
        new()
        {
            Name = name,
            Width = width,
            Height = height
        };

    private static Rect BoundsIn(Control child, Visual ancestor)
    {
        var origin = child.TranslatePoint(default, ancestor)
            ?? throw new InvalidOperationException($"'{child.Name}' is not attached.");
        return new Rect(origin, child.Bounds.Size);
    }

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 6);
        Assert.Equal(expected.Y, actual.Y, precision: 6);
        Assert.Equal(expected.Width, actual.Width, precision: 6);
        Assert.Equal(expected.Height, actual.Height, precision: 6);
    }
}
