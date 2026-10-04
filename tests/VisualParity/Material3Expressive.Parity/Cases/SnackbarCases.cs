using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SnackbarConfig(bool HasAction = true, bool HasDismiss = false, bool NewLine = false,
    bool Dark = false, bool Rtl = false, bool Buttons = false, double ContentWidth = 64, double ContentHeight = 12,
    double DismissWidth = 24, int HostWidth = 720, double? Width = null, double? Height = null,
    double MinimumWidth = 0, string Colors = "default", string Mutation = "none");

[ParitySuite("snackbar")]
public sealed class SnackbarCases
{
    public static IEnumerable<CaseData<SnackbarConfig>> StaticConfigurations()
    {
        foreach (var action in new[] { false, true })
        foreach (var dismiss in new[] { false, true })
        foreach (var newLine in action ? new[] { false, true } : new[] { false })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.slots.{action}.{dismiss}.{newLine}.{dark}.{rtl}".ToLowerInvariant(), new(action, dismiss, newLine, dark, rtl));
        yield return Data("static.new-line.no-action", new(HasAction: false, NewLine: true));
        yield return Data("static.width.narrow", new(HasDismiss: true, HostWidth: 160));
        yield return Data("static.width.narrow-new-line", new(HasDismiss: true, NewLine: true, HostWidth: 160));
        yield return Data("static.width.explicit-small", new(Width: 180));
        yield return Data("static.width.explicit-large", new(Width: 680, HasDismiss: true));
        yield return Data("static.width.minimum", new(MinimumWidth: 680));
        yield return Data("static.height.small", new(Height: 24, HasDismiss: true));
        yield return Data("static.height.large", new(Height: 120, HasDismiss: true));
        yield return Data("static.content.empty", new(HasAction: false, ContentWidth: 0, ContentHeight: 0));
        yield return Data("static.content.tall", new(HasAction: false, ContentHeight: 80));
        yield return Data("static.dismiss.zero-width", new(HasDismiss: true, DismissWidth: 0));
        foreach (var colors in new[] { "custom", "surface", "transparent" }) yield return Data($"static.colors.{colors}", new(HasDismiss: true, Colors: colors));
        foreach (var newLine in new[] { false, true }) yield return Data($"static.buttons.{(newLine ? "new-line" : "row")}", new(HasDismiss: true, NewLine: newLine, Buttons: true));
    }
    public static IEnumerable<CaseData<SnackbarConfig>> DynamicConfigurations()
    {
        foreach (var mutation in new[] { "slots", "new-line", "resize", "colors", "theme" }) yield return Data($"dynamic.{mutation}", new(Mutation: mutation));
        foreach (var mutation in new[] { "action", "dismiss" }) yield return Data($"dynamic.{mutation}", new(HasDismiss: true, Buttons: true, Mutation: mutation));
    }
    private static CaseData<SnackbarConfig> Data(string id, SnackbarConfig config) => new(id, id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))] public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))] public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<SnackbarConfig>();
        await using var host = new AvaloniaHost(writer, config.HostWidth, 240);
        var content = new Marker(config.ContentWidth, config.ContentHeight);
        var action = new Marker(config.Buttons ? 40 : 80, config.Buttons ? 16 : 24);
        var dismiss = new Marker(config.DismissWidth, 24);
        var snackbar = new Snackbar { Content = content, ActionOnNewLine = config.NewLine, MinWidth = config.MinimumWidth,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        if (config.Width is { } width) snackbar.Width = width;
        if (config.Height is { } height) snackbar.Height = height;
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Theme(config.Dark); Colors(config.Colors);
        host.Mount(new Grid { Background = Brushes.White, Children = { snackbar } });
        var actionButton = new Button { Content = action, Padding = new Thickness(12, 8),
            Foreground = snackbar.ActionForeground, Theme = (ControlTheme)snackbar.FindResource("MaterialSnackbarActionButtonTheme")! };
        var dismissButton = new IconButton { Foreground = snackbar.DismissActionForeground,
            Theme = (ControlTheme)snackbar.FindResource("MaterialSnackbarDismissButtonTheme")! };
        var actions = 0; var dismissals = 0;
        actionButton.Click += (_, _) => actions++; dismissButton.Click += (_, _) => dismissals++;
        Slots(config.HasAction, config.HasDismiss);
        host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("snackbar", "snackbar", null,
                [.. Geometry(snackbar), Measurement.Numeric("actions", actions, "count", definition: "snackbar.action-callback-count"),
                 Measurement.Numeric("dismissals", dismissals, "count", definition: "snackbar.dismiss-callback-count")]) };
            Add("content", content);
            if (snackbar.Action is not null) Add("action", action);
            if (snackbar.DismissAction is not null)
            {
                if (config.Buttons) points.Add(new("dismiss", "dismiss", "snackbar", Geometry(dismissButton)));
                else Add("dismiss", dismiss);
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
            void Add(string name, Marker marker)
            {
                var color = marker.Painted;
                points.Add(new(name, name, "snackbar", [.. Geometry(marker), Measurement.Vector("paint", "color", [color.R, color.G, color.B, color.A],
                    "srgba8", "none", "properties", "snackbar.slot.painted-solid-srgba8", "Marker.Render inherited TextElement.Foreground")]));
            }
        }
        Sample("initial");
        if (!dynamic) return;
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Frame(time); host.Draw(); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        switch (config.Mutation)
        {
            case "slots": Change(1000, "dismiss-only", () => Slots(false, true)); Change(2000, "both", () => Slots(true, true)); Change(3000, "none", () => Slots(false, false)); break;
            case "new-line": Change(1000, "new-line", () => snackbar.ActionOnNewLine = true); Change(2000, "row", () => snackbar.ActionOnNewLine = false); break;
            case "resize": Change(1000, "wide", () => content.Size = new(400, 12)); Change(2000, "tall", () => content.Size = new(20, 80)); break;
            case "colors": Change(1000, "custom", () => Colors("custom")); Change(2000, "default", () => Colors("default")); break;
            case "theme": Change(1000, "dark", () => Theme(true)); Change(2000, "light", () => Theme(false)); break;
            case "action": case "dismiss":
                var target = config.Mutation == "action" ? (Control)action : dismissButton;
                var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
                Change(1000, "down", () => { host.Input("move", point); host.Input("down", point); });
                Times(16000, 32000, 64000, 112000);
                Change(113000, "up", () => host.Input("up", point)); Times(128000, 176000, 320000, 640000);
                break;
        }
        void Slots(bool hasAction, bool hasDismiss)
        {
            snackbar.Action = hasAction ? config.Buttons ? actionButton : action : null;
            snackbar.DismissAction = hasDismiss ? config.Buttons ? dismissButton : dismiss : null;
        }
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(dark ? "#E6E0E9" : "#322F35");
            host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(dark ? "#322F35" : "#F5EFF7");
            host.Window.Resources[Sys.Color.InversePrimary] = Color.Parse(dark ? "#6750A4" : "#D0BCFF");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.Shadow] = Avalonia.Media.Colors.Black;
        }
        void Colors(string colors)
        {
            snackbar.ClearValue(Snackbar.BackgroundProperty); snackbar.ClearValue(Snackbar.ForegroundProperty);
            snackbar.ClearValue(Snackbar.ActionForegroundProperty); snackbar.ClearValue(Snackbar.DismissActionForegroundProperty); snackbar.ClearValue(Snackbar.CornerRadiusProperty);
            if (colors == "custom") { snackbar.Background = Brush.Parse("#E0F0E8"); snackbar.Foreground = Brushes.Red; snackbar.ActionForeground = Brushes.Blue; snackbar.DismissActionForeground = Brush.Parse("#00FF00"); snackbar.CornerRadius = new(12); }
            if (colors == "surface") snackbar.Background = Brush.Parse("#FEF7FF");
            if (colors == "transparent") snackbar.Background = Brushes.Transparent;
        }
    }
    private sealed class Marker(double width, double height) : Control
    {
        private Size _size = new(width, height);
        public Size Size { get => _size; set { _size = value; InvalidateMeasure(); } }
        public Color Painted { get; private set; }
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => Size.Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            var brush = TextElement.GetForeground(this); Painted = ((ISolidColorBrush)brush!).Color;
            context.DrawRectangle(brush, null, new Rect(Bounds.Size));
        }
    }
}
