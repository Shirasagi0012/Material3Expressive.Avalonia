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

/// <summary>End-to-end contracts for the Checkbox MaterialValue endpoints.</summary>
public sealed class CheckboxMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeTypedProfilesAndStableRawDependencies()
    {
        Assert.Equal(typeof(CheckboxGeometryProfile), CheckboxValueKeys.GeometryProfile.ValueType);
        Assert.Equal(typeof(CheckboxVisualColors), CheckboxValueKeys.VisualColors.ValueType);
        Assert.Equal(typeof(IImmutableBrush), CheckboxValueKeys.LabelBrush.ValueType);

        AssertDependencies(
            CheckboxValueKeys.GeometryProfile,
            CheckboxTokensKeys.ContainerShape,
            CheckboxTokensKeys.StateLayerSize,
            CheckboxTokensKeys.UnselectedOutlineWidth);
        Assert.DoesNotContain(
            CheckboxTokensKeys.IconSize,
            CheckboxValueKeys.GeometryProfile.Plan.Dependencies);
        Assert.DoesNotContain(
            CheckboxTokensKeys.ContainerSize,
            CheckboxValueKeys.GeometryProfile.Plan.Dependencies);

        AssertDependencies(
            CheckboxValueKeys.VisualColors,
            CheckboxTokensKeys.SelectedContainerColor,
            CheckboxTokensKeys.SelectedIconColor,
            CheckboxTokensKeys.UnselectedOutlineColor,
            CheckboxTokensKeys.UnselectedFocusOutlineColor,
            CheckboxTokensKeys.SelectedDisabledContainerColor,
            CheckboxTokensKeys.SelectedDisabledContainerOpacity,
            CheckboxTokensKeys.SelectedDisabledIconColor,
            CheckboxTokensKeys.UnselectedDisabledOutlineColor,
            CheckboxTokensKeys.SelectedErrorContainerColor,
            CheckboxTokensKeys.SelectedErrorIconColor,
            CheckboxTokensKeys.UnselectedErrorOutlineColor);

        Assert.DoesNotContain(
            CheckboxTokensKeys.IconSize,
            CheckboxValueKeys.VisualColors.Plan.Dependencies);
        Assert.DoesNotContain(
            CheckboxTokensKeys.ContainerSize,
            CheckboxValueKeys.VisualColors.Plan.Dependencies);
        AssertDependencies(CheckboxValueKeys.LabelBrush, McuSystemColorTokens.OnSurface);
    }

    [AvaloniaFact]
    public void DefaultProfilesUseCanonicalGeometryAndThemeVariantFallback()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var checkbox = new MaterialCheckbox { IsChecked = true };
        var host = Place(checkbox, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        try
        {
            Assert.Equal(
                new CheckboxGeometryProfile(20, 2, 40, 2),
                checkbox.GeometryProfile);

            var light = Resolve(host, CheckboxValueKeys.VisualColors, ThemeVariant.Light);
            var dark = Resolve(host, CheckboxValueKeys.VisualColors, ThemeVariant.Dark);
            var customDark = Resolve(
                host,
                CheckboxValueKeys.VisualColors,
                new ThemeVariant("HighContrastDark", ThemeVariant.Dark));
            var defaultVariant = Resolve(host, CheckboxValueKeys.VisualColors, ThemeVariant.Default);

            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ColorOf(light.SelectedContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary, isDark: true),
                ColorOf(dark.SelectedContainerColor));
            Assert.Equal(
                ColorOf(dark.SelectedContainerColor),
                ColorOf(customDark.SelectedContainerColor));
            Assert.Equal(
                ColorOf(light.SelectedContainerColor),
                ColorOf(defaultVariant.SelectedContainerColor));

            var lightLabel = ResolveBrush(host, CheckboxValueKeys.LabelBrush, ThemeVariant.Light);
            var darkLabel = ResolveBrush(host, CheckboxValueKeys.LabelBrush, ThemeVariant.Dark);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(lightLabel));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface, isDark: true),
                ColorOf(darkLabel));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindCheckboxProfileAndLabel()
    {
        var checkbox = new MaterialCheckbox { IsChecked = true };
        var theme = new MaterialTheme();
        var scope = Place(checkbox, theme);

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(firstReference));
        system.Set(McuSystemColorTokens.OnSurface, SharedToken.Alias(firstReference));

        var component = new CheckboxTokensOverrides
        {
            SelectedContainerColor = SharedToken.Alias(McuSystemColorTokens.Primary),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Equal(Colors.Orange, ColorOf(checkbox.VisualColors.SelectedContainerColor));
            Assert.Equal(Colors.Orange, ColorOf(Resolve(scope, CheckboxValueKeys.VisualColors).SelectedContainerColor));
            Assert.Equal(Colors.Orange, ColorOf(checkbox.Foreground));

            system.Set(
                McuSystemColorTokens.Primary,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(checkbox.VisualColors.SelectedContainerColor));

            system.Set(
                McuSystemColorTokens.OnSurface,
                SharedToken.Alias(secondReference));
            Assert.Equal(Colors.Purple, ColorOf(checkbox.Foreground));

            references.Set(secondReference, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(checkbox.VisualColors.SelectedContainerColor));
            Assert.Equal(Colors.Green, ColorOf(checkbox.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromSiblingAndRefreshesInPlace()
    {
        var local = new MaterialCheckbox { IsChecked = true };
        var sibling = new MaterialCheckbox { IsChecked = true };
        var localScope = Place(local, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, sibling } };
        var overrides = new CheckboxTokensOverrides
        {
            ContainerShape = SharedToken.Literal(7d),
            SelectedContainerColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(7, local.GeometryProfile.ContainerCornerRadius);
            Assert.Equal(Colors.HotPink, ColorOf(local.VisualColors.SelectedContainerColor));
            Assert.NotEqual(7, sibling.GeometryProfile.ContainerCornerRadius);
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.VisualColors.SelectedContainerColor));

            overrides.ContainerShape = SharedToken.Literal(11d);
            overrides.SelectedContainerColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(11, local.GeometryProfile.ContainerCornerRadius);
            Assert.Equal(Colors.Green, ColorOf(local.VisualColors.SelectedContainerColor));
            Assert.NotEqual(11, sibling.GeometryProfile.ContainerCornerRadius);
            Assert.NotEqual(Colors.Green, ColorOf(sibling.VisualColors.SelectedContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DisabledErrorAndNormalTargetsFollowPriorityAndDisabledAlpha()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var checkbox = new MaterialCheckbox { IsChecked = true };
        var host = Place(checkbox, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        try
        {
            var colors = checkbox.VisualColors;
            var onSurface = ThemeTestHelper.Expected(theme, SysColorToken.OnSurface);
            var expectedDisabled = WithAlpha(onSurface, .38);
            Assert.Equal(expectedDisabled, ColorOf(colors.DisabledSelectedContainerColor));
            Assert.Equal(expectedDisabled, ColorOf(colors.DisabledOutlineColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Surface),
                ColorOf(colors.DisabledSelectedIconColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Error),
                ColorOf(colors.ErrorContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnError),
                ColorOf(colors.ErrorIconColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Error),
                ColorOf(colors.ErrorOutlineColor));

            var normal = VisualTargets(checkbox, true);
            Assert.Equal(ColorOf(colors.SelectedContainerColor), ColorOf(normal.Box));
            Assert.Equal(ColorOf(colors.SelectedIconColor), ColorOf(normal.Check));

            checkbox.IsError = true;
            var error = VisualTargets(checkbox, true);
            Assert.Equal(ColorOf(colors.ErrorContainerColor), ColorOf(error.Box));
            Assert.Equal(ColorOf(colors.ErrorIconColor), ColorOf(error.Check));

            checkbox.IsEnabled = false;
            var disabled = VisualTargets(checkbox, true);
            Assert.Equal(ColorOf(colors.DisabledSelectedContainerColor), ColorOf(disabled.Box));
            Assert.Equal(ColorOf(colors.DisabledSelectedIconColor), ColorOf(disabled.Check));

            checkbox.IsChecked = false;
            var disabledUnselected = VisualTargets(checkbox, false);
            Assert.Equal(ColorOf(colors.DisabledOutlineColor), ColorOf(disabledUnselected.Border));

            checkbox.IsEnabled = true;
            var errorUnselected = VisualTargets(checkbox, false);
            Assert.Equal(ColorOf(colors.ErrorOutlineColor), ColorOf(errorUnselected.Border));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpoints()
    {
        var checkbox = new MaterialCheckbox { IsChecked = true };
        var scope = Place(checkbox, new MaterialTheme());
        var geometry = new CheckboxGeometryProfile(24, 6, 52, 3);
        var colors = TestColors();
        var label = Brush(Colors.CadetBlue);
        scope.Resources[CheckboxValueKeys.GeometryProfile] = geometry;
        scope.Resources[CheckboxValueKeys.VisualColors] = colors;
        scope.Resources[CheckboxValueKeys.LabelBrush] = label;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(geometry, Resolve(scope, CheckboxValueKeys.GeometryProfile));
            Assert.Same(colors, Resolve(scope, CheckboxValueKeys.VisualColors));
            Assert.Same(label, ResolveBrush(scope, CheckboxValueKeys.LabelBrush));
            Assert.Same(geometry, checkbox.GeometryProfile);
            Assert.Same(colors, checkbox.VisualColors);
            Assert.Same(label, checkbox.Foreground);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepForegroundAndPaddingPrecedence()
    {
        var style = new Style(selector => selector.Is<MaterialCheckbox>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialCheckbox.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialCheckbox.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialCheckbox { Content = "Styled" };
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
            var local = new MaterialCheckbox
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
        var key = CheckboxValueKeys.GeometryProfile;
        var checkbox = new MaterialCheckbox();
        var localScope = Place(checkbox, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new CheckboxGeometryProfile(30, 8, 60, 4);
        localScope.Resources[CheckboxTokensKeys.ContainerShape] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(checkbox, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(CheckboxTokensKeys.ContainerShape, exception.Token);
        Assert.Same(key, exception.ValueKey);
    }

    [AvaloniaFact]
    public void AttachDetachAndLegacySnapshotPublicationDoNotReplaceMaterialValueAuthority()
    {
        var checkbox = new MaterialCheckbox { IsChecked = true };
        var host = Place(checkbox, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var overrides = new CheckboxTokensOverrides
        {
            ContainerShape = SharedToken.Literal(9d),
            SelectedContainerColor = SharedToken.Literal(Colors.Orange),
        };
        host.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(host);

        try
        {
            Assert.Equal(9, checkbox.GeometryProfile.ContainerCornerRadius);
            Assert.Equal(Colors.Orange, ColorOf(checkbox.VisualColors.SelectedContainerColor));

            // Publish an intentionally unrelated legacy snapshot. The migrated control must keep
            // reading its typed endpoints from the MaterialTheme placement.
            MaterialThemeScope.SetResolvedTheme(
                host,
                new ResolvedTheme(ThemeTestHelper.ThemeSeededWith(Colors.Purple)));
            Assert.Equal(9, checkbox.GeometryProfile.ContainerCornerRadius);
            Assert.Equal(Colors.Orange, ColorOf(checkbox.VisualColors.SelectedContainerColor));

            host.Child = null;
            host.Child = checkbox;
            Assert.Equal(9, checkbox.GeometryProfile.ContainerCornerRadius);
            Assert.Equal(Colors.Orange, ColorOf(checkbox.VisualColors.SelectedContainerColor));

            host.Resources.MergedDictionaries.Remove(overrides);
            Assert.Equal(2, checkbox.GeometryProfile.ContainerCornerRadius);
            Assert.NotEqual(Colors.Orange, ColorOf(checkbox.VisualColors.SelectedContainerColor));
        }
        finally
        {
            window.Close();
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

    private static Border Place(MaterialCheckbox checkbox, MaterialTheme theme)
    {
        var scope = new Border { Child = checkbox };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static Color ColorOf(IBrush brush) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color WithAlpha(Color color, double opacity) =>
        new(
            (byte)Math.Round(color.A * opacity),
            color.R,
            color.G,
            color.B);

    private static CheckboxVisualColors TestColors() => new(
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
        Brush(Colors.Black));

    private static VisualTargetSnapshot VisualTargets(MaterialCheckbox checkbox, bool? state)
    {
        var method = typeof(MaterialCheckbox).GetMethod(
            "VisualTargets",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var target = method!.Invoke(checkbox, [state]);
        Assert.NotNull(target);
        var type = target!.GetType();
        return new VisualTargetSnapshot(
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("Box")!.GetValue(target)),
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("Border")!.GetValue(target)),
            Assert.IsAssignableFrom<IBrush>(type.GetProperty("Check")!.GetValue(target)));
    }

    private sealed record VisualTargetSnapshot(IBrush Box, IBrush Border, IBrush Check);
}
