using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ExtendedNavigationDensityTokenTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TabPaddingAndIndicatorMinimum_UpdateBothRowTemplates(bool native)
    {
        using var clock = new MotionTestClockScope();
        Control row = native
            ? new TabControl { Width = 100, Items = { new TabItem { Header = new Border { Width = 100, Height = 16 } } } }
            : new TabRow { Width = 100, Items = { new Tab { Content = new Border { Width = 100, Height = 16 } } } };
        var window = Show(row);
        try
        {
            Assert.Equal(68, TabRowAppearance.GetTabPositions(row)[0].ContentWidth);
            window.Resources[MaterialTokens.Comp.TabRow.TabHorizontalPadding] = 24d;
            window.UpdateLayout();
            Assert.Equal(52, TabRowAppearance.GetTabPositions(row)[0].ContentWidth);
            window.Resources[MaterialTokens.Comp.TabRow.PrimaryIndicatorMinWidth] = 60d;
            window.UpdateLayout();
            Assert.Equal(60, TabRowAppearance.GetTabPositions(row)[0].ContentWidth);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DrawerSpacing_ReusesConstrainedMeasuredGapsAfterLiveChange()
    {
        using var clock = new MotionTestClockScope();
        var item = new NavigationDrawerItem { Width = 70, Icon = new Border { Width = 20, Height = 20 },
            Content = new Border { Height = 16 }, Badge = new Border { Width = 10, Height = 10 } };
        var window = Show(item);
        try
        {
            var icon = item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Icon");
            Assert.Equal(16, icon.TranslatePoint(default, item)!.Value.X);
            window.Resources[MaterialTokens.Comp.NavigationDrawer.ItemHorizontalPadding] = new Thickness(4, 0, 6, 0);
            window.Resources[MaterialTokens.Comp.NavigationDrawer.ItemIconLabelSpace] = 7d;
            window.Resources[MaterialTokens.Comp.NavigationDrawer.ItemLabelBadgeSpace] = 9d;
            window.UpdateLayout();
            Assert.Equal(4, icon.TranslatePoint(default, item)!.Value.X);
            var label = item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_ContentPresenter");
            Assert.Equal(31, label.TranslatePoint(default, item)!.Value.X);
            Assert.Equal(14, label.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExpandedRailHeightAndSpacing_RetargetWhenResourcesChange()
    {
        using var clock = new MotionTestClockScope();
        var first = new WideNavigationRailItem { Label = "One", Icon = new Border { Width = 24, Height = 24 } };
        var second = new WideNavigationRailItem { Label = "Two", Icon = new Border { Width = 24, Height = 24 } };
        var rail = new WideNavigationRail { State = new(WideNavigationRailValue.Expanded), Items = { first, second } };
        var window = Show(rail);
        try
        {
            Assert.Equal(56, first.Bounds.Height);
            window.Resources[MaterialTokens.Comp.WideNavigationRail.ExpandedItemMinHeight] = 80d;
            window.Resources[MaterialTokens.Comp.WideNavigationRail.ExpandedItemVerticalSpace] = 10d;
            window.UpdateLayout();
            for (var i = 1; i <= 120; i++) { clock.SendFrame(TimeSpan.FromMilliseconds(i * 16)); window.UpdateLayout(); }
            Assert.Equal(80, first.Bounds.Height, 2);
            var firstY = first.TranslatePoint(default, rail)!.Value.Y;
            var secondY = second.TranslatePoint(default, rail)!.Value.Y;
            Assert.Equal(90, secondY - firstY, 2);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left;
        content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } };
        window.Show(); window.UpdateLayout(); return window;
    }
}

