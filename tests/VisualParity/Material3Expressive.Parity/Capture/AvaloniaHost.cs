using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using System.Reflection;
using ComposeAnimation.TestSupport;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace Material3Expressive.Parity;

public sealed class AvaloniaHost : IAsyncDisposable
{
    public const int RenderScale = 4;
    static readonly FieldInfo NextCommit = CompositionBatchField("_nextCommit");
    static readonly FieldInfo PendingBatch = CompositionBatchField("_pendingBatch");
    readonly CaptureWriter writer;
    readonly ManualAnimationFrameClock clock = new(Dispatcher.UIThread.VerifyAccess);
    readonly ManualTimeProvider? delayTime;
    public AnimationTestWindow Window { get; }
    public long TimeUs { get; private set; } = -2000;
    bool hasFrame;
    public long? FrameTimeUs => hasFrame ? (clock.Now.Ticks - 10_000_000) / 10 : null;

    public void AdvanceTo(long timeUs)
    {
        if (timeUs < TimeUs) throw new InvalidOperationException("Time cannot move backwards.");
        TimeUs = timeUs;
        AdvanceDelays(timeUs);
        writer.Record(timeUs, "advance", delayTime is null
            ? "logical event time only; no animation frame; framework delay timers are not virtualized"
            : "logical event time and coroutine delay scheduler (1ms); no animation frame");
    }

    public AvaloniaHost(CaptureWriter writer, int width = 240, int height = 160, ManualTimeProvider? delayTime = null)
    {
        this.writer = writer;
        this.delayTime = delayTime;
        Window = new(clock, delayTime ?? TimeProvider.System) { Width = width, Height = height, Background = Brushes.White, RequestedThemeVariant = ThemeVariant.Light };
        Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        Window.AnimationRoot.DurationScale = 1;
        Window.AnimationRoot.AnimationFailed += (_, e) => writer.Errors.Add($"{e.OperationLabel}: {e.Exception}");
    }

    public void Mount(Control content)
    {
        Window.Content = content;
        Window.SetRenderScaling(RenderScale);
        Window.Show();
        if (Window.RenderScaling != RenderScale) throw new NotSupportedException("Headless render scale was not applied.");
        Draw();
        writer.Record(TimeUs, "mount", $"{Window.Width}x{Window.Height} DIP; density={RenderScale}; no animation frame");
    }

    public void Frame(long timeUs)
    {
        if (timeUs < TimeUs) throw new InvalidOperationException("Clock cannot move backwards.");
        TimeUs = timeUs;
        AdvanceDelays(timeUs);
        clock.SendFrame(TimeSpan.FromTicks(checked(10_000_000 + timeUs * 10)));
        hasFrame = true;
        writer.Record(timeUs, "frame", "origin=1000000us; one waiter batch", new { frameTimeUs = FrameTimeUs, originUs = 1_000_000 });
        Draw();
    }

    void AdvanceDelays(long timeUs)
    {
        if (delayTime is null) return;
        var target = TimeSpan.FromMilliseconds(Math.Max(timeUs, 0) / 1000);
        // Like Compose's scheduler, resume delay continuations before sending the frame.
        // Pump at integer milliseconds so a continuation that starts another delay anchors
        // to its due time even when the observation skips a large interval. No render/frame.
        while (delayTime.Elapsed < target)
        {
            delayTime.AdvanceBy(TimeSpan.FromTicks(Math.Min(TimeSpan.TicksPerMillisecond, (target - delayTime.Elapsed).Ticks)));
            Dispatcher.UIThread.RunJobs();
        }
    }

    public void Draw()
    {
        Dispatcher.UIThread.VerifyAccess();
        var compositor = ElementComposition.GetElementVisual(Window)?.Compositor
            ?? throw new InvalidOperationException("Mount the window before drawing.");
        for (var turn = 0; turn < 20; turn++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            // Processing a completed batch can submit another one without leaving a dispatcher job.
            // Do not read the previous bitmap while that new batch is still awaiting a render tick.
            if (!Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.SystemIdle)
                && !HasPendingCompositionWork(compositor))
            {
                writer.Record(TimeUs, "draw", "dispatcher/layout/render; no animation frame");
                return;
            }
        }

        throw new TimeoutException("Dispatcher/composition work did not finish within 20 render turns.");
    }

    static bool HasPendingCompositionWork(Compositor compositor)
    {
        if (NextCommit.GetValue(compositor) is not null) return true;
        if (PendingBatch.GetValue(compositor) is not CompositionBatch batch) return false;
        if (!batch.Rendered.IsCompleted) return true;
        batch.Rendered.GetAwaiter().GetResult();
        return false;
    }

    static FieldInfo CompositionBatchField(string name)
    {
        // Avalonia 12.1.2 has no public, read-only pending-work query. RequestCommit would create
        // new work just to check for it. Keep this version-sensitive inspection local to the host.
        // This assumes ParityAppBuilder's UI-thread renderer: ForceRenderTimerTick returns after
        // rendering, even though Avalonia clears _pendingBatch as soon as it is Processed.
        var field = typeof(Compositor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.FieldType != typeof(CompositionBatch))
            throw new NotSupportedException($"Avalonia {typeof(Compositor).Assembly.GetName().Version}: " +
                $"expected Compositor.{name} to be a CompositionBatch; review the parity render barrier.");
        return field;
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
