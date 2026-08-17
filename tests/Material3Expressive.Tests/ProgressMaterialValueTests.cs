using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialTokenOverrides = Material3Expressive.Foundation.Tokens.Runtime.MaterialTokenOverrides;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the linear and circular progress MaterialValue endpoints.</summary>
public sealed class ProgressMaterialValueTests
{
    [Fact]
    public void ValueKeysHaveStableTypesPathsAndExactRawDependencies()
    {
        Assert.Equal(typeof(ProgressVisualColors), ProgressValueKeys.VisualColors.ValueType);
        Assert.Equal("Value.Progress.VisualColors", ProgressValueKeys.VisualColors.Path);
        Assert.Equal(typeof(LinearProgressMetrics), ProgressValueKeys.LinearMetrics.ValueType);
        Assert.Equal("Value.Progress.LinearMetrics", ProgressValueKeys.LinearMetrics.Path);
        Assert.Equal(typeof(CircularProgressMetrics), ProgressValueKeys.CircularMetrics.ValueType);
        Assert.Equal("Value.Progress.CircularMetrics", ProgressValueKeys.CircularMetrics.Path);

        Assert.Equal(
            [
                ProgressIndicatorTokensKeys.ActiveIndicatorColor,
                ProgressIndicatorTokensKeys.TrackColor,
                ProgressIndicatorTokensKeys.StopColor,
            ],
            ProgressValueKeys.VisualColors.Plan.Dependencies);
        Assert.Equal(
            [
                LinearProgressIndicatorTokensKeys.ActiveThickness,
                LinearProgressIndicatorTokensKeys.TrackThickness,
                LinearProgressIndicatorTokensKeys.TrackActiveSpace,
                LinearProgressIndicatorTokensKeys.Height,
                LinearProgressIndicatorTokensKeys.StopSize,
                LinearProgressIndicatorTokensKeys.ActiveWaveAmplitude,
                LinearProgressIndicatorTokensKeys.ActiveWaveWavelength,
                LinearProgressIndicatorTokensKeys.IndeterminateActiveWaveWavelength,
                LinearProgressIndicatorTokensKeys.WaveHeight,
            ],
            ProgressValueKeys.LinearMetrics.Plan.Dependencies);
        Assert.Equal(
            [
                CircularProgressIndicatorTokensKeys.ActiveThickness,
                CircularProgressIndicatorTokensKeys.TrackThickness,
                CircularProgressIndicatorTokensKeys.TrackActiveSpace,
                CircularProgressIndicatorTokensKeys.Size,
                CircularProgressIndicatorTokensKeys.ActiveWaveAmplitude,
                CircularProgressIndicatorTokensKeys.ActiveWaveWavelength,
                CircularProgressIndicatorTokensKeys.WaveSize,
            ],
            ProgressValueKeys.CircularMetrics.Plan.Dependencies);
    }

