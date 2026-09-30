using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
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
    private static Window Show(MaterialSurface surface)
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
    public void ElevatedButton_HoverPaintsStateLayer()
    {
        var styles = Assert.IsAssignableFrom<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Styles/MaterialTheme.axaml")));
        // The variant themes live in Button.axaml's own Styles.Resources, not in MaterialTheme's.
        var buttonStyles = Assert.IsAssignableFrom<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Styles/Button.axaml")));
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
