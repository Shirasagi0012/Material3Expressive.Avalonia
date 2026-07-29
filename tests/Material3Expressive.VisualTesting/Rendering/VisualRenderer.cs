using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Rendering;

/// <summary>Mounts one case in a fixed Skia-backed headless window and captures its pixels.</summary>
public sealed class VisualRenderer
{
    public RenderedImage Render(VisualCase visualCase) =>
        Render(visualCase, null, static (_, image) => image);

    /// <summary>
    /// Renders once and lets a caller inspect the mounted Avalonia tree before it is disposed.
    /// This is used by parity capture to obtain geometry and text-layout facts from the same frame.
    /// </summary>
    public T Render<T>(VisualCase visualCase, Func<VisualContext, RenderedImage, T> capture)
        => Render(visualCase, null, capture);

    /// <summary>
    /// Renders a scene after replaying a Compose-produced logical motion timeline.
    /// </summary>
    public T Render<T>(
        VisualCase visualCase,
        VisualMotionReplay? motionReplay,
        Func<VisualContext, RenderedImage, T> capture)
    {
        Dispatcher.UIThread.VerifyAccess();
        ArgumentNullException.ThrowIfNull(visualCase);
        ArgumentNullException.ThrowIfNull(capture);
        Validate(visualCase, motionReplay);

        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(visualCase.Culture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Window? window = null;
        MotionTestClockScope? motionClock = null;
        try
        {
            if (motionReplay is not null)
                motionClock = new MotionTestClockScope();

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
            Layout(window, logicalSize);
            var visualContext = new VisualContext(window, content, root)
            {
                AdvanceManualMotion = motionClock is null ? null : motionClock.AdvanceBy,
                SettleManualMotion = motionClock is null ? null : motionClock.SettleAll
            };
            visualCase.Prepare?.Invoke(visualContext);

            if (motionReplay is not null)
            {
                visualCase.Motion!.Trigger(visualContext);
                Dispatcher.UIThread.RunJobs();
                Layout(window, logicalSize);
                visualContext.DeliveredMotionFrameTicks = ReplayMotion(motionClock!, motionReplay, window, logicalSize);
                visualContext.MotionCaptureFrameIndex = motionReplay.CaptureFrameIndex;
            }

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

            return capture(visualContext, image);
        }
        finally
        {
            window?.Close();
            Dispatcher.UIThread.RunJobs();
            motionClock?.Dispose();
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static void Validate(VisualCase visualCase, VisualMotionReplay? motionReplay)
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

        if ((visualCase.Motion is null) != (motionReplay is null))
            throw new ArgumentException(
                "A motion scene and a Compose-produced replay timeline must be supplied together.",
                nameof(motionReplay));
        if (motionReplay is null)
            return;
        if (motionReplay.Origin != "first-controlled-motion-frame"
            || motionReplay.TimeUnit != "100ns-ticks"
            || motionReplay.DurationScale != 1f)
            throw new ArgumentException("The motion replay uses an unsupported origin, unit, or duration scale.", nameof(motionReplay));
        if (motionReplay.DeliveryFrameTicks.Count == 0
            || motionReplay.DeliveryFrameTicks[0] != 0
            || motionReplay.CaptureFrameIndex != motionReplay.DeliveryFrameTicks.Count - 1)
            throw new ArgumentException("The motion replay must start at zero and capture its final delivery frame.", nameof(motionReplay));
        for (var index = 1; index < motionReplay.DeliveryFrameTicks.Count; index++)
        {
            if (motionReplay.DeliveryFrameTicks[index] <= motionReplay.DeliveryFrameTicks[index - 1])
                throw new ArgumentException("Motion delivery-frame timestamps must be strictly increasing.", nameof(motionReplay));
        }
    }

    private static IReadOnlyList<long> ReplayMotion(
        MotionTestClockScope motionClock,
        VisualMotionReplay replay,
        Window window,
        Size logicalSize)
    {
        var delivered = new List<long>(replay.CaptureFrameIndex + 1);
        var previousTicks = 0L;
        for (var index = 0; index <= replay.CaptureFrameIndex; index++)
        {
            var ticks = replay.DeliveryFrameTicks[index];
            var delta = index == 0 ? ticks : ticks - previousTicks;
            motionClock.AdvanceBy(TimeSpan.FromTicks(delta));
            previousTicks = ticks;

            Dispatcher.UIThread.RunJobs();
            Layout(window, logicalSize);
            delivered.Add(ticks);
        }

        return delivered;
    }

    private static void Layout(Window window, Size logicalSize)
    {
        window.Measure(logicalSize);
        window.Arrange(new Rect(logicalSize));
    }
}
