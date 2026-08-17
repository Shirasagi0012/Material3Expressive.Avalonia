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
using SharedMaterialToken = MaterialDesignTokens.Avalonia.MaterialToken;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for segmented-button MaterialValue endpoints.</summary>
public sealed class SegmentedButtonMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeTypedProfilesAndExactRawDependencies()
    {
        Assert.Equal(
            typeof(SegmentedButtonGeometryProfile),
            SegmentedButtonValueKeys.GeometryProfile.ValueType);
        Assert.Equal(
            typeof(SegmentedButtonVisualColors),
            SegmentedButtonValueKeys.VisualColors.ValueType);
        Assert.Equal(typeof(TypographyRole), SegmentedButtonValueKeys.LabelTextStyle.ValueType);

        AssertDependencies(
            SegmentedButtonValueKeys.GeometryProfile,
            OutlinedSegmentedButtonTokensKeys.ContainerHeight,
            OutlinedSegmentedButtonTokensKeys.Shape,
            OutlinedSegmentedButtonTokensKeys.IconSize,
            OutlinedSegmentedButtonTokensKeys.OutlineWidth);
        AssertDependencies(
            SegmentedButtonValueKeys.VisualColors,
            OutlinedSegmentedButtonTokensKeys.SelectedContainerColor,
            OutlinedSegmentedButtonTokensKeys.SelectedLabelTextColor,
            OutlinedSegmentedButtonTokensKeys.OutlineColor,
            OutlinedSegmentedButtonTokensKeys.UnselectedLabelTextColor,
            OutlinedSegmentedButtonTokensKeys.DisabledLabelTextColor,
            OutlinedSegmentedButtonTokensKeys.DisabledLabelTextOpacity,
            OutlinedSegmentedButtonTokensKeys.DisabledOutlineOpacity);
        AssertDependencies(
            SegmentedButtonValueKeys.LabelTextStyle,
            OutlinedSegmentedButtonTokensKeys.LabelTextFont);
    }

    [AvaloniaFact]
    public void DefaultProfilesUseCanonicalGeometryStatesAndTypography()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new MaterialSegmentedButton { IsChecked = true, Content = "Month" };
        var scope = Place(button, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(
                new SegmentedButtonGeometryProfile(
                    40,
                    ShapeRole.CornerFull,
                    18,
                    8,
                    new Thickness(12, 8),
                    58,
                    1),
                button.GeometryProfile);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.SecondaryContainer),
                ColorOf(button.VisualColors.ActiveContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
                ColorOf(button.VisualColors.ActiveContentColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(button.VisualColors.InactiveContentColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .38),
                ColorOf(button.VisualColors.DisabledActiveContentColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.Outline), .12),
                ColorOf(button.VisualColors.DisabledBorderColor));
            Assert.Equal(TypographyRole.LabelLarge, TypographyScheme.GetTextStyle(button));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindVisualProfile()
    {
        var button = new MaterialSegmentedButton { IsChecked = true };
        var scope = Place(button, new MaterialTheme());

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.OnSecondaryContainer, SharedToken.Alias(firstReference));

        var component = new OutlinedSegmentedButtonTokensOverrides
        {
            SelectedLabelTextColor = SharedToken.Alias(McuSystemColorTokens.OnSecondaryContainer),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ColorOf(button.VisualColors.ActiveContentColor));
            Assert.Equal(Colors.Orange, ColorOf(button.Foreground));

            system.Set(
                McuSystemColorTokens.OnSecondaryContainer,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(button.VisualColors.ActiveContentColor));
            Assert.Equal(Colors.Purple, ColorOf(button.Foreground));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(button.VisualColors.ActiveContentColor));
            Assert.Equal(Colors.Green, ColorOf(button.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsolatedFromSiblingAndRefreshesInPlace()
    {
        var local = new MaterialSegmentedButton { IsChecked = true };
        var sibling = new MaterialSegmentedButton { IsChecked = true };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new OutlinedSegmentedButtonTokensOverrides
        {
            IconSize = SharedToken.Literal(28d),
            SelectedLabelTextColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(28, local.GeometryProfile.IconSize);
            Assert.Equal(Colors.HotPink, ColorOf(local.VisualColors.ActiveContentColor));
            Assert.NotEqual(28, sibling.GeometryProfile.IconSize);
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.VisualColors.ActiveContentColor));

            overrides.IconSize = SharedToken.Literal(32d);
            overrides.SelectedLabelTextColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(32, local.GeometryProfile.IconSize);
            Assert.Equal(Colors.Green, ColorOf(local.VisualColors.ActiveContentColor));
            Assert.NotEqual(32, sibling.GeometryProfile.IconSize);
            Assert.NotEqual(Colors.Green, ColorOf(sibling.VisualColors.ActiveContentColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpointsForRowAndSegment()
    {
        var button = new MaterialSegmentedButton { IsChecked = true };
        var row = new MaterialSegmentedButtonRow { Children = { button } };
        var scope = new Border { Child = row };
        MaterialThemeScope.SetTheme(scope, new MaterialTheme());

        var geometry = new SegmentedButtonGeometryProfile(
            44,
            ShapeRole.CornerMedium,
            24,
            9,
            new Thickness(21, 7),
            67,
            3);
        var colors = new SegmentedButtonVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime),
            Brush(Colors.Cyan),
            Brush(Colors.Yellow),
            Brush(Colors.Purple),
            Brush(Colors.Orange),
            Brush(Colors.Blue),
            Brush(Colors.Green),
            Brush(Colors.Red),
            Brush(Colors.White));
        scope.Resources[SegmentedButtonValueKeys.GeometryProfile] = geometry;
        scope.Resources[SegmentedButtonValueKeys.VisualColors] = colors;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, SharedTokenTestHelper.Resolve(scope, SegmentedButtonValueKeys.GeometryProfile));
            Assert.Same(colors, SharedTokenTestHelper.Resolve(scope, SegmentedButtonValueKeys.VisualColors));
            Assert.Same(geometry, button.GeometryProfile);
            Assert.Same(geometry, row.GeometryProfile);
            Assert.Same(colors, button.VisualColors);
            Assert.Equal(24, button.IconSize);
            Assert.Equal(new Thickness(21, 7), button.ContentPadding);
            Assert.Equal(67, button.MinWidth);
            Assert.Equal(Colors.Lime, ColorOf(button.Foreground));
            Assert.Equal(3, row.Overlap);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StatePolicyAndApplicationOrLocalValuesKeepTheirPrecedence()
    {
        var style = new Style(selector => selector.Is<MaterialSegmentedButton>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialSegmentedButton.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialSegmentedButton.ContentPaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialSegmentedButton { IsChecked = true };
            var styledScope = Place(styled, new MaterialTheme());
            var styledWindow = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Same(applicationBrush, styled.Foreground);
                Assert.Equal(new Thickness(7), styled.ContentPadding);
                styled.IsEnabled = false;
                Assert.Equal(
                    ColorOf(styled.VisualColors.DisabledActiveContentColor),
                    ColorOf(styled.IconBrush));
            }
            finally
            {
                styledWindow.Close();
            }

            var localBrush = Brush(Colors.HotPink);
            var local = new MaterialSegmentedButton
            {
                Foreground = localBrush,
                ContentPadding = new Thickness(3),
            };
            var localScope = Place(local, new MaterialTheme());
            var localWindow = ThemeTestHelper.ShowWindow(localScope);
            try
            {
                Assert.Same(localBrush, local.Foreground);
                Assert.Equal(new Thickness(3), local.ContentPadding);
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
        var key = SegmentedButtonValueKeys.GeometryProfile;
        var button = new MaterialSegmentedButton();
        var localScope = Place(button, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new SegmentedButtonGeometryProfile(
            30,
            ShapeRole.CornerSmall,
            18,
            8,
            new Thickness(12, 8),
            58,
            2);
        localScope.Resources[OutlinedSegmentedButtonTokensKeys.ContainerHeight] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(button, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(OutlinedSegmentedButtonTokensKeys.ContainerHeight, exception.Token);
        Assert.Same(key, exception.ValueKey);
    }

    private static void AssertDependencies(
        MaterialValueKey key,
        params SharedMaterialToken[] expected)
    {
        Assert.Equal(expected.Length, key.Plan.Dependencies.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], key.Plan.Dependencies[index]);
    }

    private static Border Place(MaterialSegmentedButton button, MaterialTheme theme)
    {
        var scope = new Border { Child = button };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static Color ColorOf(IBrush? brush) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color WithAlpha(Color color, double opacity) =>
        new(
            (byte)Math.Round(color.A * opacity),
            color.R,
            color.G,
            color.B);
}
