// Upstream contract: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 ShortNavigationBar.kt and
// ShortNavigationBarTest.kt. Auto and keyboard behavior are the D012 Avalonia adaptation.

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

public sealed class MaterialShortNavigationBarTests
{
    [AvaloniaFact]
    public void EqualWeightUsesTheCompleteWidthAnd80DipMinimumHeight()
    {
        var bar = Bar(ShortNavigationBarArrangement.EqualWeight, 4);
        ThemeTestHelper.ShowWindow(new Border { Width = 400, Child = bar });

        Assert.Equal(80, bar.Bounds.Height, precision: 10);
        Assert.All(bar.Items, item => Assert.Equal(100, item.Bounds.Width, precision: 10));
        Assert.Equal(6, IndicatorArea(bar.Items[0]).Y, precision: 10);
    }

    [AvaloniaFact]
    public void CenteredThreeItemArrangementUsesSixtyPercentOfTheWidth()
    {
        var bar = Bar(ShortNavigationBarArrangement.Centered, 3);
        ThemeTestHelper.ShowWindow(new Border { Width = 600, Child = bar });

        Assert.All(bar.Items, item => Assert.Equal(120, item.Bounds.Width, precision: 10));
        Assert.Equal(120, bar.Items[0].Bounds.X, precision: 10);
        Assert.Equal(360, bar.Items[2].Bounds.X, precision: 10);
    }

    [AvaloniaFact]
    public void AutoUsesThe600DipBoundaryWithoutReplacingItemsOrSelection()
    {
        var bar = Bar(ShortNavigationBarArrangement.Auto, 3);
        bar.SelectedIndex = 1;
        var selected = bar.Items[1];
        var host = new Border { Width = 599, Child = bar };
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(ShortNavigationBarArrangement.EqualWeight, bar.ResolvedArrangement);
        host.Width = 600;
        host.InvalidateMeasure();
        host.Measure(new Size(600, double.PositiveInfinity));
        host.Arrange(new Rect(0, 0, 600, host.DesiredSize.Height));

        Assert.Equal(ShortNavigationBarArrangement.Centered, bar.ResolvedArrangement);
        Assert.Same(selected, bar.Items[1]);
        Assert.Equal(1, bar.SelectedIndex);
        Assert.True(selected.IsChecked);
    }

    [AvaloniaFact]
    public void StartIconUsesThe40DipIndicatorAndHorizontalContent()
    {
        var item = new MaterialNavigationItem
        {
            Content = "Library",
            Icon = new Border { Width = 24, Height = 24 },
            IconPosition = NavigationItemIconPosition.Start,
            IsChecked = true
        };
        var bar = new MaterialShortNavigationBar
        {
            Arrangement = ShortNavigationBarArrangement.Centered,
            SelectedIndex = 0,
            VerticalAlignment = VerticalAlignment.Top,
            Items = { item }
        };
        ThemeTestHelper.ShowWindow(new Border { Width = 360, Child = bar });

        var indicator = IndicatorArea(item);
        Assert.Equal(40, indicator.Height, precision: 10);
        Assert.Equal(20, indicator.Y, precision: 10);
        Assert.True(indicator.Width > 56, $"Indicator width was {indicator.Width}.");
        Assert.Equal(item.Bounds.Width / 2, indicator.Center.X, precision: 10);
    }

    [AvaloniaFact]
    public void CheckedStateIsExclusiveAndCannotToggleTheSelectedItemOff()
    {
        var bar = Bar(ShortNavigationBarArrangement.EqualWeight, 3);
        ThemeTestHelper.ShowWindow(bar);

        bar.Items[1].IsChecked = true;
        Assert.Equal(1, bar.SelectedIndex);
        Assert.False(bar.Items[0].IsChecked);
        Assert.True(bar.Items[1].IsChecked);

        bar.Items[1].IsChecked = false;
        Assert.True(bar.Items[1].IsChecked);
    }

    [AvaloniaFact]
    public void ArrowKeysWrapSelectAndRespectRtlWhileTabRemainsUnclaimed()
    {
        var bar = Bar(ShortNavigationBarArrangement.EqualWeight, 3);
        bar.FlowDirection = FlowDirection.RightToLeft;
        var after = new Button { Content = "After" };
        var window = ThemeTestHelper.ShowWindow(new StackPanel { Children = { bar, after } });
        bar.Items[1].Focus();

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.True(bar.Items[0].IsFocused);
        Assert.Equal(0, bar.SelectedIndex);

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Assert.False(bar.Items[0].IsFocused);
        Assert.True(after.IsFocused || bar.Items.Any(static item => item.IsFocused));
    }

    [AvaloniaFact]
    public void AutomationExposesTabSelectionAndRejectsDisabledItems()
    {
        var bar = Bar(ShortNavigationBarArrangement.EqualWeight, 3);
        ThemeTestHelper.ShowWindow(bar);
        var peer = ControlAutomationPeer.CreatePeerForElement(bar);
        var selection = Assert.IsAssignableFrom<ISelectionProvider>(peer.GetProvider<ISelectionProvider>());
        var children = peer.GetChildren();
        var second = Assert.IsAssignableFrom<ISelectionItemProvider>(
            children[1].GetProvider<ISelectionItemProvider>());

        Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());
        Assert.All(children, child => Assert.Equal(AutomationControlType.TabItem, child.GetAutomationControlType()));
        second.Select();
        Assert.Equal(1, bar.SelectedIndex);
        Assert.Single(selection.GetSelection());
        Assert.Same(selection, second.SelectionContainer);

        bar.Items[1].IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(second.Select);
    }

    [AvaloniaFact]
    public void LongTopLabelCanIncreaseTheBarHeight()
    {
        var item = Item("One\nTwo\nThree\nFour");
        var bar = new MaterialShortNavigationBar
        {
            Arrangement = ShortNavigationBarArrangement.EqualWeight,
            Items = { item }
        };
        ThemeTestHelper.ShowWindow(new Border { Width = 160, Child = bar });

        Assert.True(bar.Bounds.Height > 80);
    }

    private static MaterialShortNavigationBar Bar(ShortNavigationBarArrangement arrangement, int count)
    {
        var bar = new MaterialShortNavigationBar
        {
            Arrangement = arrangement,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        for (var index = 0; index < count; index++)
            bar.Items.Add(Item($"Item {index + 1}"));
        return bar;
    }

    private static MaterialNavigationItem Item(string label) => new()
    {
        Content = label,
        Icon = new Border { Width = 24, Height = 24 }
    };

    private static Rect IndicatorArea(MaterialNavigationItem item) =>
        (Rect)(typeof(MaterialNavigationItem)
            .GetProperty("IndicatorArea", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(item) ?? throw new InvalidOperationException("IndicatorArea was unavailable."));
}
