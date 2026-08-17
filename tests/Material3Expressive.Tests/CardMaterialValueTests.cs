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
using MaterialTokenOverrides = Material3Expressive.Foundation.Tokens.Runtime.MaterialTokenOverrides;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the Card MaterialValue profiles and late policies.</summary>
public sealed class CardMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeStableTypesAndExactVariantDependencies()
    {
        Assert.Equal(typeof(CardVisualColors), CardValueKeys.ElevatedVisualColors.ValueType);
        Assert.Equal(typeof(CardGeometryProfile), CardValueKeys.OutlinedGeometry.ValueType);
        Assert.Equal(typeof(CardElevationProfile), CardValueKeys.FilledElevation.ValueType);

        Assert.Equal(
            [
                ElevatedCardTokensKeys.ContainerColor,
                ElevatedCardTokensKeys.DisabledContainerColor,
                ElevatedCardTokensKeys.DisabledContainerOpacity,
                McuSystemColorTokens.OnSurface,
                McuSystemColorTokens.Shadow,
            ],
            CardValueKeys.ElevatedVisualColors.Plan.Dependencies);
        Assert.Equal(
            [OutlinedCardTokensKeys.ContainerColor, OutlinedCardTokensKeys.ContainerColor,
             OutlinedCardTokensKeys.OutlineColor, OutlinedCardTokensKeys.DisabledOutlineColor,
             OutlinedCardTokensKeys.DisabledOutlineOpacity, McuSystemColorTokens.OnSurface,
             McuSystemColorTokens.Shadow],
            CardValueKeys.OutlinedVisualColors.Plan.Dependencies);
        Assert.Equal(
            [OutlinedCardTokensKeys.ContainerShape, OutlinedCardTokensKeys.OutlineWidth],
            CardValueKeys.OutlinedGeometry.Plan.Dependencies);
        Assert.Equal(
            [
                FilledCardTokensKeys.ContainerElevation,
                FilledCardTokensKeys.HoverContainerElevation,
                FilledCardTokensKeys.FocusContainerElevation,
                FilledCardTokensKeys.PressedContainerElevation,
                FilledCardTokensKeys.DraggedContainerElevation,
                FilledCardTokensKeys.DisabledContainerElevation,
            ],
            CardValueKeys.FilledElevation.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void ThreeVariantsResolveTheirDefaultProfiles()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var elevated = new MaterialCard { Variant = CardVariant.Elevated, Width = 120, Height = 60 };
        var filled = new MaterialCard { Variant = CardVariant.Filled, Width = 120, Height = 60 };
        var outlined = new MaterialCard { Variant = CardVariant.Outlined, Width = 120, Height = 60 };
        var scope = new Border
        {
            Child = new StackPanel { Children = { elevated, filled, outlined } },
        };
        MaterialThemeScope.SetTheme(scope, theme);
        ThemeTestHelper.ShowWindow(scope);


        AssertCardDefaults(
            elevated,
            ThemeTestHelper.Expected(theme, SysColorToken.SurfaceContainerLow),
            WithOpacity(ThemeTestHelper.Expected(theme, SysColorToken.Surface), .38),
            borderWidth: 0,
            restingElevation: 1);
        AssertCardDefaults(
            filled,
            ThemeTestHelper.Expected(theme, SysColorToken.SurfaceContainerHighest),
            WithOpacity(ThemeTestHelper.Expected(theme, SysColorToken.SurfaceVariant), .38),
            borderWidth: 0,
            restingElevation: 0);
        AssertCardDefaults(
            outlined,
            ThemeTestHelper.Expected(theme, SysColorToken.Surface),
            ThemeTestHelper.Expected(theme, SysColorToken.Surface),
            borderWidth: 1,
            restingElevation: 0);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant),
            ThemeTestHelper.ColorOf(outlined.BorderBrush));
    }

    [AvaloniaFact]
    public void EnabledAndDisabledProfilesUseTheCorrectVariantStateValues()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var elevated = new MaterialCard { Variant = CardVariant.Elevated, IsEnabled = false };
        var outlined = new MaterialCard { Variant = CardVariant.Outlined, IsEnabled = false };
        var scope = new Border
        {
            Child = new StackPanel { Children = { elevated, outlined } },
        };
        MaterialThemeScope.SetTheme(scope, theme);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(
            WithOpacity(ThemeTestHelper.Expected(theme, SysColorToken.Surface), .38),
            ThemeTestHelper.ColorOf(elevated.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Surface),
            ThemeTestHelper.ColorOf(outlined.Background));
        Assert.Equal(
            WithOpacity(ThemeTestHelper.Expected(theme, SysColorToken.Outline), .12),
            ThemeTestHelper.ColorOf(outlined.BorderBrush));
        Assert.Equal(1, outlined.BorderThickness.Left);
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceOverridesRebindAndTrackMutation()
    {
        var card = new MaterialCard { Variant = CardVariant.Filled };
        var scope = Place(card, new MaterialTheme());
        var referenceToken = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(referenceToken, Colors.Orange);
        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.SurfaceContainerHighest,
            SharedToken.Alias(referenceToken));
        var component = new FilledCardTokensOverrides
        {
            ContainerColor = SharedToken.Alias(McuSystemColorTokens.SurfaceContainerHighest),
        };

        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(card.Background));

        references.Set(referenceToken, Colors.Purple);
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(card.Background));

        system.Set(
            McuSystemColorTokens.SurfaceContainerHighest,
            SharedToken.Literal(Colors.Green));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(card.Background));

        component.ContainerColor = SharedToken.Literal(Colors.HotPink);
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(card.Background));
    }

    [AvaloniaFact]
    public void FlatVisualProfileOverrideReplacesOnlyThePresentationEndpoint()
    {
        var card = new MaterialCard { Variant = CardVariant.Filled };
        var scope = Place(card, new MaterialTheme());
        var custom = new CardVisualColors(
            new ImmutableSolidColorBrush(Colors.HotPink),
            new ImmutableSolidColorBrush(Colors.Purple),
            new ImmutableSolidColorBrush(Colors.Transparent),
            new ImmutableSolidColorBrush(Colors.Transparent),
            new ImmutableSolidColorBrush(Colors.Blue),
            Colors.Black);
        scope.Resources[CardValueKeys.FilledVisualColors] = custom;
        ThemeTestHelper.ShowWindow(scope);

        Assert.Same(custom.ContainerColor, card.Background);
        Assert.Same(custom.OutlineColor, card.BorderBrush);
        Assert.Same(custom, SharedTokenTestHelper.Resolve(card, CardValueKeys.FilledVisualColors));
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromASiblingCard()
    {
        var local = new MaterialCard { Variant = CardVariant.Filled };
        var sibling = new MaterialCard { Variant = CardVariant.Filled };
        var localScope = Place(local, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, sibling } };
        localScope.Resources.MergedDictionaries.Add(new FilledCardTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.HotPink),
        });
        ThemeTestHelper.ShowWindow(root);

        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(local.Background));
        Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.Background));
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepPrecedenceOverMaterialProfiles()
    {
        var style = new Style(selector => selector.Is<MaterialCard>());
        var styledBackground = new ImmutableSolidColorBrush(Colors.Orange);
        var styledBorder = new ImmutableSolidColorBrush(Colors.Blue);
        style.Add(new Setter(MaterialCard.BackgroundProperty, styledBackground));
        style.Add(new Setter(MaterialCard.BorderBrushProperty, styledBorder));
        style.Add(new Setter(MaterialCard.BorderThicknessProperty, new Thickness(5)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialCard { Variant = CardVariant.Outlined };
            var styledScope = Place(styled, new MaterialTheme());
            ThemeTestHelper.ShowWindow(styledScope);
            Assert.Same(styledBackground, styled.Background);
            Assert.Same(styledBorder, styled.BorderBrush);
            Assert.Equal(new Thickness(5), styled.BorderThickness);

            var localBackground = new ImmutableSolidColorBrush(Colors.HotPink);
            var localBorder = new ImmutableSolidColorBrush(Colors.Green);
            var local = new MaterialCard
            {
                Variant = CardVariant.Outlined,
                Background = localBackground,
                BorderBrush = localBorder,
                BorderThickness = new Thickness(7),
            };
            var localScope = Place(local, new MaterialTheme());
            ThemeTestHelper.ShowWindow(localScope);
            Assert.Same(localBackground, local.Background);
            Assert.Same(localBorder, local.BorderBrush);
            Assert.Equal(new Thickness(7), local.BorderThickness);
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void InteractionAndShapeRemainLatePoliciesOverResolvedProfiles()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var card = new MaterialCard
        {
            Variant = CardVariant.Elevated,
            IsClickable = true,
            Width = 120,
            Height = 60,
        };
        var scope = Place(card, theme);
        ThemeTestHelper.ShowWindow(scope);

        Assert.True(card.Focusable);
        var geometry = Assert.IsType<CardGeometryProfile>(card.ResolvedGeometry);
        var elevation = Assert.IsType<CardElevationProfile>(card.ResolvedElevation);
        Assert.Equal(
            new ResolvedShapeScheme(theme.ShapeScheme).GetCornerRadius(
                geometry.ContainerShape,
                new Size(120, 60)),
            new ResolvedShapeScheme(theme.ShapeScheme).GetCornerRadius(
                geometry.ContainerShape,
                new Size(120, 60)));

        Assert.Equal(elevation.Resting, elevation.For(true, InteractionState.None));
        Assert.Equal(elevation.Hovered, elevation.For(true, InteractionState.Hovered));
        Assert.Equal(elevation.Pressed, elevation.For(true, InteractionState.Pressed));
        Assert.Equal(elevation.Disabled, elevation.For(false, InteractionState.Hovered));
        Assert.Equal(elevation.Hovered, elevation.For(true, InteractionState.Hovered | InteractionState.Focused));

        card.IsClickable = false;
        Assert.False(card.Focusable);
    }

    [AvaloniaFact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatProfile()
    {
        var card = new MaterialCard { Variant = CardVariant.Filled };
        var localScope = Place(card, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[CardValueKeys.FilledVisualColors] = new CardVisualColors(
            new ImmutableSolidColorBrush(Colors.HotPink),
            new ImmutableSolidColorBrush(Colors.Purple),
            new ImmutableSolidColorBrush(Colors.Transparent),
            new ImmutableSolidColorBrush(Colors.Transparent),
            new ImmutableSolidColorBrush(Colors.Blue),
            Colors.Black);
        localScope.Resources[FilledCardTokensKeys.ContainerColor] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(card, CardValueKeys.FilledVisualColors, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(FilledCardTokensKeys.ContainerColor, exception.Token);
        Assert.Same(CardValueKeys.FilledVisualColors, exception.ValueKey);
    }

    private static Border Place(MaterialCard card, MaterialTheme theme)
    {
        var scope = new Border { Child = card };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static void AssertCardDefaults(
        MaterialCard card,
        Color enabled,
        Color disabled,
        double borderWidth,
        double restingElevation)
    {
        Assert.Equal(enabled, ThemeTestHelper.ColorOf(card.Background));
        Assert.Equal(disabled, ThemeTestHelper.ColorOf(card.ResolvedVisualColors!.DisabledContainerColor));
        Assert.Equal(borderWidth, card.BorderThickness.Left);
        Assert.Equal(restingElevation, card.ResolvedElevation!.Resting);
        Assert.Equal(ShapeRole.CornerMedium, card.ResolvedGeometry!.ContainerShape);
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb(
            (byte)Math.Round(color.A * opacity),
            color.R,
            color.G,
            color.B);
}
