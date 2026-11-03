using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TooltipLabelConfig(bool Persistent = true, bool Dark = false, bool Rtl = false,
    int X = 112, int Y = 80, double ContentWidth = 64, double ContentHeight = 8, string Action = "none");

[ParitySuite("tooltip-label")]
public sealed class TooltipLabelCases
{
    public static IEnumerable<CaseData<TooltipLabelConfig>> StaticConfigurations()
    {
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var position in new[] { (112, 80), (4, 4), (232, 168) })
            yield return Data($"static.{position.Item1}-{position.Item2}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}",
                new(Dark: dark, Rtl: rtl, X: position.Item1, Y: position.Item2));
        yield return Data("static.inactive", new(Persistent: false));
        foreach (var size in new[] { (4d, 8d), (300d, 8d), (16d, 64d) })
            yield return Data($"static.content.{size.Item1}-{size.Item2}", new(ContentWidth: size.Item1, ContentHeight: size.Item2));
    }

    public static IEnumerable<CaseData<TooltipLabelConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "overlap", "cancel", "identity", "focus", "source", "outside", "persistent", "resize", "transform", "detach" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}",
                new(Persistent: action is "persistent" or "resize" or "transform", Rtl: rtl, Action: action));
    }

    static CaseData<TooltipLabelConfig> Data(string id, TooltipLabelConfig config) => new(id, id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer);

    static async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<TooltipLabelConfig>();
        await using var host = new AvaloniaHost(writer, 256, 192);
        CaseDefaults.SetThemeColors(host, config.Dark);
        host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(config.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(config.Dark ? "#322F35" : "#F5EFF7");
        var source = new MaterialInteractionSource();
        var marker = new Marker { PreferredSize = new Size(config.ContentWidth, config.ContentHeight) };
        var tip = new PlainTooltip { Content = marker };
        var anchor = new Border { Width = 24, Height = 24, Background = Brushes.Blue };
        // The scene positions anchors in physical coordinates (Compose absoluteOffset). Only the
        // component inherits RTL; making the outer Canvas RTL would mirror the scene's input X.
        var label = new TooltipLabel { Label = tip, Content = anchor, InteractionSource = source, IsPersistent = config.Persistent,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var canvas = new Canvas { Background = Brushes.White, Children = { label } };
        Canvas.SetLeft(label, config.X); Canvas.SetTop(label, config.Y);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("state", "state", null,
                [Measurement.Flag("visible", label.IsPresented, definition: "label.popup.attached")]) };
            if (label.IsAttachedToVisualTree()) points.Add(new("anchor", "anchor", null, Geometry(anchor)));
            if (tip.IsAttachedToVisualTree())
            {
                points.Add(new("tooltip", "tooltip", null, Geometry(tip)));
                var color = marker.Painted;
                points.Add(new("content", "content", "tooltip", [.. Geometry(marker),
                    Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties",
                        "label.content.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        long time = 0;
        void Change(string id, Action action)
        { time += 16000; host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id); }
        var hover = new MaterialInteractionEvent.HoverEnter();
        var press = new MaterialInteractionEvent.Press(default);
        var drag = new MaterialInteractionEvent.DragStart();
        void Emit(string id, MaterialInteractionEvent interaction) => Change(id, () => source.Emit(interaction));
        switch (config.Action)
        {
            case "overlap":
                Emit("hover", hover); Emit("press", press); Emit("drag", drag);
                Emit("hover-exit", new MaterialInteractionEvent.HoverExit(hover));
                Emit("release", new MaterialInteractionEvent.Release(press));
                Emit("drag-stop", new MaterialInteractionEvent.DragStop(drag)); break;
            case "cancel":
                Emit("press", press); Emit("cancel-press", new MaterialInteractionEvent.CancelPress(press));
                Emit("drag", drag); Emit("cancel-drag", new MaterialInteractionEvent.DragCancel(drag)); break;
            case "identity":
                Emit("press", press); Emit("unmatched-release", new MaterialInteractionEvent.Release(new(default)));
                Emit("duplicate-start", press); Emit("first-release", new MaterialInteractionEvent.Release(press));
                Emit("second-release", new MaterialInteractionEvent.Release(press)); break;
            case "focus":
                var focus = new MaterialInteractionEvent.Focus(); Emit("focus", focus);
                Emit("hover", hover); Emit("unfocus", new MaterialInteractionEvent.Unfocus(focus));
                Emit("hover-exit", new MaterialInteractionEvent.HoverExit(hover)); break;
            case "source":
                Emit("hover", hover); var old = source;
                Change("replace-source", () => label.InteractionSource = source = new());
                Change("old-source", () => old.Emit(drag)); Emit("new-source", drag);
                Emit("drag-stop", new MaterialInteractionEvent.DragStop(drag)); break;
            case "outside":
                Emit("hover", hover);
                Change("outside-down", () => { host.Input("move", new Point(8, 180)); host.Input("down", new Point(8, 180)); });
                Change("outside-up", () => host.Input("up", new Point(8, 180)));
                Emit("still-active", drag); Emit("hover-exit", new MaterialInteractionEvent.HoverExit(hover));
                Emit("drag-stop", new MaterialInteractionEvent.DragStop(drag)); Emit("reenter", hover); break;
            case "persistent":
                Emit("ignored-press", press);
                Change("outside-down", () => { host.Input("move", new Point(8, 180)); host.Input("down", new Point(8, 180)); });
                Change("outside-up", () => host.Input("up", new Point(8, 180)));
                Change("nonpersistent", () => label.IsPersistent = false); Emit("hover", hover);
                Change("persistent", () => label.IsPersistent = true); break;
            case "resize":
                Change("move", () => { Canvas.SetLeft(label, 4); Canvas.SetTop(label, 4); });
                Change("grow", () => { anchor.Width = 40; marker.PreferredSize = new Size(120, config.ContentHeight); });
                Change("bottom-right", () => { Canvas.SetLeft(label, 212); Canvas.SetTop(label, 164); }); break;
            case "transform":
                Change("scale", () => { label.RenderTransformOrigin = RelativePoint.Center; label.RenderTransform = new ScaleTransform(1.5, 1.25); });
                Change("translate", () => label.RenderTransform = new TransformGroup { Children = { new ScaleTransform(1.5, 1.25), new TranslateTransform(8, 12) } });
                Change("reset", () => label.RenderTransform = null); break;
            case "detach":
                Emit("hover", hover); Change("detach", () => canvas.Children.Remove(label));
                Emit("detached-drag", drag); Change("reattach", () => canvas.Children.Add(label));
                Emit("press", press); Emit("release", new MaterialInteractionEvent.Release(press)); break;
        }
    }

    sealed class Marker : Control
    {
        public static readonly StyledProperty<Size> PreferredSizeProperty = AvaloniaProperty.Register<Marker, Size>(nameof(PreferredSize));
        public Size PreferredSize { get => GetValue(PreferredSizeProperty); set => SetValue(PreferredSizeProperty, value); }
        static Marker() { AffectsMeasure<Marker>(PreferredSizeProperty); }
        protected override Size MeasureOverride(Size availableSize) => PreferredSize.Constrain(availableSize);
        public Color Painted { get; private set; }
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this); Painted = ((ISolidColorBrush)brush!).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
