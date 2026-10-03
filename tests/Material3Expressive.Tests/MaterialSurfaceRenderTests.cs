using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Baseline;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Pixel-level probes for MaterialSurface's own paint. The headless app renders through the real
/// Skia backend (UseHeadlessDrawing = false), so these exercise Render — the shadow draw op and
/// the state-layer overlay exist nowhere else. The window is 160×160 white; the probe surface is
/// 100×50 centered, so it occupies x 30..130, y 55..105.
/// </summary>
public class MaterialSurfaceRenderTests
{
    [AvaloniaFact]
    public void UniformOutline_RoundsUpPhysicalStrokeWithoutMovingContent()
    {
        var child = new Border();
        var surface = ProbeSurface();
        surface.Background = Brushes.White;
        surface.BorderBrush = Brushes.Red;
        surface.BorderThickness = new Thickness(1.1);
        surface.CornerRadius = new CornerRadius(8);
        surface.Child = child;
        var window = Show(surface);
        window.SetRenderScaling(2);
        window.UpdateLayout();
        try
        {
            using var frame = window.CaptureRenderedFrame()!;
            // ceil(1.1dp * 2) = 3 physical pixels, entirely inside the outer rectangle.
            for (var y = 110; y < 113; y++) Assert.Equal((255, 0, 0), PixelAt(frame, 160, y));
            Assert.Equal((255, 255, 255), PixelAt(frame, 160, 113));
            Assert.Equal(new Size(100, 50), child.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AsymmetricOutline_InsetsEachCornerAndKeepsContentSize()
    {
        var surface = ProbeSurface();
        surface.CornerRadius = new CornerRadius(24, 8, 0, 16);
        surface.BorderBrush = Brushes.Red;
        surface.BorderThickness = new Thickness(6);
        surface.Child = new Border { Background = Brushes.Blue };
        var window = Show(surface);
        window.SetRenderScaling(2);
        window.UpdateLayout();
        try
        {
            using var frame = window.CaptureRenderedFrame()!;
            Assert.Equal(new Size(100, 50), surface.Child.Bounds.Size);
            // Different outer corner cuts and a full-stroke inset, including a sharp corner.
            Assert.Equal((255, 255, 255), PixelAt(frame, 66, 116));
            Assert.Equal((255, 0, 0), PixelAt(frame, 254, 116));
            Assert.Equal((255, 0, 0), PixelAt(frame, 254, 204));
            Assert.Equal((255, 255, 255), PixelAt(frame, 66, 204));
            Assert.Equal((255, 0, 0), PixelAt(frame, 80, 130));
            Assert.Equal((0, 0, 255), PixelAt(frame, 86, 136));
            Assert.Equal((255, 0, 0), PixelAt(frame, 160, 121));
            Assert.Equal((0, 0, 255), PixelAt(frame, 160, 123));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AsymmetricOutline_FillsShapeWhenStrokeConsumesInterior()
    {
        var surface = ProbeSurface();
        surface.CornerRadius = new CornerRadius(24, 8, 0, 16);
        surface.BorderBrush = Brushes.Red;
        surface.BorderThickness = new Thickness(30);
        surface.Child = new Border { Background = Brushes.Blue };
        var window = Show(surface);
        try
        {
            using var frame = window.CaptureRenderedFrame()!;
            Assert.Equal((255, 0, 0), PixelAt(frame, 80, 80));
            Assert.Equal((255, 0, 0), PixelAt(frame, 127, 102));
            Assert.Equal((255, 255, 255), PixelAt(frame, 33, 58));
            Assert.Equal(new Size(100, 50), surface.Child.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(0, true, 1)]
    [InlineData(0, false, 0)]
    [InlineData(1.1, true, 3)]
    public void AnimatedOutline_UsesRawWidthAndRadiusWithPhysicalStroke(double width, bool animated, int pixels)
    {
        using var clock = new MotionTestClockScope();
        var surface = ProbeSurface();
        surface.CornerRadius = new CornerRadius(8);
        surface.BorderBrush = Brushes.Red;
        surface.BorderThickness = new Thickness(width);
        surface.Child = new Border { Background = Brushes.Blue };
        ButtonSurfaceState.SetAnimateBorder(surface, animated);
        var button = new Button
        {
            Width = 100, Height = 50,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Template = new FuncControlTemplate<Button>((_, _) => surface),
        };
        MaterialInteraction.SetIsInteractive(button, true);
        var window = Show(button);
        window.SetRenderScaling(2);
        window.UpdateLayout();
        try
        {
            var outline = surface.GetVisualChildren().OfType<SurfaceOutline>().Single();
            Assert.Equal(surface.EffectiveCornerRadius, outline.CornerRadius);
            Assert.Equal(animated ? (double)(float)width : width, outline.BorderThickness.Top);
            using var frame = window.CaptureRenderedFrame()!;
            for (var y = 110; y < 110 + pixels; y++) Assert.Equal((255, 0, 0), PixelAt(frame, 160, y));
            Assert.Equal((0, 0, 255), PixelAt(frame, 160, 110 + pixels));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void AsymmetricOutline_OverlappingCornersFollowTheBackgroundContour()
    {
        var surface = ProbeSurface();
        surface.Width = 50;
        surface.CornerRadius = new CornerRadius(40, 40, 0, 0);
        surface.BorderBrush = Brushes.Red;
        surface.BorderThickness = new Thickness(6);
        surface.Child = new Border { Background = Brushes.Blue };
        var window = Show(surface);
        try
        {
            using var frame = window.CaptureRenderedFrame()!;
            // Skia fits the two top radii to 25, so both background and border use that contour.
            Assert.Equal((255, 0, 0), PixelAt(frame, 63, 63));
            Assert.Equal((255, 255, 255), PixelAt(frame, 58, 58));
            Assert.Equal((0, 0, 255), PixelAt(frame, 80, 80));
            Assert.Equal((255, 0, 0), PixelAt(frame, 58, 102));
        }
        finally { window.Close(); }
    }

    private static Window Show(Control surface)
    {
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new MaterialResource
        {
            Children = { new ColorScheme() },
        });
        var window = new MotionTestWindow()
        {
            Width = 160,
            Height = 160,
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
            Background = Brushes.White,
            Content = surface,
        };
        window.Show();
        return window;
    }

    private static MaterialSurface ProbeSurface() => new()
    {
        Width = 100,
        Height = 50,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>Reads one opaque pixel from a captured frame, honoring its byte order.</summary>
    private static (int R, int G, int B) PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    [AvaloniaFact]
    public void Elevation_PaintsShadowBelowSurface()
    {
        var surface = ProbeSurface();
        surface.ShadowElevation = 12;
        surface.CornerRadius = new CornerRadius(8);
        surface.Background = Brushes.White;
        var window = Show(surface);

        using var frame = window.CaptureRenderedFrame()!;

        // The spot shadow is a ~1.03× copy of the occluder (light at top-center), so its hard
        // edge lands only a few px below the surface at elevation 12 — scan a strip just below
        // the bottom edge and take the darkest pixel.
        var darkest = Enumerable.Range(105, 10).Select(y => PixelAt(frame, 80, y).R).Min();
        Assert.True(darkest < 245, $"expected shadow darkening just below the surface, got R={darkest}");

        // Well outside the shadow's support (~48px at elevation 12), the window stays white.
        var away = PixelAt(frame, 8, 8);
        Assert.True(away.R > 245, $"expected white away from the shadow, got {away}");
    }

    [AvaloniaFact]
    public void ElevationZero_PaintsNoShadow()
    {
        var surface = ProbeSurface();
        surface.Background = Brushes.White;
        var window = Show(surface);

        using var frame = window.CaptureRenderedFrame()!;

        var below = PixelAt(frame, 80, 120);
        Assert.True(below.R > 245, $"expected no shadow at elevation 0, got {below}");
    }

    [AvaloniaFact]
    public void RoundedClip_ClipsChildContent()
    {
        var surface = ProbeSurface();
        surface.CornerRadius = new CornerRadius(25);
        surface.Child = new Border { Background = Brushes.Red };
        // No surface background: where the child is clipped away, the white window shows through.
        var window = Show(surface);

        using var frame = window.CaptureRenderedFrame()!;

        // Surface-local (3,3) sits deep inside the 25px corner cut.
        var corner = PixelAt(frame, 33, 58);
        Assert.True(corner.R > 240 && corner.G > 240 && corner.B > 240,
            $"expected the clipped corner to show the window, got {corner}");

        var center = PixelAt(frame, 80, 80);
        Assert.True(center.R > 200 && center.G < 60 && center.B < 60,
            $"expected the child to fill the center, got {center}");
    }

    [AvaloniaFact]
    public void Border_PaintsAboveFullSizeContentAndKeepsRoundedClip()
    {
        var surface = ProbeSurface();
        surface.CornerRadius = new CornerRadius(25);
        surface.BorderThickness = new Thickness(6);
        surface.BorderBrush = Brushes.Red;
        surface.Child = new Border { Background = Brushes.Blue };
        var window = Show(surface);
        try
        {
            using (var rounded = window.CaptureRenderedFrame()!)
            {
                Assert.Equal(new Size(100, 50), surface.Child.Bounds.Size);
                Assert.Equal((255, 0, 0), PixelAt(rounded, 80, 57));
                Assert.Equal((0, 0, 255), PixelAt(rounded, 80, 62));
                Assert.Equal((255, 255, 255), PixelAt(rounded, 33, 58));
                Assert.Equal((255, 255, 255), PixelAt(rounded, 80, 54));
            }

            surface.CornerRadius = default;
            surface.BorderBrush = Brushes.Lime;
            using var square = window.CaptureRenderedFrame()!;
            Assert.Equal((0, 255, 0), PixelAt(square, 33, 58));
            Assert.Equal((0, 255, 0), PixelAt(square, 80, 57));
            Assert.Equal((0, 0, 255), PixelAt(square, 80, 62));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ElevatedButton_HoverPaintsStateLayer()
    {
        var styles = Assert.IsAssignableFrom<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        // The variant themes live in Button.axaml's own Styles.Resources, not in MaterialTheme's.
        var buttonStyles = Assert.IsAssignableFrom<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Components/Button/Button.axaml")));
        var window = new MotionTestWindow()
        {
            Width = 200,
            Height = 140,
            RequestedThemeVariant = ThemeVariant.Light,
            Background = Brushes.White,
        };
        window.Styles.Add(styles);

        var button = new Button
        {
            Content = "OK",
            Theme = Assert.IsType<ControlTheme>(buttonStyles.Resources["MaterialElevatedButtonTheme"]),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        window.Content = button;
        window.Show();

        var origin = button.TranslatePoint(default, window)!.Value;
        var cx = (int)(origin.X + button.Bounds.Width / 2);
        var cy = (int)(origin.Y + button.Bounds.Height / 2);

        using var rest = window.CaptureRenderedFrame()!;
        var restCenter = PixelAt(rest, cx, cy);

        window.MouseMove(new Point(cx, cy), RawInputModifiers.None);
        using var hover = window.CaptureRenderedFrame()!;
        var hoverCenter = PixelAt(hover, cx, cy);

        // The hover state layer (content color at 0.08) darkens the container. The elevation
        // 1→2 shadow change is below pixel quantization at 100% scaling; the shadow render path
        // itself is pinned by the standalone-surface probes above.
        Assert.True(hoverCenter.R < restCenter.R,
            $"expected the hover state layer over the container: rest {restCenter} vs hover {hoverCenter}");
    }
}
