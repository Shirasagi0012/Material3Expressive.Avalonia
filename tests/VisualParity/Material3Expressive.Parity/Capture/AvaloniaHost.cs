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
using Material3Expressive.TestSupport;

namespace Material3Expressive.Parity;

public sealed class AvaloniaHost : IAsyncDisposable
{
    public const int RenderScale = 4;
    static readonly FieldInfo NextCommit = CompositionBatchField("_nextCommit");
    static readonly FieldInfo PendingBatch = CompositionBatchField("_pendingBatch");
    static readonly Type HeadlessWindowType = typeof(AvaloniaHeadlessPlatform).Assembly.GetType("Avalonia.Headless.HeadlessWindowImpl", throwOnError: true)!;
    static readonly FieldInfo Framebuffer = HeadlessField("_lastRenderedFrame", typeof(WriteableBitmap));
    static readonly FieldInfo FramebufferSync = HeadlessField("_sync", typeof(object));
    readonly CaptureWriter writer;
    readonly ManualFrameSource clock = new(Dispatcher.UIThread.VerifyAccess);
    readonly ManualTimeProvider? delayTime;
    readonly HeadlessMouseInput mouseInput;
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
        mouseInput = new(Window);
        // Load the theme assembly even when the first case mounts only native Avalonia controls.
        // A URI string alone does not ensure that the test runner has loaded this dependency.
        var themeAssembly = typeof(Components.MaterialSurface).Assembly.GetName().Name;
        Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri($"avares://{themeAssembly}/MaterialTheme.axaml")));
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
        var pointerTimeMs = checked((ulong)(1000 + Math.Max(0, TimeUs) / 1000));
        mouseInput.Send(pointerTimeMs, operation, position);
        Dispatcher.UIThread.RunJobs();

        writer.Record(TimeUs, "input", $"{operation} ({position.X},{position.Y})",
            new
            {
                action = operation, x = position.X, y = position.Y, unit = "dip", space = "scene", pointerType = "mouse", pointerTimeMs,
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
            using var bitmap = CopyLastRenderedFrame();
            using (var stream = File.Create(Path.Combine(writer.Output, path))) bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            rasters = [PixelData.FromPng(writer.Output, "full", path)];
            writer.Record(TimeUs, "capture", "passive framebuffer copy preserving alpha format; no draw/frame");
        }

        return new(id, TimeUs, stage, points, rasters, FrameTimeUs,
        [
            new("full", rasters.Length != 0 ? "captured" : "notRequested",
                rasters.Length != 0 ? null : "Case requested a measurement-only sample.")
        ]);
    }

    WriteableBitmap CopyLastRenderedFrame()
    {
        Dispatcher.UIThread.VerifyAccess();
        var platform = Window.PlatformImpl;
        if (platform is null || platform.GetType() != HeadlessWindowType)
            throw new NotSupportedException("Parity capture requires the verified Avalonia headless framebuffer.");
        // Headless 12.1.5 GetLastRenderedFrame labels its premultiplied buffer Opaque.
        // Copy the same completed frame under its original lock, preserving actual metadata.
        // No pixels are inferred or changed and no render/animation work is requested here.
        lock (FramebufferSync.GetValue(platform) ?? throw new IOException("No framebuffer lock."))
        {
            if (Framebuffer.GetValue(platform) is not WriteableBitmap source)
                throw new IOException("No rendered frame.");
            using var buffer = source.Lock();
            return new WriteableBitmap(buffer.Format, buffer.AlphaFormat, buffer.Address,
                buffer.Size, buffer.Dpi, buffer.RowBytes);
        }
    }

    static FieldInfo HeadlessField(string name, Type expectedType)
    {
        var field = HeadlessWindowType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.FieldType != expectedType)
            throw new NotSupportedException($"Avalonia.Headless {HeadlessWindowType.Assembly.GetName().Version}: " +
                $"expected {name} to be {expectedType.Name}; review the parity framebuffer copy.");
        return field;
    }

    public async ValueTask DisposeAsync()
    {
        Window.Close();
        try
        {
            await Window.AnimationRoot.Diagnostics.EndAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (!Window.AnimationRoot.Diagnostics.IsEnding || Window.AnimationRoot.Diagnostics.ActiveTaskCount != 0
                                               || Window.AnimationRoot.Diagnostics.ActiveChildCount != 0 ||
                                               Window.AnimationRoot.Diagnostics.PendingAnimationCount != 0)
                writer.Errors.Add("Animation root retained work after close.");
        }
        finally
        {
            clock.Dispose();
            writer.Record(TimeUs, "dispose", "window and animation root");
        }
    }
}
