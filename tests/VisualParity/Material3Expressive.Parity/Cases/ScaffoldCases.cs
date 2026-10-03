using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;

namespace Material3Expressive.Parity;

public sealed record ScaffoldConfig(string Position = "end", bool Rtl = false, double? TopHeight = 40,
    double? BottomHeight = 32, double? FabWidth = 56, double FabHeight = 56, double? SnackbarWidth = 120,
    double SnackbarHeight = 24, double Width = 240, double Height = 200, double[]? Insets = null,
    double[]? Consumed = null, bool Dark = false, string? Color = null, string? ContentColor = null,
    bool ApplyPadding = true, bool Nested = false, bool Stacked = false, string Action = "none");

[ParitySuite("scaffold")]
public sealed class ScaffoldCases
{
    public static IEnumerable<CaseData<ScaffoldConfig>> StaticConfigurations()
    {
        // The placement branches depend on position, bar presence and direction. Insets are
        // deliberately asymmetric in every combination, rather than multiplied into the matrix.
        foreach (var position in new[] { "start", "center", "end", "end-overlay" })
        foreach (var bottom in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.placement.{position}.{(bottom ? "bar" : "no-bar")}.{(rtl ? "rtl" : "ltr")}",
                new(position, rtl, BottomHeight: bottom ? 32 : null, Insets: [8, 24, 12, 16]));
        // Empty lambdas versus real placeables exercise presence independently of size.
        for (var mask = 0; mask < 16; mask++)
            yield return Data($"static.slots.{mask:D2}", new(TopHeight: (mask & 1) != 0 ? 40 : null,
                BottomHeight: (mask & 2) != 0 ? 32 : null, FabWidth: (mask & 4) != 0 ? 56 : null,
                SnackbarWidth: (mask & 8) != 0 ? 120 : null));
        yield return Data("static.layout.collapsed-bars", new(TopHeight: 0, BottomHeight: 0, Insets: [8, 24, 12, 16]));
        yield return Data("static.layout.zero-width-fab", new(FabWidth: 0, Insets: [8, 24, 12, 16]));
        yield return Data("static.layout.zero-height-fab", new(FabHeight: 0, Insets: [8, 24, 12, 16]));
        yield return Data("static.layout.zero-height-snackbar", new(SnackbarHeight: 0));
        yield return Data("static.layout.constrained", new(Width: 80, Height: 100, Insets: [28, 0, 20, 12]));
        yield return Data("static.layout.excess-insets", new(Width: 80, Height: 100, Insets: [60, 120, 40, 120]));
        yield return Data("static.layout.fractional-center", new("center", Width: 241.25, Height: 200.5,
            FabWidth: 55.25, FabHeight: 56.5, SnackbarWidth: 119.5, Insets: [8.375, 24.125, 12.625, 16.375]));
        yield return Data("static.layout.unpadded-body", new(ApplyPadding: false, Insets: [8, 24, 12, 16]));
        yield return Data("static.layout.stacked-content", new(Stacked: true));
        yield return Data("static.layout.z-order", new("end-overlay", TopHeight: 200, BottomHeight: 200, SnackbarWidth: 240, SnackbarHeight: 200, ApplyPadding: false));
        foreach (var rtl in new[] { false, true })
        {
            yield return Data($"static.insets.consumed.{(rtl ? "rtl" : "ltr")}", new(Rtl: rtl, TopHeight: null, BottomHeight: null,
                Insets: [16, 30, 24, 40], Consumed: [4, 12, 8, 16]));
            yield return Data($"static.insets.nested.{(rtl ? "rtl" : "ltr")}", new(Rtl: rtl, Nested: true, Insets: [8, 24, 12, 16]));
        }
        yield return Data("static.insets.over-consumed", new(TopHeight: null, BottomHeight: null,
            Insets: [4, 8, 12, 16], Consumed: [8, 12, 16, 20]));
        // Scaffold has no size/shape variants or own animations. Theme and color matching use
        // the same layout; geometry + a marker inheriting LocalContentColor observe the result.
        yield return Data("static.color.dark", new(Dark: true));
        yield return Data("static.color.primary", new(Color: "primary"));
        yield return Data("static.color.ambient", new(Color: "#70336699"));
        yield return Data("static.color.transparent", new(Color: "#00336699"));
        yield return Data("static.color.explicit-content", new(Color: "primary", ContentColor: "#FF008877"));
    }

