using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NativeMenuPrecisionTests
{
    [AvaloniaFact]
    public void NativeFlyoutAndSubmenuInheritLivePrecisionAndReopenWithCurrentDefaults()
    {
        using var clock = new MotionTestClockScope();
        var anchor = new Border { Width = 80, Height = 40 };
        var child = new MenuItem { Header = new Border { Width = 48, Height = 16 } };
        var item = new MenuItem { Header = new Border { Width = 48, Height = 16 }, Items = { child } };
        var flyout = new MenuFlyout { Items = { item } };
        var window = DialogTests.Show(anchor);
        flyout.Popup.ShouldUseOverlayLayer = true;
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        try
        {
            MaterialPrecisionPointer.SetIsEnabled(anchor, true);
            flyout.ShowAt(anchor); Flush();
            Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
            item.IsSubMenuOpen = true; Flush();
            Assert.Equal(new Thickness(16, 12, 10, 12), child.Padding);
            MaterialPrecisionPointer.SetIsEnabled(anchor, false); Flush();
            Assert.Equal(new Thickness(12), item.Padding); Assert.Equal(new Thickness(12), child.Padding);
            flyout.Hide(); Flush(); MaterialPrecisionPointer.SetIsEnabled(anchor, true);
            flyout.ShowAt(anchor); Flush();
            Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
        }
        finally { flyout.Hide(); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void InheritedPrecisionUpdatesNativePaddingSpacingAndTrailingHint(bool rtl)
    {
        var tail = new Border { Width = 8, Height = 12 };
        var item = new MenuItem { Header = new Border { Width = 48, Height = 16 }, Icon = new Border { Width = 20, Height = 20 },
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MenuAppearance.SetTrailingContent(item, tail);
        item.Items.Add(new MenuItem { Header = "Child" });
        var parent = new StackPanel { Children = { item } };
        var window = DialogTests.Show(parent);
        item.Theme = (ControlTheme)window.FindResource("MaterialVerticalMenuItemTheme")!; window.UpdateLayout();
        try
        {
            var rows = item.GetVisualDescendants().OfType<NativeMenuRow>().ToArray();
            var slot = item.GetVisualDescendants().OfType<DropdownMenuSlotPresenter>().Single(p => p.Name == "PART_TrailingContentPresenter");
            var chevron = item.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Chevron");
            Assert.Equal(new Thickness(12), item.Padding);
            Assert.All(rows, row => Assert.Equal(8, row.Spacing));
            Assert.Equal(20, MaterialSymbol.GetDefaultSize(tail));
            MaterialPrecisionPointer.SetIsEnabled(parent, true); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
            Assert.All(rows, row => Assert.Equal(12, row.Spacing));
            Assert.Equal(24, MaterialSymbol.GetDefaultSize(tail)); Assert.Equal(24, chevron.Width);
            Assert.Equal(20, slot.PreferredMinWidth); Assert.Equal(new Size(8, 12), tail.Bounds.Size);
            MaterialPrecisionPointer.SetIsEnabled(item, false); window.UpdateLayout();
            Assert.Equal(new Thickness(12), item.Padding); Assert.Equal(20, chevron.Width);
            item.ClearValue(MaterialPrecisionPointer.IsEnabledProperty); window.UpdateLayout();
            Assert.Equal(24, chevron.Width);
            parent.Children.Clear(); parent.Children.Add(item); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
            MaterialPrecisionPointer.SetIsEnabled(parent, false); window.UpdateLayout();
            Assert.Equal(new Thickness(12), item.Padding); Assert.Equal(20, MaterialSymbol.GetDefaultSize(tail));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeCallerPaddingArrangementAndIconSizeRemainAuthoritative()
    {
        var tail = new MaterialSymbol { Width = 31, Height = 29 };
        var item = new MenuItem { Header = new Border { Width = 48, Height = 16 }, Icon = new Border { Width = 20, Height = 20 } };
        MenuAppearance.SetTrailingContent(item, tail);
        MenuAppearance.SetHorizontalArrangement(item, MaterialHorizontalArrangement.SpacedBy(3));
        var window = DialogTests.Show(item);
        item.Theme = (ControlTheme)window.FindResource("MaterialVerticalMenuItemTheme")!;
        item.Styles.Add(new Style(s => s.OfType<MenuItem>()) { Setters = { new Setter(MenuItem.PaddingProperty, new Thickness(5)) } });
        try
        {
            MaterialPrecisionPointer.SetIsEnabled(window, true);
            window.Resources[MaterialTokens.Comp.Menu.PrecisionContentSpacing] = 14d;
            window.Resources[MaterialTokens.Comp.Menu.PrecisionTrailingIconSize] = 28d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(5), item.Padding); Assert.Equal(28, MaterialSymbol.GetDefaultSize(tail));
            Assert.Equal(new Size(31, 29), tail.Bounds.Size);
            var row = item.GetVisualDescendants().OfType<NativeMenuRow>().First();
            Assert.Equal(14, row.Spacing); Assert.Equal(3, row.HorizontalArrangement!.Spacing);
            item.Styles.Clear(); window.UpdateLayout(); Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
            item.Padding = new Thickness(2); MaterialPrecisionPointer.SetIsEnabled(window, false); window.UpdateLayout();
            Assert.Equal(new Thickness(2), item.Padding); Assert.Equal(20, MaterialSymbol.GetDefaultSize(tail));
            Assert.Equal(new Size(31, 29), tail.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("ordinary")] [InlineData("checkable")] [InlineData("selectable")]
    public void IndependentExpressiveTrailingHintTracksPrecisionButSlotMinimumStaysTwenty(string kind)
    {
        DropdownMenuItem item = kind switch { "checkable" => new CheckableDropdownMenuItem(), "selectable" => new SelectableDropdownMenuItem(), _ => new() };
        var tail = new MaterialSymbol();
        item.Content = new Border { Width = 48, Height = 16 }; item.TrailingContent = tail;
        var window = DialogTests.Show(item);
        if (kind == "ordinary") item.Theme = (ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
        window.UpdateLayout();
        try
        {
            var slot = item.GetVisualDescendants().OfType<DropdownMenuSlotPresenter>().Single(p => p.Name == "PART_Trailing");
            Assert.Equal(20, MaterialSymbol.GetDefaultSize(tail)); Assert.Equal(20, slot.PreferredMinWidth);
            MaterialPrecisionPointer.SetIsEnabled(window, true); window.UpdateLayout();
            Assert.Equal(24, MaterialSymbol.GetDefaultSize(tail)); Assert.Equal(20, slot.PreferredMinWidth);
            Assert.Equal(new Size(24, 24), tail.Bounds.Size);
            window.Resources[MaterialTokens.Comp.Menu.PrecisionTrailingIconSize] = 26d; window.UpdateLayout();
            Assert.Equal(new Size(26, 26), tail.Bounds.Size);
            MaterialPrecisionPointer.SetIsEnabled(window, false); window.UpdateLayout();
            Assert.Equal(new Size(20, 20), tail.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ConstrainedExpressiveLabelConsumesRemainingWidthBeforeTrailingSpacing()
    {
        var label = new Border { Width = 48, Height = 16 };
        var item = new SelectableDropdownMenuItem { Content = label, LeadingIcon = new Border { Width = 20, Height = 20 },
            TrailingContent = new Border { Width = 24, Height = 24 } };
        var parent = new Grid { Width = 90, Children = { item } };
        MaterialPrecisionPointer.SetIsEnabled(parent, true);
        var window = DialogTests.Show(parent);
        try
        {
            var layout = item.GetVisualDescendants().OfType<DropdownMenuItemLayout>().Single();
            Assert.Equal(24, layout.Children[1].Bounds.Width);
            Assert.Equal(0, layout.Children[2].Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("MaterialBaselineMenuItemTheme")]
    [InlineData("MaterialMenuBarItemTheme")]
    public void BaselineAndMenuBarKeepTheirOwnMetrics(string theme)
    {
        var item = new MenuItem { Header = new Border { Width = 48, Height = 16 } };
        item.Items.Add(new MenuItem { Header = "Child" });
        var window = DialogTests.Show(item);
        item.Theme = (ControlTheme)window.FindResource(theme)!; window.UpdateLayout();
        try
        {
            MaterialPrecisionPointer.SetIsEnabled(window, true); window.UpdateLayout();
            Assert.Equal(new Thickness(12, 0), item.Padding);
            if (theme == "MaterialBaselineMenuItemTheme")
                Assert.Equal(24, item.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Chevron").Width);
            else Assert.All(item.GetVisualDescendants().OfType<NativeMenuRow>(), row => Assert.Equal(8, row.Spacing));
        }
        finally { window.Close(); }
    }
}
