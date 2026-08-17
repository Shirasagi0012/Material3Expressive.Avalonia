using System.Reflection;
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

/// <summary>End-to-end contracts for the MaterialSwitch MaterialValue endpoints.</summary>
public sealed class SwitchMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeTypedProfilesAndExactRawDependencies()
    {
        Assert.Equal(typeof(SwitchGeometryProfile), SwitchValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(SwitchVisualColors), SwitchValueKeys.VisualColors.ValueType);
        Assert.Equal(typeof(IImmutableBrush), SwitchValueKeys.LabelBrush.ValueType);

        AssertDependencies(
            SwitchValueKeys.GeometryProfile,
            SwitchTokensKeys.TrackWidth,
            SwitchTokensKeys.TrackHeight,
            SwitchTokensKeys.TrackOutlineWidth,
            SwitchTokensKeys.UnselectedHandleWidth,
            SwitchTokensKeys.SelectedHandleWidth,
            SwitchTokensKeys.PressedHandleWidth,
            SwitchTokensKeys.StateLayerSize);
        Assert.DoesNotContain(
            SwitchTokensKeys.SelectedIconSize,
            SwitchValueKeys.GeometryProfile.Plan.Dependencies);
        Assert.DoesNotContain(
            SwitchTokensKeys.IconHandleWidth,
            SwitchValueKeys.GeometryProfile.Plan.Dependencies);
        AssertDependencies(
            SwitchValueKeys.VisualColors,
            SwitchTokensKeys.SelectedTrackColor,
            SwitchTokensKeys.SelectedHandleColor,
            SwitchTokensKeys.UnselectedTrackColor,
            SwitchTokensKeys.UnselectedTrackOutlineColor,
            SwitchTokensKeys.UnselectedHandleColor,
            SwitchTokensKeys.DisabledSelectedTrackColor,
            SwitchTokensKeys.DisabledTrackOpacity,
            SwitchTokensKeys.DisabledSelectedHandleColor,
            SwitchTokensKeys.DisabledSelectedHandleOpacity,
            SwitchTokensKeys.DisabledUnselectedTrackColor,
            SwitchTokensKeys.DisabledUnselectedTrackOutlineColor,
            SwitchTokensKeys.DisabledUnselectedHandleColor,
            SwitchTokensKeys.DisabledUnselectedHandleOpacity);
        AssertDependencies(SwitchValueKeys.LabelBrush, McuSystemColorTokens.OnSurface);
    }

    [AvaloniaFact]
    public void DefaultProfilesUseCanonicalGeometryAndThemeVariantFallback()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var toggle = new MaterialSwitch { IsChecked = true, Content = "Switch" };
        var scope = Place(toggle, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(
                new SwitchGeometryProfile(52, 32, 2, 16, 24, 28, 40),
                toggle.GeometryProfile);

            var light = Resolve(scope, SwitchValueKeys.VisualColors, ThemeVariant.Light);
            var dark = Resolve(scope, SwitchValueKeys.VisualColors, ThemeVariant.Dark);
            var customDark = Resolve(
                scope,
                SwitchValueKeys.VisualColors,
                new ThemeVariant("HighContrastDark", ThemeVariant.Dark));
            var defaultVariant = Resolve(scope, SwitchValueKeys.VisualColors, ThemeVariant.Default);

            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ColorOf(light.SelectedTrackColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary, isDark: true),
                ColorOf(dark.SelectedTrackColor));
            Assert.Equal(
                ColorOf(dark.SelectedTrackColor),
                ColorOf(customDark.SelectedTrackColor));
            Assert.Equal(
                ColorOf(light.SelectedTrackColor),
                ColorOf(defaultVariant.SelectedTrackColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary),
                ColorOf(light.SelectedHandleColor));

            var label = ResolveBrush(scope, SwitchValueKeys.LabelBrush, ThemeVariant.Light);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(label));
            Assert.Equal(ColorOf(label), ColorOf(toggle.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindSwitchProfileAndLabel()
    {
        var toggle = new MaterialSwitch { IsChecked = true };
        var scope = Place(toggle, new MaterialTheme());

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(firstReference));
        system.Set(McuSystemColorTokens.OnSurface, SharedToken.Alias(firstReference));

        var component = new SwitchTokensOverrides
        {
            SelectedTrackColor = SharedToken.Alias(McuSystemColorTokens.Primary),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ColorOf(toggle.VisualColors.SelectedTrackColor));
            Assert.Equal(Colors.Orange, ColorOf(toggle.Foreground));

            system.Set(
                McuSystemColorTokens.Primary,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(toggle.VisualColors.SelectedTrackColor));

            system.Set(
                McuSystemColorTokens.OnSurface,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(toggle.Foreground));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(toggle.VisualColors.SelectedTrackColor));
            Assert.Equal(Colors.Green, ColorOf(toggle.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromSiblingAndRefreshesInPlace()
    {
        var local = new MaterialSwitch { IsChecked = true };
        var sibling = new MaterialSwitch { IsChecked = true };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new SwitchTokensOverrides
        {
            TrackWidth = SharedToken.Literal(68d),
            SelectedTrackColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(68, local.GeometryProfile.TrackWidth);
            Assert.Equal(Colors.HotPink, ColorOf(local.VisualColors.SelectedTrackColor));
            Assert.NotEqual(68, sibling.GeometryProfile.TrackWidth);
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.VisualColors.SelectedTrackColor));

            overrides.TrackWidth = SharedToken.Literal(72d);
            overrides.SelectedTrackColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(72, local.GeometryProfile.TrackWidth);
            Assert.Equal(Colors.Green, ColorOf(local.VisualColors.SelectedTrackColor));
            Assert.NotEqual(72, sibling.GeometryProfile.TrackWidth);
            Assert.NotEqual(Colors.Green, ColorOf(sibling.VisualColors.SelectedTrackColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CheckedAndDisabledTargetsUseTheSingleVisualProfile()
    {
        var toggle = new MaterialSwitch { IsChecked = true };
        var scope = Place(toggle, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var selected = VisualTargets(toggle, enabled: true, selected: true);
            Assert.Equal(
                ColorOf(toggle.VisualColors.SelectedTrackColor),
                ColorOf(selected.TrackColor));
            Assert.Null(selected.TrackOutlineColor);
            Assert.Equal(
                ColorOf(toggle.VisualColors.SelectedHandleColor),
                ColorOf(selected.HandleColor));

            var disabled = VisualTargets(toggle, enabled: false, selected: false);
            Assert.Equal(
                ColorOf(toggle.VisualColors.DisabledUnselectedTrackColor),
                ColorOf(disabled.TrackColor));
            Assert.Equal(
                ColorOf(toggle.VisualColors.DisabledUnselectedTrackOutlineColor),
                ColorOf(disabled.TrackOutlineColor));
            Assert.Equal(
                ColorOf(toggle.VisualColors.DisabledUnselectedHandleColor),
                ColorOf(disabled.HandleColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DisabledBrushesApplyDeclaredRawOpacity()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var toggle = new MaterialSwitch { IsChecked = true };
        var scope = Place(toggle, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .12),
                ColorOf(toggle.VisualColors.DisabledSelectedTrackColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Surface),
                ColorOf(toggle.VisualColors.DisabledSelectedHandleColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .38),
                ColorOf(toggle.VisualColors.DisabledUnselectedHandleColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpoints()
    {
        var toggle = new MaterialSwitch { IsChecked = true };
        var scope = Place(toggle, new MaterialTheme());
        var geometry = new SwitchGeometryProfile(64, 36, 3, 18, 26, 30, 44);
        var colors = TestColors();
        var label = Brush(Colors.CadetBlue);
        scope.Resources[SwitchValueKeys.GeometryProfile] = geometry;
        scope.Resources[SwitchValueKeys.VisualColors] = colors;
        scope.Resources[SwitchValueKeys.LabelBrush] = label;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, Resolve(scope, SwitchValueKeys.GeometryProfile));
            Assert.Same(colors, Resolve(scope, SwitchValueKeys.VisualColors));
            Assert.Same(label, ResolveBrush(scope, SwitchValueKeys.LabelBrush));
            Assert.Same(geometry, toggle.GeometryProfile);
            Assert.Same(colors, toggle.VisualColors);
            Assert.Same(label, toggle.Foreground);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepForegroundAndPaddingPrecedence()
    {
        var style = new Style(selector => selector.Is<MaterialSwitch>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialSwitch.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialSwitch.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialSwitch { Content = "Styled" };
            var styledScope = Place(styled, new MaterialTheme());
            var styledWindow = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Same(applicationBrush, styled.Foreground);
                Assert.Equal(new Thickness(7), styled.Padding);
            }
            finally
            {
                styledWindow.Close();
            }

            var localBrush = Brush(Colors.HotPink);
            var local = new MaterialSwitch
            {
                Content = "Local",
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

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var key = SwitchValueKeys.GeometryProfile;
        var toggle = new MaterialSwitch();
        var localScope = Place(toggle, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new SwitchGeometryProfile(30, 28, 2, 14, 22, 26, 36);
        localScope.Resources[SwitchTokensKeys.TrackWidth] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(toggle, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(SwitchTokensKeys.TrackWidth, exception.Token);
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

    private static T Resolve<T>(
        IResourceHost host,
        MaterialValueKey<T> key,
        ThemeVariant? theme = null) =>
        SharedTokenTestHelper.Resolve(host, key, theme);

    private static IImmutableBrush ResolveBrush(
        IResourceHost host,
        MaterialValueKey<IImmutableBrush> key,
        ThemeVariant? theme = null)
    {
        if (!host.TryFindResource(key, theme, out var value))
            throw new InvalidOperationException($"Resource '{key.Path}' was not found.");

        return Assert.IsAssignableFrom<IImmutableBrush>(value);
    }

    private static Border Place(MaterialSwitch toggle, MaterialTheme theme)
    {
        var scope = new Border { Child = toggle };
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

    private static SwitchVisualColors TestColors() => new(
        Brush(Colors.HotPink),
        Brush(Colors.Lime),
        Brush(Colors.Yellow),
        Brush(Colors.Purple),
        Brush(Colors.Orange),
        Brush(Colors.White),
        Brush(Colors.Black),
        Brush(Colors.Cyan),
        Brush(Colors.Blue),
        Brush(Colors.Green),
        Brush(Colors.Red),
        Brush(Colors.Brown));

    private static SwitchRenderTarget VisualTargets(
        MaterialSwitch toggle,
        bool enabled,
        bool selected)
    {
        var method = typeof(MaterialSwitch).GetMethod(
            "VisualTargets",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var target = method!.Invoke(toggle, [enabled, selected]);
        Assert.NotNull(target);
        var type = target!.GetType();
        return new SwitchRenderTarget(
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("TrackColor")!.GetValue(target)),
            type.GetProperty("TrackOutlineColor")!.GetValue(target) as IBrush,
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("HandleColor")!.GetValue(target)),
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("StateLayerColor")!.GetValue(target)));
    }

    private sealed record SwitchRenderTarget(
        IBrush TrackColor,
        IBrush? TrackOutlineColor,
        IBrush HandleColor,
        IBrush StateLayerColor);
}
