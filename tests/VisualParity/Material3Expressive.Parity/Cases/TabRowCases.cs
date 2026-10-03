using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TabRowConfig(string Variant = "primary", bool Scrollable = false, string Content = "label", bool Native = false,
    bool Rtl = false, bool Dark = false, bool Enabled = true, bool CustomColors = false, bool CustomSlots = false,
    bool NoSlots = false, int Count = 3, int Selected = 0, double Width = 352, double? Height = null,
    double EdgePadding = 52, double MinTabWidth = 90, double LabelHeight = 16, double IconSize = 24, string Action = "none");

[ParitySuite("tab-row")]
public sealed class TabRowCases
{
    public static IEnumerable<CaseData<TabRowConfig>> StaticConfigurations()
    {
        foreach (var variant in new[] { "primary", "secondary" })
        foreach (var scrollable in new[] { false, true })
        {
            var mode = $"{variant}.{(scrollable ? "scrollable" : "fixed")}";
            // All four Row variants and all three Tab overloads, through both product entrances.
            foreach (var native in new[] { false, true })
            foreach (var content in new[] { "label", "icon", "stacked", "leading", "custom" })
                yield return Data($"static.appearance.{mode}.{content}.{(native ? "native" : "controlled")}", new(variant, scrollable, content, native));
            yield return Data($"static.rtl.{mode}", new(variant, scrollable, "leading", Rtl: true, Selected: 1));
            foreach (var content in new[] { "label", "icon", "stacked", "custom" })
                yield return Data($"static.rtl.{mode}.{content}", new(variant, scrollable, content, Rtl: true, Selected: 1));
            yield return Data($"static.dark.{mode}", new(variant, scrollable, "icon", Dark: true));
            yield return Data($"static.disabled.{mode}", new(variant, scrollable, Enabled: false, CustomColors: true));
            yield return Data($"static.slots.{mode}", new(variant, scrollable, CustomSlots: true, Selected: 1));
            yield return Data($"static.no-slots.{mode}", new(variant, scrollable, NoSlots: true));
            yield return Data($"static.layout.{mode}.pixel-remainder", new(variant, scrollable, Width: 241.25));
        }
        yield return Data("static.layout.fixed.empty", new(Count: 0, NoSlots: true));
        yield return Data("static.layout.fixed.single", new(Count: 1));
        yield return Data("static.layout.fixed.tall-label", new(Content: "stacked", LabelHeight: 48));
        yield return Data("static.layout.fixed.short-height", new(Height: 24));
        yield return Data("static.layout.fixed.narrow", new(Width: 48));
        yield return Data("static.layout.fixed.narrow-leading", new(Content: "leading", Width: 48));
        yield return Data("static.layout.fixed.narrow-stacked", new(Content: "stacked", Width: 48));
        yield return Data("static.layout.scrollable.custom-padding-minimum", new(Scrollable: true, EdgePadding: 7.375, MinTabWidth: 40.375));
        yield return Data("static.layout.scrollable.tall-icon", new(Scrollable: true, Content: "icon", IconSize: 56));
        yield return Data("static.layout.scrollable.native-rtl", new(Scrollable: true, Native: true, Rtl: true));
    }
    public static IEnumerable<CaseData<TabRowConfig>> DynamicConfigurations()
    {
        foreach (var variant in new[] { "primary", "secondary" })
        foreach (var scrollable in new[] { false, true })
        foreach (var action in new[] { "select", "reverse", "resize" })
            yield return Data($"dynamic.{action}.{variant}.{(scrollable ? "scrollable" : "fixed")}", new(variant, scrollable, Count: 5, Action: action));
        foreach (var action in new[] { "press", "quick", "cancel", "disable", "disabled", "hover", "space", "enter", "theme", "colors", "content" })
            yield return Data($"dynamic.{action}.controlled", new(CustomColors: true, Enabled: action != "disabled", Action: action));
        foreach (var action in new[] { "press", "directional", "select", "reverse" })
            yield return Data($"dynamic.{action}.native", new(Native: true, CustomColors: true, Action: action));
        yield return Data("dynamic.initial-selection.scrollable", new(Scrollable: true, Count: 5, Selected: 4, Action: "initial-scroll"));
        yield return Data("dynamic.reverse.scrollable-rtl", new(Scrollable: true, Rtl: true, Count: 5, Action: "reverse"));
        foreach (var scrollable in new[] { false, true })
        foreach (var native in new[] { false, true })
            yield return Data($"dynamic.content.{(scrollable ? "scrollable" : "fixed")}.{(native ? "native" : "controlled")}", new(Scrollable: scrollable, Native: native, Action: "content"));
    }
    static CaseData<TabRowConfig> Data(string id, TabRowConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 384, 180);
        var scene = new TabScene(host, writer.Metadata.ReadConfig<TabRowConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 384, 180);
        var config = writer.Metadata.ReadConfig<TabRowConfig>(); var scene = new TabScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        var target = scene.Items[Math.Min(1, scene.Items.Count - 1)];
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(380, 175);
        void Input(long time, string action, Point position) { Advance(time); host.Input(action, position); host.Draw(); host.Frame(time); }
        Sample("initial");
        switch (config.Action)
        {
            case "select": case "reverse":
                Mutate(1000, "select-last", () => scene.Select(config.Count - 1)); Times(16000, 64000);
                if (config.Action == "reverse") Mutate(65000, "select-first", () => scene.Select(0));
                Times(96000, 160000, 400000, 1000000); break;
            case "resize":
                Mutate(1000, "narrower", () => scene.Row.Width = 241.25); Times(16000, 64000, 160000);
                Mutate(161000, "restore", () => scene.Row.Width = config.Width); Times(176000, 400000, 1000000); break;
            case "initial-scroll": Times(16000, 64000, 160000, 400000, 1000000); break;
            case "theme":
                Mutate(1000, "dark", () => scene.SetTheme(true)); Times(16000, 64000, 400000);
                Mutate(401000, "light", () => scene.SetTheme(false)); Times(416000, 480000, 1000000); break;
            case "colors":
                Mutate(1000, "change-colors", () => scene.SetColors(Color.Parse("#126E60"), Color.Parse("#895210")));
                Times(16000, 64000, 400000, 1000000); break;
            case "content":
                Mutate(1000, "grow-label", () => scene.Labels[0].PreferredWidth = 90); Times(16000, 64000, 400000, 1000000); break;
            case "space": case "enter": case "directional":
                Advance(1000); target.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : config.Action == "directional" ? Key.Right : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000, 1000000); break;
            case "hover":
                Input(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Input(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            default:
                Input(1000, "move", center); Input(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled") { Input(5000, "up", center); Sample("early-release"); Times(16000, 80000, 240000, 400000, 1000000); break; }
                Times(16000, 64000);
                if (config.Action == "disable") Mutate(65000, "disabled", () => target.IsEnabled = false);
                if (config.Action == "cancel") Input(65000, "move", outside);
                Times(80000, 160000, 240000);
                Input(241000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(256000, 400000, 1000000); break;
        }
        Advance(1100000); host.Draw(); Sample("passive-late", false);
    }

    sealed class TabScene
    {
        readonly AvaloniaHost host; readonly TabRowConfig config; int clicks;
        public ItemsControl Row { get; }
        public Control Root { get; }
        public List<Control> Items { get; } = [];
        public List<TabMarker> Labels { get; } = [];
        readonly List<TabMarker> icons = [];
        public TabScene(AvaloniaHost host, TabRowConfig config)
        {
            this.host = host; this.config = config; SetTheme(config.Dark);
            host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Row = config.Native ? new TabControl() : new TabRow();
            Row.Width = config.Width; if (config.Height is { } height) Row.Height = height;
            Row.HorizontalAlignment = HorizontalAlignment.Left; Row.VerticalAlignment = VerticalAlignment.Top;
            TabRowAppearance.SetVariant(Row, config.Variant == "primary" ? TabRowVariant.Primary : TabRowVariant.Secondary);
            TabRowAppearance.SetIsScrollable(Row, config.Scrollable); TabRowAppearance.SetEdgePadding(Row, config.EdgePadding); TabRowAppearance.SetMinTabWidth(Row, config.MinTabWidth);
            if (config.NoSlots) { TabRowAppearance.SetIndicator(Row, null); TabRowAppearance.SetDivider(Row, null); }
            if (config.CustomSlots)
            {
                TabRowAppearance.SetIndicator(Row, new FuncControlTemplate((_, _) => new Border { Height = 5, Background = Brushes.Cyan }));
                TabRowAppearance.SetDivider(Row, new FuncControlTemplate((_, _) => new Border { Height = 2, Background = Brushes.Magenta }));
            }
            for (var index = 0; index < config.Count; index++)
            {
                var label = new TabMarker { PreferredWidth = 32 + index * 16, PreferredHeight = config.LabelHeight };
                TextBlock.SetBaselineOffset(label, 12); Labels.Add(label);
                var icon = new TabMarker { PreferredWidth = config.IconSize, PreferredHeight = config.IconSize, Icon = true }; icons.Add(icon);
                var layout = config.Content switch { "leading" => TabContentLayout.Leading, "custom" => TabContentLayout.Custom, _ => TabContentLayout.Stacked };
                object? content = config.Content == "icon" ? null : label;
                object? image = config.Content is "icon" or "stacked" or "leading" ? icon : null;
                Control item;
                if (config.Native) item = new TabItem { Header = content, Icon = image };
                else
                {
                    var tab = new Tab { Content = content, Icon = image }; var captured = index;
                    tab.Click += (_, _) => { clicks++; Select(captured); }; item = tab;
                }
                item.IsEnabled = config.Enabled; TabAppearance.SetContentLayout(item, layout); Items.Add(item); Row.Items.Add(item);
            }
            if (config.CustomColors) SetColors(Color.Parse("#6750A4"), Color.Parse("#49454F"));
            Select(config.Selected);
            Root = new Grid { Margin = new Thickness(16, 16, 16, 0), Children = { Row } };
        }
        public void Select(int index)
        {
            if (Row is TabControl native) native.SelectedIndex = index;
            else { ((TabRow)Row).SelectedIndex = index; for (var i = 0; i < Items.Count; i++) ((Tab)Items[i]).IsSelected = i == index; }
        }
        public void SetColors(Color selected, Color unselected)
        {
            foreach (var item in Items) { TabAppearance.SetSelectedContentColor(item, selected); TabAppearance.SetUnselectedContentColor(item, unselected); }
        }
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var chrome = Row.GetVisualDescendants().OfType<Control>().Single(control => control.GetType().Name == "TabRowChrome");
            var points = new List<ProbePoint> { new("row", "container", null, Geometry(chrome)) };
            var scroller = Row.GetVisualDescendants().OfType<ScrollViewer>().Single();
            points.Add(new("selection", "state", "row", [Measurement.Numeric("index", Row is TabRow row ? row.SelectedIndex : ((TabControl)Row).SelectedIndex, "index", definition: "tab-row.selected-index"),
                Measurement.Numeric("scroll", scroller.Offset.X, "dip", category: "motion", definition: "tab-row.scroll-start-offset")]));
            var positions = TabRowAppearance.GetTabPositions(Row);
            for (var index = 0; index < Items.Count; index++)
            {
                var item = Items[index];
                points.Add(new($"tab-{index}", "tab", "row", [.. Geometry(item),
                    Measurement.Flag("selected", item is Tab tab ? tab.IsSelected : ((TabItem)item).IsSelected, definition: "tab.selected"),
                    Measurement.Flag("enabled", item.IsEffectivelyEnabled, definition: "tab.enabled"),
                    Measurement.Numeric("left", positions[index].Left, definition: "tab-position.start"),
                    Measurement.Numeric("width", positions[index].Width, definition: "tab-position.width"),
                    Measurement.Numeric("contentWidth", positions[index].ContentWidth, definition: "tab-position.content-width")]));
                void Marker(string id, TabMarker marker)
                {
                    var color = ((ISolidColorBrush)TextElement.GetForeground(marker)!).Color;
                    points.Add(new(id, "content", $"tab-{index}", [.. Geometry(marker),
                        Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties", "tab.content-srgba8")]));
                }
                if (config.Content != "icon") Marker($"label-{index}", Labels[index]);
                if (config.Content is "icon" or "stacked" or "leading") Marker($"icon-{index}", icons[index]);
            }
            if (!config.Native) points.Add(new("activation", "state", "row", [Measurement.Numeric("clicks", clicks, "count", definition: "tab.click-callback-count")]));
            if (Items.Count > 0 && !config.NoSlots)
            {
                var indicator = Row.GetVisualDescendants().OfType<TemplatedControl>().Single(control => control.Name == "PART_Indicator");
                points.Add(new("indicator", "indicator", "row", Geometry(indicator)));
            }
            if (!config.NoSlots)
            {
                var divider = Row.GetVisualDescendants().OfType<TemplatedControl>().Single(control => control.Name == "PART_Divider");
                points.Add(new("divider", "divider", "row", Geometry(divider)));
            }
            return points.ToArray();
        }
    }
    sealed class TabMarker : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public bool Icon { get; init; }
        double width;
        public double PreferredWidth { get => width; set { width = value; InvalidateMeasure(); } }
        public double PreferredHeight { get; init; }
        static TabMarker() => AffectsRender<TabMarker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(PreferredWidth, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this);
            if (Icon) { context.DrawRectangle(brush, null, new Rect(0, 0, Bounds.Width / 3, Bounds.Height)); context.DrawRectangle(brush, null, new Rect(Bounds.Width / 3, 0, Bounds.Width * 2 / 3, Bounds.Height / 3)); }
            else context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