    public static IEnumerable<CaseData<ScaffoldConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "bars", "fab-position", "fab-size", "snackbar", "insets", "direction", "theme", "colors", "resize" })
            yield return Data($"dynamic.{action}", new(Insets: [8, 24, 12, 16], Action: action));
        yield return Data("dynamic.nested-insets", new(Insets: [8, 24, 12, 16], Nested: true, Action: "insets"));
    }
    private static CaseData<ScaffoldConfig> Data(string id, ScaffoldConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 320, 280);
        var scene = new ScaffoldScene(host, writer.Metadata.ReadConfig<ScaffoldConfig>());
        host.Mount(scene.Root); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ScaffoldConfig>();
        await using var host = new AvaloniaHost(writer, 320, 280);
        var scene = new ScaffoldScene(host, config);
        host.Mount(scene.Root); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        void Change(long time, string id, Action action)
        {
            host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id);
        }
        Sample("initial");
        switch (config.Action)
        {
            case "bars":
                Change(1000, "removed", () => { scene.Screen.TopBar = null; scene.Screen.BottomBar = null; });
                Change(2000, "collapsed", () => scene.SetBars(0, 0));
                Change(3000, "resized", () => scene.SetBars(56, 24)); break;
            case "fab-position":
                Change(1000, "start", () => scene.Screen.FloatingActionButtonPosition = FabPosition.Start);
                Change(2000, "center", () => scene.Screen.FloatingActionButtonPosition = FabPosition.Center);
                Change(3000, "overlay", () => scene.Screen.FloatingActionButtonPosition = FabPosition.EndOverlay); break;
            case "fab-size":
                Change(1000, "zero-width", () => scene.Fab!.BoxWidth = 0);
                Change(2000, "wide", () => { scene.Fab!.BoxWidth = 80; scene.Fab.BoxHeight = 40; });
                Change(3000, "removed", () => scene.Screen.FloatingActionButton = null); break;
            case "snackbar":
                Change(1000, "removed", () => scene.Screen.SnackbarHost = null);
                Change(2000, "replaced", () => scene.SetSnackbar(160, 40));
                Change(3000, "collapsed", () => scene.Snackbar!.BoxHeight = 0); break;
            case "insets":
                Change(1000, "changed", () => scene.Screen.ContentWindowInsets = new Thickness(16, 30, 24, 40));
                Change(2000, "consumed", () => { scene.Parent.Padding = new Thickness(4, 12, 8, 16); MaterialWindowInsets.SetConsumePadding(scene.Parent, scene.Parent.Padding); });
                Change(3000, "cleared", () => { scene.Parent.Padding = default; scene.Parent.ClearValue(MaterialWindowInsets.ConsumePaddingProperty); }); break;
            case "direction":
                Change(1000, "rtl", () => scene.Root.FlowDirection = FlowDirection.RightToLeft);
                Change(2000, "ltr", () => scene.Root.FlowDirection = FlowDirection.LeftToRight); break;
            case "theme":
                Change(1000, "dark", () => scene.SetColors(true)); Change(2000, "light", () => scene.SetColors(false)); break;
            case "colors":
                Change(1000, "primary", () => scene.Screen.Background = new SolidColorBrush(Color.Parse("#6750A4")));
                Change(2000, "ambient", () => scene.Screen.Background = new SolidColorBrush(Color.Parse("#70336699")));
                Change(3000, "explicit", () => scene.Screen.Foreground = new SolidColorBrush(Color.Parse("#008877")));
                Change(4000, "default", () => { scene.Screen.ClearValue(Scaffold.BackgroundProperty); scene.Screen.ClearValue(Scaffold.ForegroundProperty); }); break;
            case "resize":
                Change(1000, "smaller", () => { scene.Screen.Width = 160; scene.Screen.Height = 140; });
                Change(2000, "restore", () => { scene.Screen.Width = 240; scene.Screen.Height = 200; }); break;
        }
        host.AdvanceTo(5000); Sample("idle-layout", image: false);
    }

    private sealed class ScaffoldScene
    {
        private readonly AvaloniaHost host;
        private readonly ScaffoldConfig config;
        private readonly Border body;
        private readonly Grid bodyContent;
        private readonly FixedBox marker;
        private readonly Scaffold? nested;
        public Scaffold Screen { get; }
        public Border? Top { get; private set; }
        public Border? Bottom { get; private set; }
        public FixedBox? Snackbar { get; private set; }
        public FixedBox? Fab { get; }
        public Border Parent { get; }
        public Grid Root { get; }

        public ScaffoldScene(AvaloniaHost host, ScaffoldConfig config)
        {
            this.host = host; this.config = config; SetColors(config.Dark);
            Screen = new Scaffold { Width = config.Width, Height = config.Height,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                FloatingActionButtonPosition = config.Position switch { "start" => FabPosition.Start, "center" => FabPosition.Center,
                    "end-overlay" => FabPosition.EndOverlay, _ => FabPosition.End }, ContentWindowInsets = Thickness(config.Insets) };
            if (config.Color is { } color) Screen.Background = new SolidColorBrush(Color.Parse(color == "primary" ? "#6750A4" : color));
            if (config.ContentColor is { } contentColor) Screen.Foreground = new SolidColorBrush(Color.Parse(contentColor));
            SetBars(config.TopHeight, config.BottomHeight); SetSnackbar(config.SnackbarWidth, config.SnackbarHeight);
            if (config.FabWidth is { } fabWidth)
                Screen.FloatingActionButton = Fab = new FixedBox { BoxWidth = fabWidth, BoxHeight = config.FabHeight, Background = Brush("#DD7722") };
            marker = new FixedBox { BoxWidth = 24, BoxHeight = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            marker.Bind(FixedBox.BackgroundProperty, marker.GetObservable(TextElement.ForegroundProperty));
            bodyContent = new FillGrid { Children = { marker } };
            if (config.Stacked) bodyContent.Children.Add(new Border { Width = 36, Height = 16, Background = Brush("#2288AA"),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
            body = new Border { Child = bodyContent };
            if (config.ApplyPadding)
            {
                body.Bind(Border.PaddingProperty, Screen.GetObservable(Scaffold.ContentPaddingProperty));
                body.Bind(MaterialWindowInsets.ConsumePaddingProperty, Screen.GetObservable(Scaffold.ContentPaddingProperty));
            }
            if (config.Nested)
            {
                bodyContent.Children.Remove(marker);
                nested = new Scaffold { ContentWindowInsets = Screen.ContentWindowInsets, Content = new FillGrid { Children = { marker } } };
                nested.Bind(Scaffold.ContentWindowInsetsProperty, Screen.GetObservable(Scaffold.ContentWindowInsetsProperty));
                bodyContent.Children.Add(nested);
            }
            Screen.Content = body;
            Parent = new Border { Padding = Thickness(config.Consumed), Child = Screen };
            MaterialWindowInsets.SetConsumePadding(Parent, Parent.Padding);
            Root = new Grid { Margin = new Thickness(16), FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { Parent } };
            TextElement.SetForeground(Root, Brush("#345678"));
        }
        public void SetBars(double? top, double? bottom)
        {
            Top = top is { } t ? new Border { Height = t, Child = new FillWidth(), Background = Brush("#336699") } : null;
            Bottom = bottom is { } b ? new Border { Height = b, Child = new FillWidth(), Background = Brush("#228866") } : null;
            // A width-filling child is the slot input, not a fixed expected measured bar width.
            Screen.TopBar = Top; Screen.BottomBar = Bottom;
        }
        public void SetSnackbar(double? width, double height) => Screen.SnackbarHost = Snackbar = width is { } w
            ? new FixedBox { BoxWidth = w, BoxHeight = height, Background = Brush("#8866AA") } : null;
        public void SetColors(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[MaterialTokens.Sys.Color.Background] = Color.Parse(dark ? "#141218" : "#FFF7FF");
            host.Window.Resources[MaterialTokens.Sys.Color.OnBackground] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
        }
        public ProbePoint[] Measure()
        {
            var points = new List<ProbePoint> { Point("scaffold", Screen, null, Padding(Screen)), Point("body", body, "scaffold"),
                Point("body-content", bodyContent, "body"), Point("marker", marker, config.Nested ? "nested" : "body-content") };
            if (Top is not null && Screen.TopBar is not null) points.Add(Point("top-bar", Top, "scaffold"));
            if (Bottom is not null && Screen.BottomBar is not null) points.Add(Point("bottom-bar", Bottom, "scaffold"));
            if (Snackbar is not null && Screen.SnackbarHost is not null) points.Add(Point("snackbar", Snackbar, "scaffold"));
            if (Fab is not null && Screen.FloatingActionButton is not null && Fab.DesiredSize.Width > 0 && Fab.DesiredSize.Height > 0)
                points.Add(Point("fab", Fab, "scaffold"));
            if (nested is not null) points.Add(Point("nested", nested, "body-content", Padding(nested)));
            return points.ToArray();
        }
        private ProbePoint Point(string id, Control control, string? parent, params Measurement[] extras) => new(id, id, parent,
            AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").Concat(extras).ToArray());
        private static Measurement Padding(Scaffold screen)
        {
            var p = screen.ContentPadding;
            return Measurement.Vector("contentPadding", "rect", [p.Left, p.Top, p.Right, p.Bottom], "dip", "logical", "geometry",
                "scaffold.content-padding.logical", "Scaffold.ContentPadding", ["start", "top", "end", "bottom"]);
        }
        private static Thickness Thickness(double[]? values) => values is null ? default : new(values[0], values[1], values[2], values[3]);
        private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
    }

    // These fixtures express fillMaxWidth/fillMaxSize, which are explicit inputs on the
    // Compose side. They do not calculate any Scaffold padding, offset or slot constraints.
    private sealed class FillWidth : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, 0);
    }
    private sealed class FillGrid : Grid
    {
        protected override Size MeasureOverride(Size availableSize) { base.MeasureOverride(availableSize); return availableSize; }
    }
    // Modifier.size requests a desired size bounded by the incoming maximum. Native Avalonia
    // Width/Height are reapplied by Layoutable.ArrangeCore and instead allow required-size
    // overflow. This leaf implements the requested-size input without Scaffold compensation.
    private sealed class FixedBox : Control
    {
        public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<FixedBox>();
        public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
        private double _boxWidth, _boxHeight;
        public double BoxWidth { get => _boxWidth; set { _boxWidth = value; InvalidateMeasure(); } }
        public double BoxHeight { get => _boxHeight; set { _boxHeight = value; InvalidateMeasure(); } }
        static FixedBox() => AffectsRender<FixedBox>(BackgroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(BoxWidth, availableSize.Width), Math.Min(BoxHeight, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(Background, null, new Rect(Bounds.Size));
    }
}
