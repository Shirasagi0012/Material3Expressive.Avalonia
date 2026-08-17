using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
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

/// <summary>End-to-end contracts for the Divider MaterialValue leaves and late control policy.</summary>
public sealed class DividerMaterialValueTests
{
    [Fact]
    public void ValueKeysExposeTheExpectedTypesAndExactRawDependencies()
    {
        Assert.Equal(typeof(IImmutableBrush), DividerValueKeys.DividerBrush.ValueType);
        Assert.Equal(typeof(double), DividerValueKeys.Thickness.ValueType);
        Assert.Equal(
            [DividerTokensKeys.Color],
            DividerValueKeys.DividerBrush.Plan.Dependencies);
        Assert.Equal(
            [DividerTokensKeys.Thickness],
            DividerValueKeys.Thickness.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void DefaultLeavesResolveTheRawDividerValues()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var divider = new MaterialDivider();
        var scope = Place(divider, theme);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(1, divider.Thickness);
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant),
            ThemeTestHelper.ColorOf(divider.DividerBrush));
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceOverridesRebindAndTrackMutation()
    {
        var divider = new MaterialDivider();
        var theme = new MaterialTheme();
        var scope = Place(divider, theme);

        var referenceToken = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(referenceToken, Colors.Orange);

        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.OutlineVariant,
            SharedToken.Alias(referenceToken));

        var component = new DividerTokensOverrides
        {
            Color = SharedToken.Alias(McuSystemColorTokens.OutlineVariant),
        };

        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(divider.DividerBrush));

        references.Set(referenceToken, Colors.Purple);
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(divider.DividerBrush));

        system.Set(
            McuSystemColorTokens.OutlineVariant,
            SharedToken.Literal(Colors.Green));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(divider.DividerBrush));

        component.Color = SharedToken.Literal(Colors.HotPink);
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(divider.DividerBrush));
    }

    [AvaloniaFact]
    public void FlatBrushOverrideReplacesOnlyThePresentationEndpoint()
    {
        var divider = new MaterialDivider();
        var scope = Place(divider, new MaterialTheme());
        var brush = new ImmutableSolidColorBrush(Colors.HotPink);
        scope.Resources[DividerValueKeys.DividerBrush] = brush;
        ThemeTestHelper.ShowWindow(scope);

        Assert.Same(brush, divider.DividerBrush);
        Assert.True(divider.TryFindResource(
            DividerValueKeys.DividerBrush,
            divider.ActualThemeVariant,
            out var value));
        Assert.Same(brush, value);
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromASiblingPlacement()
    {
        var local = new MaterialDivider();
        var sibling = new MaterialDivider();
        var localScope = Place(local, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, sibling } };
        var overrides = new DividerTokensOverrides
        {
            Color = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(root);

        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(local.DividerBrush));
        Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.DividerBrush));

        overrides.Color = SharedToken.Literal(Colors.Green);
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(local.DividerBrush));
        Assert.NotEqual(Colors.Green, ThemeTestHelper.ColorOf(sibling.DividerBrush));
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalValuesKeepPrecedenceOverMaterialLeaves()
    {
        var applicationStyle = new Style(selector => selector.Is<MaterialDivider>());
        var styledBrush = new ImmutableSolidColorBrush(Colors.Orange);
        applicationStyle.Add(new Setter(MaterialDivider.DividerBrushProperty, styledBrush));
        applicationStyle.Add(new Setter(MaterialDivider.ThicknessProperty, 7d));
        Application.Current!.Styles.Add(applicationStyle);

        try
        {
            var styled = new MaterialDivider();
            var styledScope = Place(styled, new MaterialTheme());
            ThemeTestHelper.ShowWindow(styledScope);
            Assert.Same(styledBrush, styled.DividerBrush);
            Assert.Equal(7, styled.Thickness);

            var localBrush = new ImmutableSolidColorBrush(Colors.HotPink);
            var local = new MaterialDivider
            {
                DividerBrush = localBrush,
                Thickness = 9,
            };
            var localScope = Place(local, new MaterialTheme());
            ThemeTestHelper.ShowWindow(localScope);
            Assert.Same(localBrush, local.DividerBrush);
            Assert.Equal(9, local.Thickness);
        }
        finally
        {
            Application.Current.Styles.Remove(applicationStyle);
        }
    }

    [AvaloniaFact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var divider = new MaterialDivider();
        var localScope = Place(divider, new MaterialTheme());
        var root = new Border { Child = localScope };
        var ancestorBrush = new ImmutableSolidColorBrush(Colors.HotPink);
        root.Resources[DividerValueKeys.DividerBrush] = ancestorBrush;
        localScope.Resources[DividerTokensKeys.Color] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(divider, DividerValueKeys.DividerBrush, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(DividerTokensKeys.Color, exception.Token);
        Assert.Same(DividerValueKeys.DividerBrush, exception.ValueKey);
    }

    [AvaloniaFact]
    public void OrientationAndHairlineRemainLateControlPolicies()
    {
        var horizontal = new MaterialDivider();
        horizontal.Measure(new Size(160, 80));
        horizontal.Arrange(new Rect(0, 0, 160, 1));
        Assert.Equal(new Size(0, 1), horizontal.DesiredSize);
        Assert.Equal(new Size(160, 1), horizontal.Bounds.Size);

        var vertical = new MaterialDivider { Orientation = DividerOrientation.Vertical };
        vertical.Measure(new Size(160, 80));
        vertical.Arrange(new Rect(0, 0, 1, 80));
        Assert.Equal(new Size(1, 0), vertical.DesiredSize);
        Assert.Equal(new Size(1, 80), vertical.Bounds.Size);

        var hairline = new MaterialDivider
        {
            Orientation = DividerOrientation.Horizontal,
            IsHairline = true,
            DividerBrush = new ImmutableSolidColorBrush(Colors.Black),
        };
        var window = ThemeTestHelper.ShowWindow(hairline);
        window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        hairline.Measure(new Size(160, 80));
        hairline.Arrange(new Rect(0, 0, 160, 0));

        Assert.Equal(1, hairline.Thickness);
        Assert.Equal(new Size(0, 0), hairline.DesiredSize);
        Assert.True(hairline.TryGetVisualBounds("stroke", out var bounds));
        Assert.Equal(new Rect(0, 0, 160, .5), bounds);
        window.Close();
    }

    private static Border Place(MaterialDivider divider, MaterialTheme theme)
    {
        var scope = new Border { Child = divider };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }
}
