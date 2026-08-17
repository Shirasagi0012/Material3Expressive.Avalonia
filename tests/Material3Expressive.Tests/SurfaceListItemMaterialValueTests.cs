using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Focused MaterialValue contracts for Surface and the ListItem family.</summary>
public sealed class SurfaceListItemMaterialValueTests
{
    [Fact]
    public void SurfaceValueKeyDeclaresTheCanonicalSystemDependencies()
    {
        Assert.Equal(typeof(SurfaceVisualColors), SurfaceValueKeys.VisualColors.ValueType);
        Assert.Equal(
            [
                McuSystemColorTokens.Surface,
                McuSystemColorTokens.OnSurface,
                McuSystemColorTokens.SurfaceTint,
                McuSystemColorTokens.Shadow,
            ],
            SurfaceValueKeys.VisualColors.Plan.Dependencies);
    }

    [Fact]
    public void ListItemValueKeysDeclareOnlyTheRawInputsOfTheirProfiles()
    {
        Assert.Equal(typeof(ListItemGeometryProfile), ListItemValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(ListItemVisualColors), ListItemValueKeys.VisualColors.ValueType);

        Assert.Equal(
            [
                ListTokensKeys.ItemContainerExpressiveShape,
                ListTokensKeys.ContainerShape,
                ListTokensKeys.ItemSelectedContainerExpressiveShape,
                ListTokensKeys.ItemPressedContainerExpressiveShape,
                ListTokensKeys.ItemFocusedContainerExpressiveShape,
                ListTokensKeys.ItemHoveredContainerExpressiveShape,
                ReorderListTokensKeys.ItemShape,
                ListTokensKeys.ItemContainerElevation,
                ListTokensKeys.ItemDraggedContainerElevation,
                ListTokensKeys.ItemOneLineContainerHeight,
                ListTokensKeys.ItemTwoLineContainerHeight,
                ListTokensKeys.ItemThreeLineContainerHeight,
                ListTokensKeys.ItemLeadingSpace,
                ListTokensKeys.ItemTrailingSpace,
                ListTokensKeys.ItemTopSpace,
                ListTokensKeys.ItemBottomSpace,
                ListTokensKeys.ItemBetweenSpace,
                ListTokensKeys.SegmentedGap,
                ListTokensKeys.ItemLabelTextFont,
                ListTokensKeys.ItemSupportingTextFont,
                ListTokensKeys.ItemOverlineFont,
                ListTokensKeys.ItemLeadingAvatarLabelFont,
                ListTokensKeys.ItemTrailingSupportingTextFont,
            ],
            ListItemValueKeys.GeometryProfile.Plan.Dependencies);

        Assert.Contains(McuSystemColorTokens.Shadow, ListItemValueKeys.VisualColors.Plan.Dependencies);
        Assert.Contains(ListTokensKeys.ItemDisabledLabelTextOpacity, ListItemValueKeys.VisualColors.Plan.Dependencies);
        Assert.Contains(ListTokensKeys.ItemDisabledSupportingTextOpacity, ListItemValueKeys.VisualColors.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void SurfaceAndListItemProfilesResolveThroughTheirControlThemes()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var surface = new MaterialSurface { Width = 120, Height = 40 };
        var item = new MaterialListItem { Content = "Headline", Width = 240 };
        var root = new StackPanel { Children = { surface, item } };
        MaterialThemeScope.SetTheme(root, theme);
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Surface),
                ThemeTestHelper.ColorOf(surface.Background));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ThemeTestHelper.ColorOf(surface.Foreground));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ThemeTestHelper.ColorOf(item.Foreground));

            Assert.Equal(56, item.ResolvedGeometry.OneLineContainerHeight);
            Assert.Equal(new Thickness(16, 10, 16, 10), item.ResolvedGeometry.ContentPadding);
            var panel = Assert.Single(item.GetVisualDescendants().OfType<MaterialListItemPanel>());
            Assert.Equal(12, panel.BetweenSpace);
            Assert.Equal(56, panel.OneLineMinimumHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RawOverridesRebindSurfaceAndListItemProfilesInPlace()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var surface = new MaterialSurface();
        var item = new MaterialListItem { Content = "Headline", Width = 240 };
        var root = new StackPanel { Children = { surface, item } };
        MaterialThemeScope.SetTheme(root, theme);
        root.Resources[ListTokensKeys.ItemBetweenSpace] = 21d;
        root.Resources[McuSystemColorTokens.Surface] = Colors.HotPink;
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(surface.Background));
            var panel = Assert.Single(item.GetVisualDescendants().OfType<MaterialListItemPanel>());
            Assert.Equal(21, panel.BetweenSpace);

            root.Resources[ListTokensKeys.ItemBetweenSpace] = 23d;
            root.Resources[McuSystemColorTokens.Surface] = Colors.Green;

            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(surface.Background));
            Assert.Equal(23, panel.BetweenSpace);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesArePresentationOverridesAndRespectTypedResourceIdentity()
    {
        var surface = new MaterialSurface();
        var item = new MaterialListItem { Content = "Headline", Width = 240 };
        var surfaceProfile = new SurfaceVisualColors(
            Brush(Colors.Purple),
            Brush(Colors.White),
            Colors.Orange,
            Colors.Black);
        var geometry = GeometryProfile(64, 9);
        var colors = VisualColors(Colors.OrangeRed);
        var root = new StackPanel { Children = { surface, item } };
        MaterialThemeScope.SetTheme(root, new MaterialTheme());
        root.Resources[SurfaceValueKeys.VisualColors] = surfaceProfile;
        root.Resources[ListItemValueKeys.GeometryProfile] = geometry;
        root.Resources[ListItemValueKeys.VisualColors] = colors;
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Same(surfaceProfile, surface.ResolvedVisualColors);
            Assert.Same(surfaceProfile.SurfaceBrush, surface.Background);
            Assert.Same(geometry, item.ResolvedGeometry);
            Assert.Same(colors, item.ResolvedVisualColors);
            Assert.Equal(64, item.ResolvedGeometry.OneLineContainerHeight);
            Assert.Equal(new Thickness(9, 9, 9, 9), item.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void InvalidLocalRawHitDoesNotFallBackToAnAncestorFlatProfile()
    {
        var item = new MaterialListItem();
        var localScope = new Border { Child = item };
        var root = new Border { Child = localScope };
        MaterialThemeScope.SetTheme(localScope, new MaterialTheme());
        root.Resources[ListItemValueKeys.GeometryProfile] = GeometryProfile(99, 3);
        localScope.Resources[ListTokensKeys.ItemBetweenSpace] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(item, ListItemValueKeys.GeometryProfile, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(ListTokensKeys.ItemBetweenSpace, exception.Token);
        Assert.Same(ListItemValueKeys.GeometryProfile, exception.ValueKey);
    }

    private static ListItemGeometryProfile GeometryProfile(double height, double padding) => new(
        ShapeRole.CornerExtraSmall,
        ShapeRole.CornerLarge,
        ShapeRole.CornerLarge,
        ShapeRole.CornerLarge,
        ShapeRole.CornerLarge,
        ShapeRole.CornerMedium,
        ShapeRole.CornerLarge,
        0,
        4,
        height,
        72,
        88,
        padding,
        padding,
        padding,
        padding,
        12,
        2,
        TypographyRole.BodyLarge,
        TypographyRole.BodyMedium,
        TypographyRole.LabelSmall,
        TypographyRole.TitleMedium,
        TypographyRole.LabelSmall);

    private static ListItemVisualColors VisualColors(Color color) => new(
        Brush(color), Brush(color), Brush(color), Brush(color), Brush(color), Brush(color), Brush(color),
        Brush(color), Brush(color), Brush(color), Brush(color), Brush(color), Brush(color),
        Brush(color), Brush(color), Brush(color), Brush(color), Brush(color), Brush(color),
        Brush(color), Brush(color), Brush(color), Brush(color), Brush(color), Brush(color),
        Colors.Black);

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}
