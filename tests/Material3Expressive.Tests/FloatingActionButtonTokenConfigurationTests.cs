using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

// Geometry-only content keeps these configuration/lifecycle checks independent of fonts.
public class FloatingActionButtonTokenConfigurationTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SizeDefaults_StayBelowStyleBindingAndLocalValue(bool extended)
    {
        FloatingActionButton fab = extended
            ? new ExtendedFloatingActionButton { Icon = Icon(), Content = Label() }
            : new FloatingActionButton { Icon = Icon() };
        var style = new Style(s => s.Is<FloatingActionButton>())
        {
            Setters = { new Setter(Button.CornerRadiusProperty, new CornerRadius(7)) }
        };
        using var clock = new MotionTestClockScope();
        var window = Show(fab);
        try
        {
            window.Styles.Add(style);
            fab.Size = FloatingActionButtonSize.Large;
            Flush(window);
            Assert.Equal(new CornerRadius(7), fab.CornerRadius);
            Assert.Equal(fab.CornerRadius, Part<MaterialSurface>(fab).CornerRadius);
            fab.CornerRadius = new CornerRadius(9);
            fab.Resources[Sys.Shape.ExtraLarge] = new CornerRadius(25);
            Flush(window);
            Assert.Equal(new CornerRadius(9), fab.CornerRadius);
            fab.ClearValue(Button.CornerRadiusProperty);
            Assert.Equal(new CornerRadius(7), fab.CornerRadius);
            window.Styles.Remove(style);
            Flush(window);
            Assert.Equal(new CornerRadius(25), fab.CornerRadius);

            var source = new Border { CornerRadius = new CornerRadius(11) };
            using (fab.Bind(Button.CornerRadiusProperty, new Binding(nameof(Border.CornerRadius)) { Source = source }))
            {
                fab.Size = FloatingActionButtonSize.Medium;
                fab.Resources[Sys.Shape.LargeIncreased] = new CornerRadius(22);
                source.CornerRadius = new CornerRadius(13);
                Flush(window);
                Assert.Equal(new CornerRadius(13), fab.CornerRadius);
            }
            Flush(window);
            Assert.Equal(new CornerRadius(22), fab.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExtendedTypography_SizeSelectionDoesNotOverrideAnApplicationStyle()
    {
        var textStyle = new TextStyle { FontSize = 17 };
        var fab = new ExtendedFloatingActionButton { Icon = Icon(), Content = Label() };
        var style = new Style(s => s.OfType<ExtendedFloatingActionButton>())
        {
            Setters = { new Setter(MaterialText.DefaultStyleProperty, textStyle) }
        };
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            window.Styles.Add(style);
            fab.Size = FloatingActionButtonSize.Large;
            Flush(window);
            Assert.Equal(textStyle, MaterialText.GetDefaultStyle(fab));
            window.Styles.Remove(style);
            var tokenStyle = new TextStyle { FontSize = 29 };
            fab.Resources[Comp.ExtendedFabLarge.LabelTextStyle] = tokenStyle;
            Flush(window);
            Assert.Equal(tokenStyle, MaterialText.GetDefaultStyle(fab));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Toggle_DefaultFunctionsConsumeTokenReferences_AndPreserveWholeFunctionOverrides()
    {
        var toggle = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Medium, Icon = Icon() };
        Func<float, double> styled = p => 74 + p * 3;
        Func<float, double> local = p => 70 + p * 2;
        var style = new Style(s => s.OfType<ToggleFloatingActionButton>())
        {
            Setters = { new Setter(ToggleFloatingActionButton.ContainerSizeProperty, styled) }
        };
        using var clock = new MotionTestClockScope(); var window = Show(toggle);
        try
        {
            toggle.Resources[Comp.FabMedium.ContainerHeight] = 88d;
            toggle.Resources[Comp.FabMenu.CloseButtonSize] = 64d;
            toggle.Resources[Comp.ToggleFab.MediumInitialCornerRadius] = 23d;
            Flush(window);
            Assert.Equal(88, toggle.ContainerSize(0));
            Assert.Equal(64, toggle.ContainerSize(1));
            Assert.Equal(23, toggle.ContainerCornerRadius(0));
            Assert.Equal(32, toggle.ContainerCornerRadius(1));
            window.Styles.Add(style);
            toggle.Size = FloatingActionButtonSize.Large;
            Flush(window);
            Assert.Same(styled, toggle.ContainerSize);
            toggle.ContainerSize = local;
            toggle.Resources[Comp.FabLarge.ContainerHeight] = 104d;
            Flush(window);
            Assert.Same(local, toggle.ContainerSize);
            toggle.ClearValue(ToggleFloatingActionButton.ContainerSizeProperty);
            Assert.Same(styled, toggle.ContainerSize);
            window.Styles.Remove(style);
            Flush(window);
            Assert.Equal(104, toggle.ContainerSize(0));
            Assert.Equal(64, toggle.ContainerSize(1));

            Func<float, Color> color = _ => Colors.Coral;
            Func<float, double> radius = _ => 9;
            toggle.ContainerColor = color;
            toggle.ContainerCornerRadius = radius;
            toggle.IsChecked = true;
            Frames(clock, window, 70);
            Assert.Same(color, toggle.ContainerColor);
            Assert.Same(radius, toggle.ContainerCornerRadius);
            Assert.Equal(Colors.Coral, Assert.IsAssignableFrom<ISolidColorBrush>(Part<MaterialSurface>(toggle).Background).Color);
            Assert.Equal(new CornerRadius(9), Part<MaterialSurface>(toggle).CornerRadius);
            Assert.Equal(64, Part<MaterialSurface>(toggle).Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Toggle_LocalBindingIsAnEffectiveFunction_AndDisposalRestoresCurrentVariant()
    {
        var toggle = new ToggleFloatingActionButton { Icon = Icon() };
        Func<float, double> first = _ => 71;
        Func<float, double> second = _ => 73;
        var source = new ContentControl { Content = first };
        using var clock = new MotionTestClockScope(); var window = Show(toggle);
        try
        {
            using (toggle.Bind(ToggleFloatingActionButton.ContainerSizeProperty,
                       new Binding(nameof(ContentControl.Content)) { Source = source }))
            {
                toggle.Size = FloatingActionButtonSize.Medium;
                toggle.Resources[Comp.FabMedium.ContainerHeight] = 90d;
                Flush(window);
                Assert.Same(first, toggle.ContainerSize);
                source.Content = second;
                Flush(window);
                Assert.Same(second, toggle.ContainerSize);
                Assert.Equal(73, toggle.Bounds.Width);
            }
            Flush(window);
            Assert.Equal(90, toggle.ContainerSize(0));
            Assert.Equal(90, toggle.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ReparentingAndThemeReplacement_RecomposeFunctionsFromTheCurrentScope()
    {
        var toggle = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Medium, Icon = Icon() };
        var first = new Panel { Children = { toggle } };
        var second = new Panel();
        first.Resources[Comp.FabMedium.ContainerHeight] = 82d;
        second.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
        {
            [Comp.FabMedium.ContainerHeight] = 94d
        };
        second.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [Comp.FabMedium.ContainerHeight] = 102d,
            [Comp.ToggleFab.InitialContainerColor] = Colors.Aqua
        };
        using var clock = new MotionTestClockScope(); var window = Show(new Panel { Children = { first, second } });
        try
        {
            Assert.Equal(82, toggle.ContainerSize(0));
            first.Children.Remove(toggle);
            Flush(window);
            second.Children.Add(toggle);
            Flush(window);
            Assert.Equal(94, toggle.ContainerSize(0));
            first.Resources[Comp.FabMedium.ContainerHeight] = 76d;
            Flush(window);
            Assert.Equal(94, toggle.ContainerSize(0));
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Flush(window);
            Assert.Equal(102, toggle.ContainerSize(0));
            var darkColor = toggle.ContainerColor(0);
            Assert.Equal(darkColor, Assert.IsAssignableFrom<ISolidColorBrush>(Part<MaterialSurface>(toggle).Background).Color);
            second.Resources = new ResourceDictionary { [Comp.FabMedium.ContainerHeight] = 86d };
            Flush(window);
            Assert.Equal(86, toggle.ContainerSize(0));
            Assert.NotEqual(darkColor, toggle.ContainerColor(0));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DynamicElevation_IsAWholeValue_AndExplicitConfigurationSurvivesTokenUpdates()
    {
        var fab = new FloatingActionButton { Icon = Icon() };
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            fab.Resources[Sys.Elevation.Level3] = 10d;
            fab.Resources[Sys.Elevation.Level4] = 12d;
            Flush(window);
            Assert.Equal(new FloatingActionButtonElevation(10, 10, 10, 12), fab.Elevation);
            var custom = new FloatingActionButtonElevation(1, 2, 3, 4);
            fab.Elevation = custom;
            fab.Resources[Sys.Elevation.Level3] = 14d;
            Frames(clock, window, 2);
            Assert.Same(custom, fab.Elevation);
            Assert.Equal(1, Part<MaterialSurface>(fab).TonalElevation);
            fab.ClearValue(FloatingActionButton.ElevationProperty);
            Frames(clock, window, 2);
            Assert.Equal(new FloatingActionButtonElevation(14, 14, 14, 12), fab.Elevation);
            Assert.Equal(14, Part<MaterialSurface>(fab).TonalElevation);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MotionBindingsFollowSystemAndComponentTokens_WithoutOverwritingExplicitSpecs()
    {
        var fab = new FloatingActionButton { Icon = Icon() };
        var extended = new ExtendedFloatingActionButton { Icon = Icon(), Content = Label() };
        var toggle = new ToggleFloatingActionButton { Icon = Icon() };
        var item = new FloatingActionButtonMenuItem { Icon = Icon(), Content = Label() };
        var menu = new FloatingActionButtonMenu { Expanded = true, Button = toggle, Items = { item } };
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { fab, extended, menu } });
        try
        {
            var spatial = new MotionSpring(.713579, 456.789123);
            window.Resources[Sys.Motion.Spring.SpatialFast] = spatial;
            Flush(window);
            var visibility = Assert.IsType<SpringSpec<float>>(fab.VisibilityScaleSpec);
            Assert.Equal((float)spatial.DampingRatio, visibility.DampingRatio);
            Assert.Equal((float)spatial.Stiffness, visibility.Stiffness);
            Assert.Equal(spatial, toggle.CheckedMotion);
            Assert.Equal(spatial, item.RevealWidthMotion);
            Assert.Equal(spatial, Part<ExtendedFabLayout>(extended).ExpandWidthMotion);
            var overrideSpec = new SpringSpec<float>(1, 222);
            fab.VisibilityScaleSpec = overrideSpec;
            fab.Resources[Comp.Fab.VisibilityScaleMotion] = new MotionSpring(1, 333);
            Flush(window);
            Assert.Same(overrideSpec, fab.VisibilityScaleSpec);
            fab.ClearValue(FloatingActionButton.VisibilityScaleSpecProperty);
            Flush(window);
            Assert.Equal(333, Assert.IsType<SpringSpec<float>>(fab.VisibilityScaleSpec).Stiffness);
            var slow = new MotionSpring(1, 555);
            window.Resources[Sys.Motion.Spring.EffectsSlow] = slow;
            Flush(window);
            Assert.Equal(slow, Part<FabMenuLayout>(menu).StaggerMotion);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ChangingMotionMidFlightKeepsCapturedSpec_AndAppliesOnTheNextTarget()
    {
        var first = new ToggleFloatingActionButton { Icon = Icon() };
        var second = new ToggleFloatingActionButton { Icon = Icon() };
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { first, second } });
        try
        {
            first.IsChecked = second.IsChecked = true;
            Frames(clock, window, 4);
            second.Resources[Comp.ToggleFab.CheckedMotion] = new MotionSpring(1, 60);
            Flush(window);
            for (var i = 0; i < 10; i++)
            {
                Frames(clock, window, 1);
                Assert.Equal(first.CheckedProgress, second.CheckedProgress);
            }
            Frames(clock, window, 70);
            Assert.Equal(1, first.CheckedProgress);
            Assert.Equal(1, second.CheckedProgress);
            first.IsChecked = second.IsChecked = false;
            Frames(clock, window, 6);
            Assert.NotEqual(first.CheckedProgress, second.CheckedProgress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ComposableCorrectionsRemainDistinctFromRawTokens_AndDoNotResizeArbitraryIcons()
    {
        var source = new FloatingActionButtonTokenSource();
        Assert.True(source.TryGetToken(Comp.FabLarge.IconSize, null, out var rawIcon));
        Assert.Equal(32d, rawIcon);
        Assert.True(source.TryGetToken(Comp.ExtendedFabMedium.IconLabelSpace, null, out var rawGap));
        Assert.Equal(16d, rawGap);
        var icon = Icon();
        var toggleIcon = Icon();
        var fab = new ExtendedFloatingActionButton { Size = FloatingActionButtonSize.Large, Icon = icon, Content = Label() };
        var toggle = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Large, Icon = toggleIcon };
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { fab, toggle } });
        try
        {
            Assert.Equal(36, MaterialIcon.GetDefaultSize(icon));
            Assert.Equal(36, MaterialIcon.GetDefaultSize(toggleIcon));
            Assert.Equal(16, Part<ExtendedFabLayout>(fab).IconLabelSpace);
            window.Resources[Comp.FabLarge.IconSize] = 42d;
            window.Resources[Comp.ExtendedFabLarge.IconLabelSpace] = 25d;
            Flush(window);
            Assert.Equal(36, MaterialIcon.GetDefaultSize(icon));
            Assert.Equal(16, Part<ExtendedFabLayout>(fab).IconLabelSpace);
            window.Resources[Comp.FabLarge.ContentIconSize] = 38d;
            window.Resources[Comp.ExtendedFabLarge.ContentIconLabelSpace] = 18d;
            Flush(window);
            Assert.Equal(38, MaterialIcon.GetDefaultSize(icon));
            Assert.Equal(38, toggle.IconSize);
            Assert.Equal(18, Part<ExtendedFabLayout>(fab).IconLabelSpace);
            toggle.IsChecked = true;
            Frames(clock, window, 70);
            Assert.Equal(20, toggle.IconSize);
            Assert.Equal(38, MaterialIcon.GetDefaultSize(toggleIcon));
            Assert.Equal(new Size(11, 13), toggleIcon.Bounds.Size);
            Assert.Equal(new Size(11, 13), icon.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExtendedAndMenuLayout_ConsumeDynamicDefaultsThroughPartProperties()
    {
        var extended = new ExtendedFloatingActionButton { Icon = Icon(), Content = Label() };
        var item = new FloatingActionButtonMenuItem { Icon = Icon(), Content = Label() };
        var menu = new FloatingActionButtonMenu { Expanded = true, Button = new FloatingActionButton(), Items = { item } };
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { extended, menu } });
        try
        {
            window.Resources[Comp.ExtendedFabPrimary.IconLeadingSpace] = 19d;
            window.Resources[Comp.ExtendedFabPrimary.MinimumWidth] = 92d;
            window.Resources[Comp.ExtendedFabPrimary.LeadingSpace] = 21d;
            window.Resources[Comp.ExtendedFabPrimary.TrailingSpace] = 22d;
            window.Resources[Comp.FabMenu.ContentPadding] = new Thickness(30, 0);
            window.Resources[Comp.FabMenu.HorizontalPadding] = 20d;
            window.Resources[Comp.FabMenu.ItemIconSize] = 26d;
            Flush(window);
            var layout = Part<ExtendedFabLayout>(extended);
            Assert.Equal(19, layout.IconLeadingSpace);
            Assert.Equal(92, layout.MinimumWidth);
            Assert.Equal(21, layout.LeadingSpace);
            Assert.Equal(22, layout.TrailingSpace);
            Assert.Equal(new Thickness(30, 0), Part<FabMenuItemLayout>(item).ContentPadding);
            Assert.Equal(20, Part<FabMenuLayout>(menu).HorizontalPadding);
            Assert.Equal(26, MaterialIcon.GetDefaultSize((Control)item.Icon!));
        }
        finally { window.Close(); }
    }

    private static Border Icon() => new() { Width = 11, Height = 13 };
    private static Border Label() => new() { Width = 32, Height = 16 };
    private static T Part<T>(Control control) where T : Control => control.GetVisualDescendants().OfType<T>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int count)
    {
        for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 500, Height = 800, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control;
        window.Show();
        Flush(window);
        return window;
    }
}
