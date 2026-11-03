using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record RichTooltipConfig(bool Title = true, bool Action = true, bool Dark = false, bool Rtl = false,
    double ContentWidth = 64, double ContentHeight = 8, double? Width = null, double? Height = null,
    double Maximum = 320, double? Baseline = null, string Colors = "default", string Mutation = "none");

[ParitySuite("rich-tooltip")]
public sealed class RichTooltipCases
{
    public static IEnumerable<CaseData<RichTooltipConfig>> StaticConfigurations()
    {
        foreach (var title in new[] { false, true })
        foreach (var action in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.slots.{title}-{action}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", new(title, action, dark, rtl));
        foreach (var baseline in new[] { 0d, 7d, 32d })
            yield return Data($"static.baseline.{baseline}", new(Baseline: baseline));
        foreach (var size in new[] { (0d, 0d), (400d, 8d), (8d, 64d) })
            yield return Data($"static.content.{size.Item1}-{size.Item2}", new(ContentWidth: size.Item1, ContentHeight: size.Item2));
        foreach (var height in new[] { 12d, 60d, 100d, 200d })
            yield return Data($"static.height.{height}", new(Height: height));
        yield return Data("static.width.maximum", new(ContentWidth: 400, Maximum: 100));
        yield return Data("static.width.explicit", new(ContentWidth: 400, Width: 360));
        yield return Data("static.width.narrow", new(Width: 20));
        foreach (var colors in new[] { "custom", "surface", "flat" })
            yield return Data($"static.colors.{colors}", new(Colors: colors));
    }
    public static IEnumerable<CaseData<RichTooltipConfig>> DynamicConfigurations()
    {
        foreach (var mutation in new[] { "slots", "theme", "colors", "content" })
            yield return Data($"dynamic.{mutation}", new(Mutation: mutation));
    }
    static CaseData<RichTooltipConfig> Data(string id, RichTooltipConfig config) => new(id.ToLowerInvariant(), id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<RichTooltipConfig>();
        await using var host = new AvaloniaHost(writer, 480, 300);
        var title = new Marker(32, 10); var content = new Marker(config.ContentWidth, config.ContentHeight); var action = new Marker(24, 12);
        if (config.Baseline is { } baseline)
        {
            TextBlock.SetBaselineOffset(title, baseline); TextBlock.SetBaselineOffset(content, baseline);
        }
        var tooltip = new RichTooltip { Title = config.Title ? title : null, Content = content, Action = config.Action ? action : null,
            TooltipMaxWidth = config.Maximum, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        if (config.Width is { } width) tooltip.Width = width;
        if (config.Height is { } height) tooltip.Height = height;
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Theme(config.Dark); Colors(config.Colors);
        host.Mount(new Grid { Children = { tooltip } }); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control c) => AvaloniaMeasurements.Geometry(c, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("tooltip", "tooltip", null, Geometry(tooltip)) };
            void Slot(string name, Marker marker)
            {
                var color = marker.Painted;
                points.Add(new(name, name, "tooltip", [.. Geometry(marker), Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A],
                    "srgba8", "none", "properties", "rich-tooltip.slot.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")]));
            }
            if (tooltip.Title is not null) Slot("title", title);
            Slot("content", content);
            if (tooltip.Action is not null) Slot("action", action);
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        if (!dynamic) return;
        void Change(long time, string id, Action change)
        {
            host.AdvanceTo(time); change(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id);
        }
        switch (config.Mutation)
        {
            case "slots":
                Change(1000, "text-only", () => { tooltip.Title = null; tooltip.Action = null; });
                Change(2000, "action", () => tooltip.Action = action);
                Change(3000, "all", () => tooltip.Title = title); break;
            case "theme": Change(1000, "dark", () => Theme(true)); Change(2000, "light", () => Theme(false)); break;
            case "colors": Change(1000, "custom", () => Colors("custom")); Change(2000, "default", () => Colors("default")); break;
            case "content":
                Change(1000, "wide", () => content.PreferredWidth = 400);
                Change(2000, "tall", () => { content.PreferredWidth = 8; content.PreferredHeight = 64; }); break;
        }
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.Shadow] = Avalonia.Media.Colors.Black;
        }
        void Colors(string colors)
        {
            tooltip.ClearValue(RichTooltip.BackgroundProperty); tooltip.ClearValue(RichTooltip.ForegroundProperty);
            tooltip.ClearValue(RichTooltip.TitleForegroundProperty); tooltip.ClearValue(RichTooltip.ActionForegroundProperty);
            tooltip.ClearValue(RichTooltip.CornerRadiusProperty); tooltip.ClearValue(RichTooltip.ShadowElevationProperty);
            tooltip.TonalElevation = colors == "surface" ? 6 : 0;
            if (colors == "custom")
            {
                tooltip.Background = Brush.Parse("#E0F0E8"); tooltip.Foreground = Brushes.Blue;
                tooltip.TitleForeground = Brushes.Red; tooltip.ActionForeground = Brushes.Green; tooltip.CornerRadius = new(20);
            }
            if (colors == "surface") tooltip.Background = Brush.Parse("#FEF7FF");
            if (colors == "flat") tooltip.ShadowElevation = 0;
        }
    }
    sealed class Marker : Control
    {
        public static readonly StyledProperty<double> PreferredWidthProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredWidth));
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredHeight));
        public double PreferredWidth { get => GetValue(PreferredWidthProperty); set => SetValue(PreferredWidthProperty, value); }
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        public Color Painted { get; private set; }
        public Marker(double width, double height) { PreferredWidth = width; PreferredHeight = height; }
        static Marker() { AffectsMeasure<Marker>(PreferredWidthProperty, PreferredHeightProperty); AffectsRender<Marker>(TextElement.ForegroundProperty); }
        protected override Size MeasureOverride(Size availableSize) => new Size(PreferredWidth, PreferredHeight).Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this); Painted = ((ISolidColorBrush)brush!).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
