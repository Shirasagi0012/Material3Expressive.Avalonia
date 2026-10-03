using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CardConfig(string Kind = "filled", bool Clickable = false, bool Enabled = true, bool Dark = false,
    bool Rtl = false, double? Width = 160, double? Height = 96, double ContentWidth = 64, double ContentHeight = 24,
    bool TwoChildren = true, string Custom = "none", string Action = "none", double ChildPadding = 16);

[ParitySuite("card")]
public sealed class CardCases
{
    static readonly string[] Kinds = ["filled", "elevated", "outlined"];
    static CaseData<CardConfig> Data(string id, CardConfig config) => new(id, id.Replace('.', ' '), config);
    public static IEnumerable<CaseData<CardConfig>> StaticConfigurations()
    {
        // All three variants × passive/clickable × enabled/disabled × light/dark. Passive
        // disabling is a framework property and does not select Card.kt's disabled colors.
        foreach (var kind in Kinds)
        foreach (var clickable in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{kind}.{(clickable ? "clickable" : "passive")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}", new(kind, clickable, enabled, dark));
        // Variants share content/layout. Keep outlined border in the automatic/constrained cases
        // to observe that it consumes no space. Small clickable surfaces retain outer 48 DIP space.
        foreach (var clickable in new[] { false, true })
        {
            var mode = clickable ? "clickable" : "passive";
            yield return Data($"static.layout.{mode}.automatic", new("outlined", clickable, Width: null, Height: null));
            yield return Data($"static.layout.{mode}.small", new("outlined", clickable, Width: null, Height: null, ContentWidth: 16, ContentHeight: 8, TwoChildren: false));
            yield return Data($"static.layout.{mode}.empty", new("filled", clickable, Width: null, Height: null, ContentWidth: 0, ContentHeight: 0, TwoChildren: false));
            yield return Data($"static.layout.{mode}.constrained-width", new("outlined", clickable, Width: 20));
            yield return Data($"static.layout.{mode}.constrained-height", new("outlined", clickable, Height: 12));
            yield return Data($"static.layout.{mode}.rtl", new("outlined", clickable, Rtl: true));
            yield return Data($"static.layout.{mode}.square", new("outlined", clickable, Custom: "square"));
            yield return Data($"static.layout.{mode}.asymmetric", new("outlined", clickable, Custom: "asymmetric"));
        }
        foreach (var kind in Kinds)
        {
            foreach (var enabled in new[] { false, true })
                yield return Data($"static.custom.{kind}.colors.{(enabled ? "enabled" : "disabled")}", new(kind, true, enabled, Custom: "colors"));
            yield return Data($"static.custom.{kind}.elevation", new(kind, true, Custom: "elevation"));
            yield return Data($"static.custom.{kind}.surface-color", new(kind, true, Custom: "surface"));
        }
        yield return Data("static.custom.filled.border", new(Custom: "border"));
        yield return Data("static.custom.outlined.border", new("outlined", Custom: "border"));
        yield return Data("static.custom.filled.role-color", new(Custom: "role"));
        yield return Data("static.custom.filled.ambient-color", new(Custom: "ambient"));
    }
    public static IEnumerable<CaseData<CardConfig>> DynamicConfigurations()
    {
        // State colors/elevation differ per variant; shared input branches are represented below.
        foreach (var kind in Kinds)
        foreach (var action in new[] { "hover", "press", "disable-press", "disabled-input", "theme" })
            yield return Data($"dynamic.{kind}.{action}", new(kind, true, Enabled: action != "disabled-input", Action: action));
        foreach (var action in new[] { "quick", "cancel", "space", "enter", "focus-hover", "reverse-hover", "colors", "elevation", "mode", "passive-input" })
            yield return Data($"dynamic.filled.{action}", new("filled", action != "passive-input", Custom: action is "elevation" or "focus-hover" or "reverse-hover" ? "elevation" : "none", Action: action));
        yield return Data("dynamic.filled.nested-pointer", new("filled", true, TwoChildren: false, Action: "nested-pointer"));
        yield return Data("dynamic.filled.nested-space", new("filled", true, TwoChildren: false, Action: "nested-space"));
        yield return Data("dynamic.filled.passive-nested-pointer", new("filled", false, TwoChildren: false, Action: "nested-pointer"));
        yield return Data("dynamic.filled.passive-elevation", new("filled", false, Custom: "elevation", Action: "elevation"));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 320, 200);
        var scene = new CardScene(host, writer.Metadata.ReadConfig<CardConfig>());
        host.Mount(CaseDefaults.Center(scene.Card)); host.Draw(); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<CardConfig>();
        await using var host = new AvaloniaHost(writer, 320, 200);
        var scene = new CardScene(host, config); var card = scene.Card;
        host.Mount(CaseDefaults.Center(card)); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var center = card.TranslatePoint(new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(8, 8); Sample("initial");
        switch (config.Action)
        {
            case "nested-pointer":
                var child = scene.Child!;
                var childCenter = child.TranslatePoint(new Rect(child.Bounds.Size).Center, host.Window)!.Value;
                Event(1000, "move", childCenter); Event(2000, "down", childCenter); Sample("child-press"); Times(16000, 64000);
                Event(65000, "up", childCenter); Sample("child-release"); Times(80000, 240000, 400000); break;
            case "nested-space":
                Advance(1000); scene.Child!.Focus(NavigationMethod.Tab); host.Draw(); host.Frame(1000); Sample("child-focus"); Times(16000, 160000);
                Advance(161000); host.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("child-key-down"); Times(176000, 240000);
                Advance(241000); host.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(241000); Sample("child-key-up"); Times(256000, 400000); break;
            case "theme":
                Mutate(1000, "dark", () => SetColors(host, true)); Times(16000, 80000, 400000); break;
            case "colors":
                Mutate(1000, "custom", () => card.Colors = CustomColors); Times(16000, 64000);
                Mutate(65000, "restore", () => card.Colors = null); Times(80000, 240000); break;
            case "elevation":
                Event(1000, "move", center); Times(16000, 64000);
                Mutate(65000, "configuration", () => card.Elevation = new CardElevation { Default = 1, Pressed = 2, Focused = 3, Hovered = 7, Dragged = 9, Disabled = 0 });
                Times(80000, 160000, 240000); break;
            case "mode":
                // Move is explicit: native MouseDown at a new position synthesizes an enter,
                // while ComposeScene's raw Press does not provide that hover input.
                Event(500, "move", center); Event(1000, "down", center); Times(16000, 32000);
                Mutate(33000, "passive", () => card.IsClickable = false); Event(34000, "up", center); Sample("released-passive");
                Times(48000, 160000); Mutate(161000, "clickable", () => card.IsClickable = true);
                Event(161500, "move", center); Event(162000, "down", center); Event(163000, "up", center); Sample("clicked"); Times(176000, 400000); break;
            case "space": case "enter":
                Advance(1000); card.Focus(NavigationMethod.Tab); host.Draw(); host.Frame(1000); Sample("focus"); Times(16000, 64000, 160000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(161000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-down"); Times(176000, 240000);
                Advance(241000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(241000); Sample("key-up"); Times(256000, 400000); break;
            case "hover": case "reverse-hover": case "focus-hover":
                if (config.Action == "focus-hover") { Advance(1000); card.Focus(NavigationMethod.Tab); host.Draw(); host.Frame(1000); Sample("focus"); Times(16000, 160000); }
                var start = config.Action == "focus-hover" ? 161000 : 1000;
                Event(start, "move", center); Sample("hover"); Times(start + 15000, start + 63000);
                Event(start + (config.Action == "reverse-hover" ? 65000 : 161000), "move", outside); Sample("exit");
                Times(start + 175000, start + 239000, start + 399000); break;
            default:
                Event(1000, "move", center); Event(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input" or "passive-input")
                { Event(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000); break; }
                Times(16000, 32000, 64000);
                if (config.Action == "cancel") Event(65000, "move", outside);
                if (config.Action == "disable-press") Mutate(65000, "disabled", () => card.IsEnabled = false);
                Times(80000, 160000); Event(161000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(176000, 240000, 400000);
                if (config.Action == "disable-press") { Mutate(401000, "enabled", () => card.IsEnabled = true); Times(416000, 560000); }
                break;
        }
    }
    sealed class CardScene
    {
        readonly AvaloniaHost host;
        readonly List<Marker> markers = [];
        int clicks, childClicks;
        public Card Card { get; }
        public Button? Child { get; }
        public CardScene(AvaloniaHost host, CardConfig config)
        {
            this.host = host; SetColors(host, config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            Card = config.Kind switch { "elevated" => new ElevatedCard(), "outlined" => new OutlinedCard(), _ => new Card() };
            Card.IsClickable = config.Clickable; Card.IsEnabled = config.Enabled;
            if (config.Width is { } width) Card.Width = width;
            if (config.Height is { } height) Card.Height = height;
            var column = new CardColumn();
            if (config.ContentWidth > 0 || config.ContentHeight > 0)
            {
                markers.Add(new Marker(new Size(config.ContentWidth, config.ContentHeight)) { HorizontalAlignment = HorizontalAlignment.Left });
                if (config.TwoChildren) markers.Add(new Marker(new Size(config.ContentWidth / 2, config.ContentHeight / 2)) { HorizontalAlignment = HorizontalAlignment.Left });
                if (config.Action.StartsWith("nested-", StringComparison.Ordinal))
                {
                    // This scene tests input ownership. Button keeps Expressive padding in
                    // this library; specify the same padding on both real Button APIs.
                    Child = new Button { Width = 80, Height = 48, Padding = new Thickness(config.ChildPadding, 8), Content = markers.Single() };
                    Child.Click += (_, _) => childClicks++;
                    column.Children.Add(Child);
                }
                else foreach (var marker in markers) column.Children.Add(marker);
            }
            Card.Content = column;
            Card.Click += (_, e) => { if (ReferenceEquals(e.Source, Card)) clicks++; };
            switch (config.Custom)
            {
                case "colors": Card.Colors = CustomColors; break;
                case "role": Card.Colors = new CardColors { ContainerColor = Color.Parse("#6750A4") }; break;
                case "ambient": Card.Colors = new CardColors { ContainerColor = Color.Parse("#126E60") }; break;
                case "surface": Card.Colors = new CardColors { ContainerColor = Color.Parse(config.Dark ? "#141218" : "#FEF7FF") }; Card.Elevation = CustomElevation; break;
                case "elevation": Card.Elevation = CustomElevation; break;
                case "border": Card.BorderBrush = Brushes.Red; Card.BorderThickness = new Thickness(2.25); break;
                case "square": Card.CornerRadius = new CornerRadius(0); break;
                case "asymmetric": Card.CornerRadius = new CornerRadius(3, 9, 17, 5); break;
            }
        }
        public ProbePoint[] Measure() => [new("card", "card", null,
            [.. AvaloniaMeasurements.Geometry(Card, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("shadowElevation", Card.GetVisualDescendants().OfType<MaterialSurface>().Single(s => ReferenceEquals(s.TemplatedParent, Card)).ShadowElevation,
                    "dip", category: "motion", definition: "card.shadow-elevation", source: "Surface.ShadowElevation"),
                Measurement.Numeric("clicks", clicks, "count", definition: "card.click-callback-count")]),
            .. Child is null ? Array.Empty<ProbePoint>() : [new ProbePoint("child", "button", "card",
                [.. AvaloniaMeasurements.Geometry(Child, host.Window).Where(m => m.Name is "box" or "size"),
                    Measurement.Numeric("clicks", childClicks, "count", definition: "card.child-click-callback-count")])],
            .. markers.Select((marker, index) => new ProbePoint($"content-{index}", "content", Child is null ? "card" : "child",
                AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name is "box" or "size").ToArray()))];
    }
    // Card additionally consumes these roles. Use the fixed Compose palette inputs, as in
    // CaseDefaults, so a color-scheme generator difference is not attributed to the component.
    static void SetColors(AvaloniaHost host, bool dark)
    {
        CaseDefaults.SetThemeColors(host, dark);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.SurfaceVariant] = Color.Parse(dark ? "#49454F" : "#E7E0EC");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Shadow] = Colors.Black;
    }
    public static CardColors CustomColors => new() { ContainerColor = Color.Parse("#B0336699"), ContentColor = Color.Parse("#126E60"),
        DisabledContainerColor = Color.Parse("#50336699"), DisabledContentColor = Color.Parse("#70556677") };
    public static CardElevation CustomElevation => new() { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 9, Disabled = 1 };
    sealed class Marker(Size size) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(size.Width, availableSize.Width), Math.Min(size.Height, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}
