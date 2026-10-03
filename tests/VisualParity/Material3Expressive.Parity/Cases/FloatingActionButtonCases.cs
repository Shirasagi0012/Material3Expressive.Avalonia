using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record FloatingActionButtonConfig(string Kind = "icon", string Size = "default", bool Expanded = true,
    bool Checked = false, bool Dark = false, bool Rtl = false, string Alignment = "end", int Count = 3,
    double IconWidth = 24, double IconHeight = 24, double LabelWidth = 48, double LabelHeight = 16,
    double? Width = null, double? Height = null, string Color = "default", string Elevation = "default",
    bool Visible = true, string Action = "none", bool AnimatedIcon = false);

[ParitySuite("floating-action-button")]
public sealed class FloatingActionButtonCases
{
    static CaseData<FloatingActionButtonConfig> Data(string id, FloatingActionButtonConfig config) => new(id, id.Replace('.', ' '), config);
    public static IEnumerable<CaseData<FloatingActionButtonConfig>> StaticConfigurations()
    {
        foreach (var size in new[] { "default", "small", "medium", "large" })
        {
            yield return Data($"static.icon.{size}", new(Size: size));
            yield return Data($"static.content.{size}", new("content", size));
            foreach (var expanded in new[] { true, false })
                yield return Data($"static.extended.{size}.{(expanded ? "expanded" : "collapsed")}", new("extended", size, expanded));
        }
        // Toggle has default/medium/large defaults; upstream has no small Toggle size family.
        foreach (var size in new[] { "default", "medium", "large" })
        foreach (var check in new[] { false, true })
        {
            yield return Data($"static.toggle.{size}.{(check ? "checked" : "unchecked")}", new("toggle", size, Checked: check));
            yield return Data($"static.toggle-icon.{size}.{(check ? "checked" : "unchecked")}", new("toggle", size, Checked: check, AnimatedIcon: true));
        }
        foreach (var alignment in new[] { "start", "center", "end" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.menu.{alignment}.{(rtl ? "rtl" : "ltr")}", new("menu", Alignment: alignment, Rtl: rtl));
        yield return Data("static.menu.collapsed", new("menu", Expanded: false));
        yield return Data("static.menu.empty", new("menu", Count: 0));
        yield return Data("static.menu.constrained", new("menu", Count: 5, Height: 180));
        foreach (var kind in new[] { "icon", "extended", "content", "toggle", "menu" })
        {
            yield return Data($"static.dark.{kind}", new(kind, Dark: true));
            if (kind != "menu") yield return Data($"static.rtl.{kind}", new(kind, Rtl: true));
        }
        foreach (var size in new[] { "default", "small", "medium", "large" })
        {
            yield return Data($"static.constrained.{size}", new(Size: size, Width: 32, Height: 32));
            yield return Data($"static.extended.wide-label.{size}", new("extended", size, LabelWidth: 120));
        }
        foreach (var elevation in new[] { "lowered", "flat", "custom" })
            yield return Data($"static.elevation.{elevation}", new(Elevation: elevation));
        foreach (var color in new[] { "primary", "surface", "ambient", "transparent" })
            yield return Data($"static.color.{color}", new(Color: color));
        yield return Data("static.hidden", new(Visible: false));
        yield return Data("static.large-content", new(IconWidth: 112, IconHeight: 104));
        yield return Data("static.empty-content", new(IconWidth: 0, IconHeight: 0));
    }
    public static IEnumerable<CaseData<FloatingActionButtonConfig>> DynamicConfigurations()
    {
        foreach (var size in new[] { "default", "small", "medium", "large" })
        foreach (var action in new[] { "expand", "collapse", "reverse" })
            yield return Data($"dynamic.extended.{size}.{action}", new("extended", size, Expanded: action != "expand", Action: action));
        foreach (var size in new[] { "default", "medium", "large" })
        foreach (var action in new[] { "check", "uncheck", "reverse" })
        {
            yield return Data($"dynamic.toggle.{size}.{action}", new("toggle", size, Checked: action == "uncheck", Action: action));
            yield return Data($"dynamic.toggle-icon.{size}.{action}", new("toggle", size, Checked: action == "uncheck", Action: action, AnimatedIcon: true));
        }
        foreach (var action in new[] { "expand", "collapse", "reverse", "count" })
            yield return Data($"dynamic.menu.{action}", new("menu", Expanded: action == "collapse", Action: action));
        foreach (var animatedIcon in new[] { false, true })
            yield return Data($"dynamic.{(animatedIcon ? "toggle-icon" : "toggle")}.large.check.rtl",
                new("toggle", "large", Rtl: true, Action: "check", AnimatedIcon: animatedIcon));
        foreach (var action in new[] { "press", "hover", "cancel", "quick", "hide", "show", "visibility-reverse", "theme", "elevation" })
            yield return Data($"dynamic.{action}", new(Visible: action != "show", Action: action));
        yield return Data("dynamic.toggle.click", new("toggle", Action: "press"));
        yield return Data("dynamic.menu.item-click", new("menu", Action: "press"));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 360, 400);
        var scene = new FabScene(host, writer.Metadata.ReadConfig<FloatingActionButtonConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<FloatingActionButtonConfig>();
        await using var host = new AvaloniaHost(writer, 360, 400);
        var scene = new FabScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        var target = scene.ClickTarget;
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
        void Input(long time, string operation, Point point) { Advance(time); host.Input(operation, point); host.Draw(); host.Frame(time); Sample($"{operation}-{time}"); }
        Sample("initial");
        switch (config.Action)
        {
            case "expand": case "collapse": case "reverse": case "check": case "uncheck":
                Change(1000, "state-change", () => scene.Toggle()); At(16000); At(64000);
                if (config.Action == "reverse") Change(65000, "reverse", () => scene.Toggle());
                At(96000); At(160000); At(320000); At(800000); break;
            case "hide": case "show": case "visibility-reverse":
                Change(1000, "visibility-change", () => scene.Fab!.AnimatedVisible = !scene.Fab.AnimatedVisible); At(16000); At(64000);
                if (config.Action == "visibility-reverse") Change(65000, "visibility-reverse", () => scene.Fab!.AnimatedVisible = true);
                At(160000); At(320000); At(800000); break;
            case "count":
                Change(1000, "expand", () => scene.Toggle()); At(64000);
                Change(65000, "append", () => scene.Menu!.Items.Add(scene.NewItem())); At(80000); At(96000); At(128000); At(160000); At(400000); break;
            case "theme":
                Change(1000, "dark", () => scene.SetTheme(true)); At(16000); At(160000);
                Change(161000, "light", () => scene.SetTheme(false)); At(400000); break;
            case "elevation":
                Input(1000, "move", center); At(80000);
                Change(81000, "elevation-replaced", () => scene.Fab!.Elevation = new(2, 3, 4, 5)); At(96000); At(240000); break;
            default:
                Input(1000, "move", center);
                if (config.Action != "hover") Input(2000, "down", center);
                At(16000); At(config.Action == "quick" ? 32000 : 80000);
                if (config.Action == "cancel") Input(81000, "move", new Point(350, 390));
                if (config.Action == "hover") Input(81000, "move", new Point(350, 390));
                else Input(config.Action == "quick" ? 33000 : 82000, "up", config.Action == "cancel" ? new Point(350, 390) : center);
                At(160000); At(320000); At(800000); break;
        }
        host.AdvanceTo(900000); Sample("passive-idle", false);
    }
    sealed class FabScene
    {
        readonly AvaloniaHost host;
        readonly FloatingActionButtonConfig config;
        int clicks;
        public FloatingActionButton? Fab { get; }
        public ToggleFloatingActionButton? ToggleFab { get; }
        public FloatingActionButtonMenu? Menu { get; }
        public Control Control { get; }
        public Control ClickTarget => Menu is not null && config.Action == "press" ? Menu.Items.OfType<FloatingActionButtonMenuItem>().First() : Control;
        public Control Root { get; }
        public FabScene(AvaloniaHost host, FloatingActionButtonConfig config)
        {
            this.host = host; this.config = config; SetTheme(config.Dark);
            host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            var size = Enum.Parse<FloatingActionButtonSize>(config.Size, true);
            if (config.Kind == "toggle")
            {
                ToggleFab = new() { Size = size, IsChecked = config.Checked, Content = Glyph(config.IconWidth, config.IconHeight) };
                if (config.AnimatedIcon && ToggleFab.Content is Control glyph)
                {
                    glyph.Bind(Control.WidthProperty, new Avalonia.Data.Binding(nameof(ToggleFab.IconSize)) { Source = ToggleFab });
                    glyph.Bind(Control.HeightProperty, new Avalonia.Data.Binding(nameof(ToggleFab.IconSize)) { Source = ToggleFab });
                    var brush = new SolidColorBrush();
                    brush.Bind(SolidColorBrush.ColorProperty, new Avalonia.Data.Binding(nameof(ToggleFab.IconColor)) { Source = ToggleFab });
                    TextElement.SetForeground(glyph, brush);
                }
                ToggleFab.CheckedChangeRequested += (_, e) => ToggleFab.IsChecked = e.IsChecked;
                ToggleFab.Click += (_, _) => clicks++; Control = ToggleFab;
            }
            else if (config.Kind == "menu")
            {
                var button = new FloatingActionButton { Content = Glyph(24, 24) };
                button.Click += (_, _) => clicks++;
                Menu = new() { Button = button, Expanded = config.Expanded, MenuAlignment = Enum.Parse<FloatingActionButtonMenuAlignment>(config.Alignment, true) };
                for (var i = 0; i < config.Count; i++) Menu.Items.Add(NewItem(i));
                Control = Menu;
            }
            else
            {
                Fab = config.Kind is "extended" or "content" ? new ExtendedFloatingActionButton
                {
                    Icon = config.Kind == "extended" ? Glyph(config.IconWidth, config.IconHeight) : null,
                    Content = Glyph(config.LabelWidth, config.LabelHeight), Expanded = config.Expanded
                } : new FloatingActionButton { Content = Glyph(config.IconWidth, config.IconHeight) };
                Fab.Size = size; Fab.AnimatedVisible = config.Visible;
                Fab.Click += (_, _) => clicks++;
                if (config.Elevation != "default") Fab.Elevation = config.Elevation switch
                { "lowered" => FloatingActionButtonDefaults.LoweredElevation(), "flat" => FloatingActionButtonDefaults.BottomAppBarFabElevation(), _ => new(2, 4, 6, 9) };
                if (config.Color != "default") Fab.Background = new SolidColorBrush(config.Color switch
                { "primary" => Color.Parse("#6750A4"), "surface" => Color.Parse("#FEF7FF"), "transparent" => Colors.Transparent, _ => Color.Parse("#126E60") });
                Control = Fab;
            }
            var parent = new Border { Width = config.Width ?? double.NaN, Height = config.Height ?? double.NaN,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = Control };
            Control.HorizontalAlignment = HorizontalAlignment.Center; Control.VerticalAlignment = VerticalAlignment.Center;
            TextElement.SetForeground(parent, new SolidColorBrush(Color.Parse("#EE7700")));
            Root = new FabSceneCenter { Child = parent };
        }
        public FloatingActionButtonMenuItem NewItem(int index = 3)
        {
            var item = new FloatingActionButtonMenuItem { Icon = Glyph(24, 24), Content = Glyph(config.LabelWidth + index * 8, config.LabelHeight) };
            item.Click += (_, _) => clicks++;
            return item;
        }
        static Control Glyph(double width, double height) => new FabGlyph { Width = width, Height = height };
        public void Toggle()
        {
            if (Control is ExtendedFloatingActionButton extended) extended.Expanded = !extended.Expanded;
            else if (ToggleFab is not null) ToggleFab.IsChecked = !ToggleFab.IsChecked;
            else if (Menu is not null) Menu.Expanded = !Menu.Expanded;
        }
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse(dark ? "#4F378B" : "#EADDFF");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(dark ? "#EADDFF" : "#21005D");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
        }
        public ProbePoint[] Measure()
        {
            var fields = AvaloniaMeasurements.Geometry(Control, host.Window).Where(m => m.Name is "box" or "size").ToList();
            fields.Add(Measurement.Numeric("clicks", clicks, "count", definition: "fab.click-count"));
            if (ToggleFab is not null) fields.Add(Measurement.Numeric("checkedProgress", ToggleFab.CheckedProgress, "ratio", category: "motion", definition: "fab.checked-progress"));
            // Compose omits alpha-zero items from placement and descendant semantics.
            // Submit item probes only for the currently revealed slots on both ends.
            return [new("fab", "fab", null, [..fields]), ..config.Action == "count" ? Menu!.Items.OfType<Control>().Select((item, i) => (item, i)).Where(pair => pair.item.IsHitTestVisible).Select(pair =>
                new ProbePoint($"item-{pair.i}", "menu-item", "fab", [..AvaloniaMeasurements.Geometry(pair.item, host.Window).Where(m => m.Name == "size"),
                    Measurement.Numeric("alpha", pair.item.Opacity, "ratio", category: "motion", definition: "fab.item-layer-alpha", source: "Control.Opacity")])) : []];
        }
    }
    sealed class FabGlyph : Control
    {
        static FabGlyph() => AffectsRender<FabGlyph>(TextElement.ForegroundProperty);
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
    // Compose Box's Alignment.Center rounds half-pixel offsets toward positive infinity.
    // Avalonia Grid uses its own rounding; this is the scene placement rule, not a FAB offset.
    sealed class FabSceneCenter : Decorator
    {
        protected override Size MeasureOverride(Size available) { Child?.Measure(available); return available; }
        protected override Size ArrangeOverride(Size final)
        {
            var size = Child?.DesiredSize ?? default;
            var density = LayoutHelper.GetLayoutScale(this);
            var x = Math.Floor((final.Width - size.Width) * density / 2 + 0.5) / density;
            var y = Math.Floor((final.Height - size.Height) * density / 2 + 0.5) / density;
            Child?.Arrange(new Rect(x, y, size.Width, size.Height)); return final;
        }
    }
}
