using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using Avalonia.Threading;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TooltipBoxConfig(string Position = "Above", int X = 112, int Y = 80, bool Rtl = false,
    bool Dark = false, double Spacing = 4, double ContentWidth = 64, double ContentHeight = 8,
    string Action = "none", bool Persistent = false, bool Enabled = true, bool HasAction = false, double Elevation = 0,
    double AnchorScale = 1, bool Rich = false, string PointerKind = "Touch", int LongPressMilliseconds = 500);

[ParitySuite("tooltip-box")]
public sealed class TooltipBoxCases
{
    public static IEnumerable<CaseData<TooltipBoxConfig>> StaticConfigurations()
    {
        foreach (var position in Enum.GetNames<TooltipAnchorPosition>())
        foreach (var rtl in new[] { false, true })
        foreach (var point in new[] { (112, 80), (0, 0), (232, 0), (0, 168), (232, 168) })
            yield return Data($"static.{position.ToLowerInvariant()}.{point.Item1}-{point.Item2}.{(rtl ? "rtl" : "ltr")}", new(position, point.Item1, point.Item2, rtl));
        yield return Data("static.dark", new(Dark: true));
        yield return Data("static.spacing", new(Spacing: 12));
        yield return Data("static.wide", new(ContentWidth: 300));
        yield return Data("static.tall", new(ContentHeight: 220));
        yield return Data("static.elevated", new(Elevation: 6));
        yield return Data("static.scaled-anchor", new(AnchorScale: 2));
        yield return Data("static.scaled-anchor.rtl", new(AnchorScale: 2, Rtl: true));
    }
    public static IEnumerable<CaseData<TooltipBoxConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "show", "interrupt", "hover", "focus", "escape", "outside", "request", "resize", "timeout" })
            yield return Data($"dynamic.{action}", new(Action: action));
        yield return Data("dynamic.hover.persistent", new(Action: "hover", Persistent: true));
        yield return Data("dynamic.hover.disabled", new(Action: "hover", Enabled: false));
        foreach (var persistent in new[] { false, true })
        foreach (var action in new[] { "hover-disable", "hover-disable-application" })
            yield return Data($"dynamic.{action}.{(persistent ? "persistent" : "transient")}", new(Action: action, Persistent: persistent));
        yield return Data("dynamic.tab.action", new(Action: "focus", HasAction: true));
        yield return Data("dynamic.show.rtl", new(Action: "show", Rtl: true));
        yield return Data("dynamic.show.dark", new(Action: "show", Dark: true));
        foreach (var action in new[] { "show", "interrupt", "resize" })
            yield return Data($"dynamic.rich.{action}", new(Action: action, Rich: true, HasAction: true, Elevation: 3));
    }
    public static IEnumerable<CaseData<TooltipBoxConfig>> LongPressConfigurations()
    {
        foreach (var pointer in new[] { "Touch", "Pen" })
        foreach (var persistent in new[] { false, true })
            yield return Data($"long-press.{pointer.ToLowerInvariant()}.{(persistent ? "persistent" : "transient")}",
                new(Action: "long-press", Persistent: persistent, PointerKind: pointer));
        yield return Data("long-press.custom-duration", new(Action: "long-press", LongPressMilliseconds: 750));
        yield return Data("long-press.short", new(Action: "short-press"));
        yield return Data("long-press.disabled", new(Action: "long-press", Enabled: false));
        foreach (var persistent in new[] { false, true })
            yield return Data($"long-press.disable-after-show.{(persistent ? "persistent" : "transient")}", new(Action: "long-press-disable", Persistent: persistent));
    }
    [ParityScenario("long-press", Data = nameof(LongPressConfigurations))]
    public Task LongPress(CaptureWriter writer) => Capture(writer, true);

    static CaseData<TooltipBoxConfig> Data(string id, TooltipBoxConfig config) => new(id, id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<TooltipBoxConfig>();
        await using var host = new AvaloniaHost(writer, 256, 192, new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess));
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter();
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            presenter.Bind(ContentPresenter.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers); return layers;
        });
        CaseDefaults.SetThemeColors(host, config.Dark);
        host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(config.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(config.Dark ? "#322F35" : "#F5EFF7");
        host.Window.Resources[Sys.Color.Shadow] = Colors.Black;
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(config.Dark ? "#211F26" : "#F3EDF7");
        var initiallyVisible = !dynamic || config.Action is "outside" or "request" or "resize";
        using var state = new TooltipState(initiallyVisible, config.Persistent, new MutatorMutex());
        // Isolate the long-press recognizer from native pointer-to-keyboard focus.
        // Both long-press anchors are explicitly nonfocusable; focus has separate scenarios.
        var anchor = new Border { Width = 24, Height = 24, Background = Brushes.Blue, Focusable = config.Action is not ("long-press" or "short-press" or "long-press-disable") };
        var content = new Marker { PreferredWidth = config.ContentWidth, PreferredHeight = config.ContentHeight };
        ContentControl tooltip = config.Rich
            ? new RichTooltip { Content = content, ShadowElevation = config.Elevation,
                Title = new Marker { PreferredWidth = 32, PreferredHeight = 10 }, Action = new Marker { PreferredWidth = 24, PreferredHeight = 12 } }
            : new PlainTooltip { Content = content, ShadowElevation = config.Elevation };
        var box = new TooltipBox { Content = anchor, Tooltip = tooltip, State = state,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            AnchorPosition = Enum.Parse<TooltipAnchorPosition>(config.Position), Spacing = config.Spacing,
            LongPressDuration = TimeSpan.FromMilliseconds(config.LongPressMilliseconds),
            RenderTransform = new ScaleTransform(config.AnchorScale, config.AnchorScale),
            EnableUserInput = dynamic && config.Enabled && config.Action is not ("outside" or "request" or "resize"), HasAction = config.HasAction };
        int requests = 0, presses = 0;
        var applicationCancelled = false;
        async Task ShowDefault()
        {
            try { await state.ShowAsync(); }
            catch (OperationCanceledException) { applicationCancelled = true; }
        }
        if (config.Action == "request") box.DismissRequested += (_, _) => requests++;
        var outside = new Border { Width = 16, Height = 16, Background = Brushes.Red };
        // Observe the same low-level press as the Core scene dispatch, not a different click recognizer.
        outside.PointerPressed += (_, _) => presses++;
        var canvas = new Canvas { Background = Brushes.White, Children = { box, outside } };
        Canvas.SetLeft(box, config.X); Canvas.SetTop(box, config.Y);
        Canvas.SetLeft(outside, 16); Canvas.SetTop(outside, 72);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control c) => AvaloniaMeasurements.Geometry(c, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("state", "state", null,
                [Measurement.Flag("visible", state.IsVisible, definition: "tooltip.state.is-visible"),
                 Measurement.Flag("current", state.Transition.CurrentState, definition: "tooltip.transition.current-state"),
                 Measurement.Flag("target", state.Transition.TargetState, definition: "tooltip.transition.target-state"),
                 Measurement.Numeric("requests", requests, "count", definition: "tooltip.dismiss-request-count"),
                 Measurement.Flag("applicationShowCancelled", applicationCancelled, definition: "tooltip.application-show.cancelled"),
                 Measurement.Numeric("presses", presses, "count", definition: "tooltip.underlying-press-count")]),
                new("anchor", "anchor", null, Geometry(box)) };
            if (tooltip.IsAttachedToVisualTree())
            {
                // Dynamic appearance is verified by the raster. Layout facts deliberately observe
                // the untransformed popup and local sizes: Float and Double transform matrices are
                // different representations, and are not exact cross-framework layout coordinates.
                var popup = tooltip.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
                if (dynamic) points.Add(new("popup", "popup", null, Geometry(popup)));
                points.Add(new("tooltip", "tooltip", dynamic ? "popup" : null,
                    dynamic ? Geometry(tooltip).Where(m => m.Name == "size").ToArray() : Geometry(tooltip)));
                var color = content.Painted;
                points.Add(new("content", "content", "tooltip", [.. Geometry(content).Where(m => !dynamic || m.Name == "size"),
                    Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties",
                        "tooltip.content.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial"); if (!dynamic) return;
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Show() => _ = state.ShowAsync(MutatePriority.UserInput);
        var center = anchor.TranslatePoint(new Point(12, 12), host.Window)!.Value;
        using var pointer = new Pointer(Pointer.GetNextFreeId(), Enum.Parse<PointerType>(config.PointerKind), true);
        void Touch(bool down)
        {
            if (down) anchor.RaiseEvent(new PointerPressedEventArgs(anchor, pointer, host.Window, center, (ulong)(host.TimeUs / 1000),
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            else anchor.RaiseEvent(new PointerReleasedEventArgs(anchor, pointer, host.Window, center, (ulong)(host.TimeUs / 1000),
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        }
        switch (config.Action)
        {
            case "long-press": case "short-press": case "long-press-disable":
                Change(1000, "touch-down", () => Touch(true));
                if (config.Action == "short-press") Change(250000, "short-release", () => Touch(false));
                var shift = (config.LongPressMilliseconds - 500L) * 1000;
                Times(500000 + shift, 501000 + shift, 512000 + shift, 544000 + shift, 640000 + shift, 800000 + shift);
                if (config.Action == "long-press") Change(801000 + shift, "touch-release", () => Touch(false));
                if (config.Action == "long-press-disable")
                {
                    Change(801000, "disable-input", () => box.EnableUserInput = false);
                    Change(802000, "touch-release", () => Touch(false));
                    Change(803000, "application-default", () => _ = ShowDefault());
                }
                Times(816000 + shift, 960000 + shift);
                // End before the separate TooltipState 1500ms timeout, whose Core dispatcher
                // timeout source is not virtualized by the current reference host.
                break;
            case "timeout":
                Change(1000, "show-default", () => _ = state.ShowAsync());
                Times(16000, 64000, 400000, 1500000, 1501000, 1520000, 1584000, 1904000);
                break;
            case "show":
                Change(1000, "show", Show); Times(16000, 32000, 64000, 112000, 224000, 400000);
                Change(401000, "dismiss", state.Dismiss); Times(416000, 448000, 512000, 800000); break;
            case "interrupt":
                Change(1000, "show", Show); Times(16000, 32000, 64000);
                Change(65000, "dismiss", state.Dismiss); Times(80000, 96000);
                Change(97000, "reopen", Show); Times(112000, 128000, 224000, 400000); break;
            case "hover":
                Change(1000, "enter", () => host.Input("move", center)); Times(16000, 64000, 224000, 400000);
                Change(401000, "exit", () => host.Input("move", new Point(250, 185))); Times(416000, 448000, 512000, 800000); break;
            case "hover-disable": case "hover-disable-application":
                Change(1000, "enter", () => host.Input("move", center)); Times(16000, 64000, 224000, 400000);
                if (config.Action == "hover-disable-application") Change(401000, "application-show", Show);
                Change(402000, "disable-input", () => box.EnableUserInput = false); Times(416000, 448000, 512000, 800000);
                Change(801000, "enable-input", () => box.EnableUserInput = true);
                Change(802000, "exit", () => host.Input("move", new Point(250, 185)));
                Change(803000, "reenter", () => host.Input("move", center)); Times(816000, 880000, 1200000);
                break;
            case "focus": case "escape":
                Change(1000, "focus", () => anchor.Focus()); Times(16000, 64000, 224000, 400000);
                if (config.HasAction)
                    Change(401000, "tab", () => host.Window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "Tab"));
                else if (config.Action == "escape")
                    Change(401000, "escape", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"));
                else Change(401000, "blur", () => { outside.Focusable = true; outside.Focus(); });
                Times(416000, 448000, 512000, 800000); break;
            case "outside": case "request":
                var point = outside.TranslatePoint(new Point(8, 8), host.Window)!.Value;
                Change(1000, "down", () => { host.Input("move", point); host.Input("down", point); });
                Change(2000, "up", () => host.Input("up", point)); Times(16000, 64000, 224000, 400000); break;
            case "resize":
                Change(1000, "move", () => { Canvas.SetLeft(box, 4); Canvas.SetTop(box, 4); });
                // Observe layout invalidation from Popup placement in another render
                // turn at the same animation-frame timestamp; retain the first sample.
                host.Draw(); Sample("move-redraw");
                Change(2000, "grow", () => { anchor.Width = 40; content.PreferredWidth = 160; });
                Change(3000, "direction", () => box.AnchorPosition = TooltipAnchorPosition.End); break;
        }
    }
    sealed class Marker : Control
    {
        public static readonly StyledProperty<double> PreferredWidthProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredWidth));
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredHeight));
        public double PreferredWidth { get => GetValue(PreferredWidthProperty); set => SetValue(PreferredWidthProperty, value); }
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        public Color Painted { get; private set; }
        static Marker() { AffectsMeasure<Marker>(PreferredWidthProperty, PreferredHeightProperty); AffectsRender<Marker>(TextElement.ForegroundProperty); }
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public override void Render(DrawingContext context)
        { var brush = TextElement.GetForeground(this); Painted = ((ISolidColorBrush)brush!).Color; context.DrawRectangle(brush, null, new Rect(Bounds.Size)); }
    }
}
