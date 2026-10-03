using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record WideNavigationRailConfig(bool Expanded = false, string Arrangement = "top", bool Header = false,
    bool Selected = true, bool Enabled = true, bool Label = true, bool Standalone = false, bool Dark = false,
    bool Rtl = false, bool Insets = false, double? Width = null, double? Height = null,
    double IconWidth = 24, double IconHeight = 24, double LabelWidth = 32, double LabelHeight = 16,
    double HeaderWidth = 48, double HeaderHeight = 40, bool Empty = false, bool CustomPadding = false,
    bool CustomColors = false, string IconPosition = "auto", string Action = "none", bool StandardMotion = false,
    string IndicatorPadding = "default", double SecondLabelWidth = 48, bool SingleItem = false);

[ParitySuite("wide-navigation-rail")]
public sealed class WideNavigationRailCases
{
    public static IEnumerable<CaseData<WideNavigationRailConfig>> StaticConfigurations()
    {
        // Both rail forms x every standard arrangement, with/without the independent header.
        foreach (var expanded in new[] { false, true })
        foreach (var arrangement in new[] { "top", "center", "bottom", "space-between", "space-around", "space-evenly" })
        foreach (var header in new[] { false, true })
            yield return Data($"static.arrangement.{Form(expanded)}.{arrangement}.{(header ? "header" : "no-header")}", new(expanded, arrangement, header));
        // Standalone items exercise target Top/Start, label presence and all color branches.
        foreach (var expanded in new[] { false, true })
        foreach (var label in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
            yield return Data($"static.item.{Form(expanded)}.{(label ? "label" : "icon-only")}.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}",
                new(Expanded: expanded, Label: label, Selected: selected, Enabled: enabled, Standalone: true));
        foreach (var expanded in new[] { false, true })
        {
            yield return Data($"static.dark.{Form(expanded)}", new(Expanded: expanded, Dark: true, Header: true));
            yield return Data($"static.rtl.{Form(expanded)}", new(Expanded: expanded, Rtl: true, Header: true));
            foreach (var rtl in new[] { false, true })
                yield return Data($"static.insets.{Form(expanded)}.{(rtl ? "rtl" : "ltr")}", new(Expanded: expanded, Header: true, Rtl: rtl, Insets: true));
            yield return Data($"static.padding.{Form(expanded)}", new(Expanded: expanded, CustomPadding: true, Header: true));
            yield return Data($"static.empty.{Form(expanded)}", new(Expanded: expanded, Empty: true));
            yield return Data($"static.header-only.{Form(expanded)}", new(Expanded: expanded, Empty: true, Header: true, HeaderWidth: 400));
            yield return Data($"static.wide-header.{Form(expanded)}", new(Expanded: expanded, Header: true, HeaderWidth: 280));
            yield return Data($"static.wide-label.{Form(expanded)}", new(Expanded: expanded, LabelWidth: 272));
            yield return Data($"static.tall-label.{Form(expanded)}", new(Expanded: expanded, LabelHeight: 48));
            yield return Data($"static.wide-icon.{Form(expanded)}", new(Expanded: expanded, IconWidth: 40));
            yield return Data($"static.custom-colors.{Form(expanded)}", new(Expanded: expanded, CustomColors: true));
            foreach (var arrangement in new[] { "spaced-top", "spaced-center", "spaced-bottom", "overlap" })
                yield return Data($"static.spacing.{Form(expanded)}.{arrangement}", new(Expanded: expanded, Arrangement: arrangement, Header: true));
        }
        yield return Data("static.layout.constrained-width", new(Expanded: true, Width: 140));
        yield return Data("static.layout.constrained-height", new(Expanded: true, Height: 72));
        yield return Data("static.item.constrained", new(Standalone: true, Width: 40, Height: 28));
        yield return Data("static.item.tall-icon", new(Standalone: true, Label: false, IconHeight: 40));
        yield return Data("static.item.top-override", new(Expanded: true, IconPosition: "top"));
        yield return Data("static.item.start-override", new(IconPosition: "start"));
        yield return Data("static.layout.close-item-widths", new(Expanded: true, LabelWidth: 160, SecondLabelWidth: 172));
        yield return Data("static.layout.single-space-between", new(Arrangement: "space-between", Header: true, SingleItem: true));
        yield return Data("static.layout.spaced-zero-center", new(Arrangement: "spaced-zero-center", Header: true));
        foreach (var expanded in new[] { false, true })
        foreach (var padding in new[] { "fixed", "dynamic" })
            yield return Data($"static.indicator-padding.{Form(expanded)}.{padding}", new(Expanded: expanded, Standalone: true, Rtl: true, IndicatorPadding: padding));
    }
    public static IEnumerable<CaseData<WideNavigationRailConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "expand", "collapse", "reverse", "snap", "theme" })
            yield return Data($"dynamic.{action}", new(Expanded: action == "collapse", Header: true, Action: action));
        foreach (var action in new[] { "expand", "collapse", "reverse" })
            yield return Data($"dynamic.wide.{action}", new(Expanded: action == "collapse", Header: true, HeaderWidth: 280, LabelWidth: 240, Action: action));
        foreach (var expanded in new[] { false, true })
        foreach (var action in new[] { "select", "deselect", "press", "disable", "quick", "hover" })
            yield return Data($"dynamic.item.{Form(expanded)}.{action}", new(Expanded: expanded, Selected: action != "select", Standalone: true, Action: action));
        foreach (var action in new[] { "space", "enter", "cancel", "disabled", "press-expand" })
            yield return Data($"dynamic.{action}", new(Action: action, Selected: false, Enabled: action != "disabled"));
        foreach (var action in new[] { "expand", "reverse" })
            yield return Data($"dynamic.{action}.standard-motion", new(Header: true, Action: action, StandardMotion: true));
        yield return Data("dynamic.indicator-padding", new(Action: "expand", IndicatorPadding: "dynamic"));
        foreach (var expanded in new[] { false, true })
            yield return Data($"dynamic.item.{Form(expanded)}.hover-area", new(Expanded: expanded, Selected: false, Standalone: true, Action: "hover-area"));
    }
    static string Form(bool expanded) => expanded ? "expanded" : "collapsed";
    static CaseData<WideNavigationRailConfig> Data(string id, WideNavigationRailConfig config) => new(id, id.Replace('.', ' '), config);
    internal static NavigationRailArrangement Arrange(string value) => value switch
    {
        "center" => NavigationRailArrangement.Center, "bottom" => NavigationRailArrangement.Bottom,
        "space-between" => NavigationRailArrangement.SpaceBetween, "space-around" => NavigationRailArrangement.SpaceAround,
        "space-evenly" => NavigationRailArrangement.SpaceEvenly,
        "spaced-top" => NavigationRailArrangement.SpacedBy(8),
        "spaced-center" => NavigationRailArrangement.SpacedBy(8, VerticalAlignment.Center),
        "spaced-zero-center" => NavigationRailArrangement.SpacedBy(0, VerticalAlignment.Center),
        "spaced-bottom" => NavigationRailArrangement.SpacedBy(8, VerticalAlignment.Bottom),
        "overlap" => NavigationRailArrangement.SpacedBy(-12),
        _ => NavigationRailArrangement.Top
    };

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 520, 360);
        var scene = new Scene(host, writer.Metadata.ReadConfig<WideNavigationRailConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<WideNavigationRailConfig>();
        await using var host = new AvaloniaHost(writer, 520, 360);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string description, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", description); host.Draw(); host.Frame(time); Sample(description);
        }
        var center = scene.Item.TranslatePoint(new Point(scene.Item.Bounds.Width / 2, scene.Item.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(500, 340);
        void Event(long time, string action, Point position) { Advance(time); host.Input(action, position); host.Draw(); host.Frame(time); }
        Sample("initial");
        switch (config.Action)
        {
            case "expand": case "collapse": case "reverse": case "snap":
                Mutate(1000, "expansion-change", () => { _ = config.Action == "snap" ? scene.State.SnapToAsync(WideNavigationRailValue.Expanded) : scene.State.ToggleAsync(); });
                Times(16000, 64000, 96000);
                if (config.Action == "reverse") Mutate(97000, "expansion-reverse", () => { _ = scene.State.ToggleAsync(); });
                Times(160000, 240000, 400000, 800000); break;
            case "select": case "deselect":
                Mutate(1000, "selection-change", () => scene.Item.IsSelected = !scene.Item.IsSelected);
                Times(16000, 64000, 160000, 400000, 800000); break;
            case "theme":
                Mutate(1000, "dark-theme", () => scene.SetTheme(true));
                Times(16000, 64000, 160000, 400000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            case "hover-area":
                var label = scene.Item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(c => c.Name == "PART_Label");
                var gap = label.TranslatePoint(config.Expanded ? new Point(-4, label.Bounds.Height / 2) : new Point(label.Bounds.Width / 2, -2), host.Window)!.Value;
                Event(1000, "move", gap); Sample("gap-hover"); Times(16000, 80000);
                Event(81000, "move", center); Sample("icon-hover"); Times(96000);
                Event(97000, "move", scene.Item.TranslatePoint(new Point(1, 1), host.Window)!.Value); Sample("edge-hover"); Times(160000);
                Event(161000, "move", outside); Sample("exit"); Times(176000, 400000); break;
            case "space": case "enter":
                Advance(1000); scene.Item.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000, 800000); break;
            default:
                Event(1000, "move", center); Event(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled") { Event(5000, "up", center); Sample("early-release"); Times(16000, 80000, 240000, 400000, 800000); break; }
                Times(16000, 64000);
                if (config.Action == "disable") Mutate(65000, "disabled-during-press", () => scene.Item.IsEnabled = false);
                if (config.Action == "press-expand") Mutate(65000, "expand-during-press", () => { _ = scene.State.ExpandAsync(); });
                if (config.Action == "cancel") Event(65000, "move", outside);
                Times(80000, 160000, 240000);
                Event(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 400000, 800000); break;
        }
    }

    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly WideNavigationRailConfig _config;
        private readonly Marker _icon, _label;
        private int _clicks;
        public WideNavigationRailItem Item { get; }
        public WideNavigationRail? Rail { get; }
        public WideNavigationRailState State { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, WideNavigationRailConfig config)
        {
            _host = host; _config = config;
            SetTheme(config.Dark);
            if (config.StandardMotion)
            {
                host.Window.Resources[Sys.Motion.Spring.SpatialDefaultDamping] = 0.9d;
                host.Window.Resources[Sys.Motion.Spring.SpatialDefaultStiffness] = 700d;
            }
            State = new(config.Expanded ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed);
            _icon = new Marker { Icon = true, PreferredWidth = config.IconWidth, PreferredHeight = config.IconHeight };
            _label = new Marker { PreferredWidth = config.LabelWidth, PreferredHeight = config.LabelHeight };
            Item = new WideNavigationRailItem { Icon = _icon, Label = config.Label ? _label : null,
                IsSelected = config.Selected, IsEnabled = config.Enabled };
            if (config.Standalone) Item.RailExpanded = config.Expanded;
            if (config.IndicatorPadding == "fixed") Item.IndicatorPadding = new Thickness(9, 3, 17, 7);
            if (config.IndicatorPadding == "dynamic")
            {
                Item.CollapsedIndicatorPadding = new Thickness(9, 3, 17, 7);
                Item.ExpandedIndicatorPadding = new Thickness(19, 13, 23, 17);
            }
            if (config.IconPosition != "auto") Item.IconPosition = config.IconPosition == "top" ? NavigationItemIconPosition.Top : NavigationItemIconPosition.Start;
            if (config.CustomColors)
            {
                Item.SelectedIconColor = Color.Parse("#126E60");
                Item.SelectedTextColorTopIconPosition = Color.Parse("#895210");
                Item.SelectedTextColorStartIconPosition = Color.Parse("#246893");
                Item.IndicatorColor = Color.Parse("#B2E6D6"); Item.UnselectedIconColor = Color.Parse("#365A80");
                Item.UnselectedTextColor = Color.Parse("#456789"); Item.DisabledIconColor = Color.Parse("#50365A80"); Item.DisabledTextColor = Color.Parse("#70456789");
            }
            Item.Click += (_, _) => _clicks++;
            Control content = Item;
            if (!config.Standalone)
            {
                Rail = new WideNavigationRail { State = State, Arrangement = Arrange(config.Arrangement),
                    Header = config.Header ? new Marker { PreferredWidth = config.HeaderWidth, PreferredHeight = config.HeaderHeight } : null,
                    WindowInsets = config.Insets ? new Thickness(13, 7, 5, 11) : default };
                if (config.CustomPadding) Rail.ContentPadding = new Thickness(7, 13, 11, 5);
                if (!config.Empty)
                {
                    Rail.Items.Add(Item);
                    if (!config.SingleItem)
                    {
                        Rail.Items.Add(new WideNavigationRailItem { Icon = new Marker { Icon = true, PreferredWidth = 24, PreferredHeight = 24 }, Label = new Marker { PreferredWidth = config.SecondLabelWidth, PreferredHeight = 16 } });
                        Rail.Items.Add(new WideNavigationRailItem { Icon = new Marker { Icon = true, PreferredWidth = 24, PreferredHeight = 24 } });
                    }
                }
                content = Rail;
            }
            content.HorizontalAlignment = HorizontalAlignment.Left;
            // Compose's outer Box uses TopStart even when the child has an explicit
            // height. Avalonia Stretch would center that fixed-height child instead.
            content.VerticalAlignment = config.Standalone || config.Height is not null ? VerticalAlignment.Top : VerticalAlignment.Stretch;
            if (config.Width is { } width) content.Width = width;
            if (config.Height is { } height) content.Height = height;
            Root = new Border { Padding = new Thickness(16), FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Child = new Grid { Children = { content } } };
        }
        public void SetTheme(bool dark)
        {
            _host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            // The same stock Compose ColorScheme is input to both ends, as for regular Rail.
            _host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            _host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            _host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
            _host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
            _host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
            _host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, _host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint>();
            if (Rail is not null) points.Add(new("rail", "container", null, [.. Geometry(Rail),
                Measurement.Text("targetValue", State.TargetValue.ToString(), definition: "wide-navigation-rail.state.target-value"),
                Measurement.Text("currentValue", State.CurrentValue.ToString(), definition: "wide-navigation-rail.state.current-value"),
                Measurement.Flag("isAnimating", State.IsAnimating, definition: "wide-navigation-rail.state.is-animating")]));
            if (_config.Empty) return points.ToArray();
            points.Add(new("item", "navigation-item", Rail is null ? null : "rail", [.. Geometry(Item),
                Measurement.Flag("selected", Item.IsSelected, definition: "navigation-item.selected"),
                Measurement.Flag("enabled", Item.IsEffectivelyEnabled, definition: "navigation-item.enabled"),
                Measurement.Numeric("clicks", _clicks, "count", definition: "navigation-item.click-callback-count")]));
            void Content(string id, Marker marker)
            {
                var color = ((ISolidColorBrush)TextElement.GetForeground(marker)!).Color;
                points.Add(new(id, id, "item", [.. Geometry(marker),
                    Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties", "navigation-item.content-inherited-srgba8", "Marker consumed TextElement.Foreground")]));
            }
            Content("icon", _icon);
            if (_config.Label) Content("label", _label);
            return points.ToArray();
        }
    }
    private sealed class Marker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public bool Icon { get; init; }
        public double PreferredWidth { get; init; }
        public double PreferredHeight { get; init; }
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(PreferredWidth, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this);
            if (Icon)
            {
                context.DrawRectangle(brush, null, new Rect(0, 0, Bounds.Width / 3, Bounds.Height));
                context.DrawRectangle(brush, null, new Rect(Bounds.Width / 3, 0, Bounds.Width * 2 / 3, Bounds.Height / 3));
            }
            else context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}

