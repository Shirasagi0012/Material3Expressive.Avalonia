using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Focused structural and deterministic render tests for the Surface and Divider contracts.
/// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25, Surface.kt:550-620,
/// ColorScheme.kt:1151-1154, Divider.kt and tokens/DividerTokens.kt (VERSION v0_117).
/// </summary>
public class SurfaceTests
{
    [AvaloniaFact]
    public void TonalElevation_UsesTheAndroidXCurveAtZeroOneAndFourDp()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var surfaceColor = ThemeTestHelper.Expected(theme, SysColorToken.Surface);
        var surface = SurfaceInTheme(theme, new SolidColorBrush(surfaceColor));
        var tint = ThemeTestHelper.Expected(theme, SysColorToken.SurfaceTint);

        foreach (var elevation in new[] { 0d, 1d, 4d })
        {
            surface.TonalElevation = elevation;

            var expected = elevation <= 0
                ? surfaceColor
                : CompositeOver(tint, TonalAlpha(elevation), surfaceColor);

            Assert.Equal(expected, ThemeTestHelper.ColorOf(RenderedSurfaceFill(surface)));
        }
    }

    [AvaloniaFact]
    public void NestedSurfaces_AccumulateOnlyTonalElevation()
    {
        var parent = new MaterialSurface
        {
            TonalElevation = 2,
            ShadowElevation = 12,
            Content = new MaterialSurface
            {
                TonalElevation = 3,
                ShadowElevation = 24
            }
        };
        var child = Assert.IsType<MaterialSurface>(parent.Content);

        ThemeTestHelper.ShowWindow(parent);

        Assert.Equal(2, parent.AbsoluteTonalElevation);
        Assert.Equal(5, child.AbsoluteTonalElevation);

        parent.ShadowElevation = 48;

        // Compose AbsoluteTonalElevation is independent from the shadow/elevation draw pass.
        Assert.Equal(5, child.AbsoluteTonalElevation);
    }

    [AvaloniaFact]
    public void CustomAndNonSolidBackgrounds_AreNotTonalTinted()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var custom = new SolidColorBrush(Colors.MediumVioletRed);
        var gradient = new LinearGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Colors.CornflowerBlue, 0),
                new GradientStop(Colors.MediumSeaGreen, 1)
            }
        };

        foreach (var background in new IBrush[] { custom, gradient })
        {
            var surface = SurfaceInTheme(theme, background);
            surface.TonalElevation = 8;

            Assert.Same(background, RenderedSurfaceFill(surface));
        }
    }

    [AvaloniaFact]
    public void ExplicitSurfaceColors_SurviveAThemeReplacement()
    {
        var initial = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var replacement = ThemeTestHelper.ThemeSeededWith(Colors.OrangeRed);
        var background = new SolidColorBrush(Colors.CornflowerBlue);
        var foreground = new SolidColorBrush(Colors.DarkGoldenrod);
        var surface = new MaterialSurface
        {
            Width = 80,
            Height = 32,
            Background = background,
            Foreground = foreground
        };
        var host = new Border { Child = surface };
        MaterialThemeScope.SetTheme(host, initial);
        ThemeTestHelper.ShowWindow(host);

        MaterialThemeScope.SetTheme(host, replacement);

        Assert.Same(background, surface.Background);
        Assert.Same(foreground, surface.Foreground);
    }

    private static MaterialSurface SurfaceInTheme(MaterialTheme theme, IBrush background)
    {
        var surface = new MaterialSurface
        {
            Width = 80,
            Height = 32,
            Background = background
        };
        MaterialThemeScope.SetTheme(surface, theme);
        ThemeTestHelper.ShowWindow(surface);
        return surface;
    }

    private static IBrush RenderedSurfaceFill(MaterialSurface surface)
    {
        surface.Measure(new Size(80, 32));
        surface.Arrange(new Rect(0, 0, 80, 32));

        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
            surface.Render(context);

        var fill = Assert.Single(drawing.Children.OfType<GeometryDrawing>(), item => item.Brush is not null);
        return Assert.IsAssignableFrom<IBrush>(fill.Brush);
    }

    private static double TonalAlpha(double elevation) =>
        ((4.5 * Math.Log(elevation + 1)) + 2) / 100;

    private static Color CompositeOver(Color foreground, double foregroundAlpha, Color background)
    {
        var fgAlpha = Math.Clamp((foreground.A / 255d) * foregroundAlpha, 0, 1);
        var bgAlpha = background.A / 255d;
        var outAlpha = fgAlpha + (bgAlpha * (1 - fgAlpha));
        if (outAlpha <= 0)
            return Colors.Transparent;

        byte Channel(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round(
                (((foregroundChannel / 255d) * fgAlpha) + ((backgroundChannel / 255d) * bgAlpha * (1 - fgAlpha))) / outAlpha * 255,
                MidpointRounding.AwayFromZero);

        return Color.FromArgb(
            (byte)Math.Round(outAlpha * 255, MidpointRounding.AwayFromZero),
            Channel(foreground.R, background.R),
            Channel(foreground.G, background.G),
            Channel(foreground.B, background.B));
    }
}

public class DividerTests
{
    // Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
    // Divider.kt; DividerTest.kt:42-101; DividerScreenshotTest.kt:45-109; and
    // tokens/DividerTokens.kt (VERSION v0_117). The default rule is 1dp OutlineVariant;
    // Dp.Hairline has zero layout size but paints a physical-pixel stroke.

