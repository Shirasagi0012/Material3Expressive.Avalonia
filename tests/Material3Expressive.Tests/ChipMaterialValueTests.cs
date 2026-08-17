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

/// <summary>End-to-end contracts for the Chip MaterialValue endpoints.</summary>
public sealed class ChipMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeTypedProfilesAndStableRawDependencies()
    {
        Assert.Equal(typeof(ChipGeometryProfile), ChipValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(ChipVisualColors), ChipValueKeys.AssistVisualColors.ValueType);
        Assert.Equal(typeof(TypographyRole), ChipValueKeys.FilterLabelTextStyle.ValueType);

        AssertDependencies(
            ChipValueKeys.GeometryProfile,
            AssistChipTokensKeys.ContainerHeight,
            AssistChipTokensKeys.ContainerShape,
            ChipsTokensKeys.SelectedShape,
            AssistChipTokensKeys.FlatOutlineWidth,
            AssistChipTokensKeys.IconSize);
        AssertDependencies(
            ChipValueKeys.FilterVisualColors,
            FilterChipTokensKeys.UnselectedLabelTextColor,
            FilterChipTokensKeys.UnselectedLeadingIconColor,
            FilterChipTokensKeys.UnselectedTrailingIconColor,
            AssistChipTokensKeys.FlatOutlineColor,
            FilterChipTokensKeys.FlatSelectedContainerColor,
            FilterChipTokensKeys.SelectedLabelTextColor,
            FilterChipTokensKeys.SelectedLeadingIconColor,
            AssistChipTokensKeys.DisabledLabelTextColor,
            AssistChipTokensKeys.DisabledLabelTextOpacity,
            AssistChipTokensKeys.DisabledIconColor,
            AssistChipTokensKeys.DisabledIconOpacity,
            AssistChipTokensKeys.FlatDisabledOutlineColor,
            AssistChipTokensKeys.FlatDisabledOutlineOpacity);
        AssertDependencies(ChipValueKeys.FilterLabelTextStyle, FilterChipTokensKeys.LabelTextFont);
    }

    [AvaloniaFact]
    public void DefaultProfilesUseCanonicalGeometryAndVariantPolicy()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var filter = new MaterialChip { Variant = ChipVariant.Filter, Content = "Filter" };
        var assist = new MaterialChip { Variant = ChipVariant.Assist, Content = "Assist" };
        var host = new StackPanel { Children = { filter, assist } };
        var scope = new Border { Child = host };
        MaterialThemeScope.SetTheme(scope, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(
                new ChipGeometryProfile(
                    32,
                    ShapeRole.CornerSmall,
                    ShapeRole.CornerFull,
                    1,
                    18,
                    new Thickness(16, 0, 16, 0),
                    8),
                filter.GeometryProfile);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant),
                ColorOf(filter.LabelBrush));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ColorOf(filter.IconBrush));
            Assert.Equal(TypographyRole.LabelLarge, TypographyScheme.GetTextStyle(filter));

            filter.IsChecked = true;
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
                ColorOf(filter.LabelBrush));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
                ColorOf(filter.IconBrush));

            assist.IsChecked = true;
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(assist.LabelBrush));
            Assert.Equal(
                ShapeRole.CornerSmall,
                assist.GeometryProfile.ContainerShape);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentAndSystemAliasesRebindVisualProfileAndTypography()
    {
        var chip = new MaterialChip { Variant = ChipVariant.Filter, IsChecked = true };
        var scope = Place(chip, new MaterialTheme());

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(firstReference));
        system.Set(McuSystemColorTokens.OnSurfaceVariant, SharedToken.Alias(firstReference));
        system.Set(McuSystemColorTokens.OnSecondaryContainer, SharedToken.Alias(firstReference));

        var component = new FilterChipTokensOverrides
        {
            SelectedLabelTextColor = SharedToken.Alias(McuSystemColorTokens.OnSecondaryContainer),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ColorOf(chip.LabelBrush));

            system.Set(
                McuSystemColorTokens.OnSecondaryContainer,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(chip.LabelBrush));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(chip.LabelBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsolatedFromSiblingAndRefreshesInPlace()
    {
        var local = new MaterialChip { Variant = ChipVariant.Filter, IsChecked = true };
        var sibling = new MaterialChip { Variant = ChipVariant.Filter, IsChecked = true };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new FilterChipTokensOverrides
        {
            SelectedLabelTextColor = SharedToken.Literal(Colors.HotPink),
            UnselectedLabelTextColor = SharedToken.Literal(Colors.Lime),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(Colors.HotPink, ColorOf(local.LabelBrush));
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.LabelBrush));

            overrides.SelectedLabelTextColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(local.LabelBrush));
            Assert.NotEqual(Colors.Green, ColorOf(sibling.LabelBrush));

            local.IsChecked = false;
            Assert.Equal(Colors.Lime, ColorOf(local.LabelBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpoints()
    {
        var chip = new MaterialChip { Variant = ChipVariant.Input, IsChecked = true };
        var scope = Place(chip, new MaterialTheme());
        var geometry = new ChipGeometryProfile(
            40,
            ShapeRole.CornerMedium,
            ShapeRole.CornerFull,
            3,
            24,
            new Thickness(21, 0, 21, 0),
            9);
        var colors = new ChipVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime),
            Brush(Colors.Cyan),
            Brush(Colors.Yellow),
            Brush(Colors.Purple),
            Brush(Colors.Orange),
            Brush(Colors.Blue),
            Brush(Colors.Green),
            Brush(Colors.Red),
            Brush(Colors.White),
            Brush(Colors.Black),
            Brush(Colors.Gray),
            Brush(Colors.Brown));
        scope.Resources[ChipValueKeys.GeometryProfile] = geometry;
        scope.Resources[ChipValueKeys.InputVisualColors] = colors;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, SharedTokenTestHelper.Resolve(scope, ChipValueKeys.GeometryProfile));
            Assert.Same(colors, SharedTokenTestHelper.Resolve(scope, ChipValueKeys.InputVisualColors));
            Assert.Same(geometry, chip.GeometryProfile);
            Assert.Same(colors, chip.VisualColors);
            Assert.Equal(24, chip.IconSize);
            Assert.Equal(9, chip.IconLabelSpace);
            Assert.Equal(Colors.Green, ColorOf(chip.LabelBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var key = ChipValueKeys.GeometryProfile;
        var chip = new MaterialChip();
        var localScope = Place(chip, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new ChipGeometryProfile(
            30,
            ShapeRole.CornerSmall,
            ShapeRole.CornerFull,
            2,
            18,
            new Thickness(16, 0, 16, 0),
            8);
        localScope.Resources[AssistChipTokensKeys.ContainerHeight] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(chip, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(AssistChipTokensKeys.ContainerHeight, exception.Token);
        Assert.Same(key, exception.ValueKey);
    }

    [AvaloniaFact]
    public void StatePolicyAndApplicationOrLocalValuesKeepTheirPrecedence()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var style = new Style(selector => selector.Is<MaterialChip>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialChip.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialChip.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialChip { Variant = ChipVariant.Filter, IsChecked = true };
            var styledScope = Place(styled, theme);
            var styledWindow = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Same(applicationBrush, styled.Foreground);
                Assert.Equal(new Thickness(7), styled.Padding);

                styled.IsEnabled = false;
                Assert.Equal(
                    WithAlpha(ThemeTestHelper.Expected(
                        theme,
                        SysColorToken.OnSurface), .38),
                    ColorOf(styled.LabelBrush));
            }
            finally
            {
                styledWindow.Close();
            }

            var localBrush = Brush(Colors.HotPink);
            var local = new MaterialChip
            {
                Variant = ChipVariant.Assist,
                Foreground = localBrush,
                Padding = new Thickness(3),
            };
            var localScope = Place(local, new MaterialTheme());
            var localWindow = ThemeTestHelper.ShowWindow(localScope);
            try
            {
                Assert.Same(localBrush, local.Foreground);
                Assert.Equal(new Thickness(3), local.Padding);
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

    private static void AssertDependencies(
        MaterialValueKey key,
        params SharedMaterialToken[] expected)
    {
        Assert.Equal(expected.Length, key.Plan.Dependencies.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], key.Plan.Dependencies[index]);
    }

    private static Border Place(MaterialChip chip, MaterialTheme theme)
    {
        var scope = new Border { Child = chip };
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
