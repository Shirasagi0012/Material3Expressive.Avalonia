using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PlainTooltipConfig(double ContentWidth = 24, double ContentHeight = 8,
    bool Dark = false, bool Rtl = false, string Colors = "default", double? Width = null,
    double? Height = null, double Maximum = 200, double Minimum = 0, string Action = "none");

[ParitySuite("plain-tooltip")]
public sealed class PlainTooltipCases
{
    public static IEnumerable<CaseData<PlainTooltipConfig>> StaticConfigurations()
    {
        foreach (var size in new[] { (0d, 0d), (4d, 8d), (64d, 16d), (300d, 8d), (8d, 48d) })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.content.{size.Item1}-{size.Item2}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}",
                new(size.Item1, size.Item2, dark, rtl));
        foreach (var colors in new[] { "custom", "surface", "elevated" })
            yield return Data($"static.colors.{colors}", new(Colors: colors));
        yield return Data("static.width.maximum", new(300, Maximum: 100));
        yield return Data("static.width.explicit", new(400, Width: 300));
        yield return Data("static.size.small", new(4, Width: 20, Height: 12));
        yield return Data("static.width.minimum", new(300, Maximum: 100, Minimum: 160));
    }
    public static IEnumerable<CaseData<PlainTooltipConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "theme", "content", "colors" })
            yield return Data($"dynamic.{action}", new(Action: action));
    }
    static CaseData<PlainTooltipConfig> Data(string id, PlainTooltipConfig config) => new(id, id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<PlainTooltipConfig>();
        await using var host = new AvaloniaHost(writer, 400, 240);
        var content = new Marker { PreferredWidth = config.ContentWidth, PreferredHeight = config.ContentHeight };
        var tooltip = new PlainTooltip { Content = content, TooltipMaxWidth = config.Maximum, MinWidth = config.Minimum,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        if (config.Width is { } width) tooltip.Width = width;
        if (config.Height is { } height) tooltip.Height = height;
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Theme(config.Dark); Colors(config.Colors);
        host.Mount(new Grid { Children = { tooltip } }); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control c) => AvaloniaMeasurements.Geometry(c, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var color = content.Painted;
            writer.Submit(host.Snapshot(id, id,
            [new("tooltip", "tooltip", null, Geometry(tooltip)), new("content", "content", "tooltip",
                [.. Geometry(content), Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A],
                    "srgba8", "none", "properties", "plain-tooltip.content.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")])]));
        }
        Sample("initial");
        if (!dynamic) return;
        void Change(long time, string id, Action action)
        {
            host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id);
        }
        switch (config.Action)
        {
            case "theme": Change(1000, "dark", () => Theme(true)); Change(2000, "light", () => Theme(false)); break;
            case "content":
                Change(1000, "wide", () => content.PreferredWidth = 300);
                Change(2000, "tall", () => { content.PreferredWidth = 8; content.PreferredHeight = 48; });
                Change(3000, "empty", () => { content.PreferredWidth = 0; content.PreferredHeight = 0; }); break;
            case "colors": Change(1000, "custom", () => Colors("custom")); Change(2000, "default", () => Colors("default")); break;
        }
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(dark ? "#E6E0E9" : "#322F35");
            host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(dark ? "#322F35" : "#F5EFF7");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.Shadow] = Avalonia.Media.Colors.Black;
        }
        void Colors(string colors)
        {
            tooltip.ClearValue(PlainTooltip.BackgroundProperty); tooltip.ClearValue(PlainTooltip.ForegroundProperty);
            tooltip.ClearValue(PlainTooltip.CornerRadiusProperty);
            tooltip.TonalElevation = colors == "surface" ? 6 : 0;
            tooltip.ShadowElevation = colors == "elevated" ? 6 : 0;
            if (colors == "custom") { tooltip.Background = Brush.Parse("#E0F0E8"); tooltip.Foreground = Brushes.Blue; tooltip.CornerRadius = new(12); }
            if (colors == "surface") tooltip.Background = Brush.Parse("#FEF7FF");
        }
    }
    sealed class Marker : Control
    {
        public static readonly StyledProperty<double> PreferredWidthProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredWidth));
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredHeight));
        public double PreferredWidth { get => GetValue(PreferredWidthProperty); set => SetValue(PreferredWidthProperty, value); }
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        public Color Painted { get; private set; }
        static Marker() { AffectsMeasure<Marker>(PreferredWidthProperty, PreferredHeightProperty); AffectsRender<Marker>(TextElement.ForegroundProperty); }
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this); Painted = ((ISolidColorBrush)brush!).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