    [AvaloniaFact]
    public void DefaultProfilesResolveForBothShapesAndPreserveLateStateOwnership()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var linear = new MaterialLinearProgressIndicator
        {
            Value = .4,
            IsWavy = true,
        };
        var circular = new MaterialCircularProgressIndicator
        {
            Value = .6,
            IsIndeterminate = true,
            IsWavy = true,
        };
        // A MaterialTheme is a single-placement provider; use one instance per sibling scope.
        var root = new StackPanel
        {
            Children =
            {
                Place(linear, theme),
                Place(circular, ThemeTestHelper.ThemeSeededWith(Colors.Teal)),
            }
        };
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(4, linear.ResolvedMetrics.ActiveThickness);
            Assert.Equal(10, linear.ResolvedMetrics.WaveSize);
            Assert.Equal(4, linear.ResolvedMetrics.StopSize);
            Assert.Equal(48, circular.ResolvedMetrics.WaveSize);
            Assert.Equal(40, circular.ResolvedMetrics.Size);
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Primary),
                ThemeTestHelper.ColorOf(linear.ResolvedVisualColors.ActiveIndicatorColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.SecondaryContainer),
                ThemeTestHelper.ColorOf(circular.ResolvedVisualColors.TrackColor));

            Assert.Equal(.4, linear.Value);
            Assert.True(linear.IsWavy);
            Assert.Equal(.6, circular.Value);
            Assert.True(circular.IsIndeterminate);
            Assert.True(circular.IsWavy);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRefreshProfilesInPlace()
    {
        var theme = new MaterialTheme();
        var linear = new MaterialLinearProgressIndicator();
        var scope = Place(linear, theme);

        var referenceToken = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(referenceToken, Colors.Orange);

        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.Primary,
            SharedToken.Alias(referenceToken));

        var component = new ProgressIndicatorTokensOverrides
        {
            ActiveIndicatorColor = SharedToken.Alias(McuSystemColorTokens.Primary),
            TrackColor = SharedToken.Literal(Colors.CadetBlue),
        };
        var metrics = new LinearProgressIndicatorTokensOverrides
        {
            WaveHeight = SharedToken.Literal(17d),
        };

        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(metrics);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.Orange,
                ThemeTestHelper.ColorOf(linear.ResolvedVisualColors.ActiveIndicatorColor));
            Assert.Equal(Colors.CadetBlue,
                ThemeTestHelper.ColorOf(linear.ResolvedVisualColors.TrackColor));
            Assert.Equal(17, linear.ResolvedMetrics.WaveSize);

            references.Set(referenceToken, Colors.Purple);
            Assert.Equal(Colors.Purple,
                ThemeTestHelper.ColorOf(linear.ResolvedVisualColors.ActiveIndicatorColor));

            component.ActiveIndicatorColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(Colors.Green,
                ThemeTestHelper.ColorOf(linear.ResolvedVisualColors.ActiveIndicatorColor));

            metrics.WaveHeight = SharedToken.Literal(23d);
            Assert.Equal(23, linear.ResolvedMetrics.WaveSize);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RawProfilesAreIsolatedToTheirMaterialThemePlacement()
    {
        var local = new MaterialLinearProgressIndicator();
        var sibling = new MaterialLinearProgressIndicator();
        var localScope = Place(local, new MaterialTheme());
        var siblingScope = Place(sibling, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingScope } };
        var overrides = new LinearProgressIndicatorTokensOverrides
        {
            WaveHeight = SharedToken.Literal(19d),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(19, local.ResolvedMetrics.WaveSize);
            Assert.Equal(10, sibling.ResolvedMetrics.WaveSize);

            overrides.WaveHeight = SharedToken.Literal(27d);
            Assert.Equal(27, local.ResolvedMetrics.WaveSize);
            Assert.Equal(10, sibling.ResolvedMetrics.WaveSize);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideResolvedEndpointsWithoutChangingRawGraph()
    {
        var linear = new MaterialLinearProgressIndicator();
        var scope = Place(linear, new MaterialTheme());
        var metrics = new LinearProgressMetrics(5, 6, 7, 8, 9, 10, 11, 12, 13);
        var colors = new ProgressVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Lime),
            Brush(Colors.Cyan));
        scope.Resources[ProgressValueKeys.LinearMetrics] = metrics;
        scope.Resources[ProgressValueKeys.VisualColors] = colors;
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(metrics, linear.ResolvedMetrics);
            Assert.Same(colors, linear.ResolvedVisualColors);
            Assert.Same(metrics, SharedTokenTestHelper.Resolve(linear, ProgressValueKeys.LinearMetrics));
            Assert.Same(colors, SharedTokenTestHelper.Resolve(linear, ProgressValueKeys.VisualColors));

            // The presentation override is endpoint-local; Raw defaults remain independent.
            Assert.Equal(4, SharedTokenTestHelper.Resolve(linear, LinearProgressIndicatorTokensKeys.ActiveThickness));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvalidLocalRawHitDoesNotFallBackToAncestorFlatProfile()
    {
        var linear = new MaterialLinearProgressIndicator();
        var localScope = Place(linear, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[ProgressValueKeys.LinearMetrics] =
            new LinearProgressMetrics(30, 30, 30, 30, 30, 30, 30, 30, 30);
        localScope.Resources[LinearProgressIndicatorTokensKeys.WaveHeight] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(linear, ProgressValueKeys.LinearMetrics, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(LinearProgressIndicatorTokensKeys.WaveHeight, exception.Token);
        Assert.Same(ProgressValueKeys.LinearMetrics, exception.ValueKey);
    }

    [AvaloniaFact]
    public void PublicStateAndLocalProfileValuesKeepNormalPrecedence()
    {
        var localMetrics = new LinearProgressMetrics(21, 22, 23, 24, 25, 26, 27, 28, 29);
        var linear = new MaterialLinearProgressIndicator
        {
            Value = .25,
            IsIndeterminate = true,
            IsWavy = true,
            ResolvedMetrics = localMetrics,
        };
        var scope = Place(linear, new MaterialTheme());
        var overrides = new LinearProgressIndicatorTokensOverrides
        {
            WaveHeight = SharedToken.Literal(37d),
        };
        scope.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(localMetrics, linear.ResolvedMetrics);
            Assert.Equal(.25, linear.Value);
            Assert.True(linear.IsIndeterminate);
            Assert.True(linear.IsWavy);

            overrides.WaveHeight = SharedToken.Literal(41d);
            Assert.Same(localMetrics, linear.ResolvedMetrics);
            Assert.Equal(41, SharedTokenTestHelper.Resolve(linear, LinearProgressIndicatorTokensKeys.WaveHeight));
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
