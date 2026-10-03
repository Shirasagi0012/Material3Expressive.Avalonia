using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record NavigationDrawerConfig(string Kind = "modal", bool Open = true, bool StatefulSheet = true,
    bool Selected = true, bool Icon = true, bool Badge = true, bool Rtl = false, bool Dark = false,
    bool Insets = false, bool Empty = false, bool Custom = false, double? Width = null,
    int HostWidth = 520, bool Gestures = true, bool Veto = false, string Action = "none");

[ParitySuite("navigation-drawer")]
public sealed class NavigationDrawerCases
{
    public static IEnumerable<CaseData<NavigationDrawerConfig>> StaticConfigurations()
    {
        foreach (var kind in new[] { "modal", "dismissible", "permanent" })
        {
            // Item slots and selection change row geometry and the three independent color providers.
            foreach (var selected in new[] { false, true })
            foreach (var icon in new[] { false, true })
            foreach (var badge in new[] { false, true })
                yield return Data($"static.{kind}.{(selected ? "selected" : "unselected")}.{(icon ? "icon" : "no-icon")}.{(badge ? "badge" : "no-badge")}", new(kind, Selected: selected, Icon: icon, Badge: badge));
            yield return Data($"static.{kind}.rtl", new(kind, Rtl: true));
            yield return Data($"static.{kind}.dark", new(kind, Dark: true));
            yield return Data($"static.{kind}.insets", new(kind, Insets: true));
            yield return Data($"static.{kind}.rtl-insets", new(kind, Insets: true, Rtl: true));
            yield return Data($"static.{kind}.empty", new(kind, Empty: true));
            yield return Data($"static.{kind}.custom", new(kind, Custom: true));
            foreach (var width in new[] { 100d, 260d, 420d }) yield return Data($"static.{kind}.width-{width}", new(kind, Width: width));
            foreach (var width in new[] { 180, 320 }) yield return Data($"static.{kind}.host-{width}", new(kind, HostWidth: width));
            if (kind == "permanent") continue;
            yield return Data($"static.{kind}.closed", new(kind, Open: false));
            yield return Data($"static.{kind}.stateless-sheet", new(kind, StatefulSheet: false));
        }
    }
    public static IEnumerable<CaseData<NavigationDrawerConfig>> DynamicConfigurations()
    {
        foreach (var kind in new[] { "modal", "dismissible" })
        {
            foreach (var action in new[] { "open", "close", "reverse", "reopen", "snap", "escape", "drag-close", "drag-return", "drag-open", "drag-disabled", "drag-fast", "theme", "resize", "selection", "press", "cancel", "hover", "keyboard", "veto-open", "veto-close" })
                yield return Data($"dynamic.{kind}.{action}", new(kind, Open: action is not ("open" or "reverse" or "reopen" or "snap" or "drag-open" or "veto-open"),
                    Gestures: action != "drag-disabled", Veto: action.StartsWith("veto"), Action: action));
            yield return Data($"dynamic.{kind}.rtl-drag-close", new(kind, Rtl: true, Action: "drag-close"));
            yield return Data($"dynamic.{kind}.stateless-open", new(kind, Open: false, StatefulSheet: false, Action: "open"));
        }
        yield return Data("dynamic.modal.scrim", new(Action: "scrim"));
        yield return Data("dynamic.modal.scrim-disabled", new(Gestures: false, Action: "scrim"));
        yield return Data("dynamic.modal.scrim-veto", new(Veto: true, Action: "scrim"));
        yield return Data("dynamic.permanent.selection", new("permanent", Action: "selection"));
        yield return Data("dynamic.permanent.theme", new("permanent", Action: "theme"));
    }
    private static CaseData<NavigationDrawerConfig> Data(string id, NavigationDrawerConfig c) => new(id, id.Replace('.', ' '), c);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NavigationDrawerConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, 320);
        var scene = new Scene(host, config); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NavigationDrawerConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, 320);
        var scene = new Scene(host, config); host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        if (config.Action is "escape" or "keyboard") scene.Item.Focus(NavigationMethod.Tab);
        host.Draw();
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action action, bool sample = true) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); if (sample) Sample(id); }
        void Input(long time, string action, Point p) { Advance(time); scene.Input(action, p); host.Draw(); }
        Sample("initial");
        switch (config.Action)
        {
            case "open": case "close": case "reverse": case "reopen": case "veto-open": case "veto-close":
                Mutate(1000, "state-change", () => { _ = scene.State.TargetValue == DrawerValue.Closed ? scene.State.OpenAsync() : scene.State.CloseAsync(); });
                Times(16000, 32000, 64000, 96000);
                if (config.Action == "reverse") Mutate(97000, "reverse", () => { _ = scene.State.CloseAsync(); });
                Times(160000, 256000, 400000, 800000, 1200000);
                if (config.Action == "reopen")
                {
                    Mutate(1201000, "close", () => { _ = scene.State.CloseAsync(); }); Times(1232000, 1264000, 1600000);
                    Mutate(1601000, "reopen", () => { _ = scene.State.OpenAsync(); }); Times(1616000, 1664000, 2000000, 2800000);
                }
                break;
            case "snap": Mutate(1000, "snap-open", () => { _ = scene.State.SnapToAsync(DrawerValue.Open); });
                Times(16000); Mutate(17000, "snap-close", () => { _ = scene.State.SnapToAsync(DrawerValue.Closed); }); Times(32000); break;
            // Compose configuration invalidations commit at the next recomposition frame. Observe
            // that committed layout/paint on both ends, rather than stale reference coordinates.
            case "theme": Mutate(1000, "dark-theme", () => scene.SetTheme(true), sample: false); Times(16000, 64000); break;
            case "selection": Mutate(1000, "selection-change", () => scene.Item.IsSelected = !scene.Item.IsSelected, sample: false); Times(16000, 64000); break;
            case "resize": Mutate(1000, "sheet-width", () => scene.Sheet.Width = 260, sample: false); Times(16000, 64000); break;
            case "escape": case "keyboard":
                var key = config.Action == "escape" ? Key.Escape : Key.Enter;
                var physical = key == Key.Escape ? PhysicalKey.Escape : PhysicalKey.Enter;
                Mutate(1000, "key-down", () => host.Window.KeyPress(key, RawInputModifiers.None, physical, null)); Times(16000);
                Mutate(17000, "key-up", () => host.Window.KeyRelease(key, RawInputModifiers.None, physical, null)); Times(32000, 64000, 160000, 400000, 800000); break;
            case "scrim":
                var outside = new Point(config.Rtl ? 20 : 500, 280);
                Input(1000, "move", outside); Input(2000, "down", outside); Sample("scrim-down");
                Input(5000, "up", outside); Sample("scrim-up"); Times(16000, 64000, 160000, 400000, 800000); break;
            case "press": case "cancel": case "hover":
                var point = scene.Item.TranslatePoint(new Point(80, 28), host.Window)!.Value;
                Input(1000, "move", point); Sample("hover");
                if (config.Action != "hover") { Input(2000, "down", point); Sample("down"); Times(16000, 64000, 160000);
                    if (config.Action == "cancel") Input(161000, "move", new Point(500, 310));
                    Input(162000, "up", config.Action == "cancel" ? new Point(500, 310) : point); Sample("up"); }
                else Input(2000, "move", new Point(500, 310));
                Times(192000, 320000, 640000); break;
            default:
                var sign = config.Rtl ? -1 : 1;
                var opening = config.Action == "drag-open";
                var origin = new Point(config.Rtl ? 410 : 110, 250);
                var distance = config.Action == "drag-return" ? 60 : 240;
                Input(1000, "move", origin); Input(2000, "down", origin); Sample("drag-down");
                Input(18000, "move", origin + new Point((opening ? 1 : -1) * 40 * sign, 0)); Sample("drag-start");
                var end = origin + new Point((opening ? 1 : -1) * distance * sign, 0);
                Input(34000, "move", end); Sample("drag-move");
                Input(config.Action == "drag-fast" ? 35000 : 90000, "up", end); Sample("drag-up");
                Times(96000, 160000, 256000, 400000, 800000); break;
        }
    }
    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly NavigationDrawerConfig _config;
        private readonly Marker _page = new() { Fill = true };
        private readonly Marker _icon = new() { PreferredWidth = 24, PreferredHeight = 24 };
        private readonly Marker _label = new() { PreferredWidth = 48, PreferredHeight = 16 };
        private readonly Marker _badge = new() { PreferredWidth = 20, PreferredHeight = 12 };
        private readonly IMouseDevice _mouse;
        private int _clicks;
        private bool _pressed;
        public DrawerState State { get; }
        public NavigationDrawer Drawer { get; }
        public DrawerSheet Sheet { get; }
        public NavigationDrawerItem Item { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, NavigationDrawerConfig c)
        {
            _host = host; _config = c; SetTheme(c.Dark);
            _mouse = (IMouseDevice)host.Window.PlatformImpl!.GetType().GetProperty("MouseDevice")!.GetValue(host.Window.PlatformImpl)!;
            State = new(c.Open ? DrawerValue.Open : DrawerValue.Closed, _ => !c.Veto);
            Drawer = c.Kind switch { "modal" => new ModalNavigationDrawer { State = State, GesturesEnabled = c.Gestures },
                "dismissible" => new DismissibleNavigationDrawer { State = State, GesturesEnabled = c.Gestures }, _ => new PermanentNavigationDrawer() };
            Sheet = c.Kind switch { "modal" => new ModalDrawerSheet { State = c.StatefulSheet ? State : null },
                "dismissible" => new DismissibleDrawerSheet { State = c.StatefulSheet ? State : null }, _ => new PermanentDrawerSheet() };
            Item = new NavigationDrawerItem { Content = _label, Icon = c.Icon ? _icon : null, Badge = c.Badge ? _badge : null, IsSelected = c.Selected };
            Item.Click += (_, _) => _clicks++;
            if (c.Width is { } width) Sheet.Width = width;
            if (c.Insets) Sheet.WindowInsets = new Thickness(13, 7, 5, 11);
            if (c.Custom)
            {
                Sheet.Background = Brush.Parse("#D6E8F6"); Sheet.Foreground = Brush.Parse("#246893"); Sheet.CornerRadius = new CornerRadius(12); Sheet.TonalElevation = 6;
                if (Drawer is ModalNavigationDrawer modal) modal.ScrimBrush = Brush.Parse("#66365A80");
                Item.SelectedContainerColor = Color.Parse("#E0F0E8"); Item.SelectedIconColor = Color.Parse("#126E60");
                Item.SelectedTextColor = Color.Parse("#345678"); Item.SelectedBadgeColor = Color.Parse("#864321");
            }
            if (!c.Empty) Sheet.Content = new StackPanel { Margin = new Thickness(12, 16), Children = { new Marker { PreferredWidth = 48, PreferredHeight = 32, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left }, Item } };
            Drawer.DrawerContent = Sheet; Drawer.Content = _page;
            Root = new Border { Padding = new Thickness(16), Background = Brushes.White, Child = Drawer, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        }
        public void Input(string action, Point p)
        {
            var type = action switch { "down" => RawPointerEventType.LeftButtonDown, "up" => RawPointerEventType.LeftButtonUp, _ => RawPointerEventType.Move };
            var platform = _host.Window.PlatformImpl!;
            var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), _mouse,
                (ulong)(1000 + Math.Max(0, _host.TimeUs) / 1000), platform.GetType().GetProperty("InputRoot")!.GetValue(platform), type, p,
                action == "move" && _pressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!;
            ((Action<RawInputEventArgs>)platform.GetType().GetProperty("Input")!.GetValue(platform)!)(raw);
            if (action == "down") _pressed = true; if (action == "up") _pressed = false;
        }
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(_host, dark);
            _host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            _host.Window.Resources[Sys.Color.SurfaceContainerLow] = Color.Parse(dark ? "#1D1B20" : "#F7F2FA");
            _host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            _host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
            _host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
            _host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control c) => AvaloniaMeasurements.Geometry(c, _host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var facts = new List<Measurement>(Geometry(_page));
            if (_config.Kind != "permanent") facts.AddRange([
                Measurement.Text("currentValue", State.CurrentValue.ToString(), definition: "drawer.state.settled-value"),
                Measurement.Text("targetValue", State.TargetValue.ToString(), definition: "drawer.state.target-value"),
                Measurement.Flag("isAnimationRunning", State.IsAnimationRunning, definition: "drawer.state.is-animation-running"),
                Measurement.Numeric("offset", State.CurrentOffset, "dip", "local", "motion", "drawer.state.offset", "DrawerState.CurrentOffset")]);
            var points = new List<ProbePoint> { new("page", "page", null, facts.ToArray()) };
            var visible = _config.Kind == "permanent" || State.CurrentOffset > -Sheet.Bounds.Width;
            if (visible)
            {
                points.Add(new("sheet", "sheet", null, Geometry(Sheet)));
                if (!_config.Empty)
                {
                    points.Add(new("item", "navigation-item", "sheet", [.. Geometry(Item), Measurement.Flag("selected", Item.IsSelected, definition: "drawer-item.selected"),
                        Measurement.Flag("focused", Item.IsFocused, definition: "drawer-item.focused"),
                        Measurement.Numeric("clicks", _clicks, "count", definition: "drawer-item.clicks")]));
                    void Marker(string id, Control c)
                    {
                        var color = ((ISolidColorBrush)TextElement.GetForeground(c)!).Color;
                        points.Add(new(id, id, "item", [.. Geometry(c), Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A],
                            "srgba8", "none", "properties", "drawer-item.inherited-color", "TextElement.Foreground consumed by Marker.Render")]));
                    }
                    Marker("label", _label); if (_config.Icon) Marker("icon", _icon); if (_config.Badge) Marker("badge", _badge);
                }
            }
            return points.ToArray();
        }
    }
    private sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public double PreferredWidth { get; init; }
        public double PreferredHeight { get; init; }
        public bool Fill { get; init; }
        protected override Size MeasureOverride(Size size) => Fill ? size : new(Math.Min(PreferredWidth, size.Width), Math.Min(PreferredHeight, size.Height));
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        public override void Render(DrawingContext context) => context.DrawRectangle(Fill ? Brush.Parse("#F0F0F0") : TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
