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

/// <summary>End-to-end contracts for the shared Slider and RangeSlider MaterialValue endpoints.</summary>
public sealed class SliderMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeSharedProfilesAndExactRawDependencies()
    {
        Assert.Equal(typeof(SliderGeometryProfile), SliderValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(SliderVisualColors), SliderValueKeys.VisualColors.ValueType);

        AssertDependencies(
            SliderValueKeys.GeometryProfile,
            SliderTokensKeys.ActiveTrackHeight,
            SliderTokensKeys.HandleWidth,
            SliderTokensKeys.PressedHandleWidth,
            SliderTokensKeys.HandleHeight,
            SliderTokensKeys.ActiveHandlePadding,
            SliderTokensKeys.StopIndicatorSize);
        Assert.DoesNotContain(
            SliderTokensKeys.StopIndicatorTrailingSpace,
            SliderValueKeys.GeometryProfile.Plan.Dependencies);

        AssertDependencies(
            SliderValueKeys.VisualColors,
            SliderTokensKeys.ActiveTrackColor,
            SliderTokensKeys.InactiveTrackColor,
            SliderTokensKeys.HandleColor,
            SliderTokensKeys.DisabledActiveTrackColor,
            SliderTokensKeys.DisabledActiveTrackOpacity,
            SliderTokensKeys.DisabledInactiveTrackColor,
            SliderTokensKeys.DisabledInactiveTrackOpacity,
            SliderTokensKeys.DisabledHandleColor,
            SliderTokensKeys.DisabledHandleOpacity);
        Assert.DoesNotContain(
            SliderTokensKeys.StopIndicatorColor,
            SliderValueKeys.VisualColors.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void DefaultProfilesAreSharedAndUseCanonicalThemeColors()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var slider = new MaterialSlider { Value = .4 };
        var range = new MaterialRangeSlider { StartValue = .25, EndValue = .75 };
        var scope = new Border
        {
            Child = new StackPanel { Children = { slider, range } },
        };
        MaterialThemeScope.SetTheme(scope, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var expectedGeometry = new SliderGeometryProfile(16, 4, 2, 44, 6, 4);
            Assert.Equal(expectedGeometry, slider.GeometryProfile);
            Assert.Equal(expectedGeometry, range.GeometryProfile);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ThemeTestHelper.ColorOf(slider.VisualColors.ActiveTrackColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.SecondaryContainer),
                ThemeTestHelper.ColorOf(slider.VisualColors.InactiveTrackColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ThemeTestHelper.ColorOf(range.VisualColors.ActiveTrackColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .38),
                ThemeTestHelper.ColorOf(slider.VisualColors.DisabledHandleColor));
            Assert.Same(slider.VisualColors.HandleColor, slider.VisualColors.StateLayerColor);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentAndReferenceAliasesRefreshBothSliderControlsInPlace()
    {
        var slider = new MaterialSlider { Value = .4 };
        var range = new MaterialRangeSlider { StartValue = .25, EndValue = .75 };
        var scope = new Border
        {
            Child = new StackPanel { Children = { slider, range } },
        };
        MaterialThemeScope.SetTheme(scope, new MaterialTheme());

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(firstReference));

        var component = new SliderTokensOverrides
        {
            ActiveTrackColor = SharedToken.Alias(McuSystemColorTokens.Primary),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(slider.VisualColors.ActiveTrackColor));
            Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(range.VisualColors.ActiveTrackColor));

            system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(slider.VisualColors.ActiveTrackColor));
            Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(range.VisualColors.ActiveTrackColor));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(slider.VisualColors.ActiveTrackColor));
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(range.VisualColors.ActiveTrackColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedAndRefreshesBothControls()
    {
        var localSlider = new MaterialSlider();
        var localRange = new MaterialRangeSlider();
        var siblingSlider = new MaterialSlider();
        var siblingRange = new MaterialRangeSlider();
        var localScope = Place(
            new StackPanel { Children = { localSlider, localRange } },
            new MaterialTheme());
        var siblingScope = Place(
            new StackPanel { Children = { siblingSlider, siblingRange } },
            new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new SliderTokensOverrides
        {
            HandleWidth = SharedToken.Literal(8d),
            InactiveTrackColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(8, localSlider.GeometryProfile.HandleWidth);
            Assert.Equal(8, localRange.GeometryProfile.HandleWidth);
            Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(localSlider.VisualColors.InactiveTrackColor));
            Assert.NotEqual(8, siblingSlider.GeometryProfile.HandleWidth);
            Assert.NotEqual(8, siblingRange.GeometryProfile.HandleWidth);
            Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(siblingRange.VisualColors.InactiveTrackColor));

            overrides.HandleWidth = SharedToken.Literal(10d);
            overrides.InactiveTrackColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(10, localSlider.GeometryProfile.HandleWidth);
            Assert.Equal(10, localRange.GeometryProfile.HandleWidth);
            Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(localSlider.VisualColors.InactiveTrackColor));
            Assert.NotEqual(10, siblingSlider.GeometryProfile.HandleWidth);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpointsForBothControls()
    {
        var slider = new MaterialSlider();
        var range = new MaterialRangeSlider();
        var scope = Place(new StackPanel { Children = { slider, range } }, new MaterialTheme());
        var geometry = new SliderGeometryProfile(20, 7, 3, 50, 9, 5);
        var colors = new SliderVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime),
            Brush(Colors.Cyan),
            Brush(Colors.White),
            Brush(Colors.Black),
            Brush(Colors.Blue),
            Brush(Colors.Orange));
        scope.Resources[SliderValueKeys.GeometryProfile] = geometry;
        scope.Resources[SliderValueKeys.VisualColors] = colors;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, slider.GeometryProfile);
            Assert.Same(geometry, range.GeometryProfile);
            Assert.Same(colors, slider.VisualColors);
            Assert.Same(colors, range.VisualColors);
            Assert.Same(geometry, SharedTokenTestHelper.Resolve(slider, SliderValueKeys.GeometryProfile));
            Assert.Same(colors, SharedTokenTestHelper.Resolve(range, SliderValueKeys.VisualColors));
            Assert.Equal(16, SharedTokenTestHelper.Resolve(slider, SliderTokensKeys.ActiveTrackHeight));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatProfile()
    {
        var slider = new MaterialSlider();
        var localScope = Place(slider, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[SliderValueKeys.GeometryProfile] = new SliderGeometryProfile(30, 6, 2, 44, 6, 4);
        localScope.Resources[SliderTokensKeys.HandleWidth] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(slider, SliderValueKeys.GeometryProfile, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(SliderTokensKeys.HandleWidth, exception.Token);
        Assert.Same(SliderValueKeys.GeometryProfile, exception.ValueKey);
    }

    private static void AssertDependencies(
        MaterialValueKey key,
        params SharedMaterialToken[] expected)
    {
        Assert.Equal(expected.Length, key.Plan.Dependencies.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], key.Plan.Dependencies[index]);
    }

    private static Border Place(Control child, MaterialTheme theme)
    {
        var scope = new Border { Child = child };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static Color WithAlpha(Color color, double opacity) =>
        new(
            (byte)Math.Round(color.A * opacity),
            color.R,
            color.G,
            color.B);
}
