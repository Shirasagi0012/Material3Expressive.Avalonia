using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

internal sealed class PickerDialogScene
{
    readonly AvaloniaHost host;
    readonly PickerDialogConfig config;
    readonly Dictionary<string, Marker> markers = new();
    readonly IMouseDevice mouse;
    int requests, confirms, dismisses, toggles;
    public Dialog Dialog { get; }
    public Control Root { get; }

    public PickerDialogScene(AvaloniaHost host, PickerDialogConfig config)
    {
        this.host = host; this.config = config;
        SetTheme(config.Dark);
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        host.Window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var presenter = new ContentPresenter();
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            presenter.Bind(ContentPresenter.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            scope.Register(layers.Name, layers); return layers;
        });
        mouse = (IMouseDevice)host.Window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(host.Window.PlatformImpl)!;
        Dialog = config.Date ? new DatePickerDialog() : new TimePickerDialog();
        Dialog.AnimateTransition = config.Action is "transition" or "interrupt" or "immediate";
        Dialog.IsOpen = !Dialog.AnimateTransition;
        Dialog.UsePlatformDefaultWidth = config.PlatformWidth;
        Dialog.DismissOnEscape = Dialog.DismissOnPointerOutside = config.AllowDismiss;
        Dialog.DismissRequested += (_, _) => requests++;
        if (config.Width is { } width) Dialog.Width = width;
        if (config.Height is { } height) Dialog.Height = height;
        Dialog.Content = Create("content", config.ContentWidth, config.ContentHeight);
        var confirm = Create("confirm", config.ButtonWidth, 40);
        var dismiss = Create("dismiss", config.ButtonWidth, 40);
        if (Dialog is DatePickerDialog date)
        {
            date.ConfirmButton = confirm; date.DismissButton = config.Dismiss ? dismiss : null;
            if (config.Colors == "surface") date.TonalElevation = 6;
        }
        else if (Dialog is TimePickerDialog time)
        {
            time.Title = Create("title", 100, 24);
            time.ConfirmButton = confirm; time.DismissButton = config.Dismiss ? dismiss : null;
            var toggle = Create("toggle", 40, 48);
            time.ModeToggleButton = config.Toggle ? toggle : null;
        }
        if (config.Colors == "custom") { Dialog.Background = Brush.Parse("#E0F0E8"); Dialog.CornerRadius = new CornerRadius(12); }
        else if (config.Colors == "surface") Dialog.Background = Brush.Parse("#FFFBFE");
        else if (config.Colors == "transparent") Dialog.Background = Brushes.Transparent;
        else if (config.Colors == "primary") Dialog.Background = Brush.Parse("#6750A4");
        Root = new Grid { Children = { Dialog } };
    }

    Marker Create(string id, double width, double height, IBrush? fixedBrush = null)
    {
        var marker = new Marker { PreferredWidth = width, PreferredHeight = height, FixedBrush = fixedBrush, Focusable = true, FocusAdorner = null };
        markers[id] = marker;
        bool pressed = false;
        marker.PointerPressed += (_, e) => { if (e.GetCurrentPoint(marker).Properties.IsLeftButtonPressed) pressed = true; };
        marker.PointerReleased += (_, _) =>
        {
            if (pressed)
            {
                if (id == "confirm") confirms++;
                if (id == "dismiss") dismisses++;
                if (id == "toggle") { toggles++; markers["content"].PreferredHeight = 100; }
            }
            pressed = false;
        };
        return marker;
    }

    void SetTheme(bool dark)
    {
        CaseDefaults.SetThemeColors(host, dark);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse(dark ? "#2B2930" : "#ECE6F0");
        host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
        host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FFFBFE");
    }

    ProbePoint[] Measure()
    {
        var points = new List<ProbePoint>
        {
            new("state", "dialog", null, [
                Measurement.Flag("open", Dialog.IsOpen, definition: "dialog.application-open-state"),
                Measurement.Numeric("requests", requests, "count", definition: "dialog.dismiss-request-count"),
                Measurement.Numeric("confirms", confirms, "count", definition: "dialog.confirm-callback-count"),
                Measurement.Numeric("toggles", toggles, "count", definition: "dialog.mode-toggle-callback-count"),
                Measurement.Numeric("dismisses", dismisses, "count", definition: "dialog.dismiss-button-callback-count")])
        };
        if (!Dialog.IsOpen) return points.ToArray();
        var container = host.Window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Surface");
        points.Add(new("container", "dialog", null, AvaloniaMeasurements.Geometry(container, host.Window).Where(m => m.Name == "size").ToArray()));
        foreach (var (id, marker) in markers.Where(p => p.Value.IsAttachedToVisualTree()))
        {
            var box = new Rect(marker.Bounds.Size).TransformToAABB(marker.TransformToVisual(container)!.Value);
            // Compose localPositionOf is a physical coordinate. A native RTL
            // parent can expose a mirrored local coordinate while painting the
            // same pixels; remove only that coordinate orientation, not layout.
            if (container.GetSelfAndVisualAncestors().Aggregate(false, (mirrored, visual) => mirrored ^ visual.HasMirrorTransform))
                box = new Rect(container.Bounds.Width - box.Right, box.Y, box.Width, box.Height);
            var color = ((ISolidColorBrush)(marker.FixedBrush ?? TextElement.GetForeground(marker))!).Color;
            points.Add(new(id, id, "container", [
                Measurement.Vector("box", "rect", [box.X, box.Y, box.Width, box.Height], "dip", "local", "geometry",
                    "dialog.slot-box.relative-container", "Control rectangle in dialog content coordinates, normalized to physical RTL orientation"),
                Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties",
                    "dialog.slot-inherited-color", "Marker rendered inherited TextElement.Foreground")]));
        }
        return points.ToArray();
    }

    void Input(string action, Point point)
    {
        // Dialog opening changes focus and releases capture. Use the existing headless device
        // (rather than creating another mouse); provide the same logical time as Compose.
        var type = action switch { "down" => RawPointerEventType.LeftButtonDown, "up" => RawPointerEventType.LeftButtonUp, _ => RawPointerEventType.Move };
        var platform = host.Window.PlatformImpl!;
        var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), mouse,
            (ulong)(1000 + host.TimeUs / 1000), platform.GetType().GetProperty("InputRoot")!.GetValue(platform), type, point, RawInputModifiers.None)!;
        ((Action<RawInputEventArgs>)platform.GetType().GetProperty("Input")!.GetValue(platform)!)(raw);
    }

    public static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<PickerDialogConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, config.HostHeight);
        var scene = new PickerDialogScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        Sample("initial");
        if (!dynamic) return;
        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; }
            host.AdvanceTo(time);
        }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id);
        }
        void Times(params long[] times)
        {
            foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        }
        switch (config.Action)
        {
            case "theme": Change(1000, "dark", () => scene.SetTheme(true)); Change(2000, "light", () => scene.SetTheme(false)); break;
            case "slots":
                void Slots(bool visible)
                {
                    if (scene.Dialog is DatePickerDialog date) date.DismissButton = visible ? scene.markers["dismiss"] : null;
                    if (scene.Dialog is TimePickerDialog time) { time.DismissButton = visible ? scene.markers["dismiss"] : null; time.ModeToggleButton = visible ? scene.markers["toggle"] : null; }
                }
                Change(1000, "remove-slots", () => Slots(false));
                Change(2000, "restore", () => Slots(true)); break;
            case "mode":
                Change(1000, "input-mode", () => scene.markers["content"].PreferredHeight = 100);
                Change(2000, "picker-mode", () => scene.markers["content"].PreferredHeight = config.ContentHeight); break;
            case "resize":
                Change(1000, "wide", () => scene.markers["confirm"].PreferredWidth = 220);
                Change(2000, "restore", () => scene.markers["confirm"].PreferredWidth = config.ButtonWidth); break;
            case "escape":
                Change(1000, "escape-down", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"));
                Change(2000, "escape-up", () => host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape")); break;
            case "outside":
            case "confirm":
            case "dismiss":
            case "toggle":
                var point = config.Action == "outside" ? new Point(10, 10) :
                    scene.markers[config.Action].TranslatePoint(new Point(20, 20), host.Window)!.Value;
                Change(1000, "down", () => { scene.Input("move", point); scene.Input("down", point); });
                Change(2000, "up", () => scene.Input("up", point)); break;
            case "transition":
                Change(1000, "open", () => scene.Dialog.IsOpen = true);
                Times(16000, 32000, 64000, 112000, 208000, 224000);
                Change(225000, "close", () => scene.Dialog.IsOpen = false);
                Times(240000, 272000, 320000, 336000);
                Change(337000, "reopen", () => scene.Dialog.IsOpen = true);
                Times(352000, 400000, 544000, 560000);
                break;
            case "interrupt":
                Change(1000, "open", () => scene.Dialog.IsOpen = true);
                Times(16000, 32000, 64000);
                Change(65000, "close-during-entry", () => scene.Dialog.IsOpen = false);
                Times(80000, 96000);
                Change(97000, "reopen-during-exit", () => scene.Dialog.IsOpen = true);
                Times(112000, 128000, 160000, 304000, 320000);
                Change(321000, "close", () => scene.Dialog.IsOpen = false);
                Times(336000, 352000, 384000, 448000);
                break;
            case "immediate":
                Change(1000, "open", () => scene.Dialog.IsOpen = true);
                Change(2000, "close-before-animation-frame", () => scene.Dialog.IsOpen = false);
                Times(16000, 32000);
                break;
        }
        host.AdvanceTo(host.TimeUs + 1000); Sample("idle", false);
    }

    sealed class Marker : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        public double PreferredWidth { get => _width; set { _width = value; InvalidateMeasure(); } }
        double _width;
        public double PreferredHeight { get => _height; set { _height = value; InvalidateMeasure(); } }
        double _height;
        public IBrush? FixedBrush { get; set; }
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.DrawRectangle(FixedBrush ?? TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
