using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Rendering;

/// <summary>Mounts one case in a fixed Skia-backed headless window and captures its pixels.</summary>
public sealed class VisualRenderer
{
    public RenderedImage Render(VisualCase visualCase)
    {
        ArgumentNullException.ThrowIfNull(visualCase);
        Validate(visualCase);

        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(visualCase.Culture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Window? window = null;
        try
        {
            var theme = visualCase.Theme.CreateMaterialTheme();
            var context = new VisualCaseContext(visualCase.Theme, FontEnvironment.Roboto);
            var content = visualCase.CreateContent(context);
            var logicalSize = new Size(
                visualCase.Viewport.Width / visualCase.RenderScaling,
                visualCase.Viewport.Height / visualCase.RenderScaling);

            var surface = new ResolvedColorScheme(theme.ColorScheme!)
                .GetColor(SysColorToken.Surface, visualCase.Theme.IsDark);
            var root = new Border
            {
                Background = new SolidColorBrush(surface),
                Child = content,
                FlowDirection = visualCase.FlowDirection
            };
            // Off-screen Android/Compose captures use grayscale antialiasing. LCD subpixel
            // antialiasing introduces renderer-specific RGB fringes that cannot be compared as
            // component colour and is also dependent on the physical panel's pixel geometry.
            TextOptions.SetTextRenderingMode(root, TextRenderingMode.Antialias);

            window = new Window
            {
                Width = logicalSize.Width,
                Height = logicalSize.Height,
                WindowDecorations = WindowDecorations.None,
                CanResize = false,
                ShowInTaskbar = false,
                RequestedThemeVariant = visualCase.Theme.Variant,
                Content = root
            };

            MaterialThemeScope.SetTheme(window, theme);
            window.Show();
            window.SetRenderScaling(visualCase.RenderScaling);

            Dispatcher.UIThread.RunJobs();
            window.Measure(logicalSize);
            window.Arrange(new Rect(logicalSize));
            visualCase.Prepare?.Invoke(new VisualContext(window, content));

            // The first frame applies layout/template changes; the second is the stable image.
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
            Dispatcher.UIThread.RunJobs();

            using var frame = window.CaptureRenderedFrame()
                ?? throw new InvalidOperationException($"'{visualCase.Id}' rendered no frame.");
            using var stream = new MemoryStream();
            frame.Save(stream, PngBitmapEncoderOptions.Default);
            var image = RenderedImage.DecodePng(stream.ToArray());

            if (image.Width != visualCase.Viewport.Width || image.Height != visualCase.Viewport.Height)
            {
                throw new InvalidOperationException(
                    $"'{visualCase.Id}' requested {visualCase.Viewport.Width}x{visualCase.Viewport.Height} " +
                    $"pixels but rendered {image.Width}x{image.Height}.");
            }

            return image;
        }
        finally
        {
            window?.Close();
            Dispatcher.UIThread.RunJobs();
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static void Validate(VisualCase visualCase)
    {
        if (string.IsNullOrWhiteSpace(visualCase.Id))
            throw new ArgumentException("A visual case needs a stable ID.", nameof(visualCase));
        if (visualCase.Id.IndexOfAny(Path.GetInvalidPathChars()) >= 0
            || visualCase.Id.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException($"'{visualCase.Id}' is not a safe baseline path.", nameof(visualCase));
        if (visualCase.Viewport.Width <= 0 || visualCase.Viewport.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(visualCase), "The viewport must be positive.");
        if (visualCase.RenderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(visualCase), "Render scaling must be positive.");
        if (string.IsNullOrWhiteSpace(visualCase.Culture))
            throw new ArgumentException("The culture must be explicit.", nameof(visualCase));
        _ = CultureInfo.GetCultureInfo(visualCase.Culture);
    }
}
