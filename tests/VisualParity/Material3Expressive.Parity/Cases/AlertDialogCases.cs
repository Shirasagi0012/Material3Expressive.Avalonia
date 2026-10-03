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

public sealed record AlertDialogConfig(bool Basic = false, bool Icon = true, bool Title = true, bool Text = true,
    bool Dismiss = true, bool Dark = false, bool Rtl = false, bool PlatformWidth = true,
    double? Width = null, double? Height = null, int HostWidth = 520, int HostHeight = 360,
    double ContentWidth = 160, double ContentHeight = 40, double ButtonWidth = 80,
    string Colors = "default", string Action = "none", bool AllowDismiss = true);

[ParitySuite("alert-dialog")]
public sealed class AlertDialogCases
{
    public static IEnumerable<CaseData<AlertDialogConfig>> StaticConfigurations()
    {
        // All optional-slot combinations and both color schemes. Button wrapping and RTL
        // are separate branches, rather than unrelated dimensions multiplied into this matrix.
        foreach (var icon in new[] { false, true })
        foreach (var title in new[] { false, true })
        foreach (var text in new[] { false, true })
        foreach (var dismiss in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.slots.{(icon ? "icon" : "no-icon")}.{(title ? "title" : "no-title")}.{(text ? "text" : "no-text")}.{(dismiss ? "two" : "one")}.{(dark ? "dark" : "light")}",
                new(Icon: icon, Title: title, Text: text, Dismiss: dismiss, Dark: dark));
        foreach (var rtl in new[] { false, true })
        {
            yield return Data($"static.buttons.stack.{(rtl ? "rtl" : "ltr")}", new(Rtl: rtl, ButtonWidth: 180));
            yield return Data($"static.buttons.row.{(rtl ? "rtl" : "ltr")}", new(Rtl: rtl));
            yield return Data($"static.height.constrained.{(rtl ? "rtl" : "ltr")}", new(Rtl: rtl, HostHeight: 200, ContentHeight: 300));
        }
        yield return Data("static.height.explicit", new(Height: 300));
        yield return Data("static.rtl.no-icon", new(Rtl: true, Icon: false));
        yield return Data("static.rtl.single-action", new(Rtl: true, Dismiss: false));
        yield return Data("static.width.explicit-small", new(Width: 180));
        foreach (var color in new[] { "custom", "surface", "transparent" })
            yield return Data($"static.colors.{color}", new(Colors: color));
    }

    public static IEnumerable<CaseData<AlertDialogConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "theme", "slots", "resize", "outside", "escape", "confirm", "dismiss", "transition", "interrupt", "immediate" })
            yield return Data($"dynamic.{action}", new(Action: action));
        foreach (var action in new[] { "outside", "escape" })
            yield return Data($"dynamic.{action}.disabled", new(Action: action, AllowDismiss: false));
        yield return Data("dynamic.transition.rtl", new(Action: "transition", Rtl: true));
        yield return Data("dynamic.transition.dark", new(Action: "transition", Dark: true));
    }

    internal static CaseData<AlertDialogConfig> Data(string id, AlertDialogConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => AlertDialogScene.Capture(writer, false);

    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => AlertDialogScene.Capture(writer, true);
}

[ParitySuite("basic-alert-dialog")]
public sealed class BasicAlertDialogCases
{
    public static IEnumerable<CaseData<AlertDialogConfig>> StaticConfigurations()
    {
        foreach (var width in new[] { 20d, 300d, 700d })
            yield return AlertDialogCases.Data($"static.content.{width}", new(Basic: true, ContentWidth: width));
        yield return AlertDialogCases.Data("static.width.explicit-small", new(Basic: true, Width: 100));
        yield return AlertDialogCases.Data("static.width.explicit-large", new(Basic: true, Width: 400));
        yield return AlertDialogCases.Data("static.width.no-platform", new(Basic: true, PlatformWidth: false, ContentWidth: 700, HostWidth: 640));
        yield return AlertDialogCases.Data("static.width.medium-window", new(Basic: true, HostWidth: 640, HostHeight: 480, ContentWidth: 700));
        yield return AlertDialogCases.Data("static.width.large-window", new(Basic: true, HostWidth: 640, HostHeight: 600, ContentWidth: 700));
        yield return AlertDialogCases.Data("static.width.narrow-window", new(Basic: true, HostWidth: 200));
        yield return AlertDialogCases.Data("static.content.rtl", new(Basic: true, Rtl: true));
    }
    public static IEnumerable<CaseData<AlertDialogConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "outside", "escape", "transition", "interrupt", "immediate" })
            yield return AlertDialogCases.Data($"dynamic.{action}", new(Basic: true, Action: action));
        yield return AlertDialogCases.Data("dynamic.transition.rtl", new(Basic: true, Action: "transition", Rtl: true));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => AlertDialogScene.Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => AlertDialogScene.Capture(writer, true);
}

