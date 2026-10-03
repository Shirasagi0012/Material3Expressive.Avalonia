using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ModalWideNavigationRailConfig(bool Hide = false, bool Expanded = false, bool Header = false,
    string Arrangement = "top", bool Rtl = false, bool Dark = false, bool Insets = false, bool Empty = false,
    bool CustomColors = false, bool CustomPadding = false, double HeaderTopPadding = 0, double LabelWidth = 32,
    double? Width = null, string Action = "none");

[ParitySuite("modal-wide-navigation-rail")]
public sealed class ModalWideNavigationRailCases
{
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> StaticConfigurations()
    {
        foreach (var hide in new[] { false, true })
        foreach (var expanded in new[] { false, true })
        foreach (var header in new[] { false, true })
        foreach (var arrangement in new[] { "top", "center", "bottom", "space-between", "space-around", "space-evenly" })
            yield return Data($"static.{Mode(hide)}.{Form(expanded)}.{arrangement}.{(header ? "header" : "no-header")}", new(hide, expanded, header, arrangement));
        foreach (var hide in new[] { false, true })
        {
            yield return Data($"static.{Mode(hide)}.rtl", new(hide, true, true, Rtl: true));
            yield return Data($"static.{Mode(hide)}.dark", new(hide, true, true, Dark: true));
            yield return Data($"static.{Mode(hide)}.insets", new(hide, true, true, Insets: true));
            yield return Data($"static.{Mode(hide)}.rtl-insets", new(hide, true, true, Rtl: true, Insets: true));
            yield return Data($"static.{Mode(hide)}.padding", new(hide, true, true, CustomPadding: true));
            yield return Data($"static.{Mode(hide)}.header-padding", new(hide, true, true, HeaderTopPadding: 12));
            yield return Data($"static.{Mode(hide)}.empty-header-padding", new(hide, true, HeaderTopPadding: 12));
            yield return Data($"static.{Mode(hide)}.empty", new(hide, true, Empty: true));
            yield return Data($"static.{Mode(hide)}.wide", new(hide, true, true, LabelWidth: 272));
            yield return Data($"static.{Mode(hide)}.custom-colors", new(hide, true, true, CustomColors: true));
            yield return Data($"static.{Mode(hide)}.width", new(hide, true, Width: 140));
            yield return Data($"static.{Mode(hide)}.spaced", new(hide, true, true, Arrangement: "spaced-center"));
        }
    }
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> DynamicConfigurations()
    {
        foreach (var hide in new[] { false, true })
        foreach (var action in new[] { "expand", "collapse", "reverse", "scrim", "escape", "escape-no-back", "theme" })
            yield return Data($"dynamic.{Mode(hide)}.{action}", new(hide, action is not ("expand" or "reverse"), true, Action: action));
        foreach (var action in new[] { "drag-close", "drag-return", "fling-close", "fling-return" })
            yield return Data($"dynamic.hidden.{action}", new(true, true, true, Action: action));
        yield return Data("dynamic.hidden.rtl-drag-close", new(true, true, true, Rtl: true, Action: "drag-close"));
        yield return Data("dynamic.retained.drag-disabled", new(false, true, true, Action: "drag-close"));
        foreach (var hide in new[] { false, true })
            yield return Data($"dynamic.{Mode(hide)}.reopen", new(hide, false, true, Action: "reopen"));
    }
    private static string Mode(bool hide) => hide ? "hidden" : "retained";
    private static string Form(bool expanded) => expanded ? "expanded" : "collapsed";
    private static CaseData<ModalWideNavigationRailConfig> Data(string id, ModalWideNavigationRailConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 520, 360);
        var scene = new Scene(host, writer.Metadata.ReadConfig<ModalWideNavigationRailConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ModalWideNavigationRailConfig>();
        await using var host = new AvaloniaHost(writer, 520, 360);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        if (config.Action.StartsWith("escape")) { ((Control)scene.Rail.ExpandedHeader!).Focus(NavigationMethod.Tab); host.Draw(); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Input(long time, string action, Point point)
        {
            Advance(time);
            scene.Input(action, point); host.Draw(); host.Frame(time);
        }
        Sample("initial");
        switch (config.Action)
        {
            case "reopen":
                Mutate(1000, "first-open", () => { _ = scene.Rail.State.ExpandAsync(); });
                Times(16000, 32000, 64000, 400000, 1200000);
                Mutate(1201000, "close", () => { _ = scene.Rail.State.CollapseAsync(); });
                Times(1232000, 1264000, 1600000, 2400000);
                Mutate(2401000, "reopen", () => { _ = scene.Rail.State.ExpandAsync(); });
                Times(2416000, 2432000, 2464000, 2800000, 3600000); break;
            case "expand": case "collapse": case "reverse":
                Mutate(1000, "expansion-change", () => { _ = scene.Rail.State.ToggleAsync(); });
                Times(16000, 32000, 64000, 96000);
                if (config.Action == "reverse") Mutate(97000, "expansion-reverse", () => { _ = scene.Rail.State.ToggleAsync(); });
                Times(160000, 240000, 400000, 800000, 1200000); break;
            case "theme":
                Mutate(1000, "dark-theme", () => scene.SetTheme(true)); Times(16000, 64000, 160000, 400000); break;
            case "escape": case "escape-no-back":
                Mutate(1000, "key-down", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"));
                Times(16000, 64000);
                Mutate(65000, "key-up", () => host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"));
                Times(80000, 160000, 400000, 800000); break;
            case "scrim":
                Input(1000, "move", new Point(500, 340)); Input(2000, "down", new Point(500, 340)); Sample("scrim-down");
                Input(5000, "up", new Point(500, 340)); Sample("scrim-up"); Times(16000, 64000, 160000, 400000, 800000, 1200000); break;
            default:
                var direction = config.Rtl ? -1 : 1;
                var origin = new Point(config.Rtl ? 410 : 110, 300);
                var distance = config.Action == "drag-return" ? 60 : 160;
                Input(1000, "move", origin); Input(2000, "down", origin); Sample("drag-down");
                Input(18000, "move", origin + new Point(-40 * direction, 0)); Sample("drag-start");
                Input(34000, "move", origin + new Point(-distance * direction, 0)); Sample("drag-move");
                if (config.Action == "fling-return") Input(50000, "move", origin + new Point(-120 * direction, 0));
                var release = config.Action.StartsWith("fling") ? 51000 : 90000;
                var end = origin + new Point(-(config.Action == "fling-return" ? 120 : distance) * direction, 0);
                Input(release, "up", end); Sample("drag-up"); Times(96000, 160000, 240000, 400000, 800000, 1200000); break;
        }
    }

    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly ModalWideNavigationRailConfig _config;
        private readonly Marker _page, _icon, _label;
        private readonly IMouseDevice _mouse;
        private int _clicks;
        public ModalWideNavigationRail Rail { get; }
        public WideNavigationRailItem Item { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, ModalWideNavigationRailConfig config)
        {
            _host = host; _config = config; SetTheme(config.Dark);
            // Window enables its PopupOverlayLayer in this named template part.
            // A VisualLayerManager placed in Content only enables the ordinary overlay.
            host.Window.Template = new FuncControlTemplate<Window>((owner, scope) =>
            {
                var presenter = new ContentPresenter();
                presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
                presenter.Bind(ContentPresenter.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
                var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
                scope.Register(layers.Name, layers);
                return layers;
            });
            // Headless's existing mouse device, through its public runtime property.
            // The normal raw pipeline receives the same logical timestamps as Compose.
            _mouse = (IMouseDevice)host.Window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(host.Window.PlatformImpl)!;
            _icon = new Marker { Icon = true, PreferredWidth = 24, PreferredHeight = 24 };
            _label = new Marker { PreferredWidth = config.LabelWidth, PreferredHeight = 16 };
            Item = new WideNavigationRailItem { Icon = _icon, Label = _label, IsSelected = true };
            Item.Click += (_, _) => _clicks++;
            Rail = new ModalWideNavigationRail { HideOnCollapse = config.Hide, State = new(config.Expanded ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed),
                Arrangement = WideNavigationRailCases.Arrange(config.Arrangement), ExpandedHeaderTopPadding = config.HeaderTopPadding,
                ShouldDismissOnBackPress = config.Action != "escape-no-back", WindowInsets = config.Insets ? new Thickness(13, 7, 5, 11) : default };
            if (config.Header)
            {
                Rail.Header = new Marker { PreferredWidth = 48, PreferredHeight = 40 };
                Rail.ExpandedHeader = new Marker { PreferredWidth = 48, PreferredHeight = 40, Focusable = config.Action.StartsWith("escape"), FocusAdorner = null };
            }
            if (config.CustomPadding) Rail.ContentPadding = new Thickness(7, 13, 11, 5);
            if (config.Width is { } width) Rail.Width = width;
            if (config.CustomColors)
            {
                Rail.Background = Brush.Parse("#E0F0E8"); Rail.Foreground = Brush.Parse("#126E60");
                Rail.ModalBackground = Brush.Parse("#D6E8F6"); Rail.ModalForeground = Brush.Parse("#246893");
                Rail.ScrimBrush = Brush.Parse("#66365A80"); Rail.ExpandedCornerRadius = new CornerRadius(28); Rail.CollapsedCornerRadius = new CornerRadius(8);
            }
            if (!config.Empty)
            {
                Rail.Items.Add(Item);
                Rail.Items.Add(new WideNavigationRailItem { Icon = new Marker { Icon = true, PreferredWidth = 24, PreferredHeight = 24 }, Label = new Marker { PreferredWidth = 48, PreferredHeight = 16 } });
                Rail.Items.Add(new WideNavigationRailItem { Icon = new Marker { Icon = true, PreferredWidth = 24, PreferredHeight = 24 } });
            }
            _page = new Marker { Page = true }; Grid.SetColumn(_page, 1);
            Root = new VisualLayerManager { EnableOverlayLayer = true, Child = new Border { Padding = new Thickness(16),
                FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Child = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Children = { Rail, _page } } } };
        }
        public void Input(string action, Point position)
        {
            var type = action switch { "down" => RawPointerEventType.LeftButtonDown, "up" => RawPointerEventType.LeftButtonUp, _ => RawPointerEventType.Move };
            var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), _mouse,
                (ulong)(1000 + Math.Max(0, _host.TimeUs) / 1000), _host.Window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(_host.Window.PlatformImpl), type, position,
                action == "move" && _pressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!;
            var input = (Action<RawInputEventArgs>)_host.Window.PlatformImpl!.GetType().GetProperty("Input")!.GetValue(_host.Window.PlatformImpl)!;
            input(raw);
            if (action == "down") _pressed = true; if (action == "up") _pressed = false;
        }
        private bool _pressed;
        public void SetTheme(bool dark)
        {
            _host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            _host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
            _host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            _host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
            _host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
            _host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
            _host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
            _host.Window.Resources[Sys.Color.Scrim] = Colors.Black;
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, _host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("page", "page", null, [.. Geometry(_page),
                Measurement.Text("targetValue", Rail.State.TargetValue.ToString(), definition: "wide-navigation-rail.state.target-value"),
                Measurement.Text("currentValue", Rail.State.CurrentValue.ToString(), definition: "wide-navigation-rail.state.current-value"),
                Measurement.Flag("isAnimating", Rail.State.IsAnimating, definition: "wide-navigation-rail.state.is-animating")]) };
            if (_config.Empty || !Item.IsEffectivelyVisible || !Item.IsAttachedToVisualTree()) return points.ToArray();
            points.Add(new("item", "navigation-item", null, [.. Geometry(Item), Measurement.Numeric("clicks", _clicks, "count", definition: "navigation-item.click-callback-count")]));
            void Marker(string id, Control marker)
            {
                var color = ((ISolidColorBrush)TextElement.GetForeground(marker)!).Color;
                points.Add(new(id, id, "item", [.. Geometry(marker), Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties", "navigation-item.content-inherited-srgba8", "Marker consumed TextElement.Foreground")]));
            }
            Marker("icon", _icon); Marker("label", _label); return points.ToArray();
        }
    }
    private sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public bool Icon { get; init; }
        public bool Page { get; init; }
        public double PreferredWidth { get; init; }
        public double PreferredHeight { get; init; }
        protected override Size MeasureOverride(Size availableSize) => Page ? default : new(Math.Min(PreferredWidth, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        public override void Render(DrawingContext context)
        {
            var brush = Page ? Brush.Parse("#F0F0F0") : TextElement.GetForeground(this);
            if (Icon)
            {
                context.DrawRectangle(brush, null, new Rect(0, 0, Bounds.Width / 3, Bounds.Height));
                context.DrawRectangle(brush, null, new Rect(Bounds.Width / 3, 0, Bounds.Width * 2 / 3, Bounds.Height / 3));
            }
            else context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
