using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
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

/// <summary>End-to-end contracts for the LoadingIndicator MaterialValue endpoints.</summary>
public sealed class LoadingIndicatorMaterialValueTests
{
    [Fact]
    public void ValueKeysHaveStableTypesPathsAndExactRawDependencies()
    {
        Assert.Equal(
            typeof(LoadingIndicatorGeometryProfile),
            LoadingIndicatorValueKeys.StandardGeometry.ValueType);
        Assert.Equal(
            "Value.LoadingIndicator.Standard.Geometry",
            LoadingIndicatorValueKeys.StandardGeometry.Path);
        Assert.Equal(
            typeof(LoadingIndicatorVisualColors),
            LoadingIndicatorValueKeys.ContainedVisualColors.ValueType);
        Assert.Equal(
            "Value.LoadingIndicator.Contained.VisualColors",
            LoadingIndicatorValueKeys.ContainedVisualColors.Path);

        var geometryDependencies = new MaterialToken[]
        {
            LoadingIndicatorTokensKeys.ActiveSize,
            LoadingIndicatorTokensKeys.ContainerWidth,
            LoadingIndicatorTokensKeys.ContainerHeight,
            LoadingIndicatorTokensKeys.ContainerShape,
        };
        Assert.Equal(geometryDependencies, LoadingIndicatorValueKeys.StandardGeometry.Plan.Dependencies);
        Assert.Equal(geometryDependencies, LoadingIndicatorValueKeys.ContainedGeometry.Plan.Dependencies);
        Assert.Equal(
            [LoadingIndicatorTokensKeys.ActiveIndicatorColor],
            LoadingIndicatorValueKeys.StandardVisualColors.Plan.Dependencies);
        Assert.Equal(
            [
                LoadingIndicatorTokensKeys.ContainedActiveColor,
                LoadingIndicatorTokensKeys.ContainedContainerColor,
            ],
            LoadingIndicatorValueKeys.ContainedVisualColors.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void StandardAndContainedProfilesResolveTheirVariantPolicy()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var standard = new MaterialLoadingIndicator();
        var contained = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var root = new StackPanel
        {
            Children =
            {
                Place(standard, theme),
                Place(contained, ThemeTestHelper.ThemeSeededWith(Colors.Teal)),
            }
        };
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.False(standard.HasResolvedContainer);
            Assert.True(contained.HasResolvedContainer);
            Assert.Equal(38, standard.ResolvedMetrics.ActiveSize);
            Assert.Equal(48, standard.ResolvedMetrics.ContainerWidth);
            Assert.Equal(48, standard.ResolvedMetrics.ContainerHeight);
            Assert.Equal(ShapeRole.CornerFull, contained.ResolvedMetrics.ContainerShape);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ThemeTestHelper.ColorOf(standard.ResolvedVisualColors.ActiveIndicatorColor));
            Assert.Equal(
                Colors.Transparent,
                ThemeTestHelper.ColorOf(standard.ResolvedVisualColors.ContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnPrimaryContainer),
                ThemeTestHelper.ColorOf(contained.ResolvedVisualColors.ActiveIndicatorColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.PrimaryContainer),
                ThemeTestHelper.ColorOf(contained.ResolvedVisualColors.ContainerColor));

            Assert.Equal(48, standard.Bounds.Width, 3);
            Assert.Equal(48, standard.Bounds.Height, 3);
            Assert.Equal(48, contained.Bounds.Width, 3);
            Assert.Equal(48, contained.Bounds.Height, 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRefreshTheSelectedProfile()
    {
        var indicator = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var scope = Place(indicator, new MaterialTheme());

        var referenceToken = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(referenceToken, Colors.Orange);

        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.PrimaryContainer,
            SharedToken.Alias(referenceToken));

        var component = new LoadingIndicatorTokensOverrides
        {
            ContainedContainerColor = SharedToken.Alias(McuSystemColorTokens.PrimaryContainer),
            ContainedActiveColor = SharedToken.Literal(Colors.CadetBlue),
            ActiveSize = SharedToken.Literal(31d),
            ContainerWidth = SharedToken.Literal(56d),
            ContainerHeight = SharedToken.Literal(52d),
            ContainerShape = SharedToken.Literal(ShapeRole.CornerMedium),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.CadetBlue,
                ThemeTestHelper.ColorOf(indicator.ResolvedVisualColors.ActiveIndicatorColor));
            Assert.Equal(Colors.Orange,
                ThemeTestHelper.ColorOf(indicator.ResolvedVisualColors.ContainerColor));
            Assert.Equal(31, indicator.ResolvedMetrics.ActiveSize);
            Assert.Equal(56, indicator.ResolvedMetrics.ContainerWidth);
            Assert.Equal(52, indicator.ResolvedMetrics.ContainerHeight);
            Assert.Equal(ShapeRole.CornerMedium, indicator.ResolvedMetrics.ContainerShape);

            references.Set(referenceToken, Colors.Purple);
            Assert.Equal(Colors.Purple,
                ThemeTestHelper.ColorOf(indicator.ResolvedVisualColors.ContainerColor));

            component.ContainedActiveColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(Colors.Green,
                ThemeTestHelper.ColorOf(indicator.ResolvedVisualColors.ActiveIndicatorColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RawProfilesAreIsolatedToTheirMaterialThemePlacement()
    {
        var local = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var sibling = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new LoadingIndicatorTokensOverrides
        {
            ActiveSize = SharedToken.Literal(29d),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(29, local.ResolvedMetrics.ActiveSize);
            Assert.Equal(38, sibling.ResolvedMetrics.ActiveSize);

            overrides.ActiveSize = SharedToken.Literal(33d);
            Assert.Equal(33, local.ResolvedMetrics.ActiveSize);
            Assert.Equal(38, sibling.ResolvedMetrics.ActiveSize);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideOnlyTheSelectedPresentationEndpoints()
    {
        var indicator = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var scope = Place(indicator, new MaterialTheme());
        var geometry = new LoadingIndicatorGeometryProfile(
            25,
            60,
            54,
            ShapeRole.CornerLarge,
            HasContainer: true);
        var colors = new LoadingIndicatorVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime));
        scope.Resources[LoadingIndicatorValueKeys.ContainedGeometry] = geometry;
        scope.Resources[LoadingIndicatorValueKeys.ContainedVisualColors] = colors;
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(geometry, indicator.ResolvedMetrics);
            Assert.Same(colors, indicator.ResolvedVisualColors);
            Assert.Same(
                geometry,
                SharedTokenTestHelper.Resolve(indicator, LoadingIndicatorValueKeys.ContainedGeometry));
            Assert.Same(
                colors,
                SharedTokenTestHelper.Resolve(indicator, LoadingIndicatorValueKeys.ContainedVisualColors));
            Assert.Equal(
                38,
                SharedTokenTestHelper.Resolve(indicator, LoadingIndicatorTokensKeys.ActiveSize));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatProfile()
    {
        var indicator = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        var localScope = Place(indicator, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[LoadingIndicatorValueKeys.ContainedGeometry] =
            new LoadingIndicatorGeometryProfile(30, 30, 30, ShapeRole.CornerFull, true);
        localScope.Resources[LoadingIndicatorTokensKeys.ContainerShape] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(
                indicator,
                LoadingIndicatorValueKeys.ContainedGeometry,
                ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(LoadingIndicatorTokensKeys.ContainerShape, exception.Token);
        Assert.Same(LoadingIndicatorValueKeys.ContainedGeometry, exception.ValueKey);
    }

    [AvaloniaFact]
    public void PublicStateAndLocalProfilesKeepNormalPrecedence()
    {
        var localGeometry = new LoadingIndicatorGeometryProfile(
            22,
            50,
            46,
            ShapeRole.CornerSmall,
            HasContainer: false);
        var indicator = new MaterialLoadingIndicator
        {
            Variant = LoadingIndicatorVariant.Contained,
            Value = .25,
            IsIndeterminate = false,
            ResolvedMetrics = localGeometry,
        };
        var scope = Place(indicator, new MaterialTheme());
        var overrides = new LoadingIndicatorTokensOverrides
        {
            ActiveSize = SharedToken.Literal(35d),
        };
        scope.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(localGeometry, indicator.ResolvedMetrics);
            Assert.Equal(.25, indicator.Value);
            Assert.False(indicator.IsIndeterminate);
            Assert.Equal(22, indicator.ResolvedMetrics.ActiveSize);

            indicator.IsIndeterminate = true;
            indicator.Value = .75;
            overrides.ActiveSize = SharedToken.Literal(41d);

            Assert.True(indicator.IsIndeterminate);
            Assert.Equal(.75, indicator.Value);
            Assert.Same(localGeometry, indicator.ResolvedMetrics);
            Assert.Equal(41, SharedTokenTestHelper.Resolve(indicator, LoadingIndicatorTokensKeys.ActiveSize));
        }
        finally
        {
            window.Close();
        }
    }

    private static Border Place(Control control, MaterialTheme theme)
    {
        var scope = new Border { Child = control };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}
