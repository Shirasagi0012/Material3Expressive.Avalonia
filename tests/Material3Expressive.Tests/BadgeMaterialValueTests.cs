using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using MaterialTokenOverrides = Material3Expressive.Foundation.Tokens.Runtime.MaterialTokenOverrides;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the Badge MaterialValue profiles and late policies.</summary>
public sealed class BadgeMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeStableTypesAndExactVariantDependencies()
    {
        Assert.Equal(typeof(BadgeGeometryProfile), BadgeValueKeys.SmallGeometry.ValueType);
        Assert.Equal(typeof(BadgeGeometryProfile), BadgeValueKeys.LargeGeometry.ValueType);
        Assert.Equal(typeof(BadgeVisualColors), BadgeValueKeys.SmallVisualColors.ValueType);
        Assert.Equal(typeof(BadgeVisualColors), BadgeValueKeys.LargeVisualColors.ValueType);

        Assert.Equal(
            [BadgeTokensKeys.Size, BadgeTokensKeys.Shape, BadgeTokensKeys.LargeLabelTextFont],
            BadgeValueKeys.SmallGeometry.Plan.Dependencies);
        Assert.Equal(
            [BadgeTokensKeys.LargeSize, BadgeTokensKeys.LargeShape, BadgeTokensKeys.LargeLabelTextFont],
            BadgeValueKeys.LargeGeometry.Plan.Dependencies);
        Assert.Equal(
            [BadgeTokensKeys.Color, BadgeTokensKeys.LargeLabelTextColor],
            BadgeValueKeys.SmallVisualColors.Plan.Dependencies);
        Assert.Equal(
            [BadgeTokensKeys.LargeColor, BadgeTokensKeys.LargeLabelTextColor],
            BadgeValueKeys.LargeVisualColors.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void DefaultVariantsResolveProfilesAndKeepCountStateInTheControl()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var small = new MaterialBadge { Variant = BadgeVariant.Small, Count = 7 };
        var large = new MaterialBadge { Variant = BadgeVariant.Large, Count = 7 };
        var scope = Place(
            new StackPanel { Children = { small, large } },
            theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(6, small.GeometryProfile.Size);
            Assert.Equal(new Thickness(0), small.GeometryProfile.ContentPadding);
            Assert.Equal(ShapeRole.CornerFull, small.GeometryProfile.ContainerShape);
            Assert.False(small.HasLabel);

            Assert.Equal(16, large.GeometryProfile.Size);
            Assert.Equal(new Thickness(4, 0), large.GeometryProfile.ContentPadding);
            Assert.Equal(TypographyRole.LabelSmall, large.GeometryProfile.LabelTextStyle);
            Assert.True(large.HasLabel);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Error),
                ThemeTestHelper.ColorOf(large.VisualColors.ContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnError),
                ThemeTestHelper.ColorOf(large.VisualColors.LabelTextColor));

            large.Count = null;
            Assert.False(large.HasLabel);
            large.Count = 1;
            Assert.True(large.HasLabel);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindAndTrackMutation()
    {
        var badge = new MaterialBadge { Variant = BadgeVariant.Large };
        var scope = Place(badge, new MaterialTheme());
        var referenceToken = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(referenceToken, Colors.Orange);

        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.Error,
            SharedToken.Alias(referenceToken));

        var component = new BadgeTokensOverrides
        {
            LargeColor = SharedToken.Alias(McuSystemColorTokens.Error),
            LargeLabelTextColor = SharedToken.Literal(Colors.White),
            LargeLabelTextFont = SharedToken.Literal(TypographyRole.LabelMedium),
        };

        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(badge.VisualColors.ContainerColor));
            Assert.Equal(Colors.White, ThemeTestHelper.ColorOf(badge.VisualColors.LabelTextColor));
            Assert.Equal(TypographyRole.LabelMedium, badge.GeometryProfile.LabelTextStyle);

            references.Set(referenceToken, Colors.Purple);
            Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(badge.VisualColors.ContainerColor));

            system.Set(
                McuSystemColorTokens.Error,
                SharedToken.Literal(Colors.Green));
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(badge.VisualColors.ContainerColor));

            component.LargeColor = SharedToken.Literal(Colors.HotPink);
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(badge.VisualColors.ContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideOnlyThePresentationEndpoints()
    {
        var badge = new MaterialBadge { Variant = BadgeVariant.Large, Count = 8 };
        var scope = Place(badge, new MaterialTheme());
        var geometry = new BadgeGeometryProfile(
            24,
            ShapeRole.CornerMedium,
            new Thickness(7, 0),
            TypographyRole.LabelLarge);
        var colors = new BadgeVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.CadetBlue));
        scope.Resources[BadgeValueKeys.LargeGeometry] = geometry;
        scope.Resources[BadgeValueKeys.LargeVisualColors] = colors;
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(geometry, badge.GeometryProfile);
            Assert.Same(colors, badge.VisualColors);
            Assert.Equal(24, badge.MinHeight);
            Assert.Equal(new Thickness(7, 0), badge.Padding);
            Assert.Same(colors.LabelTextColor, badge.Foreground);
            Assert.Same(
                geometry,
                SharedTokenTestHelper.Resolve(badge, BadgeValueKeys.LargeGeometry));
            Assert.Same(
                colors,
                SharedTokenTestHelper.Resolve(badge, BadgeValueKeys.LargeVisualColors));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromASiblingBadge()
    {
        var local = new MaterialBadge { Variant = BadgeVariant.Large };
        var sibling = new MaterialBadge { Variant = BadgeVariant.Large };
        var localScope = Place(local, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, sibling } };
        localScope.Resources[BadgeTokensKeys.LargeColor] = Colors.HotPink;
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(local.VisualColors.ContainerColor));
            Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.VisualColors.ContainerColor));

            localScope.Resources[BadgeTokensKeys.LargeColor] = Colors.Green;
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(local.VisualColors.ContainerColor));
            Assert.NotEqual(Colors.Green, ThemeTestHelper.ColorOf(sibling.VisualColors.ContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepPrecedenceOverMaterialProfiles()
    {
        var style = new Style(selector => selector.Is<MaterialBadge>());
        var styledBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialBadge.ForegroundProperty, styledBrush));
        style.Add(new Setter(MaterialBadge.PaddingProperty, new Thickness(9, 0)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialBadge { Variant = BadgeVariant.Large };
            var styledScope = Place(styled, new MaterialTheme());
            var styledWindow = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Same(styledBrush, styled.Foreground);
                Assert.Equal(new Thickness(9, 0), styled.Padding);
            }
            finally
            {
                styledWindow.Close();
            }

            var localBrush = Brush(Colors.HotPink);
            var local = new MaterialBadge
            {
                Variant = BadgeVariant.Large,
                Foreground = localBrush,
                Padding = new Thickness(3, 0),
            };
            var localScope = Place(local, new MaterialTheme());
            var localWindow = ThemeTestHelper.ShowWindow(localScope);
            try
            {
                Assert.Same(localBrush, local.Foreground);
                Assert.Equal(new Thickness(3, 0), local.Padding);
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
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var badge = new MaterialBadge { Variant = BadgeVariant.Large };
        var localScope = Place(badge, new MaterialTheme());
        var root = new Border { Child = localScope };
        var ancestor = new BadgeVisualColors(Brush(Colors.HotPink), Brush(Colors.White));
        root.Resources[BadgeValueKeys.LargeVisualColors] = ancestor;
        localScope.Resources[BadgeTokensKeys.LargeColor] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(badge, BadgeValueKeys.LargeVisualColors, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(BadgeTokensKeys.LargeColor, exception.Token);
        Assert.Same(BadgeValueKeys.LargeVisualColors, exception.ValueKey);
    }

    private static Border Place(Control child, MaterialTheme theme)
    {
        var scope = new Border { Child = child };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}
