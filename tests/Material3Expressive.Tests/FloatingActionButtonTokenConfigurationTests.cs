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
            Setters = { new Setter(MaterialText.StyleProperty, textStyle) }
        };
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            window.Styles.Add(style);
            fab.Size = FloatingActionButtonSize.Large;
            Flush(window);
            Assert.Equal(textStyle, MaterialText.GetStyle(fab));
            window.Styles.Remove(style);
            var tokenStyle = new TextStyle { FontSize = 29 };
            fab.Resources[Comp.ExtendedFabLarge.LabelTextStyle] = tokenStyle;
            Flush(window);
            Assert.Equal(tokenStyle, MaterialText.GetStyle(fab));
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

    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Medium, 12d)]
    [InlineData(FloatingActionButtonSize.Large, 16d)]
    public void ExtendedGapTokenChanges_RecomputeTheActualLabelPositionAndContainerWidth(FloatingActionButtonSize size, double initialGap)
    {
        var icon = Icon(); var label = Label();
        var fab = new ExtendedFloatingActionButton { Size = size, Icon = icon, Content = label };
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var clip = Part<FabLabelClip>(fab);
            var initialWidth = fab.Bounds.Width;
            var initialLabelX = label.TranslatePoint(default, fab)!.Value.X;
            var key = size == FloatingActionButtonSize.Medium
                ? Comp.ExtendedFabMedium.ContentIconLabelSpace : Comp.ExtendedFabLarge.ContentIconLabelSpace;
            Assert.Equal(initialGap + label.Width, clip.DesiredSize.Width);
            fab.Resources[key] = initialGap + 7;
            Flush(window);
            Assert.Equal(initialGap + 7 + label.Width, clip.DesiredSize.Width);
            Assert.Equal(initialLabelX + 7, label.TranslatePoint(default, fab)!.Value.X);
            Assert.Equal(initialWidth + 7, fab.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_SettledGapTracksNaturalBounds_AndNextCollapseStartsAtThatWidth()
    {
        var icon = Icon(); var label = new Border { Width = 160, Height = 16 };
        var fab = new ExtendedFloatingActionButton { Icon = icon, Content = label };
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            Frames(clock, window, 2);
            var clip = Part<FabLabelClip>(fab);
            Assert.Equal(172, clip.Bounds.Width);
            var initialWidth = fab.Bounds.Width;
            var initialLabelX = label.TranslatePoint(default, fab)!.Value.X;
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 21d;
            Flush(window);
            Assert.Equal(initialWidth + 9, fab.Bounds.Width);
            Assert.Equal(181, clip.DesiredSize.Width);
            Assert.Equal(181, clip.Bounds.Width);
            Assert.Equal(initialLabelX + 9, label.TranslatePoint(default, fab)!.Value.X);
            Assert.Equal(clip.Bounds.Width, label.TranslatePoint(default, clip)!.Value.X + label.Bounds.Width);
            Assert.Equal(0, clock.PendingFrameCount); // Settled size changes are ordinary measurement.

            fab.Expanded = false;
            Flush(window);
            Assert.Equal(181, clip.Bounds.Width); // No jump back to the old saved 172px extent.
            Frames(clock, window, 70);
            Assert.Equal(56, fab.Bounds.Width);
            fab.Expanded = true;
            Frames(clock, window, 70);
            Assert.Equal(initialWidth + 9, fab.Bounds.Width);
            Assert.Equal(181, clip.Bounds.Width);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 14d;
            Flush(window);
            Assert.Equal(initialWidth + 2, fab.Bounds.Width);
            Assert.Equal(174, clip.Bounds.Width);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void VisibilityUnavailableSpec_DefersBothChannelsAndReplaysTheCurrentTarget(int missingKind)
    {
        var fab = new FloatingActionButton { Icon = Icon() };
        var source = new ContentControl();
        IDisposable? binding = null;
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<FabVisibilityLayout>(fab);
            // Schedule first, then withdraw the specification before the shared frame.
            fab.AnimatedVisible = false;
            switch (missingKind)
            {
                case 0: fab.VisibilityScaleSpec = null; break;
                case 1:
                    binding = fab.Bind(FloatingActionButton.VisibilityScaleSpecProperty,
                        new Binding(nameof(ContentControl.Content)) { Source = source });
                    break;
                default: fab.Resources[Comp.Fab.VisibilityScaleMotion] = new TokenRef(MissingMotion); break;
            }
            Flush(window);
            Assert.Null(fab.VisibilityScaleSpec);
            Frames(clock, window, 20);
            Assert.Equal(1, layout.ScaleProgress);
            Assert.Equal(1, layout.Alpha);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            // An unavailable target is not submitted or forgotten when it changes again.
            fab.AnimatedVisible = true;
            fab.AnimatedVisible = false;
            Frames(clock, window, 4);
            switch (missingKind)
            {
                case 0: fab.ClearValue(FloatingActionButton.VisibilityScaleSpecProperty); break;
                case 1: source.Content = new SpringSpec<float>(1, 800); break;
                default: fab.Resources.Remove(Comp.Fab.VisibilityScaleMotion); break;
            }
            Frames(clock, window, 70);
            Assert.Equal(0, layout.ScaleProgress);
            Assert.Equal(0, layout.Alpha);

            fab.Resources[Comp.Fab.VisibilityAlphaMotion] = default(MotionSpring);
            fab.AnimatedVisible = true;
            Frames(clock, window, 10);
            Assert.Null(fab.VisibilityAlphaSpec);
            Assert.Equal(0, layout.Alpha);
            Assert.Equal(0, layout.ScaleProgress);
            fab.Resources.Remove(Comp.Fab.VisibilityAlphaMotion);
            Frames(clock, window, 70);
            Assert.Equal(1, layout.Alpha);
            Assert.Equal(1, layout.ScaleProgress);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { binding?.Dispose(); window.Close(); }
    }

    [AvaloniaFact]
    public void ToggleUnavailableMotion_DoesNotConstructAZeroSpring_AndRestorationReplaysCheckedTarget()
    {
        var toggle = new ToggleFloatingActionButton { Icon = Icon() };
        using var clock = new MotionTestClockScope(); var window = Show(toggle);
        try
        {
            toggle.IsChecked = true;
            toggle.Resources[Comp.ToggleFab.CheckedMotion] = new TokenRef(MissingMotion);
            Frames(clock, window, 10);
            Assert.Null(toggle.CheckedMotion);
            Assert.Equal(0, toggle.CheckedProgress);
            toggle.Resources[Comp.ToggleFab.CheckedMotion] = default(MotionSpring);
            Frames(clock, window, 10);
            Assert.Equal(0, toggle.CheckedProgress);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            toggle.Resources.Remove(Comp.ToggleFab.CheckedMotion);
            Frames(clock, window, 70);
            Assert.Equal(1, toggle.CheckedProgress);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Default, false)]
    [InlineData(FloatingActionButtonSize.Default, true)]
    [InlineData(FloatingActionButtonSize.Medium, false)]
    [InlineData(FloatingActionButtonSize.Medium, true)]
    [InlineData(FloatingActionButtonSize.Large, false)]
    [InlineData(FloatingActionButtonSize.Large, true)]
    public void ExtendedUnavailableMotion_HoldsBothChannelsAndRecoversTheRequestedDirection(FloatingActionButtonSize size, bool initialExpanded)
    {
        var fab = new ExtendedFloatingActionButton { Size = size, Expanded = initialExpanded, Icon = Icon(), Content = Label() };
        var widthKey = size == FloatingActionButtonSize.Default
            ? initialExpanded ? Comp.ExtendedFabPrimary.CollapseWidthMotion : Comp.ExtendedFabPrimary.ExpandWidthMotion
            : Comp.ExtendedFab.WidthMotion;
        var alphaKey = size == FloatingActionButtonSize.Default
            ? initialExpanded ? Comp.ExtendedFabPrimary.CollapseAlphaMotion : Comp.ExtendedFabPrimary.ExpandAlphaMotion
            : Comp.ExtendedFab.AlphaMotion;
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Resources[widthKey] = new TokenRef(MissingMotion);
            fab.Resources[alphaKey] = new TokenRef(MissingMotion);
            fab.Expanded = !initialExpanded;
            Frames(clock, window, 10);
            Assert.Equal(initialExpanded ? 1 : 0, layout.LabelAlpha);
            if (size != FloatingActionButtonSize.Default)
                Assert.Equal(initialExpanded ? 1 : 0, layout.WidthProgress);
            fab.Resources.Remove(widthKey);
            Frames(clock, window, 10);
            Assert.Equal(initialExpanded ? 1 : 0, layout.LabelAlpha);
            if (size != FloatingActionButtonSize.Default)
                Assert.Equal(initialExpanded ? 1 : 0, layout.WidthProgress);
            fab.Resources[alphaKey] = default(MotionSpring);
            Frames(clock, window, 10);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            Assert.Equal(initialExpanded ? 1 : 0, layout.LabelAlpha);
            fab.Resources.Remove(alphaKey);
            Frames(clock, window, 70);
            Assert.Equal(initialExpanded ? 0 : 1, layout.LabelAlpha);
            Assert.Equal(!initialExpanded, Part<FabLabelClip>(fab).IsVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MenuUnavailableMotion_PreservesStaggerAndItemTargetsUntilEachConfigurationRecovers()
    {
        var item = new FloatingActionButtonMenuItem { Icon = Icon(), Content = Label() };
        var menu = new FloatingActionButtonMenu { Button = new FloatingActionButton(), Items = { item } };
        using var clock = new MotionTestClockScope(); var window = Show(menu);
        try
        {
            var layout = Part<FabMenuLayout>(menu);
            menu.Resources[Comp.FabMenu.StaggerMotion] = new TokenRef(MissingMotion);
            item.Resources[Comp.FabMenu.ItemWidthMotion] = new TokenRef(MissingMotion);
            item.Resources[Comp.FabMenu.ItemAlphaMotion] = new TokenRef(MissingMotion);
            menu.Expanded = true;
            Frames(clock, window, 10);
            Assert.Null(layout.StaggerMotion);
            Assert.Equal(0, layout.VisibleCount);
            menu.Resources[Comp.FabMenu.StaggerMotion] = default(MotionSpring);
            Frames(clock, window, 10);
            Assert.Equal(0, layout.VisibleCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            menu.Resources.Remove(Comp.FabMenu.StaggerMotion);
            Frames(clock, window, 70);
            Assert.Equal(1, layout.VisibleCount);
            Assert.Equal(0, item.RevealWidth);
            Assert.Equal(0, item.RevealAlpha);
            item.Resources.Remove(Comp.FabMenu.ItemWidthMotion);
            Frames(clock, window, 10);
            Assert.Equal(0, item.RevealWidth);
            item.Resources[Comp.FabMenu.ItemAlphaMotion] = default(MotionSpring);
            Frames(clock, window, 10);
            Assert.Equal(0, item.RevealAlpha);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            item.Resources.Remove(Comp.FabMenu.ItemAlphaMotion);
            Frames(clock, window, 70);
            Assert.Equal(1, item.RevealWidth);
            Assert.Equal(1, item.RevealAlpha);
            Assert.True(item.IsHitTestVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);

            // A deferred collapse can be superseded before configuration returns.
            item.Resources[Comp.FabMenu.ItemAlphaMotion] = new TokenRef(MissingMotion);
            menu.Expanded = false;
            Frames(clock, window, 70);
            Assert.Equal(0, layout.VisibleCount);
            Assert.Equal(1, item.RevealAlpha);
            menu.Expanded = true;
            Frames(clock, window, 70);
            item.Resources.Remove(Comp.FabMenu.ItemAlphaMotion);
            Frames(clock, window, 10);
            Assert.Equal(1, item.RevealAlpha);
            Assert.Equal(1, item.RevealWidth);
            menu.Expanded = false;
            Frames(clock, window, 70);
            Assert.Equal(0, item.RevealAlpha);
            Assert.Equal(0, item.RevealWidth);
            Assert.False(item.IsHitTestVisible);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DeferredMotionRequests_DoNotLeakAcrossDetachAndReparent()
    {
        var fab = new FloatingActionButton { Icon = Icon() };
        var toggle = new ToggleFloatingActionButton { Icon = Icon() };
        var controls = new StackPanel { Children = { fab, toggle } };
        var first = new Panel { Children = { controls } };
        var second = new Panel();
        first.Resources[Comp.Fab.VisibilityScaleMotion] = new TokenRef(MissingMotion);
        first.Resources[Comp.ToggleFab.CheckedMotion] = new TokenRef(MissingMotion);
        using var clock = new MotionTestClockScope(); var window = Show(new Panel { Children = { first, second } });
        try
        {
            fab.AnimatedVisible = false;
            toggle.IsChecked = true;
            Frames(clock, window, 5);
            Assert.Equal(1, Part<FabVisibilityLayout>(fab).Alpha);
            Assert.Equal(0, toggle.CheckedProgress);
            first.Children.Remove(controls);
            Frames(clock, window, 2);
            Assert.Equal(0, clock.PendingFrameCount);
            second.Children.Add(controls);
            Flush(window);
            Assert.Equal(0, Part<FabVisibilityLayout>(fab).Alpha);
            Assert.Equal(1, toggle.CheckedProgress);
            fab.AnimatedVisible = true;
            toggle.IsChecked = false;
            Frames(clock, window, 70);
            Assert.Equal(1, Part<FabVisibilityLayout>(fab).Alpha);
            Assert.Equal(0, toggle.CheckedProgress);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateMultiBindings_AreBelowLocalPartConfiguration_AndClearValueRestoresTokens()
    {
        var fab = new FloatingActionButton { Icon = Icon() };
        var extended = new ExtendedFloatingActionButton { Size = FloatingActionButtonSize.Medium, Icon = Icon(), Content = Label() };
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { fab, extended } });
        try
        {
            var ordinaryLayout = Part<FabLayout>(fab);
            var layout = Part<ExtendedFabLayout>(extended);
            var iconPresenter = extended.GetVisualDescendants().OfType<Control>().Single(x => x.Name == "PART_Icon");
            ordinaryLayout.ContainerMinSize = 73;
            layout.LeadingSpace = 39;
            var localMotion = new MotionSpring(.8, 654);
            layout.ExpandWidthMotion = localMotion;
            MaterialIcon.SetDefaultSize(iconPresenter, 41);
            extended.Resources[Comp.ExtendedFabMedium.LeadingSpace] = 31d;
            extended.Resources[Comp.ExtendedFab.WidthMotion] = new MotionSpring(1, 444);
            extended.Resources[Comp.FabMedium.IconSize] = 35d;
            fab.Resources[Comp.FabBaseline.ContainerHeight] = 61d;
            Flush(window);
            Assert.Equal(73, ordinaryLayout.ContainerMinSize);
            Assert.Equal(39, layout.LeadingSpace);
            Assert.Equal(localMotion, layout.ExpandWidthMotion);
            Assert.Equal(41, MaterialIcon.GetDefaultSize(iconPresenter));
            ordinaryLayout.ClearValue(FabLayout.ContainerMinSizeProperty);
            layout.ClearValue(ExtendedFabLayout.LeadingSpaceProperty);
            layout.ClearValue(ExtendedFabLayout.ExpandWidthMotionProperty);
            iconPresenter.ClearValue(MaterialIcon.DefaultSizeProperty);
            Flush(window);
            Assert.Equal(61, ordinaryLayout.ContainerMinSize);
            Assert.Equal(31, layout.LeadingSpace);
            Assert.Equal(new MotionSpring(1, 444), layout.ExpandWidthMotion);
            Assert.Equal(35, MaterialIcon.GetDefaultSize(iconPresenter));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_RapidGapRetargetsPreserveValueVelocityAndTheAlphaTask()
    {
        var fab = ClassicFab(); var comparison = ClassicFab();
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { fab, comparison } });
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            var comparisonLayout = Part<ExtendedFabLayout>(comparison);
            fab.Expanded = comparison.Expanded = true;
            Frames(clock, window, 4);
            var width = ClassicWidth(layout);
            var alphaTask = ClassicField(layout, "_classicAlphaTask");
            Assert.True(width.IsRunning);
            Assert.True(width.State.VelocityVector[0] > 0);
            foreach (var gap in new[] { 20d, 35d, 8d, 29d })
            {
                var before = width.State;
                fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = gap;
                Flush(window);
                Assert.Equal(before.Value, width.Value);
                Assert.Equal(before.VelocityVector[0], width.State.VelocityVector[0]);
                Assert.Equal(160 + gap, width.TargetValue);
                Assert.Same(alphaTask, ClassicField(layout, "_classicAlphaTask"));
                Assert.True((bool)ClassicField(layout, "_classicTransitionActive")!);
                Assert.True(width.IsRunning);
            }
            for (var i = 0; i < 10; i++)
            {
                Frames(clock, window, 1);
                Assert.Equal(comparisonLayout.LabelAlpha, layout.LabelAlpha);
            }
            Frames(clock, window, 70);
            Assert.Equal(189, Part<FabLabelClip>(fab).Bounds.Width);
            Assert.Equal(236, fab.Bounds.Width);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_SpringTokenAloneDoesNotRestartAnUnchangedWidthTarget()
    {
        var fab = ClassicFab(); var comparison = ClassicFab();
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { fab, comparison } });
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            var comparisonLayout = Part<ExtendedFabLayout>(comparison);
            fab.Expanded = comparison.Expanded = true;
            Frames(clock, window, 4);
            var widthTask = ClassicField(layout, "_classicWidthTask");
            var alphaTask = ClassicField(layout, "_classicAlphaTask");
            fab.Resources[Comp.ExtendedFabPrimary.ExpandWidthMotion] = new MotionSpring(1, 60);
            Flush(window);
            Assert.Same(widthTask, ClassicField(layout, "_classicWidthTask"));
            Assert.Same(alphaTask, ClassicField(layout, "_classicAlphaTask"));
            for (var i = 0; i < 8; i++)
            {
                Frames(clock, window, 1);
                Assert.Equal(ClassicWidth(comparisonLayout).Value, ClassicWidth(layout).Value);
                Assert.Equal(comparisonLayout.LabelAlpha, layout.LabelAlpha);
            }
            // A new measured target does consume the current width specification.
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 40d;
            Flush(window);
            Assert.Equal(200, ClassicWidth(layout).TargetValue);
            Assert.NotSame(widthTask, ClassicField(layout, "_classicWidthTask"));
            Assert.Same(alphaTask, ClassicField(layout, "_classicAlphaTask"));
            Frames(clock, window, 140);
            Assert.Equal(200, Part<FabLabelClip>(fab).Bounds.Width);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_GapRetargetsAndVisibilityReversalsReachTheLatestExtent()
    {
        var fab = ClassicFab();
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Expanded = true;
            Frames(clock, window, 4);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 32d;
            Flush(window);
            Assert.Equal(192, ClassicWidth(layout).TargetValue);
            fab.Expanded = false;
            Frames(clock, window, 3);
            var collapseTask = ClassicField(layout, "_classicWidthTask");
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 19d;
            Flush(window);
            Assert.Equal(0, ClassicWidth(layout).TargetValue);
            Assert.Same(collapseTask, ClassicField(layout, "_classicWidthTask"));
            fab.Expanded = true;
            Frames(clock, window, 2);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 43d;
            Flush(window);
            Frames(clock, window, 1);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 15d;
            Flush(window);
            fab.Expanded = false;
            Frames(clock, window, 3);
            fab.Expanded = true;
            Frames(clock, window, 4);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 27d;
            Flush(window);
            Frames(clock, window, 100);
            Assert.Equal(187, Part<FabLabelClip>(fab).Bounds.Width);
            Assert.Equal(234, fab.Bounds.Width);
            Assert.Equal(1, layout.LabelAlpha);
            AssertClassicSettled(layout);
            fab.Expanded = false;
            Frames(clock, window, 70);
            Assert.Equal(56, fab.Bounds.Width);
            Assert.Equal(0, layout.LabelAlpha);
            Assert.False(Part<FabLabelClip>(fab).IsVisible);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_DeferredMeasuredTargetCannotReportTheOldSegmentAsComplete()
    {
        var fab = ClassicFab();
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Expanded = true;
            Frames(clock, window, 4);
            var alphaTask = ClassicField(layout, "_classicAlphaTask");
            fab.Resources[Comp.ExtendedFabPrimary.ExpandWidthMotion] = new TokenRef(MissingMotion);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 30d;
            Flush(window);
            Assert.Equal(172, ClassicWidth(layout).TargetValue);
            Frames(clock, window, 70);
            Assert.Equal(172, Part<FabLabelClip>(fab).Bounds.Width);
            Assert.Equal(190, Part<FabLabelClip>(fab).DesiredSize.Width);
            Assert.True((bool)ClassicField(layout, "_classicTransitionActive")!);
            fab.Resources.Remove(Comp.ExtendedFabPrimary.ExpandWidthMotion);
            Flush(window);
            Assert.Equal(190, ClassicWidth(layout).TargetValue);
            Assert.Same(alphaTask, ClassicField(layout, "_classicAlphaTask"));
            Frames(clock, window, 70);
            Assert.Equal(190, Part<FabLabelClip>(fab).Bounds.Width);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_DetachCancelsRetargetTasks_AndReattachmentDoesNotReuseThem()
    {
        var fab = ClassicFab(); var host = new Panel { Children = { fab } };
        using var clock = new MotionTestClockScope(); var window = Show(host);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Expanded = true;
            Frames(clock, window, 4);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 28d;
            Flush(window);
            var oldWidth = ClassicWidth(layout);
            var oldWidthTask = Assert.IsAssignableFrom<Task>(ClassicField(layout, "_classicWidthTask"));
            var oldAlphaTask = Assert.IsAssignableFrom<Task>(ClassicField(layout, "_classicAlphaTask"));
            host.Children.Remove(fab);
            Frames(clock, window, 2);
            Assert.True(oldWidthTask.IsCanceled);
            Assert.True(oldAlphaTask.IsCanceled);
            AssertClassicSettled(layout);
            Assert.Null(ClassicField(layout, "_attachScope"));
            Assert.Equal(0, clock.PendingFrameCount);
            host.Children.Add(fab);
            Flush(window);
            Assert.NotSame(oldWidth, ClassicWidth(layout));
            Assert.Equal(188, Part<FabLabelClip>(fab).Bounds.Width);
            AssertClassicSettled(layout);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 31d;
            Flush(window);
            Assert.Equal(191, Part<FabLabelClip>(fab).Bounds.Width);
            fab.Expanded = false;
            Frames(clock, window, 4);
            host.Children.Remove(fab);
            Frames(clock, window, 2);
            AssertClassicSettled(layout);
            Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Medium)]
    [InlineData(FloatingActionButtonSize.Large)]
    public void ClassicExtended_CrossSizeReversalRetiresOldCompletionBeforeReturningToDefault(FloatingActionButtonSize expressiveSize)
    {
        var fab = ClassicFab();
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Expanded = true;
            Frames(clock, window, 4);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 24d;
            Flush(window);
            Assert.True((bool)ClassicField(layout, "_classicTransitionActive")!);
            fab.Size = expressiveSize;
            fab.Expanded = false;
            Frames(clock, window, 3);
            AssertClassicSettled(layout);
            Frames(clock, window, 70);
            fab.Size = FloatingActionButtonSize.Default;
            fab.Expanded = true;
            Frames(clock, window, 4);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 32d;
            Flush(window);
            Frames(clock, window, 70);
            Assert.Equal(192, Part<FabLabelClip>(fab).Bounds.Width);
            Assert.Equal(239, fab.Bounds.Width);
            AssertClassicSettled(layout);
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 18d;
            Flush(window);
            Assert.Equal(178, Part<FabLabelClip>(fab).Bounds.Width);
            Assert.Equal(225, fab.Bounds.Width);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClassicExtended_ReturningToVisibleRetargetKeepsThe400SpringSegment()
    {
        var fab = ClassicFab(); fab.Expanded = true;
        using var clock = new MotionTestClockScope(); var window = Show(fab);
        try
        {
            var layout = Part<ExtendedFabLayout>(fab);
            fab.Expanded = false;
            Frames(clock, window, 4);
            fab.Expanded = true;
            Frames(clock, window, 2);
            Assert.True((bool)ClassicField(layout, "_classicReturningToVisible")!);
            // The Visible -> Visible segment uses its fixed interruption spring,
            // even if the ordinary expand specification is changed before retarget.
            fab.Resources[Comp.ExtendedFabPrimary.ExpandWidthMotion] = new MotionSpring(1, 60);
            Flush(window);
            var width = ClassicWidth(layout);
            var before = width.State;
            var alphaTask = ClassicField(layout, "_classicAlphaTask");
            var expected = new TargetBasedAnimation<float>(
                new SpringSpec<float>(stiffness: 400, visibilityThreshold: 1), before.TypeConverter,
                before.Value, 196f, initialVelocityVector: AnimationVector.From(before.VelocityVector));
            fab.Resources[Comp.ExtendedFabPrimary.IconLabelSpace] = 36d;
            Flush(window);
            Assert.Equal(before.Value, width.Value);
            // SpringSimulation's critical t=0 derivative evaluates -omega*x +
            // (v + omega*x), which reconstructs v with double cancellation roundoff
            // (observed 2.27e-13). Pin that analytic result exactly, and separately
            // require preservation to 12 decimal places; the integer trajectory below
            // remains exact rather than weakening the actual motion comparison.
            var initialVelocity = new double[1];
            expected.GetVelocityVector(TimeSpan.Zero, initialVelocity);
            Assert.Equal(initialVelocity[0], width.State.VelocityVector[0]);
            Assert.Equal(before.VelocityVector[0], width.State.VelocityVector[0], precision: 12);
            Assert.Equal(196, width.TargetValue);
            Assert.Same(alphaTask, ClassicField(layout, "_classicAlphaTask"));
            for (var frame = 1; frame <= 8; frame++)
            {
                Frames(clock, window, 1);
                Assert.Equal(expected.GetValue(TimeSpan.FromMilliseconds(frame * 16)), width.Value);
            }
            Frames(clock, window, 70);
            Assert.Equal(196, Part<FabLabelClip>(fab).Bounds.Width);
            AssertClassicSettled(layout);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    private static ExtendedFloatingActionButton ClassicFab() => new()
    {
        Expanded = false, Icon = Icon(), Content = new Border { Width = 160, Height = 16 }
    };
    private static object? ClassicField(ExtendedFabLayout layout, string name) =>
        typeof(ExtendedFabLayout).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(layout);
    private static Animatable<float> ClassicWidth(ExtendedFabLayout layout) =>
        Assert.IsType<Animatable<float>>(ClassicField(layout, "_classicWidth"));
    private static void AssertClassicSettled(ExtendedFabLayout layout)
    {
        Assert.False((bool)ClassicField(layout, "_classicTransitionActive")!);
        Assert.Null(ClassicField(layout, "_classicWidthTask"));
        Assert.Null(ClassicField(layout, "_classicAlphaTask"));
    }

    private static readonly MaterialTokenKey<MotionSpring> MissingMotion = new("test.fab.missing-motion");

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
