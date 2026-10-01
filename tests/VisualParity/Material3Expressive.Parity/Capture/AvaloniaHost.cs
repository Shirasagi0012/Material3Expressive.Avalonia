using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace Material3Expressive.Parity;

public sealed class AvaloniaHost : IAsyncDisposable
{
    readonly CaptureWriter writer;
    readonly ManualAnimationFrameClock clock = new(Dispatcher.UIThread.VerifyAccess);
    public AnimationTestWindow Window { get; }
    public long TimeUs { get; private set; } = -2000;
    bool hasFrame;
    public long? FrameTimeUs => hasFrame ? (clock.Now.Ticks - 10_000_000) / 10 : null;

    public void AdvanceTo(long timeUs)
    {
        if (timeUs < TimeUs) throw new InvalidOperationException("Time cannot move backwards.");
        TimeUs = timeUs;
        writer.Record(timeUs, "advance", "logical event time only; no animation frame; framework delay timers are not virtualized");
    }

    public AvaloniaHost(CaptureWriter writer, int width = 240, int height = 160)
    {
        this.writer = writer;
        Window = new(clock) { Width = width, Height = height, Background = Brushes.White, RequestedThemeVariant = ThemeVariant.Light };
        Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        Window.AnimationRoot.DurationScale = 1;
        Window.AnimationRoot.AnimationFailed += (_, e) => writer.Errors.Add($"{e.OperationLabel}: {e.Exception}");
    }

    public void Mount(Control content)
    {
        Window.Content = content;
        Window.Show();
        if (Window.RenderScaling != 1) throw new NotSupportedException("This host requires rendering scale 1.");
        Draw();
        writer.Record(TimeUs, "mount", $"{Window.Width}x{Window.Height} DIP; density=1; no animation frame");
    }

    public void Frame(long timeUs)
    {
        if (timeUs < TimeUs) throw new InvalidOperationException("Clock cannot move backwards.");
        TimeUs = timeUs;
        clock.SendFrame(TimeSpan.FromTicks(checked(10_000_000 + timeUs * 10)));
        hasFrame = true;
        writer.Record(timeUs, "frame", "origin=1000000us; one waiter batch", new { frameTimeUs = FrameTimeUs, originUs = 1_000_000 });
        Draw();
    }

    public void Draw()
    {
        for (var turn = 0; turn < 20; turn++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            if (!Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.SystemIdle))
            {
                writer.Record(TimeUs, "draw", "dispatcher/layout/render; no animation frame");
                return;
            }
        }

        throw new TimeoutException("Render did not settle within 20 turns.");
    }

    public void Input(string operation, Point position)
    {
        switch (operation)
        {
            case "down": Window.MouseDown(position, MouseButton.Left); break;
            case "up": Window.MouseUp(position, MouseButton.Left); break;
            case "move": Window.MouseMove(position); break;
            default: throw new ArgumentException(operation);
        }

        writer.Record(TimeUs, "input", $"{operation} ({position.X},{position.Y})",
            new
            {
                action = operation, x = position.X, y = position.Y, unit = "dip", space = "scene", pointerType = "mouse",
                button = operation == "move" ? null : "primary"
            });
    }

    public Sample Snapshot(string id, string stage, ProbePoint[] points, bool image = true)
    {
        RasterObservation[] rasters = [];
        if (image || writer.ExtraImages)
        {
            var path = $"images/{id}.png";
            Directory.CreateDirectory(Path.Combine(writer.Output, "images"));
            using var bitmap = Window.GetLastRenderedFrame() ?? throw new IOException("No rendered frame.");
            using (var stream = File.Create(Path.Combine(writer.Output, path))) bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            rasters = [PixelData.FromPng(writer.Output, "full", path)];
            writer.Record(TimeUs, "capture", "passive bitmap read; no draw/frame");
        }

        return new(id, TimeUs, stage, points, rasters, FrameTimeUs,
        [
            new("full", rasters.Length != 0 ? "captured" : "notRequested",
                rasters.Length != 0 ? null : "Case requested a measurement-only sample.")
        ]);
    }

    public async ValueTask DisposeAsync()
    {
        Window.Close();
        try
        {
            await Window.AnimationRoot.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            if (!Window.AnimationRoot.IsEnding || Window.AnimationRoot.ActiveTaskCount != 0
                                               || Window.AnimationRoot.ActiveChildCount != 0 ||
                                               Window.AnimationRoot.PendingAnimationCount != 0)
                writer.Errors.Add("Animation root retained work after close.");
        }
        finally
        {
            clock.Dispose();
            writer.Record(TimeUs, "dispose", "window and animation root");
        }
    }
}
