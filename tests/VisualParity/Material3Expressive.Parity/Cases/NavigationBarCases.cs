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

public sealed record NavigationBarConfig(string Labels = "always", bool Selected = false, bool Enabled = true,
    bool Dark = false, bool Rtl = false, bool Insets = false, bool Standalone = false, int Count = 3,
    double Width = 352, double? Height = null, double IconWidth = 24, double IconHeight = 24,
    double LabelWidth = 32, double LabelHeight = 16, string Action = "none", bool CustomColors = false,
    bool StandardMotion = false, string Container = "default", double TonalElevation = 0, bool NestedSurface = false);

[ParitySuite("navigation-bar")]
public sealed class NavigationBarCases
{
    public static IEnumerable<CaseData<NavigationBarConfig>> StaticConfigurations()
    {
        // 3/4/5 destinations cross every label/selection/enabled branch. Width allocation
        // depends on count, while label policy affects vertical geometry and indicator opacity.
        foreach (var count in new[] { 3, 4, 5 })
        foreach (var labels in new[] { "none", "always", "selected-only" })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
            yield return Data($"static.appearance.{count}.{labels}.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}", new(labels, selected, enabled, Count: count));
        // Theme and direction do not select a separate item layout algorithm.
        foreach (var labels in new[] { "none", "always", "selected-only" })
            yield return Data($"static.dark.{labels}", new(labels, Selected: true, Dark: true));
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.insets.{(rtl ? "rtl" : "ltr")}", new(Selected: true, Insets: true, Rtl: rtl));
        yield return Data("static.layout.empty", new(Count: 0));
        yield return Data("static.layout.single", new(Count: 1, Selected: true));
        yield return Data("static.layout.remainder", new(Count: 3, Width: 351.75, Selected: true));
        yield return Data("static.layout.narrow", new(Count: 5, Width: 72, Selected: true));
        yield return Data("static.layout.standalone", new(Standalone: true, Width: 112, Selected: true));
        yield return Data("static.layout.wide-icon", new(Selected: true, IconWidth: 40));
        yield return Data("static.layout.tall-icon", new(Labels: "none", Selected: true, IconHeight: 40));
        yield return Data("static.layout.wide-label", new(Selected: true, LabelWidth: 104));
        yield return Data("static.layout.tall-label", new(Selected: true, LabelHeight: 48));
        yield return Data("static.layout.constrained-height", new(Selected: true, Height: 28));
        yield return Data("static.layout.expanded-height", new(Selected: true, Height: 120));
        yield return Data("static.layout.rtl", new(Selected: true, Rtl: true));
        foreach (var enabled in new[] { false, true })
            yield return Data($"static.custom-colors.{(enabled ? "enabled" : "disabled")}", new(Selected: true, Enabled: enabled, CustomColors: true));
        foreach (var container in new[] { "primary", "ambient", "override" })
            yield return Data($"static.content-color.{container}", new(Container: container));
        yield return Data("static.tonal.surface", new(Selected: true, Container: "surface", TonalElevation: 3));
        yield return Data("static.tonal.nested-surface", new(Selected: true, Container: "surface", TonalElevation: 3, NestedSurface: true));
    }

    public static IEnumerable<CaseData<NavigationBarConfig>> DynamicConfigurations()
    {
        foreach (var labels in new[] { "none", "always", "selected-only" })
        {
            yield return Data($"dynamic.select.{labels}", new(labels, Action: "select"));
            yield return Data($"dynamic.press.{labels}", new(labels, Action: "press"));
        }
        foreach (var action in new[] { "deselect", "reverse", "hover", "cancel", "quick", "disable", "theme", "space", "enter", "disabled" })
            yield return Data($"dynamic.{action}", new(Labels: "selected-only", Selected: action == "deselect", Enabled: action != "disabled", Action: action));
        yield return Data("dynamic.select.standard-motion", new(Labels: "selected-only", Action: "select", StandardMotion: true));
        yield return Data("dynamic.press.rtl", new(Labels: "none", Rtl: true, Action: "press"));
        yield return Data("dynamic.press.wide-icon", new(Labels: "none", IconWidth: 40, Action: "press"));
        yield return Data("dynamic.hover-area", new(Labels: "always", Action: "hover-area"));
    }

