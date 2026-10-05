using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationTokenConfigurationTests
{
    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void NavigationPressMapping_ConsumesTheEffectiveLayoutGeometry(int family)
    {
        Button item = family switch
        {
            0 => new NavigationRailItem { Icon = Icon(), Label = Label() },
            1 => new NavigationBarItem { Icon = Icon(), Label = Label() },
            2 => new ShortNavigationBarItem { Icon = Icon(), Label = Label() },
            _ => new WideNavigationRailItem { Icon = Icon(), Label = Label() }
        };
        item.Width = 120;
        using var clock = new MotionTestClockScope(); var window = Show(item);
        try
        {
            switch (family)
            {
                case 0:
                    var rail = Part<NavigationRailItemLayout>(item);
                    rail.ItemMinWidth = 140; rail.IndicatorWidth = 100;
                    break;
                case 1: Part<NavigationBarItemLayout>(item).IndicatorWidth = 100; break;
                case 2: Part<ShortNavigationBarItemLayout>(item).IndicatorWidth = 100; break;
                default: Part<WideNavigationRailItemLayout>(item).ActiveIndicatorWidth = 100; break;
            }
            Flush(window);
            var mapped = MaterialInteraction.GetOrCreate(item).PressPositionMapper!(new Point(60, 20));
            Assert.Equal(new Point(family == 0 ? 40 : 60 - (item.Bounds.Width - 100) / 2, family == 0 ? 20 : 8), mapped);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NavigationMotion_TracksSystemReferencesAndComponentOverrides()
    {
        var item = new NavigationRailItem { Icon = Icon(), Label = Label() };
        using var clock = new MotionTestClockScope(); var window = Show(item);
        try
        {
            var layout = Part<NavigationRailItemLayout>(item);
            var system = new SpringSpec(0.7, 321);
            item.Resources[Sys.Motion.Spring.EffectsDefault] = system; Flush(window);
            Assert.Equal(system, layout.EffectsAnimation);
            var component = new SpringSpec(1, 765);
            item.Resources[Comp.NavigationRail.EffectsAnimation] = component; Flush(window);
            Assert.Equal(component, layout.EffectsAnimation);
            item.Resources[Sys.Motion.Spring.EffectsDefault] = new SpringSpec(1, 999); Flush(window);
            Assert.Equal(component, layout.EffectsAnimation);
            item.Resources.Remove(Comp.NavigationRail.EffectsAnimation); Flush(window);
            Assert.Equal(999, layout.EffectsAnimation.Stiffness);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WideRail_ConfigBindingsRemainLiveWithoutReplacingHoistedState()
    {
        var state = new WideNavigationRailState();
        var item = new WideNavigationRailItem { Icon = Icon(), Label = Label() };
        var rail = new WideNavigationRail { State = state }; rail.Items.Add(item);
        using var clock = new MotionTestClockScope(); var window = Show(rail);
        try
        {
            var layout = Part<WideNavigationRailLayout>(rail);
            rail.Resources[Comp.WideNavigationRail.CollapsedContainerWidth] = 112d;
            rail.Resources[Comp.WideNavigationRail.HeaderSpace] = 31d;
            rail.Resources[Comp.WideNavigationRail.ItemHorizontalPadding] = 17d;
            var typography = new TextStyle { FontSize = 19 };
            rail.Resources[Comp.WideNavigationRail.TopIconLabelTextStyle] = typography;
            var spring = new SpringSpec(.6, 450);
            rail.Resources[Comp.WideNavigationRail.StateAnimation] = spring;
            Flush(window);
            Assert.Equal(112, layout.CollapsedContainerWidth);
            Assert.Equal(31, layout.HeaderSpace);
            Assert.Equal(17, Part<WideNavigationRailItemLayout>(item).ItemHorizontalPadding);
            Assert.Equal(typography, item.TopIconLabelTextStyle);
            Assert.Equal(spring, rail.StateAnimation);
            Assert.Same(state, rail.State);
            item.IndicatorPadding = new Thickness(7);
            rail.Resources[Comp.WideNavigationRail.ActiveIndicatorWidth] = 72d; Flush(window);
            Assert.Equal(new Thickness(7), Part<WideNavigationRailItemLayout>(item).IndicatorPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void SecondaryTabDefaults_DoNotOverrideApplicationStyles(bool native)
    {
        Control row = native ? new TabControl() : new TabRow();
        TabRowAppearance.SetVariant(row, TabRowVariant.Secondary);
        var foreground = native ? TabRowAppearance.ContentColorProperty : TemplatedControl.ForegroundProperty;
        var indicator = new FuncControlTemplate((_, _) => new Border { Height = 5 });
        var style = new Style { Setters = { new Setter(foreground, Brushes.Magenta), new Setter(TabRowAppearance.IndicatorProperty, indicator) } };
        row.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(row);
        try
        {
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(row.GetValue(foreground)).Color);
            Assert.Same(indicator, TabRowAppearance.GetIndicator(row));
            row.Resources[Comp.TabRow.SecondaryContentColor] = Colors.Green; Flush(window);
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(row.GetValue(foreground)).Color);
            row.SetValue(foreground, Brushes.Cyan); Flush(window);
            Assert.Equal(Colors.Cyan, Assert.IsAssignableFrom<ISolidColorBrush>(row.GetValue(foreground)).Color);
            row.ClearValue(foreground); Flush(window);
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(row.GetValue(foreground)).Color);
            row.Styles.Remove(style); Flush(window);
            Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(row.GetValue(foreground)).Color);
            row.Resources[Comp.TabRow.EdgePadding] = 37d; row.Resources[Comp.TabRow.MinTabWidth] = 105d; Flush(window);
            Assert.Equal(37, TabRowAppearance.GetEdgePadding(row));
            Assert.Equal(105, TabRowAppearance.GetMinTabWidth(row));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TabContainerHeights_FollowLiveConfigurationAndContentFacts()
    {
        var tab = new Tab { Icon = Icon(), Content = Label() };
        var row = new TabRow(); row.Items.Add(tab);
        using var clock = new MotionTestClockScope(); var window = Show(row);
        try
        {
            Assert.Equal(72, row.Bounds.Height);
            row.Resources[Comp.TabRow.LargeContainerHeight] = 108d; Flush(window);
            Assert.Equal(108, Part<TabHeader>(tab).LargeContainerHeight);
            Assert.Equal(108, row.Bounds.Height);
            tab.Icon = null; Flush(window); Assert.Equal(48, row.Bounds.Height);
            row.Resources[Comp.TabRow.ContainerHeight] = 60d; Flush(window); Assert.Equal(60, row.Bounds.Height);
            tab.Icon = Icon(); Flush(window); Assert.Equal(108, row.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FloatingToolbar_ConditionalDefaultsPreserveStylesLocalValuesAndClearValue()
    {
        var toolbar = new HorizontalFloatingToolbar { Content = new Border { Width = 80, Height = 48 } };
        var style = new Style { Setters = { new Setter(TemplatedControl.BackgroundProperty, Brushes.Magenta), new Setter(FloatingToolbar.ExpandedShadowElevationProperty, 7d) } };
        toolbar.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(toolbar);
        try
        {
            toolbar.Classes.Add("vibrant"); toolbar.FloatingActionButton = new Border(); Flush(window);
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(toolbar.Background).Color);
            Assert.Equal(7, toolbar.ExpandedShadowElevation);
            toolbar.ExpandedShadowElevation = 9; Flush(window); Assert.Equal(9, toolbar.ExpandedShadowElevation);
            toolbar.ClearValue(FloatingToolbar.ExpandedShadowElevationProperty); Flush(window); Assert.Equal(7, toolbar.ExpandedShadowElevation);
            toolbar.Styles.Remove(style); Flush(window); Assert.Equal(1, toolbar.ExpandedShadowElevation);
            toolbar.Resources[Comp.FloatingToolbar.ExpandedShadowElevationWithFab] = 4d; Flush(window);
            Assert.Equal(4, toolbar.ExpandedShadowElevation);
            toolbar.FloatingActionButton = null; Flush(window); Assert.Equal(0, toolbar.ExpandedShadowElevation);
            Assert.NotNull(toolbar.AnimationSpec);
            toolbar.Resources[Comp.FloatingToolbar.SideContentAnimation] = new SpringSpec(.9, 350); Flush(window);
            Assert.All(toolbar.GetVisualDescendants().OfType<FloatingToolbarVisibility>(), part => Assert.Equal(350, part.SizeAnimation.Stiffness));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Drawer_DefaultWidthAndMotionAreEffectiveConfiguration()
    {
        var sheet = new PermanentDrawerSheet { Content = new Border { Width = 20, Height = 20 } };
        var drawer = new DismissibleNavigationDrawer { DrawerContent = sheet, Content = new Border() };
        using var clock = new MotionTestClockScope(); var window = Show(drawer);
        try
        {
            drawer.Resources[Comp.NavigationDrawer.ContainerWidth] = 310d;
            drawer.Resources[Comp.NavigationDrawer.MinimumContainerWidth] = 260d;
            var spring = new SpringSpec(.8, 333); drawer.Resources[Comp.NavigationDrawer.OpenAnimation] = spring;
            Flush(window);
            Assert.Equal(310, sheet.ContainerWidth);
            Assert.Equal(310, Part<DrawerSheetLayout>(sheet).MaximumWidth);
            Assert.Equal(260, Part<DrawerSheetLayout>(sheet).MinimumWidth);
            Assert.Equal(spring, drawer.OpenAnimation);
        }
        finally { window.Close(); }
    }

    private static Border Icon() => new() { Width = 24, Height = 24 };
    private static Border Label() => new() { Width = 32, Height = 16 };
    private static T Part<T>(Control control) where T : Control => control.GetVisualDescendants().OfType<T>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 500, Height = 300, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new MaterialTheme()); window.Content = new Grid { Children = { control } }; window.Show(); Flush(window); return window;
    }
}
