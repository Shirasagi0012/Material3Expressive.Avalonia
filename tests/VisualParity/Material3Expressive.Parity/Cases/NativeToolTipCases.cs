using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record NativeToolTipConfig(double ContentWidth = 64, double ContentHeight = 16,
    bool Dark = false, bool Rtl = false, bool Custom = false);

// Static surface parity only. Raw-input and popup integration are tested separately;
// the native placement and service intentionally retain Avalonia behavior.
[ParitySuite("native-tooltip")]
public sealed class NativeToolTipCases
{
    public static IEnumerable<CaseData<NativeToolTipConfig>> Configurations()
    {
        foreach (var size in new[] { (0d, 0d), (64d, 16d), (300d, 8d), (8d, 48d) })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"content.{size.Item1}-{size.Item2}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}",
                "Native ToolTip static surface", new(size.Item1, size.Item2, dark, rtl));
        foreach (var dark in new[] { false, true })
            yield return new($"custom.{(dark ? "dark" : "light")}", "Native ToolTip local appearance overrides", new(Dark: dark, Custom: true));
    }

    [ParityScenario("static", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NativeToolTipConfig>();
        await using var host = new AvaloniaHost(writer, 400, 240);
        var content = new Marker(config.ContentWidth, config.ContentHeight);
        var tip = new ToolTip { Content = content };
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        CaseDefaults.SetThemeColors(host, config.Dark);
        host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(config.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(config.Dark ? "#322F35" : "#F5EFF7");
        if (config.Custom) { tip.Background = Brush.Parse("#E0F0E8"); tip.Foreground = Brushes.Blue; tip.CornerRadius = new(12); }
        host.Mount(CaseDefaults.Center(tip));
        host.Frame(-1000); host.Frame(0);
        Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window)
            .Where(measurement => measurement.Name is "box" or "size").ToArray();
        var color = content.Painted;
        writer.Submit(host.Snapshot("initial", "Static native ToolTip surface",
            [new("tooltip", "tooltip", null, Geometry(tip)), new("content", "content", "tooltip",
                [.. Geometry(content), Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A],
                    "srgba8", "none", "properties", "tooltip.content.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")])]));
    }

    sealed class Marker(double width, double height) : Control
    {
        public Color Painted { get; private set; }
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this);
            Painted = ((ISolidColorBrush)brush!).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
