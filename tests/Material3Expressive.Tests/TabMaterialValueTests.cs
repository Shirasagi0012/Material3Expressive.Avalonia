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
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedMaterialToken = MaterialDesignTokens.Avalonia.MaterialToken;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the Material tabs value endpoints.</summary>
public sealed class TabMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeProfilesAndOnlyDeclaredRawDependencies()
    {
        Assert.Equal(typeof(TabItemGeometryProfile), TabValueKeys.PrimaryItemGeometry.ValueType);
        Assert.Equal(typeof(TabItemGeometryProfile), TabValueKeys.SecondaryItemGeometry.ValueType);
        Assert.Equal(typeof(TabRowGeometryProfile), TabValueKeys.PrimaryRowGeometry.ValueType);
        Assert.Equal(typeof(TabRowGeometryProfile), TabValueKeys.SecondaryRowGeometry.ValueType);
        Assert.Equal(typeof(TabItemVisualColors), TabValueKeys.PrimaryItemVisualColors.ValueType);
        Assert.Equal(typeof(TabItemVisualColors), TabValueKeys.SecondaryItemVisualColors.ValueType);
        Assert.Equal(typeof(TabRowVisualColors), TabValueKeys.PrimaryRowVisualColors.ValueType);
        Assert.Equal(typeof(TabRowVisualColors), TabValueKeys.SecondaryRowVisualColors.ValueType);

        Assert.Empty(TabValueKeys.PrimaryItemGeometry.Plan.Dependencies);
        AssertDependencies(
            TabValueKeys.SecondaryItemGeometry,
            SecondaryNavigationTabTokensKeys.ContainerHeight,
            SecondaryNavigationTabTokensKeys.IconSize,
            SecondaryNavigationTabTokensKeys.LabelTextFont);
        Assert.Empty(TabValueKeys.PrimaryRowGeometry.Plan.Dependencies);
        AssertDependencies(TabValueKeys.SecondaryRowGeometry, SecondaryNavigationTabTokensKeys.DividerHeight);

        AssertDependencies(
            TabValueKeys.PrimaryItemVisualColors,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.Primary);
        AssertDependencies(
            TabValueKeys.SecondaryItemVisualColors,
            SecondaryNavigationTabTokensKeys.ActiveIconColor,
            SecondaryNavigationTabTokensKeys.ActiveLabelTextColor,
            SecondaryNavigationTabTokensKeys.ActiveIconColor,
            SecondaryNavigationTabTokensKeys.ActiveLabelTextColor,
            McuSystemColorTokens.Primary);
        AssertDependencies(
            TabValueKeys.PrimaryRowVisualColors,
            McuSystemColorTokens.Surface,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.OutlineVariant);
        AssertDependencies(
            TabValueKeys.SecondaryRowVisualColors,
            SecondaryNavigationTabTokensKeys.ContainerColor,
            McuSystemColorTokens.Primary,
            McuSystemColorTokens.OutlineVariant);
    }

    [AvaloniaFact]
    public void PrimaryAndSecondarySelectorsResolveDifferentProfiles()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var primary = CreateTabs(TabVariant.Primary);
        var secondary = CreateTabs(TabVariant.Secondary);
        var host = new StackPanel { Children = { primary, secondary } };
        MaterialThemeScope.SetTheme(host, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        try
        {
            Assert.Equal(new TabItemGeometryProfile(48, 64, 24, TypographyRole.TitleSmall), primary.Items[0].GeometryProfile);
            Assert.Equal(new TabItemGeometryProfile(48, 64, 24, TypographyRole.TitleSmall), secondary.Items[0].GeometryProfile);
            Assert.Equal(new TabRowGeometryProfile(3, 3, 1, true), primary.GeometryProfile);
            Assert.Equal(new TabRowGeometryProfile(3, 0, 1, false), secondary.GeometryProfile);
            Assert.True(primary.Items[0].IsChecked);
            Assert.False(primary.Items[1].IsChecked);
            Assert.True(secondary.Items[0].IsChecked);
            Assert.False(secondary.Items[1].IsChecked);
            Assert.Equal(
                ColorOf(primary.Items[0].VisualColors.ActiveLabelTextColor),
                ColorOf(primary.Items[0].EffectiveForeground));

            primary.Items[0].IsEnabled = false;
            Assert.True(primary.Items[0].IsChecked);
            Assert.Equal(24, primary.Items[0].IconSize);
            Assert.Equal(
                ThemeTestHelper.Expected(
                    theme,
                    SysColorToken.Primary),
                ColorOf(primary.Items[0].IconBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FrameworkInheritedForegroundDoesNotMaskMaterialValueLabelColor()
    {
        var tabs = CreateTabs(TabVariant.Primary);
        var scope = Place(tabs, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            var tab = tabs.Items[0];
            Assert.Equal(
                ThemeTestHelper.Expected(ThemeTestHelper.ThemeSeededWith(Colors.Teal), SysColorToken.Primary),
                ColorOf(tab.EffectiveForeground));
            tab.IsChecked = false;
            Assert.Equal(
                ThemeTestHelper.Expected(ThemeTestHelper.ThemeSeededWith(Colors.Teal), SysColorToken.OnSurfaceVariant),
                ColorOf(tab.EffectiveForeground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RawAliasMutationAndPlacementScopeRefreshProfiles()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var local = CreateTabs(TabVariant.Secondary);
        var sibling = CreateTabs(TabVariant.Secondary);
        var localScope = Place(local, theme);
        var root = new StackPanel { Children = { localScope, sibling } };
        MaterialThemeScope.SetTheme(root, ThemeTestHelper.ThemeSeededWith(Colors.Teal));

        var first = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens.Get(
            MaterialColorUtilities.Avalonia.Tokens.RefPaletteToken.Primary,
            40);
        var second = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens.Get(
            MaterialColorUtilities.Avalonia.Tokens.RefPaletteToken.Secondary,
            80);
        var references = new MaterialTokenOverrides();
        references.Set(first, Colors.Orange);
        references.Set(second, Colors.Purple);
        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(first));
        var component = new MaterialTokenOverrides();
        component.Set(
            SecondaryNavigationTabTokensKeys.ContainerHeight,
            57d);
        component.Set(
            SecondaryNavigationTabTokensKeys.ActiveLabelTextColor,
            SharedToken.Alias(McuSystemColorTokens.Primary));
        localScope.Resources.MergedDictionaries.Add(component);
        localScope.Resources.MergedDictionaries.Add(system);
        localScope.Resources.MergedDictionaries.Add(references);

        var window = ThemeTestHelper.ShowWindow(root);
        try
        {
            Assert.Equal(57, local.Items[0].GeometryProfile.ContainerHeight);
            Assert.Equal(Colors.Orange, ColorOf(local.Items[0].VisualColors.ActiveLabelTextColor));
            Assert.NotEqual(57, sibling.Items[0].GeometryProfile.ContainerHeight);

            system.Set(McuSystemColorTokens.Primary, SharedToken.Alias(second));
            Assert.Equal(Colors.Purple, ColorOf(local.Items[0].VisualColors.ActiveLabelTextColor));
            Assert.Equal(Colors.Purple, ColorOf(local.VisualColors.ActiveIndicatorColor));

            references.Set(second, Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(local.Items[0].VisualColors.ActiveLabelTextColor));
            Assert.NotEqual(Colors.Green, ColorOf(sibling.Items[0].VisualColors.ActiveLabelTextColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideTypedEndpoints()
    {
        var tabs = CreateTabs(TabVariant.Primary);
        var scope = Place(tabs, new MaterialTheme());
        var itemGeometry = new TabItemGeometryProfile(52, 70, 28, TypographyRole.LabelLarge);
        var rowGeometry = new TabRowGeometryProfile(4, 2, 2, false);
        var itemColors = TestItemColors();
        var rowColors = TestRowColors();
        scope.Resources[TabValueKeys.PrimaryItemGeometry] = itemGeometry;
        scope.Resources[TabValueKeys.PrimaryRowGeometry] = rowGeometry;
        scope.Resources[TabValueKeys.PrimaryItemVisualColors] = itemColors;
        scope.Resources[TabValueKeys.PrimaryRowVisualColors] = rowColors;

        var window = ThemeTestHelper.ShowWindow(scope);
        try
        {
            Assert.Same(itemGeometry, tabs.Items[0].GeometryProfile);
            Assert.Same(rowGeometry, tabs.GeometryProfile);
            Assert.Same(itemColors, tabs.Items[0].VisualColors);
            Assert.Same(rowColors, tabs.VisualColors);
            Assert.Equal(28, tabs.Items[0].IconSize);
            Assert.Equal(52, tabs.Items[0].MinHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var tabs = CreateTabs(TabVariant.Secondary);
        var localScope = Place(tabs, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[TabValueKeys.SecondaryItemGeometry] =
            new TabItemGeometryProfile(30, 60, 20, TypographyRole.TitleSmall);
        localScope.Resources[SecondaryNavigationTabTokensKeys.ContainerHeight] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(localScope, TabValueKeys.SecondaryItemGeometry, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(SecondaryNavigationTabTokensKeys.ContainerHeight, exception.Token);
        Assert.Same(TabValueKeys.SecondaryItemGeometry, exception.ValueKey);
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepTabPrecedence()
    {
        var style = new Style(selector => selector.Is<MaterialTab>());
        var applicationBrush = Brush(Colors.Orange);
        style.Add(new Setter(MaterialTab.ForegroundProperty, applicationBrush));
        style.Add(new Setter(MaterialTab.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            // An unchecked tab intentionally gets the inactive-state ControlTheme setter. Keep
            // this tab checked so the test isolates ordinary application-style precedence.
            var styled = new MaterialTab { Content = "Styled", IsChecked = true };
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
            var local = new MaterialTab
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
            Application.Current!.Styles.Remove(style);
        }
    }

    private static MaterialTabs CreateTabs(TabVariant variant)
    {
        var tabs = new MaterialTabs
        {
            Variant = variant,
            SelectedIndex = 0,
        };
        tabs.Items.Add(new MaterialTab { Content = "One" });
        tabs.Items.Add(new MaterialTab { Content = "Two" });
        return tabs;
    }

    private static Border Place(Control control, MaterialTheme theme)
    {
        var scope = new Border { Child = control };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static void AssertDependencies(
        MaterialValueKey key,
        params SharedMaterialToken[] expected)
    {
        Assert.Equal(expected.Length, key.Plan.Dependencies.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], key.Plan.Dependencies[index]);
    }

    private static TabItemVisualColors TestItemColors() => new(
        Brush(Colors.HotPink),
        Brush(Colors.Lime),
        Brush(Colors.Cyan),
        Brush(Colors.Yellow),
        Brush(Colors.Orange));

    private static TabRowVisualColors TestRowColors() => new(
        Brush(Colors.Purple),
        Brush(Colors.Green),
        Brush(Colors.Blue));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static Color ColorOf(IBrush? brush) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