    static CaseData<NavigationBarConfig> Data(string id, NavigationBarConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 384, 200);
        var scene = new BarScene(host, writer.Metadata.ReadConfig<NavigationBarConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NavigationBarConfig>();
        await using var host = new AvaloniaHost(writer, 384, 200);
        var scene = new BarScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; }
            host.AdvanceTo(time);
        }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        // An event frame commits Compose's recomposition and Avalonia's pending target
        // update before sampling. Both ends receive it; the 16ms grid continues unchanged.
        void Event(long time, string action, Point position) { Advance(time); host.Input(action, position); host.Draw(); host.Frame(time); }
        void Mutate(long time, string description, Action action) { Advance(time); action(); writer.Record(time, "mutation", description); host.Draw(); host.Frame(time); Sample(description); }
        var center = scene.Item.TranslatePoint(new Point(scene.Item.Bounds.Width / 2, 32), host.Window)!.Value; var outside = new Point(380, 190);
        Sample("initial");
        switch (config.Action)
        {
            case "select": case "deselect": case "reverse":
                Mutate(1000, "selection-change", () => scene.Item.IsSelected = !scene.Item.IsSelected);
                Times(16000, 64000);
                if (config.Action == "reverse") Mutate(65000, "selection-reverse", () => scene.Item.IsSelected = false);
                Times(96000, 160000, 400000, 800000); break;
            case "theme":
                Mutate(1000, "dark-theme", () => scene.SetTheme(true)); Times(16000, 64000, 160000, 400000);
                Mutate(401000, "light-theme", () => scene.SetTheme(false)); Times(416000, 480000, 800000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            case "hover-area":
                // Read the rendered label position; the 4dp gap belongs to the item's selectable Box.
                var label = scene.Item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(c => c.Name == "PART_Label");
                var gap = label.TranslatePoint(new Point(label.Bounds.Width / 2, -2), host.Window)!.Value;
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
                if (config.Action == "cancel") Event(65000, "move", outside);
                Times(80000, 160000, 240000);
                Event(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 400000, 800000); break;
        }
    }

    sealed class BarScene
    {
        readonly AvaloniaHost host;
        readonly NavigationBarConfig config;
        readonly BarMarker icon, label;
        readonly BarMarker? contentMarker;
        readonly NavigationBar? bar;
        readonly List<NavigationBarItem> items = [];
        int clicks;
        public NavigationBarItem Item { get; }
        public Control Root { get; }
        public BarScene(AvaloniaHost host, NavigationBarConfig config)
        {
            this.host = host; this.config = config;
            SetTheme(config.Dark);
            if (config.StandardMotion)
            {
                host.Window.Resources[Sys.Motion.Spring.SpatialFastDamping] = 0.9d;
                host.Window.Resources[Sys.Motion.Spring.SpatialFastStiffness] = 1400d;
            }
            icon = new BarMarker { PreferredWidth = config.IconWidth, PreferredHeight = config.IconHeight, Icon = true };
            label = new BarMarker { PreferredWidth = config.LabelWidth, PreferredHeight = config.LabelHeight };
            Item = new NavigationBarItem { Icon = icon, Label = config.Labels == "none" ? null : label,
                AlwaysShowLabel = config.Labels == "always", IsSelected = config.Selected, IsEnabled = config.Enabled };
            if (config.CustomColors)
            {
                Item.SelectedIconColor = Color.Parse("#126E60"); Item.SelectedTextColor = Color.Parse("#895210");
                Item.IndicatorColor = Color.Parse("#B2E6D6"); Item.UnselectedIconColor = Color.Parse("#365A80");
                Item.UnselectedTextColor = Color.Parse("#456789"); Item.DisabledIconColor = Color.Parse("#50365A80");
                Item.DisabledTextColor = Color.Parse("#70456789");
            }
            Item.Click += (_, _) => { clicks++; Item.IsSelected = true; };
            Control content;
            if (config.Standalone)
            {
                Item.Width = config.Width;
                if (config.Height is { } height) Item.Height = height;
                items.Add(Item); content = Item;
            }
            else
            {
                bar = new NavigationBar { Width = config.Width, TonalElevation = config.TonalElevation,
                    WindowInsets = config.Insets ? new Thickness(13, 7, 5, 11) : default };
                if (config.Height is { } height) bar.Height = height;
                if (config.Container is "primary" or "ambient" or "override")
                {
                    bar.Background = config.Container == "primary" ? new SolidColorBrush(Color.Parse("#6750A4")) : Brushes.Magenta;
                    if (config.Container == "override") bar.Foreground = Brushes.Cyan;
                    bar.Items.Add(contentMarker = new BarMarker { PreferredWidth = 32, PreferredHeight = 16 });
                }
                if (config.Container == "surface") bar.Background = new SolidColorBrush(Color.Parse("#FEF7FF"));
                for (var index = 0; index < config.Count; index++)
                {
                    var item = index == 0 ? Item : new NavigationBarItem { Icon = new BarMarker { PreferredWidth = 24, PreferredHeight = 24, Icon = true } };
                    bar.Items.Add(item); items.Add(item);
                }
                content = bar;
            }
            if (config.NestedSurface)
                content = new MaterialSurface { TonalElevation = 4, Child = content };
            content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
            content.Margin = new Thickness(16, 16, 0, 0);
            Root = new Grid { FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { content } };
            if (config.Container != "default") TextElement.SetForeground(Root, Brushes.Teal);
        }
        public void SetTheme(bool dark)
        {
            host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            // Pair stock Compose colors, not the independently generated MCU palette.
            host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
            host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
            host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
            host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
            host.Window.Resources[Sys.Color.Primary] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.OnPrimary] = Color.Parse(dark ? "#381E72" : "#FFFFFF");
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint>();
            if (bar is not null) points.Add(new("bar", "container", null, Geometry(bar)));
            foreach (var (item, index) in items.Select((item, index) => (item, index)))
                points.Add(new(index == 0 ? "item" : $"item-{index}", "navigation-item", bar is null ? null : "bar", [.. Geometry(item),
                    Measurement.Flag("selected", item.IsSelected, definition: "navigation-item.selected", source: "NavigationBarItem.IsSelected"),
                    Measurement.Flag("enabled", item.IsEffectivelyEnabled, definition: "navigation-item.enabled", source: "InputElement.IsEffectivelyEnabled"),
                    .. index == 0 ? new[] { Measurement.Numeric("clicks", clicks, "count", definition: "navigation-item.click-callback-count") } : []]));
            void Marker(string id, BarMarker marker, string parent = "item")
            {
                var brush = (ISolidColorBrush)TextElement.GetForeground(marker)!;
                points.Add(new(id, id, parent, [.. Geometry(marker), Measurement.Vector("color", "color", [brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A],
                    "srgba8", "none", "properties", "navigation-item.content-inherited-srgba8", "TextElement.Foreground consumed by BarMarker.Render")]));
            }
            if (items.Count > 0)
            {
                Marker("icon", icon);
                if (config.Labels == "always" || config.Action == "none" && config.Labels != "none" && Item.IsSelected) Marker("label", label);
            }
            if (contentMarker is not null) Marker("content", contentMarker, "bar");
            return points.ToArray();
        }
    }

    sealed class BarMarker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public bool Icon { get; init; }
        public double PreferredWidth { get; init; }
        public double PreferredHeight { get; init; }
        // Counterpart of Compose Modifier.size: a preferred size constrained by
        // the real slot's available space. Avalonia Width would mean required size.
        protected override Size MeasureOverride(Size availableSize) => new(
            Math.Min(PreferredWidth, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        static BarMarker() => AffectsRender<BarMarker>(TextElement.ForegroundProperty);
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
