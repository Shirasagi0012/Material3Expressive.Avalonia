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

/// <summary>End-to-end contracts for the RadioButton MaterialValue endpoints.</summary>
public sealed class RadioButtonMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeTypedProfilesAndExactRawDependencies()
    {
        Assert.Equal(typeof(RadioButtonGeometryProfile), RadioButtonValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(RadioButtonVisualColors), RadioButtonValueKeys.VisualColors.ValueType);
        Assert.Equal(typeof(IImmutableBrush), RadioButtonValueKeys.LabelBrush.ValueType);

        AssertDependencies(
            RadioButtonValueKeys.GeometryProfile,
            RadioButtonTokensKeys.IconSize,
            RadioButtonTokensKeys.StateLayerSize);
        AssertDependencies(
            RadioButtonValueKeys.VisualColors,
            RadioButtonTokensKeys.SelectedIconColor,
            RadioButtonTokensKeys.UnselectedIconColor,
            RadioButtonTokensKeys.DisabledSelectedIconColor,
            RadioButtonTokensKeys.DisabledSelectedIconOpacity,
            RadioButtonTokensKeys.DisabledUnselectedIconColor,
            RadioButtonTokensKeys.DisabledUnselectedIconOpacity);
        AssertDependencies(RadioButtonValueKeys.LabelBrush, McuSystemColorTokens.OnSurface);
    }

    [AvaloniaFact]
    public void DefaultProfilesUseCanonicalGeometryStatesAndLabel()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var radio = new MaterialRadioButton { IsChecked = true, Content = "Radio" };
        var scope = Place(radio, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(new RadioButtonGeometryProfile(20, 40, 2, 10), radio.GeometryProfile);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ColorOf(radio.VisualColors.SelectedColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant),
                ColorOf(radio.VisualColors.UnselectedColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .38),
                ColorOf(radio.VisualColors.DisabledSelectedColor));
            Assert.Equal(
                WithAlpha(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), .38),
                ColorOf(radio.VisualColors.DisabledUnselectedColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(radio.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindProfilesAndLabel()
    {
        var radio = new MaterialRadioButton { IsChecked = true };
        var scope = Place(radio, new MaterialTheme());

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(firstReference));
        system.Set(McuSystemColorTokens.OnSurface, SharedToken.Alias(firstReference));

        var component = new RadioButtonTokensOverrides
        {
            SelectedIconColor = SharedToken.Alias(McuSystemColorTokens.Primary),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ColorOf(radio.VisualColors.SelectedColor));
            Assert.Equal(Colors.Orange, ColorOf(radio.Foreground));

            system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(radio.VisualColors.SelectedColor));

            system.Set(McuSystemColorTokens.OnSurface, SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(radio.Foreground));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(radio.VisualColors.SelectedColor));
            Assert.Equal(Colors.Green, ColorOf(radio.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsolatedFromSiblingAndRefreshesInPlace()
    {
        var local = new MaterialRadioButton { IsChecked = true };
        var sibling = new MaterialRadioButton { IsChecked = true };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new RadioButtonTokensOverrides
        {
            IconSize = SharedToken.Literal(28d),
            SelectedIconColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(28, local.GeometryProfile.IconSize);
            Assert.Equal(Colors.HotPink, ColorOf(local.VisualColors.SelectedColor));
            Assert.NotEqual(28, sibling.GeometryProfile.IconSize);
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.VisualColors.SelectedColor));

            overrides.IconSize = SharedToken.Literal(32d);
            overrides.SelectedIconColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(32, local.GeometryProfile.IconSize);
            Assert.Equal(Colors.Green, ColorOf(local.VisualColors.SelectedColor));
            Assert.NotEqual(32, sibling.GeometryProfile.IconSize);
            Assert.NotEqual(Colors.Green, ColorOf(sibling.VisualColors.SelectedColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpoints()
    {
        var radio = new MaterialRadioButton { IsChecked = true };
        var scope = Place(radio, new MaterialTheme());
        var geometry = new RadioButtonGeometryProfile(24, 52, 3, 11);
        var colors = new RadioButtonVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime),
            Brush(Colors.Cyan),
            Brush(Colors.Yellow));
        var label = Brush(Colors.CadetBlue);
        scope.Resources[RadioButtonValueKeys.GeometryProfile] = geometry;
        scope.Resources[RadioButtonValueKeys.VisualColors] = colors;
        scope.Resources[RadioButtonValueKeys.LabelBrush] = label;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, SharedTokenTestHelper.Resolve(scope, RadioButtonValueKeys.GeometryProfile));
            Assert.Same(colors, SharedTokenTestHelper.Resolve(scope, RadioButtonValueKeys.VisualColors));
            Assert.Same(label, ResolveBrush(scope, RadioButtonValueKeys.LabelBrush));
            Assert.Same(geometry, radio.GeometryProfile);
            Assert.Same(colors, radio.VisualColors);
            Assert.Same(label, radio.Foreground);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StatesUseTheProfileWithoutRebuildingGeometryOrMotion()
    {
        var radio = new MaterialRadioButton { IsChecked = true };
        var scope = Place(radio, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var interaction = MaterialElementContext.Ensure(radio).Interaction;
            Assert.Equal(20, radio.GeometryProfile.IconSize);
            Assert.True(radio.Bounds.Height >= radio.GeometryProfile.StateLayerSize);
            Assert.Equal(20, interaction.RippleRadius);

            radio.IsChecked = false;
            Assert.Equal(40, radio.GeometryProfile.StateLayerSize);
            radio.IsEnabled = false;
            Assert.InRange(ColorOf(radio.VisualColors.DisabledSelectedColor).A, (byte)96, (byte)98);
            Assert.NotEqual(
                ColorOf(radio.VisualColors.UnselectedColor),
                ColorOf(radio.VisualColors.DisabledUnselectedColor));
            Assert.Equal(20, interaction.RippleRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepForegroundAndPaddingPrecedence()
    {
        var style = new Style(selector => selector.Is<MaterialRadioButton>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialRadioButton.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialRadioButton.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialRadioButton { Content = "Styled" };
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
            var local = new MaterialRadioButton
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
        var key = RadioButtonValueKeys.GeometryProfile;
        var radio = new MaterialRadioButton();
        var localScope = Place(radio, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new RadioButtonGeometryProfile(30, 60, 4, 12);
        localScope.Resources[RadioButtonTokensKeys.IconSize] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(radio, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(RadioButtonTokensKeys.IconSize, exception.Token);
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

    private static Border Place(MaterialRadioButton radio, MaterialTheme theme)
    {
        var scope = new Border { Child = radio };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush ResolveBrush(
        IResourceHost host,
        MaterialValueKey<IImmutableBrush> key,
        ThemeVariant? theme = null)
    {
        if (!host.TryFindResource(key, theme, out var value))
            throw new InvalidOperationException($"Resource '{key.Path}' was not found.");

        return Assert.IsAssignableFrom<IImmutableBrush>(value);
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
