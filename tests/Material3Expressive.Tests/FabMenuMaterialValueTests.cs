using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using MaterialDesignTokens.Avalonia;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the FabMenu MaterialValue endpoints.</summary>
public sealed class FabMenuMaterialValueTests
{
    [Fact]
    public void ValueKeysHaveStableTypesPathsAndExactRawDependencies()
    {
        Assert.Equal(typeof(double), FabMenuValueKeys.ItemSpacing.ValueType);
        Assert.Equal("Value.FabMenu.ItemSpacing", FabMenuValueKeys.ItemSpacing.Path);
        Assert.Equal(typeof(double), FabMenuValueKeys.CloseButtonSpacing.ValueType);
        Assert.Equal("Value.FabMenu.CloseButtonSpacing", FabMenuValueKeys.CloseButtonSpacing.Path);

        Assert.Equal(
            [FabMenuBaselineTokensKeys.ListItemBetweenSpace],
            FabMenuValueKeys.ItemSpacing.Plan.Dependencies);
        Assert.Equal(
            [FabMenuBaselineTokensKeys.CloseButtonBetweenSpace],
            FabMenuValueKeys.CloseButtonSpacing.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void DefaultSpacingComesFromTheTwoLeafEndpoints()
    {
        var menu = new MaterialFabMenu();
        var scope = Place(menu, new MaterialTheme());
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(FabMenuBaselineTokens.ListItemBetweenSpace, menu.ItemSpacing);
        Assert.Equal(FabMenuBaselineTokens.CloseButtonBetweenSpace, menu.CloseButtonSpacing);
        Assert.Equal(FabMenuBaselineTokens.ListItemBetweenSpace,
            SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.ItemSpacing));
        Assert.Equal(FabMenuBaselineTokens.CloseButtonBetweenSpace,
            SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.CloseButtonSpacing));
    }

    [AvaloniaFact]
    public void RawOverridesRefreshBothDynamicResourceConsumersInPlace()
    {
        var menu = new MaterialFabMenu();
        var scope = Place(menu, new MaterialTheme());
        var overrides = new MaterialTokenOverrides();
        scope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        overrides.Set(FabMenuBaselineTokensKeys.ListItemBetweenSpace, 13d);
        overrides.Set(FabMenuBaselineTokensKeys.CloseButtonBetweenSpace, 19d);

        Assert.Equal(13d, menu.ItemSpacing);
        Assert.Equal(19d, menu.CloseButtonSpacing);
        Assert.Equal(13d, SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.ItemSpacing));
        Assert.Equal(19d, SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.CloseButtonSpacing));

        overrides.Set(FabMenuBaselineTokensKeys.ListItemBetweenSpace, 23d);
        Assert.Equal(23d, menu.ItemSpacing);
    }

    [AvaloniaFact]
    public void FlatOverridesTakePrecedenceOverTheResolvedLeafValues()
    {
        var menu = new MaterialFabMenu();
        var scope = Place(menu, new MaterialTheme());
        scope.Resources[FabMenuValueKeys.ItemSpacing] = 31d;
        scope.Resources[FabMenuValueKeys.CloseButtonSpacing] = 37d;
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(31d, menu.ItemSpacing);
        Assert.Equal(37d, menu.CloseButtonSpacing);
        Assert.Equal(31d, SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.ItemSpacing));
        Assert.Equal(37d, SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.CloseButtonSpacing));
    }

    [AvaloniaFact]
    public void RawOverridesStayIsolatedToTheirPlacementScope()
    {
        var local = new MaterialFabMenu();
        var sibling = new MaterialFabMenu();
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new MaterialTokenOverrides();
        overrides.Set(FabMenuBaselineTokensKeys.ListItemBetweenSpace, 41d);
        localScope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(root);

        Assert.Equal(41d, local.ItemSpacing);
        Assert.Equal(FabMenuBaselineTokens.ListItemBetweenSpace, sibling.ItemSpacing);

        overrides.Set(FabMenuBaselineTokensKeys.ListItemBetweenSpace, 43d);
        Assert.Equal(43d, local.ItemSpacing);
        Assert.Equal(FabMenuBaselineTokens.ListItemBetweenSpace, sibling.ItemSpacing);
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesRetainPrecedenceOverMaterialSetters()
    {
        var style = new Style(selector => selector.Is<MaterialFabMenu>());
        style.Add(new Setter(MaterialFabMenu.ItemSpacingProperty, 47d));
        style.Add(new Setter(MaterialFabMenu.CloseButtonSpacingProperty, 53d));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialFabMenu();
            var styledScope = Place(styled, new MaterialTheme());
            ThemeTestHelper.ShowWindow(styledScope);

            Assert.Equal(47d, styled.ItemSpacing);
            Assert.Equal(53d, styled.CloseButtonSpacing);

            var local = new MaterialFabMenu
            {
                ItemSpacing = 59d,
                CloseButtonSpacing = 61d,
            };
            var localScope = Place(local, new MaterialTheme());
            ThemeTestHelper.ShowWindow(localScope);
            Assert.Equal(59d, local.ItemSpacing);
            Assert.Equal(61d, local.CloseButtonSpacing);
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void LocalValuesRemainStableWhenRawSourcesMutate()
    {
        var menu = new MaterialFabMenu { ItemSpacing = 67d };
        var scope = Place(menu, new MaterialTheme());
        var overrides = new MaterialTokenOverrides();
        scope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        overrides.Set(FabMenuBaselineTokensKeys.ListItemBetweenSpace, 71d);
        overrides.Set(FabMenuBaselineTokensKeys.CloseButtonBetweenSpace, 73d);

        Assert.Equal(67d, menu.ItemSpacing);
        Assert.Equal(73d, menu.CloseButtonSpacing);
    }

    [AvaloniaFact]
    public void ExpansionAndLateItemsRemainControlOwnedWhenSpacingChanges()
    {
        var menu = new MaterialFabMenu();
        menu.Items.Add(new MaterialFab { Label = "One" });
        var scope = Place(menu, new MaterialTheme());
        ThemeTestHelper.ShowWindow(scope);

        Assert.False(menu.Items[0].IsVisible);
        menu.IsExpanded = true;
        Assert.True(menu.Items[0].Opacity >= 0);

        menu.Items.Add(new MaterialFab { Label = "Two" });
        Assert.Equal(FabVariant.Primary, menu.Items[1].Variant);
        menu.ItemSpacing = 11d;
        menu.CloseButtonSpacing = 17d;
        Assert.Equal(11d, menu.ItemSpacing);
        Assert.Equal(17d, menu.CloseButtonSpacing);
    }

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var menu = new MaterialFabMenu();
        var localScope = Place(menu, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[FabMenuValueKeys.ItemSpacing] = 89d;
        localScope.Resources[FabMenuBaselineTokensKeys.ListItemBetweenSpace] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(menu, FabMenuValueKeys.ItemSpacing, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(FabMenuBaselineTokensKeys.ListItemBetweenSpace, exception.Token);
        Assert.Same(FabMenuValueKeys.ItemSpacing, exception.ValueKey);
    }

    private static Border Place(MaterialFabMenu menu, MaterialTheme theme)
    {
        var scope = new Border { Child = menu };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }
}
