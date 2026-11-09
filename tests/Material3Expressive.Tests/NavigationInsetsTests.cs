using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationInsetsTests
{
    [AvaloniaTheory]
    [InlineData("bar", false)] [InlineData("bar", true)]
    [InlineData("short", false)] [InlineData("short", true)]
    [InlineData("rail", false)] [InlineData("rail", true)]
    [InlineData("wide", false)] [InlineData("wide", true)]
    [InlineData("expanded", false)] [InlineData("expanded", true)]
    public void ConsumedInsetsReachLogicalItemContentAndUpdatePhysicalPadding(string kind, bool rtl)
    {
        var insets = new Thickness(13, 7, 5, 11);
        var marker = new Border { Width = 24, Height = 24 };
        var nested = new MaterialWindowInsetsPadding { Insets = insets, Child = marker };
        ItemsControl nav = kind switch {
            "wide" or "expanded" => new WideNavigationRail { WindowInsets = insets,
                State = new WideNavigationRailState(kind == "expanded" ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed),
                Items = { new WideNavigationRailItem { Icon = nested } } },
            "rail" => new NavigationRail { WindowInsets = insets, Items = { new NavigationRailItem { Icon = nested } } },
            "short" => new ShortNavigationBar { WindowInsets = insets, Items = { new ShortNavigationBarItem { Icon = nested } } },
            _ => new NavigationBar { WindowInsets = insets, Items = { new NavigationBarItem { Icon = nested } } }
        };
        var root = new Border { Child = nav, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        using var clock = new MotionTestClockScope();
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = root; window.Show(); Flush(window);
        try
        {
            var padding = nav.GetVisualDescendants().OfType<MaterialWindowInsetsBorder>().Single();
            Assert.Equal(new Thickness(rtl ? 3 : 9, 4, rtl ? 9 : 3, 5), padding.Padding);
            Assert.Equal(insets, nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(new Size(24, 24), nested.Bounds.Size);
            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)); Flush(window);
            Assert.Equal(default, padding.Padding);
            Assert.Equal(new Thickness(30), nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); Flush(window);
            Assert.Equal(new Thickness(rtl ? 5 : 13, 7, rtl ? 13 : 5, 11), padding.Padding);
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaTheory]
    [InlineData(false, false, "actions")] [InlineData(false, true, "actions")]
    [InlineData(true, false, "actions")] [InlineData(true, true, "actions")]
    [InlineData(false, false, "fab")] [InlineData(false, true, "fab")]
    [InlineData(false, false, "custom")] [InlineData(false, true, "custom")]
    public void BottomBarsPublishConsumptionToTheirRealSlots(bool flexible, bool rtl, string slot)
    {
        var insets = new Thickness(13, 7, 5, 11);
        var marker = new Border { Width = 24, Height = 24 };
        var nested = new MaterialWindowInsetsPadding { Insets = insets, Child = marker };
        BottomAppBar bar = flexible ? new FlexibleBottomAppBar() : new BottomAppBar();
        bar.WindowInsets = insets;
        if (slot == "fab") bar.FloatingActionButton = nested;
        else if (slot == "custom") bar.CustomContent = nested;
        else bar.Items.Add(nested);
        var root = new Border { Child = bar, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        using var clock = new MotionTestClockScope();
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = root; window.Show(); Flush(window);
        try
        {
            Assert.Equal(new Thickness(9, 4, 3, 5), bar.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(insets, nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(new Size(24, 24), marker.Bounds.Size);
            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)); Flush(window);
            Assert.Equal(default, bar.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(new Thickness(30), nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); Flush(window);
            Assert.Equal(insets, bar.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