// Local fixture shared only by these two closely related component entry points.
internal sealed class AlertDialogScene
{
    readonly AvaloniaHost host;
    readonly AlertDialogConfig config;
    readonly Dictionary<string, Marker> markers = new();
    readonly IMouseDevice mouse;
    int requests, confirms, dismisses;
    public BasicAlertDialog Dialog { get; }
    public Control Root { get; }

    public AlertDialogScene(AvaloniaHost host, AlertDialogConfig config)
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
        Dialog = config.Basic ? new BasicAlertDialog() : new AlertDialog();
        Dialog.AnimateTransition = config.Action is "transition" or "interrupt" or "immediate";
        Dialog.IsOpen = !Dialog.AnimateTransition;
        Dialog.UsePlatformDefaultWidth = config.PlatformWidth;
        Dialog.DismissOnEscape = Dialog.DismissOnPointerOutside = config.AllowDismiss;
        Dialog.DismissRequested += (_, _) => requests++;
        if (config.Width is { } width) Dialog.Width = width;
        if (config.Height is { } height) Dialog.Height = height;
        if (config.Basic) Dialog.Content = Create("content", config.ContentWidth, config.ContentHeight, Brushes.Blue);
        else
        {
            var alert = (AlertDialog)Dialog;
            alert.ConfirmButton = Create("confirm", config.ButtonWidth, 40);
            alert.DismissButton = config.Dismiss ? Create("dismiss", config.ButtonWidth, 40) : null;
            alert.Icon = config.Icon ? Create("icon", 24, 24) : null;
            alert.Title = config.Title ? Create("title", 100, 24) : null;
            alert.Content = config.Text ? Create("text", config.ContentWidth, config.ContentHeight) : null;
            if (config.Colors == "custom")
            {
                alert.Background = Brush.Parse("#E0F0E8"); alert.IconForeground = Brushes.Blue;
                alert.TitleForeground = Brushes.Red; alert.TextForeground = Brushes.Green;
                alert.CornerRadius = new CornerRadius(12);
            }
            else if (config.Colors == "surface")
            {
                alert.Background = Brush.Parse("#FFFBFE"); alert.TonalElevation = 6;
            }
            else if (config.Colors == "transparent") alert.Background = Brushes.Transparent;
        }
        Root = new Grid { Children = { Dialog } };
    }

    Marker Create(string id, double width, double height, IBrush? fixedBrush = null)
    {
        var marker = new Marker { PreferredWidth = width, PreferredHeight = height, FixedBrush = fixedBrush, Focusable = true, FocusAdorner = null };
        markers[id] = marker;
        bool pressed = false;
        marker.PointerPressed += (_, e) => { if (e.GetCurrentPoint(marker).Properties.IsLeftButtonPressed) pressed = true; };
        marker.PointerReleased += (_, _) => { if (pressed) { if (id == "confirm") confirms++; if (id == "dismiss") dismisses++; } pressed = false; };
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
                Measurement.Numeric("dismisses", dismisses, "count", definition: "dialog.dismiss-button-callback-count")])
        };
        if (!Dialog.IsOpen) return points.ToArray();
        var container = host.Window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Surface");
        points.Add(new("container", "dialog", null, AvaloniaMeasurements.Geometry(container, host.Window).Where(m => m.Name == "size").ToArray()));
        foreach (var (id, marker) in markers.Where(p => p.Value.IsAttachedToVisualTree()))
        {
            var box = new Rect(marker.Bounds.Size).TransformToAABB(marker.TransformToVisual(container)!.Value);
            var color = ((ISolidColorBrush)(marker.FixedBrush ?? TextElement.GetForeground(marker))!).Color;
            points.Add(new(id, id, "container", [
                Measurement.Vector("box", "rect", [box.X, box.Y, box.Width, box.Height], "dip", "local", "geometry",
                    "dialog.slot-box.relative-container", "Control local rectangle transformed to dialog content (including native RTL)"),
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
        var config = writer.Metadata.ReadConfig<AlertDialogConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, config.HostHeight);
        var scene = new AlertDialogScene(host, config);
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
                var alert = (AlertDialog)scene.Dialog;
                Change(1000, "remove-icon-title", () => { alert.Icon = null; alert.Title = null; });
                Change(2000, "restore", () => { alert.Icon = scene.markers["icon"]; alert.Title = scene.markers["title"]; }); break;
            case "resize":
                Change(1000, "wide", () => scene.markers["confirm"].PreferredWidth = 220);
                Change(2000, "restore", () => scene.markers["confirm"].PreferredWidth = config.ButtonWidth); break;
            case "escape":
                Change(1000, "escape-down", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"));
                Change(2000, "escape-up", () => host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape")); break;
            case "outside":
            case "confirm":
            case "dismiss":
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
        public double PreferredHeight { get; set; }
        public IBrush? FixedBrush { get; set; }
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.DrawRectangle(FixedBrush ?? TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