    [AvaloniaFact]
    public void HorizontalDivider_ReservesItsThicknessAndStretchesAlongTheHorizontalAxis()
    {
        var divider = new MaterialDivider
        {
            Thickness = 3,
            DividerBrush = new SolidColorBrush(Colors.Black)
        };

        divider.Measure(new Size(160, 80));
        divider.Arrange(new Rect(0, 0, 160, 3));

        Assert.Equal(new Size(0, 3), divider.DesiredSize);
        Assert.Equal(new Size(160, 3), divider.Bounds.Size);

        var line = RenderedLine(divider);
        Assert.Equal(new Rect(0, 1.5, 160, 0), line.Geometry.Bounds);
        Assert.Equal(3, line.Pen.Thickness);
    }

    [AvaloniaFact]
    public void HairlineDivider_ReservesNoCrossAxisLayoutAndExposesOnePhysicalPixelFootprintAtEachScale()
    {
        AssertHairline(scale: 1, expectedStrokeThickness: 1);
        AssertHairline(scale: 2, expectedStrokeThickness: .5);
    }

    [AvaloniaFact]
    public void ZeroThickness_RemainsAnExplicitNoLineRatherThanOptingIntoHairline()
    {
        var divider = new MaterialDivider
        {
            Thickness = 0,
            DividerBrush = new SolidColorBrush(Colors.Black)
        };

        divider.Measure(new Size(160, 80));
        divider.Arrange(new Rect(0, 0, 160, 0));

        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
            divider.Render(context);

        Assert.Equal(new Size(0, 0), divider.DesiredSize);
        Assert.Empty(drawing.Children);
    }

    [AvaloniaFact]
    public void DefaultDividerBrush_UsesOutlineVariantForEachThemeVariant()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var divider = new MaterialDivider();
        var host = new Border { Child = divider };
        MaterialThemeScope.SetTheme(host, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant), ThemeTestHelper.ColorOf(divider.DividerBrush));

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant, isDark: true), ThemeTestHelper.ColorOf(divider.DividerBrush));
    }

    [AvaloniaFact]
    public void VerticalDivider_ReservesItsThicknessAndStretchesAlongTheVerticalAxis()
    {
        var divider = new MaterialDivider
        {
            Orientation = DividerOrientation.Vertical,
            Thickness = 3,
            DividerBrush = new SolidColorBrush(Colors.Black)
        };

        divider.Measure(new Size(160, 80));
        divider.Arrange(new Rect(0, 0, 3, 80));

        Assert.Equal(new Size(3, 0), divider.DesiredSize);
        Assert.Equal(new Size(3, 80), divider.Bounds.Size);

        var line = RenderedLine(divider);
        Assert.Equal(new Rect(1.5, 0, 0, 80), line.Geometry.Bounds);
        Assert.Equal(3, line.Pen.Thickness);
    }

    [AvaloniaFact]
    public void ExplicitDividerBrush_SurvivesAThemeReplacement()
    {
        var initial = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var replacement = ThemeTestHelper.ThemeSeededWith(Colors.OrangeRed);
        var brush = new SolidColorBrush(Colors.MediumVioletRed);
        var divider = new MaterialDivider { DividerBrush = brush };
        var host = new Border { Child = divider };
        MaterialThemeScope.SetTheme(host, initial);
        ThemeTestHelper.ShowWindow(host);

        MaterialThemeScope.SetTheme(host, replacement);

        Assert.Same(brush, divider.DividerBrush);
    }

    private static void AssertHairline(double scale, double expectedStrokeThickness)
    {
        AssertHairline(DividerOrientation.Horizontal, scale, expectedStrokeThickness);
        AssertHairline(DividerOrientation.Vertical, scale, expectedStrokeThickness);
    }

    private static void AssertHairline(
        DividerOrientation orientation,
        double scale,
        double expectedStrokeThickness)
    {
        var divider = new MaterialDivider
        {
            Orientation = orientation,
            IsHairline = true,
            DividerBrush = new SolidColorBrush(Colors.Black)
        };
        var window = ThemeTestHelper.ShowWindow(divider);
        window.SetRenderScaling(scale);
        Dispatcher.UIThread.RunJobs();

        divider.Measure(new Size(160, 80));
        divider.Arrange(orientation == DividerOrientation.Horizontal
            ? new Rect(0, 0, 160, 0)
            : new Rect(0, 0, 0, 80));

        Assert.Equal(new Size(0, 0), divider.DesiredSize);
        Assert.False(divider.IsHitTestVisible);
        Assert.False(divider.Focusable);
        Assert.False(divider.ClipToBounds);

        Assert.True(divider.TryGetVisualBounds("stroke", out var strokeBounds));
        Assert.Equal(
            orientation == DividerOrientation.Horizontal
                ? new Rect(0, 0, 160, expectedStrokeThickness)
                : new Rect(0, 0, expectedStrokeThickness, 80),
            strokeBounds);

        window.Close();
    }

    private static (Geometry Geometry, IPen Pen) RenderedLine(MaterialDivider divider)
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
            divider.Render(context);

        var line = Assert.IsType<GeometryDrawing>(Assert.Single(drawing.Children));
        return (Assert.IsAssignableFrom<Geometry>(line.Geometry), Assert.IsAssignableFrom<IPen>(line.Pen));
    }
}
