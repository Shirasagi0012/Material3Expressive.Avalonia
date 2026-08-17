using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the MaterialFab MaterialValue profiles.</summary>
public sealed class FabMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeStableTypesAndExactRawDependencies()
    {
        Assert.Equal(typeof(FabVisualColors), FabValueKeys.PrimaryVisualColors.ValueType);
        Assert.Equal(typeof(FabElevationProfile), FabValueKeys.SecondaryElevation.ValueType);
        Assert.Equal(typeof(FabGeometryProfile), FabValueKeys.LargeGeometry.ValueType);

        Assert.Equal(
            [FabPrimaryContainerTokensKeys.ContainerColor,
             FabPrimaryContainerTokensKeys.IconColor,
             McuSystemColorTokens.Shadow],
            FabValueKeys.PrimaryVisualColors.Plan.Dependencies);
        Assert.Equal(
            [McuSystemColorTokens.TertiaryContainer,
             McuSystemColorTokens.OnTertiaryContainer,
             McuSystemColorTokens.Shadow],
            FabValueKeys.TertiaryVisualColors.Plan.Dependencies);
        Assert.Equal(
            [FabSecondaryContainerTokensKeys.ContainerElevation,
             FabSecondaryContainerTokensKeys.HoveredContainerElevation,
             FabSecondaryContainerTokensKeys.FocusedContainerElevation,
             FabSecondaryContainerTokensKeys.PressedContainerElevation,
             FabSecondaryContainerTokensKeys.LoweredContainerElevation],
            FabValueKeys.SecondaryElevation.Plan.Dependencies);
        Assert.Equal(
            [FabLargeTokensKeys.ContainerHeight,
             FabLargeTokensKeys.ContainerWidth,
             FabLargeTokensKeys.ContainerShape,
             FabLargeTokensKeys.IconSize],
            FabValueKeys.LargeGeometry.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void AllVariantsAndSizesResolveThroughExplicitSelectors()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var primary = new MaterialFab { Content = "+" };
        var secondary = new MaterialFab { Variant = FabVariant.Secondary, Content = "+" };
        var tertiary = new MaterialFab { Variant = FabVariant.Tertiary, Content = "+" };
        var surface = new MaterialFab { Variant = FabVariant.Surface, Content = "+" };
        var medium = new MaterialFab { Size = FabSize.Medium, Content = "+" };
        var large = new MaterialFab { Size = FabSize.Large, Content = "+" };
        var scope = Place(
            new StackPanel
            {
                Children = { primary, secondary, tertiary, surface, medium, large },
            },
            theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.PrimaryContainer),
                ThemeTestHelper.ColorOf(primary.Background));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
                ThemeTestHelper.ColorOf(secondary.Foreground));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.TertiaryContainer),
                ThemeTestHelper.ColorOf(tertiary.Background));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ThemeTestHelper.ColorOf(surface.Foreground));

            Assert.Equal(FabBaselineTokens.ContainerHeight, primary.MinHeight);
            Assert.Equal(FabBaselineTokens.IconSize, primary.IconSize);
            Assert.Equal(FabMediumTokens.ContainerHeight, medium.MinHeight);
            Assert.Equal(FabMediumTokens.IconSize, medium.IconSize);
            Assert.Equal(FabLargeTokens.ContainerHeight, large.MinHeight);
            Assert.Equal(FabLargeTokens.IconSize, large.IconSize);
            Assert.Equal(FabSize.Small, primary.Size);
            Assert.Equal(ShapeRole.CornerLarge, primary.ResolvedGeometry.ContainerShape);
            Assert.Equal(ShapeRole.CornerMedium, primary.ResolvedGeometry.PressedContainerShape);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindVisualProfileInPlace()
    {
        var fab = new MaterialFab { Content = "+" };
        var theme = new MaterialTheme();
        var scope = Place(fab, theme);
        var reference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(reference, Colors.Orange);
        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.PrimaryContainer, SharedToken.Alias(reference));
        var component = new MaterialTokenOverrides();
        component.Set(
            FabPrimaryContainerTokensKeys.ContainerColor,
            SharedToken.Alias(McuSystemColorTokens.PrimaryContainer));
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(fab.Background));

            references.Set(reference, Colors.Purple);
            Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(fab.Background));

            system.Set(
                McuSystemColorTokens.PrimaryContainer,
                SharedToken.Literal(Colors.Green));
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(fab.Background));

            component.Set(
                FabPrimaryContainerTokensKeys.ContainerColor,
                SharedToken.Literal(Colors.HotPink));
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(fab.Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RawOverridesRemainScopedAndFlatProfileOverrideIsPresentationOnly()
    {
        var local = new MaterialFab { Content = "+" };
        var sibling = new MaterialFab { Content = "+" };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var localRaw = new MaterialTokenOverrides();
        localRaw.Set(FabPrimaryContainerTokensKeys.ContainerColor, Colors.HotPink);
        localScope.Resources.MergedDictionaries.Add(localRaw);
        var custom = new FabVisualColors(
            new ImmutableSolidColorBrush(Colors.Green),
            new ImmutableSolidColorBrush(Colors.White),
            new ImmutableSolidColorBrush(Colors.White),
            Colors.Black);
        localScope.Resources[FabValueKeys.PrimaryVisualColors] = custom;
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Same(custom.ContainerColor, local.Background);
            Assert.Same(custom.ContentColor, local.Foreground);
            Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.Background));
            Assert.Same(custom, SharedTokenTestHelper.Resolve(local, FabValueKeys.PrimaryVisualColors));

            localScope.Resources.Remove(FabValueKeys.PrimaryVisualColors);
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(local.Background));
            Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesRetainPrecedenceOverMaterialSetters()
    {
        var style = new Style(selector => selector.Is<MaterialFab>());
        var styledBackground = new ImmutableSolidColorBrush(Colors.Orange);
        var styledForeground = new ImmutableSolidColorBrush(Colors.Blue);
        style.Add(new Setter(MaterialFab.BackgroundProperty, styledBackground));
        style.Add(new Setter(MaterialFab.ForegroundProperty, styledForeground));
        style.Add(new Setter(MaterialFab.MinHeightProperty, 71d));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialFab { Content = "+" };
            var styledScope = Place(styled, new MaterialTheme());
            var window = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Same(styledBackground, styled.Background);
                Assert.Same(styledForeground, styled.Foreground);
                Assert.Equal(71, styled.MinHeight);
            }
            finally
            {
                window.Close();
            }

            var localBackground = new ImmutableSolidColorBrush(Colors.HotPink);
            var local = new MaterialFab
            {
                Content = "+",
                Background = localBackground,
                MinHeight = 73,
            };
            var localScope = Place(local, new MaterialTheme());
            var localWindow = ThemeTestHelper.ShowWindow(localScope);
            try
            {
                Assert.Same(localBackground, local.Background);
                Assert.Equal(73, local.MinHeight);
            }
            finally
            {
                localWindow.Close();
            }
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void ShapeElevationAndExtendedStateRemainLateControlPolicies()
    {
        var fab = new MaterialFab { Content = "+" };
        var scope = Place(fab, new MaterialTheme());
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var geometry = fab.ResolvedGeometry;
            var elevation = fab.ResolvedElevation;
            Assert.Equal(ShapeRole.CornerLarge, geometry.ContainerShape);
            Assert.Equal(ShapeRole.CornerMedium, geometry.PressedContainerShape);
            Assert.Equal(elevation.Lowered, elevation.For(true, InteractionState.None));
            Assert.Equal(elevation.Pressed, elevation.For(false, InteractionState.Pressed));
            Assert.Equal(elevation.Hovered, elevation.For(false, InteractionState.Hovered));
            Assert.Equal(elevation.Focused, elevation.For(false, InteractionState.Focused));

            Assert.False(fab.IsExtended);
            Assert.Equal(default, fab.Padding);
            fab.Label = "Compose";
            Assert.True(fab.IsExtended);
            Assert.Equal(geometry.LabelSpace, fab.Padding.Left);
            fab.Size = FabSize.Large;
            Assert.Equal(FabLargeTokens.ContainerShape, fab.ResolvedGeometry.ContainerShape);
            Assert.Equal(FabLargeTokens.IconSize, fab.IconSize);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatProfile()
    {
        var fab = new MaterialFab();
        var localScope = Place(fab, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[FabValueKeys.PrimaryVisualColors] = new FabVisualColors(
            new ImmutableSolidColorBrush(Colors.HotPink),
            new ImmutableSolidColorBrush(Colors.White),
            new ImmutableSolidColorBrush(Colors.White),
            Colors.Black);
        localScope.Resources[FabPrimaryContainerTokensKeys.ContainerColor] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(fab, FabValueKeys.PrimaryVisualColors, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(FabPrimaryContainerTokensKeys.ContainerColor, exception.Token);
        Assert.Same(FabValueKeys.PrimaryVisualColors, exception.ValueKey);
    }

    private static Border Place(Control control, MaterialTheme theme)
    {
        var scope = new Border { Child = control };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }
}
