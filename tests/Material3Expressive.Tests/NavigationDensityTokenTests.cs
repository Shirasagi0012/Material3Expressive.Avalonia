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

public class NavigationDensityTokenTests
{
    [AvaloniaFact]
    public void NavigationBar_ItemSpacingResource_ReallocatesAndRepositionsItems()
    {
        var bar = new NavigationBar();
        var first = new NavigationBarItem { Icon = Icon() };
        var second = new NavigationBarItem { Icon = Icon() };
        bar.Items.Add(first);
        bar.Items.Add(second);
        var window = Show(bar);
        try
        {
            Assert.Equal(116, first.Bounds.Width);
            window.Resources[MaterialTokens.Comp.NavigationBar.ItemSpacing] = 20d;
            window.UpdateLayout();
            Assert.Equal(110, first.Bounds.Width);
            Assert.Equal(130, second.TranslatePoint(default, bar)!.Value.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NavigationRail_ContainerHeaderAndItemSpacing_UpdateIndependently()
    {
        var rail = new NavigationRail { Header = new Border { Width = 48, Height = 40 } };
        var first = new NavigationRailItem { Icon = Icon() };
        var second = new NavigationRailItem { Icon = Icon() };
        rail.Items.Add(first);
        rail.Items.Add(second);
        var window = Show(rail);
        try
        {
            Assert.Equal(60, first.TranslatePoint(default, rail)!.Value.Y);
            window.Resources[MaterialTokens.Comp.NavigationRail.ItemSpacing] = 12d;
            window.UpdateLayout();
            Assert.Equal(60, first.TranslatePoint(default, rail)!.Value.Y);
            Assert.Equal(128, second.TranslatePoint(default, rail)!.Value.Y);
            window.Resources[MaterialTokens.Comp.NavigationRail.VerticalPadding] = 10d;
            window.Resources[MaterialTokens.Comp.NavigationRail.HeaderItemSpacing] = 24d;
            window.UpdateLayout();
            Assert.Equal(74, first.TranslatePoint(default, rail)!.Value.Y);
            Assert.Equal(142, second.TranslatePoint(default, rail)!.Value.Y);
            rail.Header = null;
            window.UpdateLayout();
            Assert.Equal(10, first.TranslatePoint(default, rail)!.Value.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShortNavigationBar_MinimumTokens_UpdateCenteredIntrinsicAllocation()
    {
        var bar = new ShortNavigationBar { Arrangement = ShortNavigationBarArrangement.Centered };
        var item = new ShortNavigationBarItem { Icon = Icon() };
        bar.Items.Add(item);
        bar.Items.Add(new ShortNavigationBarItem { Icon = Icon() });
        var window = Show(bar);
        try
        {
            Assert.Equal(60, item.Bounds.Width);
            window.Resources[MaterialTokens.Comp.ShortNavigationBar.ItemMinWidth] = 80d;
            window.Resources[MaterialTokens.Comp.ShortNavigationBar.ItemMinHeight] = 80d;
            window.UpdateLayout();
            Assert.Equal(new Size(80, 80), item.Bounds.Size);
            Assert.Equal(40, item.TranslatePoint(default, bar)!.Value.X);
            Assert.Equal(80, bar.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShortNavigationBar_TopSpacing_UpdatesIntrinsicHeightAndLeavesStartSpacingIndependent()
    {
        var bar = new ShortNavigationBar();
        var item = new ShortNavigationBarItem { Icon = Icon(), Label = new Border { Width = 32, Height = 16 } };
        bar.Items.Add(item);
        var window = Show(bar);
        try
        {
            Assert.Equal(64, bar.Bounds.Height);
            window.Resources[MaterialTokens.Comp.ShortNavigationBar.TopIndicatorLabelSpacing] = 16d;
            window.Resources[MaterialTokens.Comp.ShortNavigationBar.IconLabelSpace] = 20d;
            window.UpdateLayout();
            Assert.Equal(76, bar.Bounds.Height);
            var icon = Part(item, "PART_Icon");
            var label = Part(item, "PART_Label");
            Assert.Equal(44, label.TranslatePoint(default, item)!.Value.Y - icon.TranslatePoint(default, item)!.Value.Y);
            item.IconPosition = NavigationItemIconPosition.Start;
            window.UpdateLayout();
            Assert.Equal(44, label.TranslatePoint(default, item)!.Value.X - icon.TranslatePoint(default, item)!.Value.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NavigationBar_UnboundedPaddingResource_UpdatesDesiredWidth()
    {
        var item = new NavigationBarItem { Icon = Icon() };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { item } };
        var window = Show(row);
        try
        {
            Assert.Equal(112, item.Bounds.Width);
            window.Resources[MaterialTokens.Comp.NavigationBar.UnboundedItemHorizontalPadding] = 60d;
            window.UpdateLayout();
            Assert.Equal(144, item.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndicatorLabelSpacing_UpdatesItemMeasurementAndPlacement(bool rail)
    {
        Control item = rail
            ? new NavigationRailItem { Icon = Icon(), Label = new Border { Width = 32, Height = 48 } }
            : new NavigationBarItem { Width = 112, Icon = Icon(), Label = new Border { Width = 32, Height = 48 } };
        var window = Show(item);
        try
        {
            Assert.Equal(88, item.Bounds.Height);
            window.Resources[rail ? MaterialTokens.Comp.NavigationRail.IndicatorLabelSpacing : MaterialTokens.Comp.NavigationBar.IndicatorLabelSpacing] = 12d;
            window.UpdateLayout();
            Assert.Equal(96, item.Bounds.Height);
            Assert.Equal(40, Part(item, "PART_Label").TranslatePoint(default, item)!.Value.Y - Part(item, "PART_Icon").TranslatePoint(default, item)!.Value.Y);
        }
        finally { window.Close(); }
    }

    private static Border Icon() => new() { Width = 24, Height = 24 };
    private static ContentPresenter Part(Control item, string name) => item.GetVisualDescendants().OfType<ContentPresenter>().Single(part => part.Name == name);

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 240, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } };
        window.Show();
        window.UpdateLayout();
        return window;
    }
}
