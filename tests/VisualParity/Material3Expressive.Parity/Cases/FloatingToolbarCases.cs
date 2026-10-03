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

public sealed record FloatingToolbarConfig(bool Horizontal = true, string Fab = "none", bool Expanded = true,
    bool Vibrant = false, bool Dark = false, bool Rtl = false, bool Leading = true, bool Trailing = true,
    double ContentWidth = 48, double ContentHeight = 48, double? MaxWidth = null, double? MaxHeight = null,
    string Content = "glyph", string Custom = "none", string Action = "none", string Exit = "bottom");

[ParitySuite("floating-toolbar")]
public sealed class FloatingToolbarCases
{
    public static IEnumerable<CaseData<FloatingToolbarConfig>> StaticConfigurations()
    {
        // Every supported orientation / overload / style / state / theme / direction combination.
        foreach (var horizontal in new[] { true, false })
        foreach (var fab in new[] { "none", "start", "end" })
        foreach (var expanded in new[] { true, false })
        foreach (var vibrant in new[] { true, false })
        foreach (var dark in new[] { true, false })
        foreach (var rtl in new[] { true, false })
            yield return Data($"static.appearance.{Axis(horizontal)}.{fab}.{(expanded ? "expanded" : "collapsed")}.{(vibrant ? "vibrant" : "standard")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", new(horizontal, fab, expanded, vibrant, dark, rtl));
        foreach (var horizontal in new[] { true, false })
        {
            foreach (var sides in new[] { "neither", "leading", "trailing" })
                yield return Data($"static.slots.{Axis(horizontal)}.{sides}", new(horizontal, Leading: sides == "leading", Trailing: sides == "trailing"));
            foreach (var fab in new[] { "none", "start", "end" })
            {
                yield return Data($"static.layout.{Axis(horizontal)}.{fab}.constrained", new(horizontal, fab, MaxWidth: horizontal ? 88 : null, MaxHeight: horizontal ? null : 88));
                yield return Data($"static.layout.{Axis(horizontal)}.{fab}.overflow", new(horizontal, fab, ContentWidth: horizontal ? 320 : 48, ContentHeight: horizontal ? 48 : 320,
                    MaxWidth: horizontal ? 176 : null, MaxHeight: horizontal ? null : 176));
                yield return Data($"static.layout.{Axis(horizontal)}.{fab}.empty", new(horizontal, fab, Content: "empty", Leading: false, Trailing: false));
            }
            foreach (var custom in new[] { "padding", "shape", "colors", "elevation" })
                yield return Data($"static.custom.{Axis(horizontal)}.{custom}", new(horizontal, Custom: custom));
            yield return Data($"static.interactive-padding.{Axis(horizontal)}", new(horizontal, Expanded: false, Content: "button"));
        }
    }
    public static IEnumerable<CaseData<FloatingToolbarConfig>> DynamicConfigurations()
    {
        foreach (var horizontal in new[] { true, false })
        foreach (var fab in new[] { "none", "start", "end" })
        foreach (var action in new[] { "expand", "collapse", "reverse" })
            yield return Data($"dynamic.{Axis(horizontal)}.{fab}.{action}", new(horizontal, fab, Expanded: action != "expand", Action: action));
        foreach (var horizontal in new[] { true, false })
        {
            yield return Data($"dynamic.{Axis(horizontal)}.theme", new(horizontal, "end", Action: "theme"));
            yield return Data($"dynamic.{Axis(horizontal)}.balanced-padding", new(horizontal, Content: "button", Action: "reverse"));
            foreach (var exit in new[] { "start", "end", "top", "bottom" })
            foreach (var rtl in new[] { true, false })
                yield return Data($"dynamic.{Axis(horizontal)}.scroll.{exit}.{(rtl ? "rtl" : "ltr")}", new(horizontal, Rtl: rtl, Action: "scroll", Exit: exit));
            yield return Data($"dynamic.{Axis(horizontal)}.snap", new(horizontal, Action: "snap"));
            yield return Data($"dynamic.{Axis(horizontal)}.fling", new(horizontal, Action: "fling"));
        }
    }
    static string Axis(bool horizontal) => horizontal ? "horizontal" : "vertical";
    static CaseData<FloatingToolbarConfig> Data(string id, FloatingToolbarConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 400, 440);
        var scene = new Scene(host, writer.Metadata.ReadConfig<FloatingToolbarConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<FloatingToolbarConfig>();
        await using var host = new AvaloniaHost(writer, 400, 440);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        if (config.Action == "theme")
        {
            Change(1000, "dark", () => scene.SetTheme(true)); At(64000);
            Change(65000, "light", () => scene.SetTheme(false)); At(160000);
        }
        else if (config.Action is "scroll" or "snap" or "fling")
        {
            Change(1000, "scroll", () => scene.Toolbar.ScrollBehavior!.OnPostScroll(-40));
            if (config.Action == "scroll") Change(65000, "scroll-back", () => scene.Toolbar.ScrollBehavior!.OnPostScroll(40));
            else Change(2000, "settle", () => scene.Settling = scene.Toolbar.ScrollBehavior!.OnPostFlingAsync(config.Action == "fling" ? -400 : 0));
            At(96000); At(160000); At(320000); At(800000);
            if (scene.Settling is not null) await scene.Settling;
        }
        else
        {
            Change(1000, "state-change", () => scene.Toolbar.Expanded = !scene.Toolbar.Expanded); At(16000); At(64000);
            if (config.Action == "reverse") Change(65000, "reverse", () => scene.Toolbar.Expanded = !scene.Toolbar.Expanded);
            At(96000); At(160000); At(320000); At(800000);
        }
        host.AdvanceTo(900000); Sample("passive-idle", false);
    }
    sealed class Scene
    {
        readonly AvaloniaHost host; readonly FloatingToolbarConfig config;
        public FloatingToolbar Toolbar { get; }
        public Control Root { get; }
        public Task<float>? Settling { get; set; }
        readonly Control content;
        readonly FloatingActionButton? fab;
        public Scene(AvaloniaHost host, FloatingToolbarConfig config)
        {
            this.host = host; this.config = config; SetTheme(config.Dark);
            host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Toolbar = config.Horizontal ? new HorizontalFloatingToolbar { FloatingActionButtonPosition = config.Fab == "start" ? FloatingToolbarHorizontalFabPosition.Start : FloatingToolbarHorizontalFabPosition.End }
                : new VerticalFloatingToolbar { FloatingActionButtonPosition = config.Fab == "start" ? FloatingToolbarVerticalFabPosition.Top : FloatingToolbarVerticalFabPosition.Bottom };
            Toolbar.Expanded = config.Expanded;
            if (config.Vibrant) Toolbar.Classes.Add("vibrant");
            content = config.Content switch { "empty" => new Panel(), "button" => new Button { Content = new Panel() }, _ => Glyph(config.ContentWidth, config.ContentHeight) };
            Toolbar.Content = content;
            if (config.Fab == "none")
            {
                if (config.Leading) Toolbar.LeadingContent = Glyph(24, 24);
                if (config.Trailing) Toolbar.TrailingContent = Glyph(24, 24);
            }
            else
            {
                fab = new FloatingActionButton { Content = Glyph(24, 24) };
                fab.Theme = (ControlTheme)host.Window.FindResource(config.Vibrant ? "MaterialVibrantFloatingToolbarFabTheme" : "MaterialStandardFloatingToolbarFabTheme")!;
                Toolbar.FloatingActionButton = fab;
            }
            if (config.MaxWidth is { } width) Toolbar.MaxWidth = width;
            if (config.MaxHeight is { } height) Toolbar.MaxHeight = height;
            if (config.Custom == "padding") Toolbar.Padding = new Thickness(4, 12, 16, 8);
            if (config.Custom == "shape") Toolbar.CornerRadius = new CornerRadius(8);
            if (config.Custom == "colors") { Toolbar.Background = Brushes.Navy; Toolbar.Foreground = Brushes.Lime; }
            if (config.Custom == "elevation") { Toolbar.ExpandedShadowElevation = 3; Toolbar.CollapsedShadowElevation = 1; }
            if (config.Action is "scroll" or "snap" or "fling") Toolbar.ScrollBehavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(Enum.Parse<FloatingToolbarExitDirection>(config.Exit, true));
            Root = new SceneCenter { Child = Toolbar };
        }
        // Modifier.size requests an intrinsic size which the parent's constraints can
        // reduce. Native Control.Width instead forces overflow during ArrangeCore.
        static Control Glyph(double width, double height) => new ToolbarGlyph(new Size(width, height));
        public void SetTheme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
            host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse(dark ? "#4F378B" : "#EADDFF");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(dark ? "#EADDFF" : "#21005D");
            host.Window.Resources[Sys.Color.TertiaryContainer] = Color.Parse(dark ? "#633B48" : "#FFD8E4");
            host.Window.Resources[Sys.Color.OnTertiaryContainer] = Color.Parse(dark ? "#FFD8E4" : "#31111D");
        }
        public ProbePoint[] Measure()
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var fields = Geometry(Toolbar).ToList();
            if (Toolbar.ScrollBehavior is { } behavior)
            {
                fields.Add(Measurement.Numeric("offset", behavior.State.Offset, "dip", category: "motion", definition: "floating-toolbar.scroll-offset"));
                fields.Add(Measurement.Numeric("offsetLimit", behavior.State.OffsetLimit, "dip", definition: "floating-toolbar.scroll-limit"));
                fields.Add(Measurement.Numeric("contentOffset", behavior.State.ContentOffset, "dip", definition: "floating-toolbar.content-offset"));
            }
            var points = new List<ProbePoint> { new("toolbar", "toolbar", null, [..fields]) };
            if (config.Action is not ("scroll" or "snap" or "fling")) points.Add(new("content", "content", "toolbar", Geometry(content)));
            if (fab is not null) points.Add(new("fab", "fab", "toolbar", Geometry(fab)));
            return [..points];
        }
    }
    sealed class ToolbarGlyph(Size natural) : Control
    {
        static ToolbarGlyph() => AffectsRender<ToolbarGlyph>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size available) => natural;
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
    sealed class SceneCenter : Decorator
    {
        protected override Size MeasureOverride(Size available) { Child?.Measure(available); return available; }
        protected override Size ArrangeOverride(Size final)
        {
            var size = Child?.DesiredSize ?? default; var scale = LayoutHelper.GetLayoutScale(this);
            Child?.Arrange(new Rect(Math.Floor((final.Width - size.Width) * scale / 2 + .5) / scale,
                Math.Floor((final.Height - size.Height) * scale / 2 + .5) / scale, size.Width, size.Height));
            return final;
        }
    }
}
