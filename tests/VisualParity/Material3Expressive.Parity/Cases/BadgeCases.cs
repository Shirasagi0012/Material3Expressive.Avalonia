using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record BadgeConfig(string Host = "badge", string Content = "small", bool Dark = false, bool Rtl = false,
    string Colors = "default", double? Width = null, double? Height = null, double? BoxWidth = null, double? BoxHeight = null,
    double BoxMinWidth = 0, double BoxMinHeight = 0, double? MaxWidth = null, double? MaxHeight = null,
    double AnchorWidth = 24, double AnchorHeight = 24, string Action = "none", bool Start = false);

[ParitySuite("badge")]
public sealed class BadgeCases
{
    public static IEnumerable<CaseData<BadgeConfig>> StaticConfigurations()
    {
        // Both Badge branches, including present-but-empty and Row-like composite content.
        foreach (var content in new[] { "dot", "empty", "small", "wide", "tall", "row" })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{content}.{(dark ? "dark" : "light")}", new(Content: content, Dark: dark));
        foreach (var content in new[] { "dot", "small", "wide", "tall" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.box.{content}.{(rtl ? "rtl" : "ltr")}", new("box", content, Rtl: rtl));
        foreach (var colors in new[] { "custom", "primary", "explicit" })
            yield return Data($"static.colors.{colors}", new(Colors: colors));
        yield return Data("static.layout.explicit-short", new(Width: 24, Height: 6));
        yield return Data("static.layout.constrained", new(MaxWidth: 12, MaxHeight: 8));
        yield return Data("static.layout.large", new(Width: 40, Height: 32));
        foreach (var content in new[] { "dot", "small" })
        {
            yield return Data($"static.box.fixed.{content}", new("box", content, BoxWidth: 40, BoxHeight: 40));
            yield return Data($"static.box.minimum.{content}", new("box", content, BoxMinWidth: 40, BoxMinHeight: 40));
        }
        foreach (var width in new[] { 6d, 6.25d, 7d })
            yield return Data($"static.box.offset-threshold.{width.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('.', '-')}",
                new("box", "dot", Width: width, Height: 6));
        yield return Data("static.box.large-anchor", new("box", "small", AnchorWidth: 40, AnchorHeight: 36));
        // These are the actual upstream badgeBounds providers. Tall content exercises top
        // clamping; wide content exercises end clamping, independently of text metrics.
        foreach (var host in new[] { "rail", "bar", "short", "wide" })
        foreach (var content in new[] { "wide", "tall" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.navigation.{host}.{content}.{(rtl ? "rtl" : "ltr")}", new(host, content, Rtl: rtl));
        foreach (var host in new[] { "short", "wide" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.navigation.{host}.start.{(rtl ? "rtl" : "ltr")}", new(host, "wide", Rtl: rtl, Start: true));
    }

    public static IEnumerable<CaseData<BadgeConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "content", "theme", "colors", "rtl", "resize", "input", "interactive" })
            yield return Data($"dynamic.{action}", new("box", Action: action));
        yield return Data("dynamic.navigation.resize", new("rail", "wide", Action: "resize"));
        yield return Data("dynamic.navigation.bounds", new("bar", "wide", Action: "bounds"));
    }
    private static CaseData<BadgeConfig> Data(string id, BadgeConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 200, 160);
        var scene = new Scene(host, writer.Metadata.ReadConfig<BadgeConfig>());
        host.Mount(CaseDefaults.Center(scene.Root)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<BadgeConfig>();
        await using var host = new AvaloniaHost(writer, 200, 160);
        var scene = new Scene(host, config);
        host.Mount(CaseDefaults.Center(scene.Root)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure()));
        void Mutate(long time, string id, Action change)
        {
            host.AdvanceTo(time); change(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id);
        }
        Sample("initial");
        switch (config.Action)
        {
            case "content":
                Mutate(1000, "dot", () => scene.SetContent("dot"));
                Mutate(2000, "wide", () => scene.SetContent("wide"));
                Mutate(3000, "empty", () => scene.SetContent("empty"));
                Mutate(4000, "small", () => scene.SetContent("small")); break;
            case "theme":
                Mutate(1000, "dark", () => scene.SetTheme(true));
                Mutate(2000, "light", () => scene.SetTheme(false)); break;
            case "colors":
                Mutate(1000, "custom", () => scene.SetColors("custom"));
                Mutate(2000, "primary", () => scene.SetColors("primary"));
                Mutate(3000, "ambient", () => { scene.SetColors("custom"); TextElement.SetForeground(host.Window, Brushes.Green); }); break;
            case "rtl":
                Mutate(1000, "rtl", () => host.Window.FlowDirection = FlowDirection.RightToLeft);
                Mutate(2000, "ltr", () => host.Window.FlowDirection = FlowDirection.LeftToRight); break;
            case "resize":
                Mutate(1000, "larger", () => { scene.Anchor.PreferredWidth = 40; scene.Anchor.PreferredHeight = 36; });
                Mutate(2000, "smaller", () => { scene.Anchor.PreferredWidth = 16; scene.Anchor.PreferredHeight = 16; }); break;
            case "bounds":
                Mutate(1000, "narrow", () => scene.Root.Width = 60);
                Mutate(2000, "wide", () => scene.Root.Width = 96); break;
            case "input": case "interactive":
                // Pick a point painted by the badge that overlaps the clickable anchor.
                var point = scene.Anchor.TranslatePoint(new Point(20, 5), host.Window)!.Value;
                host.AdvanceTo(1000); host.Input("move", point); host.Input("down", point); host.Draw(); Sample("down");
                host.AdvanceTo(2000); host.Input("up", point); host.Draw(); Sample("up"); break;
        }
    }

    private sealed class Scene
    {
        private readonly AvaloniaHost _host;
        private readonly BadgeConfig _config;
        private readonly List<Marker> _content = [];
        private int _clicks;
        private int _badgeClicks;
        public Badge Badge { get; } = new();
        public Marker Anchor { get; }
        public BadgedBox Box { get; }
        public Control Root { get; }

        public Scene(AvaloniaHost host, BadgeConfig config)
        {
            _host = host; _config = config;
            SetTheme(config.Dark);
            TextElement.SetForeground(host.Window, Brushes.Magenta);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            Anchor = new Marker { PreferredWidth = config.AnchorWidth, PreferredHeight = config.AnchorHeight, FixedBrush = Brushes.Blue };
            var pressed = false;
            Anchor.PointerPressed += (_, _) => pressed = true;
            Anchor.PointerReleased += (_, _) => { if (pressed) _clicks++; pressed = false; };
            SetContent(config.Content); SetColors(config.Colors);
            if (config.Action == "interactive") Badge.IsHitTestVisible = true;
            if (config.Width is { } width) Badge.Width = width;
            if (config.Height is { } height) Badge.Height = height;
            if (config.MaxWidth is { } maxWidth) Badge.MaxWidth = maxWidth;
            if (config.MaxHeight is { } maxHeight) Badge.MaxHeight = maxHeight;
            Box = new BadgedBox { MinWidth = config.BoxMinWidth, MinHeight = config.BoxMinHeight };
            if (config.Host != "badge") { Box.Content = Anchor; Box.Badge = Badge; }
            if (config.BoxWidth is { } boxWidth) Box.Width = boxWidth;
            if (config.BoxHeight is { } boxHeight) Box.Height = boxHeight;
            Root = config.Host switch
            {
                "badge" => Badge,
                "box" => Box,
                "rail" => new NavigationRailItem { Icon = Box, IsSelected = true },
                "bar" => new NavigationBarItem { Icon = Box, IsSelected = true, Width = 80 },
                "short" => new ShortNavigationBarItem { Icon = Box, IsSelected = true, Width = 80,
                    Label = config.Start ? new Marker { PreferredWidth = 32, PreferredHeight = 16, FixedBrush = Brushes.Blue } : null,
                    IconPosition = config.Start ? NavigationItemIconPosition.Start : NavigationItemIconPosition.Top },
                "wide" => new WideNavigationRailItem { Icon = Box, IsSelected = true, RailExpanded = config.Start,
                    Label = config.Start ? new Marker { PreferredWidth = 32, PreferredHeight = 16, FixedBrush = Brushes.Blue } : null,
                    IconPosition = config.Start ? NavigationItemIconPosition.Start : NavigationItemIconPosition.Top },
                _ => throw new ArgumentException(config.Host),
            };
        }

        public void SetContent(string content)
        {
            _content.Clear();
            Marker Make(double width, double height)
            {
                var marker = new Marker { PreferredWidth = width, PreferredHeight = height, VerticalAlignment = VerticalAlignment.Center };
                if (_config.Action == "interactive")
                {
                    var pressed = false;
                    marker.PointerPressed += (_, _) => pressed = true;
                    marker.PointerReleased += (_, _) => { if (pressed) _badgeClicks++; pressed = false; };
                }
                _content.Add(marker); return marker;
            }
            Badge.Content = content switch
            {
                "dot" => null,
                "empty" => new Panel(),
                "small" => Make(4, 8),
                "wide" => Make(64, 8),
                "tall" => Make(8, 48),
                "row" => new StackPanel { Orientation = Orientation.Horizontal, Children = { Make(4, 8), Make(12, 4) } },
                _ => throw new ArgumentException(content),
            };
        }

        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(_host, dark);
            _host.Window.Resources[Sys.Color.Error] = Color.Parse(dark ? "#F2B8B5" : "#B3261E");
            _host.Window.Resources[Sys.Color.OnError] = Color.Parse(dark ? "#601410" : "#FFFFFF");
        }
        public void SetColors(string colors)
        {
            Badge.ClearValue(Badge.BackgroundProperty); Badge.ClearValue(Badge.ForegroundProperty);
            if (colors == "custom") Badge.Background = new SolidColorBrush(Color.Parse("#408060A0"));
            if (colors == "primary") Badge.Background = new SolidColorBrush(Color.Parse("#6750A4"));
            if (colors == "explicit") { Badge.Background = Brushes.Navy; Badge.Foreground = Brushes.Lime; }
        }

        public ProbePoint[] Measure()
        {
            var points = new List<ProbePoint>();
            static Measurement[] Geometry(Control control, Window window) => AvaloniaMeasurements.Geometry(control, window).Where(m => m.Name is "box" or "size").ToArray();
            if (_config.Host != "badge")
            {
                points.Add(new("box", "box", null, Geometry(Box, _host.Window)));
                points.Add(new("anchor", "anchor", "box", Geometry(Anchor, _host.Window)));
                if (_config.Host != "box") points.Add(new("item", "item", null, Geometry(Root, _host.Window)));
            }
            points.Add(new("badge", "badge", _config.Host == "badge" ? null : "box", Geometry(Badge, _host.Window)));
            for (var i = 0; i < _content.Count; i++)
            {
                var marker = _content[i]; var color = marker.Painted;
                points.Add(new($"content-{i}", "content", "badge", [.. Geometry(marker, _host.Window),
                    Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties",
                        "badge.content.painted-solid-srgba8", "Marker.Render actual inherited TextElement.Foreground") ]));
            }
            if (_config.Action is "input" or "interactive") points.Add(new("input", "input", null,
                [Measurement.Numeric("clicks", _clicks, "count", definition: "badge.anchor.click-count"),
                 Measurement.Numeric("badgeClicks", _badgeClicks, "count", definition: "badge.content.click-count")]));
            return points.ToArray();
        }
    }

    private sealed class Marker : Control
    {
        public static readonly StyledProperty<double> PreferredWidthProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredWidth));
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredHeight));
        public double PreferredWidth { get => GetValue(PreferredWidthProperty); set => SetValue(PreferredWidthProperty, value); }
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        static Marker()
        {
            AffectsMeasure<Marker>(PreferredWidthProperty, PreferredHeightProperty);
            AffectsRender<Marker>(TextElement.ForegroundProperty);
        }
        // Intrinsic content obeys the supplied maximum, like Compose Modifier.size.
        // Avalonia Width is a hard local dimension and can overflow the parent's slot.
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public IBrush? FixedBrush { get; init; }
        public Color Painted { get; private set; }
        public override void Render(DrawingContext context)
        {
            var brush = FixedBrush ?? TextElement.GetForeground(this);
            Painted = (brush as ISolidColorBrush ?? throw new InvalidOperationException("Marker requires a solid foreground.")).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
