using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class CommandMenuDensityTokenTests
{
    [AvaloniaFact]
    public void CommandBar_DynamicLayoutTokensReachMeasureAndArrange_AndPublicPaddingWins()
    {
        using var clock = new MotionTestClockScope();
        var command = new CommandBarButton { Icon = new Border { Width = 20, Height = 20 } };
        var bar = new CommandBar { PrimaryCommands = [command], IsDynamicOverflowEnabled = false };
        var window = DialogTests.Show(bar);
        try
        {
            bar.Theme = (ControlTheme)window.FindResource("MaterialCommandBarIconTheme")!;
            window.UpdateLayout();
            var layout = Part<CommandBarItemLayout>(command, "CommandLayout");
            Assert.Equal(48, layout.DesiredSize.Width);
            window.Resources[MaterialTokens.Comp.CommandBar.ItemMinWidth] = 92d;
            window.Resources[MaterialTokens.Comp.CommandBar.ItemMinHeight] = 72d;
            window.Resources[MaterialTokens.Comp.CommandBar.IndicatorHorizontalInset] = 20d;
            window.Resources[MaterialTokens.Comp.CommandBar.ContainerPadding] = new Thickness(11, 3);
            window.UpdateLayout();
            Assert.Equal(92, layout.DesiredSize.Width);
            Assert.Equal(72, layout.DesiredSize.Height);
            var ripple = Part<MaterialSurface>(command, "PART_IndicatorRipple");
            Assert.Equal(10, ripple.Bounds.Left);
            Assert.Equal(layout.Bounds.Width - 20, ripple.Bounds.Width);
            Assert.Equal(new Thickness(11, 3), bar.Padding);
            bar.Padding = new Thickness(7);
            window.Resources[MaterialTokens.Comp.CommandBar.ContainerPadding] = new Thickness(18);
            window.UpdateLayout();
            Assert.Equal(new Thickness(7), bar.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeMenu_DynamicSpacingPreservesSlotDirections(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Action", Icon = new Border { Width = 20, Height = 20 },
            InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.A), Items = { new MenuItem { Header = "Child" } } };
        var window = DialogTests.Show(item);
        try
        {
            item.Theme = (ControlTheme)window.FindResource(baseline ? "MaterialBaselineMenuItemTheme" : "MaterialVerticalMenuItemTheme")!;
            window.UpdateLayout();
            var key = baseline ? MaterialTokens.Comp.Menu.BaselineContentSpacing : MaterialTokens.Comp.Menu.VerticalContentSpacing;
            window.Resources[key] = 19d;
            window.UpdateLayout();
            var leading = Part<Control>(item, "PART_LeadingIcon");
            var header = Part<Avalonia.Controls.Presenters.ContentPresenter>(item, "PART_HeaderPresenter");
            Assert.Equal(19, header.TranslatePoint(default, item)!.Value.X -
                leading.TranslatePoint(new Point(leading.Bounds.Width, 0), item)!.Value.X);
            var trailing = Part<Panel>(item, "PART_Trailing");
            var visible = trailing.Children.Where(child => child.IsVisible).ToArray();
            Assert.Equal(2, visible.Length); // Native gesture and submenu arrow.
            Assert.Equal(19, visible[1].Bounds.Left - visible[0].Bounds.Right);
            Assert.True(trailing.TranslatePoint(default, item)!.Value.X -
                header.TranslatePoint(new Point(header.Bounds.Width, 0), item)!.Value.X >= 19);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExpressiveMenu_DynamicMarginsPreserveSupportingStateAndPublicSpacing()
    {
        using var clock = new MotionTestClockScope();
        var item = new DropdownMenuItem { Content = "Action", LeadingSpacing = 23 };
        var window = DialogTests.Show(item);
        try
        {
            item.Theme = (ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
            window.UpdateLayout();
            var chrome = Part<MenuItemChrome>(item, "PART_ItemChrome");
            window.Resources[MaterialTokens.Comp.Menu.ItemMargin] = new Thickness(9, 0);
            window.Resources[MaterialTokens.Comp.Menu.SupportingItemMargin] = new Thickness(9, 5);
            window.Resources[MaterialTokens.Comp.Menu.MinimumInteractiveSize] = 64d;
            window.Resources[MaterialTokens.Comp.Menu.VerticalContentSpacing] = 17d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(9, 0), chrome.Margin);
            Assert.Equal(64, chrome.MinimumInteractiveHeight);
            Assert.Equal(23, item.LeadingSpacing);
            Assert.Equal(17, item.TrailingSpacing);
            item.SupportingText = "Details";
            window.UpdateLayout();
            Assert.Equal(new Thickness(9, 5), chrome.Margin);
            item.SupportingText = null;
            window.UpdateLayout();
            Assert.Equal(new Thickness(9, 0), chrome.Margin);
        }
        finally { window.Close(); }
    }

    private static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
}
