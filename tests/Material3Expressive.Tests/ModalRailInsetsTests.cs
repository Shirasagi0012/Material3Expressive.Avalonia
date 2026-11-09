using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ModalRailInsetsTests
{
    private static readonly Thickness Insets = new(13, 7, 5, 11);

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ActualPresentationRoot_DeterminesConsumption(bool hide, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var nested = new MaterialWindowInsetsPadding { Insets = Insets, Child = new Border { Width = 24, Height = 24 } };
        var inlineHeader = new MaterialWindowInsetsPadding { Insets = Insets, Child = new Border { Width = 32, Height = 24 } };
        var modalHeader = new MaterialWindowInsetsPadding { Insets = Insets, Child = new Border { Width = 32, Height = 24 } };
        var item = new WideNavigationRailItem { Icon = nested, Label = "Page" };
        var rail = new ModalWideNavigationRail { WindowInsets = Insets, HideOnCollapse = hide, Header = inlineHeader,
            ExpandedHeader = modalHeader, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Items = { item } };
        var parent = new Border { Child = rail };
        MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(40));
        var window = DialogTests.Show(parent, 240);
        try
        {
            if (!hide)
            {
                Assert.Equal(default, Remaining(Part(window, "PART_InlineInsets")));
                Assert.Equal(new Thickness(40), Total(nested));
                Assert.Equal(default, Remaining(inlineHeader));
            }
            for (var opening = 0; opening < 2; opening++)
            {
                _ = rail.State.ExpandAsync(); Advance(clock, window);
                Assert.True(rail.IsModalVisible);
                Assert.Equal(Insets, Remaining(Part(window, "PART_ModalInsets")));
                Assert.Equal(Insets, Total(nested));
                Assert.Equal(default, Remaining(nested));
                Assert.Equal(default, Remaining(modalHeader));
                Assert.Equal(Insets, Total(modalHeader));
                Assert.Same(item, rail.NavigationItems.Single());
                // Changing the declaration ancestor cannot leak into the Dialog root.
                MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(60)); window.UpdateLayout();
                Assert.Equal(Insets, Total(nested));
                _ = rail.State.CollapseAsync(); Advance(clock, window);
                Assert.False(rail.IsModalVisible);
                if (!hide) Assert.Equal(new Thickness(60), Total(nested));
            }
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnModifiersReplayInDialog_AndRefreshPhysicalInsets(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var nested = new MaterialWindowInsetsPadding { Insets = Insets, Child = new Border { Width = 24, Height = 24 } };
        var rail = new ModalWideNavigationRail { Width = 220, WindowInsets = Insets, State = new(WideNavigationRailValue.Expanded),
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Items = { new WideNavigationRailItem { Icon = nested } } };
        MaterialWindowInsets.SetConsumePadding(rail, new Thickness(3, 2, 9, 1));
        var parent = new Border { Child = rail };
        MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(40));
        var window = DialogTests.Show(parent, 240);
        try
        {
            var modal = Part(window, "PART_ModalInsets");
            Assert.Equal(rtl ? new Thickness(4, 5, 2, 10) : new Thickness(10, 5, 0, 10), Remaining(modal));
            Assert.Equal(rtl ? Insets : new Thickness(13, 7, 9, 11), Total(nested));
            Assert.Equal(220, window.GetVisualDescendants().Single(c => c.Name == "PART_ModalSurface").Bounds.Width);
            MaterialWindowInsets.SetConsumeInsets(rail, new Thickness(30)); window.UpdateLayout();
            Assert.Equal(default, Remaining(modal)); Assert.Equal(new Thickness(30), Total(nested));
            rail.WindowInsets = new Thickness(36, 34, 32, 38); window.UpdateLayout();
            Assert.Equal(new Thickness(6, 4, 2, 8), Remaining(modal));
            Assert.Equal(rail.WindowInsets, Total(nested));
            window.Content = null; window.UpdateLayout(); window.Content = parent; Advance(clock, window);
            Assert.True(rail.IsModalVisible);
            Assert.Equal(rail.WindowInsets, Total(nested));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void GeneratedHeaderAndItems_KeepIdentityAndAcquireCurrentScope()
    {
        using var clock = new MotionTestClockScope();
        var created = new List<MaterialWindowInsetsPadding>();
        var rail = new ModalWideNavigationRail { WindowInsets = Insets, Header = "Header", State = new(WideNavigationRailValue.Expanded) };
        rail.HeaderTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            var content = new MaterialWindowInsetsPadding { Insets = Insets, Child = new Border { Width = 24, Height = 24 } };
            created.Add(content); return content;
        });
        var parent = new Border { Child = rail }; MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(40));
        var window = DialogTests.Show(parent, 240);
        try
        {
            Assert.Equal(2, created.Count);
            var modalHeader = created.Single(c => Total(c) == Insets);
            var item = new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 } };
            rail.Items.Add(item); window.UpdateLayout();
            Assert.Equal(Insets, Total(item));
            rail.WindowInsets = new Thickness(18); window.UpdateLayout();
            Assert.Equal(2, created.Count); Assert.Equal(new Thickness(18), Total(modalHeader));
            rail.Items.Remove(item); window.UpdateLayout();
            MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(60));
            Assert.Equal(default, Total(item));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachedContent_ReparentedWithoutOldOwnerLayout_DropsFormerScope(bool expanded)
    {
        using var clock = new MotionTestClockScope();
        var inlineHeader = new Border { Width = 24, Height = 24 };
        var modalHeader = new Border { Width = 24, Height = 24 };
        var item = new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 } };
        var rail = new ModalWideNavigationRail { WindowInsets = Insets, Header = inlineHeader, ExpandedHeader = modalHeader,
            State = new(expanded ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed), Items = { item } };
        var parent = new Border { Child = rail }; MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(40));
        var window = DialogTests.Show(parent, 240);
        try
        {
            window.Content = null;
            rail.Items.Remove(item); rail.Header = null; rail.ExpandedHeader = null;
            var replacement = new StackPanel { Children = { item, inlineHeader, modalHeader } };
            MaterialWindowInsets.SetConsumeInsets(replacement, new Thickness(4));
            window.Content = replacement; window.UpdateLayout();
            Assert.Equal(new Thickness(4), Total(item));
            Assert.Equal(new Thickness(4), Total(inlineHeader));
            Assert.Equal(new Thickness(4), Total(modalHeader));
            MaterialWindowInsets.SetConsumeInsets(parent, new Thickness(70));
            Assert.Equal(new Thickness(4), Total(item));
            Assert.Equal(new Thickness(4), Total(inlineHeader));
            Assert.Equal(new Thickness(4), Total(modalHeader));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClosedHiddenContent_ReusedBeforeOldLayout_DropsFormerScope()
    {
        using var clock = new MotionTestClockScope();
        var header = new Border { Width = 24, Height = 24 };
        var item = new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 } };
        var rail = new ModalWideNavigationRail { HideOnCollapse = true, WindowInsets = Insets, ExpandedHeader = header,
            State = new(WideNavigationRailValue.Expanded), Items = { item } };
        var host = new Grid { Children = { rail } };
        var window = DialogTests.Show(host, 240);
        try
        {
            _ = rail.State.CollapseAsync(); Advance(clock, window);
            rail.Items.Remove(item); rail.ExpandedHeader = null;
            var replacement = new StackPanel { Children = { item, header } };
            MaterialWindowInsets.SetConsumeInsets(replacement, new Thickness(4));
            host.Children.Add(replacement); window.UpdateLayout();
            Assert.Equal(new Thickness(4), Total(item)); Assert.Equal(new Thickness(4), Total(header));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DensityChange_ReRoundsCallerConsumptionInBothRoots()
    {
        using var clock = new MotionTestClockScope();
        var rail = new ModalWideNavigationRail { WindowInsets = new Thickness(10), Items = { new WideNavigationRailItem { Label = "Page" } } };
        MaterialWindowInsets.SetConsumePadding(rail, new Thickness(.4));
        var window = DialogTests.Show(rail, 240);
        try
        {
            Assert.Equal(new Thickness(10), Remaining(Part(window, "PART_InlineInsets")));
            window.SetRenderScaling(2); window.UpdateLayout();
            Assert.Equal(new Thickness(9.5), Remaining(Part(window, "PART_InlineInsets")));
            _ = rail.State.ExpandAsync(); Advance(clock, window);
            Assert.Equal(new Thickness(9.5), Remaining(Part(window, "PART_ModalInsets")));
        }
        finally { window.Close(); }
    }

    private static Thickness Total(Control control) => control.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
    private static Thickness Remaining(Control control) => control.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty);
    private static MaterialWindowInsetsPadding Part(Control root, string name) => root.GetVisualDescendants().OfType<MaterialWindowInsetsPadding>().Single(c => c.Name == name);
    private static void Advance(MotionTestClockScope clock, Window window)
    {
        for (var i = 0; i < 80; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
    }
}
