using System.Reflection;
using Avalonia.Media;
using ComposeAnimation.Runtime;
using MaterialSpringSpec = Material3Expressive.Foundation.Motion.SpringSpec;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationMotionReadinessTests
{
    // An unresolved reference exercises missing configuration, rather than a
    // wrong-CLR-type resource rejected by Avalonia before the control can observe it.
    private static readonly TokenRef MissingAnimation = new(new MaterialTokenKey<Material3Expressive.Foundation.Motion.SpringSpec>("test.navigation.missing-motion"));
    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void ItemMotion_MissingConfigurationRestoresPendingTarget_AcrossReparent(int family)
    {
        Button item = family switch
        {
            0 => new NavigationRailItem { Icon = Box(24), Label = Box(16) },
            1 => new NavigationBarItem { Icon = Box(24), Label = Box(16) },
            2 => new ShortNavigationBarItem { Icon = Box(24), Label = Box(16) },
            _ => new WideNavigationRailItem { Icon = Box(24), Label = Box(16) }
        };
        MaterialTokenKey key = family switch
        {
            0 => Comp.NavigationRail.IndicatorSizeAnimation,
            1 => Comp.NavigationBar.IndicatorSizeAnimation,
            2 => Comp.ShortNavigationBar.IndicatorAnimation,
            _ => Comp.WideNavigationRail.ItemSpatialAnimation
        };
        void Select(bool value)
        {
            switch (item)
            {
                case NavigationRailItem x: x.IsSelected = value; break;
                case NavigationBarItem x: x.IsSelected = value; break;
                case ShortNavigationBarItem x: x.IsSelected = value; break;
                case WideNavigationRailItem x: x.IsSelected = value; break;
            }
        }
        float Progress() => family switch
        {
            0 => Part<NavigationRailItemLayout>(item).SizeProgress,
            1 => Part<NavigationBarItemLayout>(item).SizeProgress,
            2 => Part<ShortNavigationBarItemLayout>(item).SelectionProgress,
            _ => Part<WideNavigationRailItemLayout>(item).SelectionProgress
        };
        item.Resources[key] = MissingAnimation;
        using var clock = new MotionTestClockScope(); var host = new Grid { Children = { item } }; var window = Show(host);
        try
        {
            Select(true); Advance(clock, window, 5); Assert.Equal(0, Progress());
            item.Resources.Remove(key); Advance(clock, window); Assert.Equal(1, Progress());
            item.Resources[key] = MissingAnimation;
            host.Children.Remove(item); host.Children.Add(item); Flush(window);
            Select(false); Advance(clock, window, 5); Assert.Equal(1, Progress());
            item.Resources.Remove(key); Advance(clock, window); Assert.Equal(0, Progress());
            Assert.Null(MotionTestClockScope.Error(item));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WideState_WaitsForFirstValidConfiguration_WithoutReplacingStateOrQueuingActions()
    {
        var state = new WideNavigationRailState();
        var rail = new WideNavigationRail { State = state };
        rail.Resources[Comp.WideNavigationRail.StateAnimation] = MissingAnimation;
        using var clock = new MotionTestClockScope(); var window = Show(rail);
        try
        {
            Assert.Same(state, rail.State);
            var error = Assert.Throws<InvalidOperationException>(() => { _ = state.ExpandAsync(); });
            Assert.Contains("motion configuration is not ready", error.Message);
            Assert.Equal(WideNavigationRailValue.Collapsed, state.TargetValue);
            rail.Resources.Remove(Comp.WideNavigationRail.StateAnimation); Flush(window);
            var expand = state.ExpandAsync(); Advance(clock, window);
            Assert.True(expand.IsCompletedSuccessfully);
            Assert.Equal(WideNavigationRailValue.Expanded, state.CurrentValue);
            // The fixed upstream state captures its first valid spec for its lifetime.
            rail.Resources[Comp.WideNavigationRail.StateAnimation] = MissingAnimation;
            var collapse = state.CollapseAsync(); Advance(clock, window);
            Assert.True(collapse.IsCompletedSuccessfully);
            Assert.Equal(WideNavigationRailValue.Collapsed, state.CurrentValue);
            Assert.Same(state, rail.State); Assert.Null(MotionTestClockScope.Error(rail));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FloatingToolbar_MissingInitialAndReparentedSpecs_DoNotConstructInvalidChannels()
    {
        var toolbar = new HorizontalFloatingToolbar { Content = Box(40), LeadingContent = Box(32) };
        MaterialTokenKey[] keys = [Comp.FloatingToolbar.SideContentAnimation, Comp.FloatingToolbar.ExpansionAnimation, Comp.FloatingToolbar.ElevationAnimation, Comp.FloatingToolbar.BalancedPaddingAnimation];
        foreach (var key in keys) toolbar.Resources[key] = MissingAnimation;
        using var clock = new MotionTestClockScope(); var host = new Grid { Children = { toolbar } }; var window = Show(host);
        try
        {
            toolbar.Expanded = false; Advance(clock, window, 5);
            Assert.Equal(1, Part<FloatingToolbarLayout>(toolbar).ExpandedProgress);
            foreach (var key in keys) toolbar.Resources.Remove(key);
            Advance(clock, window); Assert.Equal(0, Part<FloatingToolbarLayout>(toolbar).ExpandedProgress);
            toolbar.Expanded = true; Advance(clock, window); Assert.Equal(1, Part<FloatingToolbarLayout>(toolbar).ExpandedProgress);
            foreach (var key in keys) toolbar.Resources[key] = MissingAnimation;
            host.Children.Remove(toolbar); host.Children.Add(toolbar); Flush(window);
            toolbar.Expanded = false; Advance(clock, window, 5);
            Assert.Equal(1, Part<FloatingToolbarLayout>(toolbar).ExpandedProgress);
            foreach (var key in keys) toolbar.Resources.Remove(key);
            Advance(clock, window); Assert.Equal(0, Part<FloatingToolbarLayout>(toolbar).ExpandedProgress);
            Assert.Null(MotionTestClockScope.Error(toolbar));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TabColorMotion_MissingSpecs_ReplaysSelectionWhenConfigurationReturns()
    {
        var tab = new Tab { Content = Box(24), SelectedContentColor = Avalonia.Media.Colors.Blue, UnselectedContentColor = Avalonia.Media.Colors.Red };
        var row = new TabRow(); row.Items.Add(tab);
        row.Resources[Comp.TabRow.SelectedContentAnimation] = MissingAnimation;
        row.Resources[Comp.TabRow.UnselectedContentAnimation] = MissingAnimation;
        using var clock = new MotionTestClockScope(); var window = Show(row);
        try
        {
            tab.IsSelected = true; Advance(clock, window, 5);
            Assert.Equal(Avalonia.Media.Colors.Red, Part<TabHeader>(tab).PaintedContentColor);
            row.Resources.Remove(Comp.TabRow.SelectedContentAnimation); row.Resources.Remove(Comp.TabRow.UnselectedContentAnimation);
            Advance(clock, window); Assert.Equal(Avalonia.Media.Colors.Blue, Part<TabHeader>(tab).PaintedContentColor);
            row.Resources[Comp.TabRow.SelectedContentAnimation] = MissingAnimation;
            tab.IsSelected = false; Advance(clock, window, 5);
            Assert.Equal(Avalonia.Media.Colors.Blue, Part<TabHeader>(tab).PaintedContentColor);
            row.Resources.Remove(Comp.TabRow.SelectedContentAnimation); Advance(clock, window);
            Assert.Equal(Avalonia.Media.Colors.Red, Part<TabHeader>(tab).PaintedContentColor);
            Assert.Null(MotionTestClockScope.Error(tab));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void TabMotionConfiguration_PrecedesNewTarget_AndDoesNotRestartSameTarget(bool recoverFromMissing)
    {
        var tab = new Tab { Content = Box(24), SelectedContentColor = Colors.Blue, UnselectedContentColor = Colors.Red };
        var row = new TabRow(); row.Items.Add(tab);
        row.Resources[Comp.TabRow.SelectedContentAnimation] = new MaterialSpringSpec(1, 75);
        row.Resources[Comp.TabRow.UnselectedContentAnimation] = new MaterialSpringSpec(1, 90);
        using var clock = new MotionTestClockScope(); var window = Show(row);
        try
        {
            Advance(clock, window, 3);
            var header = Part<TabHeader>(tab);
            var channel = Assert.IsType<Transition<bool>.TransitionAnimationState<Color>>(
                typeof(TabHeader).GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(header));
            if (recoverFromMissing)
            {
                row.Resources[Comp.TabRow.SelectedContentAnimation] = MissingAnimation;
                tab.IsSelected = true;
                Advance(clock, window, 2);
                Assert.Equal(Colors.Red, channel.TargetValue);
            }
            // Both changes are applied before the same next-frame target commit.
            row.Resources[Comp.TabRow.SelectedContentAnimation] = new MaterialSpringSpec(.65, 800);
            tab.IsSelected = true;
            Advance(clock, window, 1);
            var entering = Assert.IsType<SpringSpec<Color>>(channel.AnimationSpec);
            Assert.Equal(.65, entering.DampingRatio);
            Assert.Equal(800, entering.Stiffness);
            Assert.Equal(Colors.Blue, channel.TargetValue);
            Assert.False(channel.IsFinished);

            var running = channel.Animation;
            row.Resources[Comp.TabRow.SelectedContentAnimation] = new MaterialSpringSpec(1, 50);
            Advance(clock, window, 1);
            Assert.Same(running, channel.Animation);
            Assert.Equal(800, Assert.IsType<SpringSpec<Color>>(channel.AnimationSpec).Stiffness);

            row.Resources[Comp.TabRow.UnselectedContentAnimation] = new MaterialSpringSpec(1, 420);
            tab.IsSelected = false;
            Advance(clock, window, 1);
            Assert.Equal(420, Assert.IsType<SpringSpec<Color>>(channel.AnimationSpec).Stiffness);
            Assert.Equal(Colors.Red, channel.TargetValue);
            Assert.Null(MotionTestClockScope.Error(tab));
        }
        finally { window.Close(); }
    }

    private static Border Box(double size) => new() { Width = size, Height = size };
    private static T Part<T>(Control control) where T : Control => control.GetVisualDescendants().OfType<T>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Advance(MotionTestClockScope clock, Window window, int frames = 120)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 500, Height = 300, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new MaterialTheme()); window.Content = control; window.Show(); Flush(window); return window;
    }
}
