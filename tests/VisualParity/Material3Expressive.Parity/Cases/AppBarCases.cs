using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Material3Expressive.Components;
using Material3Expressive.TestSupport;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;
using ComposeAnimation;

namespace Material3Expressive.Parity;

public sealed record AppBarConfig(string Variant = "small", string Alignment = "start", bool Subtitle = false,
    bool Rtl = false, double Width = 360, double TitleWidth = 100, double TitleHeight = 20,
    double? ExpandedHeight = null, double? CollapsedHeight = null, double[]? Insets = null, double[]? Padding = null,
    double[]? Consumed = null, double Fraction = 0, int Actions = 2, bool Navigation = true, bool Fab = false,
    bool Custom = false, string Arrangement = "start", double Spacing = 8, bool Dark = false,
    double Elevation = 0, string Behavior = "exit", string Action = "none", double Velocity = 0, bool NoSpecs = false);

[ParitySuite("appbar")]
public sealed class AppBarCases
{
    public static IEnumerable<CaseData<AppBarConfig>> StaticConfigurations()
    {
        yield return Data("static.appearance.small", new());
        yield return Data("static.appearance.center", new("center", "center"));
        foreach (var variant in new[] { "small", "mediumFlexible", "largeFlexible", "twoRows" })
        foreach (var subtitle in variant == "small" ? new[] { true } : new[] { false, true })
        foreach (var alignment in new[] { "start", "center", "end" })
            yield return Data($"static.appearance.{variant}.{alignment}.{(subtitle ? "subtitle" : "title")}", new(variant, alignment, subtitle));
        foreach (var variant in new[] { "medium", "large" }) yield return Data($"static.appearance.{variant}", new(variant));
        foreach (var variant in new[] { "medium", "large", "mediumFlexible", "largeFlexible", "twoRows" })
        foreach (var fraction in new[] { .5, 1 })
            yield return Data($"static.collapse.{variant}.{fraction}", new(variant, Fraction: fraction));
        foreach (var variant in new[] { "small", "center", "medium", "large", "mediumFlexible", "largeFlexible", "twoRows" })
        {
            yield return Data($"static.layout.{variant}.rtl-insets", new(variant, variant == "center" ? "center" : "start", Rtl: true, Insets: [8, 24, 12, 0]));
            yield return Data($"static.layout.{variant}.overflow-title", new(variant, variant == "center" ? "center" : "start", TitleHeight: 180, Width: 200));
        }
        yield return Data("static.layout.single.padding", new(Subtitle: true, Padding: [8, 6, 12, 4]));
        yield return Data("static.layout.center.asymmetric-actions", new("center", "center", Actions: 4));
        yield return Data("static.layout.small.no-slots", new(Navigation: false, Actions: 0));
        yield return Data("static.insets.consumed", new("mediumFlexible", Subtitle: true, Insets: [16, 30, 24, 0], Consumed: [4, 12, 8, 0]));
        yield return Data("static.color.dark", new("largeFlexible", Subtitle: true, Dark: true, Fraction: .5));
        foreach (var fab in new[] { false, true }) yield return Data($"static.bottom.normal.{(fab ? "fab" : "actions")}", new("bottom", Fab: fab, Actions: 3));
        foreach (var arrangement in new[] { "start", "center", "end", "spaceBetween", "spaceAround", "spaceEvenly", "spacedBy" })
        {
            yield return Data($"static.bottom.flexible.{arrangement}", new("flexibleBottom", Arrangement: arrangement, Actions: 3));
            yield return Data($"static.bottom.narrow.{arrangement}", new("flexibleBottom", Arrangement: arrangement, Actions: 4, Width: 140));
        }
        yield return Data("static.bottom.spacedby.single", new("flexibleBottom", Arrangement: "spacedBy", Actions: 1));
        yield return Data("static.bottom.spacedby.negative", new("flexibleBottom", Arrangement: "spacedBy", Actions: 4, Spacing: -8, Width: 180));
        foreach (var variant in new[] { "bottom", "flexibleBottom" })
        {
            yield return Data($"static.bottom.{variant}.custom", new(variant, Custom: true, Fab: true, Padding: [12, 8, 16, 4]));
            yield return Data($"static.bottom.{variant}.rtl-insets", new(variant, Rtl: true, Insets: [8, 0, 12, 20], Actions: 3));
            yield return Data($"static.bottom.{variant}.collapsed", new(variant, Fraction: .5));
            yield return Data($"static.bottom.{variant}.tonal", new(variant, Elevation: variant == "bottom" ? 6 : 0, Dark: true));
        }
    }
    private static CaseData<AppBarConfig> Data(string id, AppBarConfig config) => new(id.ToLowerInvariant(), id, config);
    public static IEnumerable<CaseData<AppBarConfig>> DynamicConfigurations()
    {
        foreach (var behavior in new[] { "pinned", "enter", "exit", "bottom" })
            yield return Data($"dynamic.scroll.{behavior}", new(behavior == "bottom" ? "bottom" : "medium", Behavior: behavior, Action: "scroll"));
        foreach (var variant in new[] { "medium", "bottom" })
        {
            foreach (var fraction in new[] { .25, .75 })
                yield return Data($"dynamic.snap.{variant}.{fraction}", new(variant, Fraction: fraction, Action: "fling"));
            foreach (var velocity in new[] { -600.0, 600.0 })
                yield return Data($"dynamic.fling.{variant}.{velocity}", new(variant, Fraction: .4, Velocity: velocity, Action: "fling"));
            yield return Data($"dynamic.interrupt.{variant}", new(variant, Fraction: .4, Velocity: -600, Action: "interrupt"));
            yield return Data($"dynamic.no-specs.{variant}", new(variant, Fraction: .4, Velocity: -600, Action: "fling", NoSpecs: true));
            foreach (var fraction in new[] { 0.0, .005, 1 })
                yield return Data($"dynamic.terminal.{variant}.{fraction}", new(variant, Fraction: fraction, Velocity: 600, Action: "fling"));
        }
        yield return Data("dynamic.color.single", new(Action: "colors"));
        yield return Data("dynamic.color.two-rows", new("largeFlexible", Subtitle: true, Action: "colors"));
        yield return Data("dynamic.layout", new("mediumFlexible", Subtitle: true, Insets: [8, 24, 12, 0], Action: "layout"));
        foreach (var variant in new[] { "bottom", "flexibleBottom" })
            yield return Data($"dynamic.custom.{variant}", new(variant, Fab: variant == "bottom", Action: "custom"));
        foreach (var variant in new[] { "medium", "bottom" })
        foreach (var action in new[] { "drag", "drag-fling" })
            yield return Data($"dynamic.input.{variant}.{action}", new(variant, Action: action, Fraction: variant == "bottom" ? .4 : 0));
        yield return Data("dynamic.input.pinned", new("medium", Behavior: "pinned", Action: "drag"));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 420, 300);
        var scene = new Scene(host, writer.Metadata.ReadConfig<AppBarConfig>());
        host.Mount(scene.Scope); scene.SetFraction(); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 420, 300);
        var c = writer.Metadata.ReadConfig<AppBarConfig>(); var scene = new Scene(host, c);
        host.Mount(scene.Scope); scene.SetFraction(); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(true), image));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        // Apply the mutation before the composition/animation frame, then place/draw it.
        // A pre-frame Draw would apply Avalonia properties immediately while Compose
        // configuration still awaited this frame's recomposition.
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        switch (c.Action)
        {
            case "scroll":
                Change(1000, "up-pre", () => scene.Scroll(-25, -25));
                Change(17000, "up-boundary", () => scene.Scroll(-50, -50));
                Change(33000, "up-body", () => scene.Scroll(-40, -32));
                Change(49000, "down-body", () => scene.Scroll(20, 20));
                Change(65000, "down-boundary", () => scene.Scroll(50, 10));
                Change(81000, "cannot-scroll", () => { scene.CanScroll = false; scene.Scroll(-25, -25); });
                Change(97000, "at-start", () => { scene.CanScroll = true; scene.AtStart = true; scene.Scroll(30, 0); });
                Times(160000, 400000); break;
            case "fling": case "interrupt":
                Change(1000, "post-fling", () => scene.Fling((float)c.Velocity));
                Times(16000, 32000, 64000);
                if (c.Action == "interrupt") Change(65000, "interrupted", scene.Scope.Interrupt);
                Times(96000, 160000, 240000, 400000, 800000); break;
            case "colors":
                Change(1000, "overlap", () => scene.Scroll(-30, -30)); Times(16000, 64000);
                Change(65000, "color-target", () => scene.SetColors(true)); Times(96000, 160000, 240000, 400000);
                Change(401000, "color-restore", () => scene.SetColors(false)); Times(416000, 480000, 640000, 800000); break;
            case "layout":
                Change(1000, "half", () => scene.SetFraction(.5f));
                Change(17000, "rtl", () => scene.Root.FlowDirection = FlowDirection.RightToLeft);
                Change(33000, "insets", () => ((TopAppBar)scene.Bar).WindowInsets = new(16, 30, 24, 0));
                Change(49000, "height", () => ((TopAppBar)scene.Bar).ExpandedHeight = 160);
                Change(65000, "restore", () => { ((TopAppBar)scene.Bar).ExpandedHeight = double.NaN; scene.SetFraction(0); }); break;
            case "custom":
                Change(1000, "replace", () => ((BottomAppBar)scene.Bar).CustomContent = new Fill { Children = { scene.Make("custom", 80, 24) } });
                Change(17000, "padding", () => ((BottomAppBar)scene.Bar).ContentPadding = new(12, 8, 16, 4));
                Change(33000, "restore", () => ((BottomAppBar)scene.Bar).CustomContent = null); break;
            case "drag": case "drag-fling":
                var origin = new Point(300, c.Variant == "bottom" ? 70 : 100);
                void Input(long time, string action, Point point)
                {
                    Advance(time); scene.Input(action, point); writer.Record(time, "input", $"{action} ({point.X},{point.Y})");
                    host.Frame(time); Sample($"{action}-{time}");
                }
                Input(2000, "down", origin); Input(18000, "move", origin + new Point(0, -12)); Input(34000, "move", origin + new Point(0, -28));
                if (c.Action == "drag-fling") Input(50000, "move", origin + new Point(0, -44));
                Input(c.Action == "drag" ? 90000 : 51000, "up", origin + new Point(0, c.Action == "drag" ? -28 : -44));
                Times(96000, 160000, 240000, 400000, 800000); break;
        }
        Advance(900000); Sample("idle", false);
    }
    private sealed class Scene
    {
        private bool _pressed;
        private readonly AvaloniaHost _host;
        private readonly AppBarConfig _config;
        private readonly Dictionary<string, Marker> _markers = [];
        private readonly TopAppBarState _top = new();
        private readonly BottomAppBarState _bottom = new();
        internal Grid Root { get; }
        internal Control Bar { get; }
        internal AppBarScrollScope Scope { get; }
        internal bool CanScroll
        {
            get => Bar is TopAppBar top ? top.ScrollBehavior!.CanScroll : ((BottomAppBar)Bar).ScrollBehavior!.CanScroll;
            set
            {
                if (Bar is TopAppBar top) top.ScrollBehavior!.CanScroll = value;
                else ((BottomAppBar)Bar).ScrollBehavior!.CanScroll = value;
            }
        }
        internal bool AtStart
        {
            get => (Bar as TopAppBar)?.ScrollBehavior?.IsScrollingContentAtStart ?? false;
            set { if (Bar is TopAppBar top) top.ScrollBehavior!.IsScrollingContentAtStart = value; }
        }
        private Vector _consumed, _available, _flingResult;
        private Task<Vector>? _flingTask;
        private static Thickness Thickness(double[]? values) => values is null ? default : new(values[0], values[1], values[2], values[3]);
        internal Scene(AvaloniaHost host, AppBarConfig config)
        {
            _host = host; _config = config;
            CaseDefaults.SetThemeColors(host, config.Dark);
            host.Window.Resources[MaterialTokens.Sys.Color.Surface] = Color.Parse(config.Dark ? "#141218" : "#FFF7FF");
            host.Window.Resources[MaterialTokens.Sys.Color.SurfaceContainer] = Color.Parse(config.Dark ? "#211F26" : "#F3EDF7");
            host.Window.Resources[MaterialTokens.Sys.Color.SurfaceTint] = Color.Parse(config.Dark ? "#D0BCFF" : "#6750A4");
            var bottom = config.Variant is "bottom" or "flexibleBottom";
            if (bottom)
            {
                BottomAppBar bar = config.Variant == "bottom" ? new BottomAppBar() : new FlexibleBottomAppBar(); Bar = bar;
                bar.ScrollBehavior = new BottomAppBarScrollBehavior { State = _bottom };
                if (config.NoSpecs)
                {
                    bar.ScrollBehavior.SnapAnimationSpec = null;
                    bar.ScrollBehavior.FlingAnimationSpec = null;
                }
                bar.WindowInsets = Thickness(config.Insets); bar.TonalElevation = config.Elevation;
                if (config.Padding is not null) bar.ContentPadding = Thickness(config.Padding);
                if (config.ExpandedHeight is { } height) bar.ExpandedHeight = height;
                bar.HorizontalArrangement = config.Arrangement switch { "center" => AppBarArrangement.Center, "end" => AppBarArrangement.End,
                    "spaceBetween" => AppBarArrangement.SpaceBetween, "spaceAround" => AppBarArrangement.SpaceAround,
                    "spaceEvenly" => AppBarArrangement.SpaceEvenly, "spacedBy" => AppBarArrangement.SpacedBy, _ => AppBarArrangement.Start };
                bar.Spacing = config.Spacing;
                if (config.Fab) bar.FloatingActionButton = Make("fab", 56, 56);
                if (config.Custom) bar.CustomContent = new Fill { Children = { Make("custom", 80, 24) } };
                for (var i = 0; i < config.Actions; i++) bar.Items.Add(Make($"action-{i}", 48, 48));
            }
            else
            {
                TopAppBar bar = config.Variant switch { "center" => new CenterAlignedTopAppBar(), "medium" => new MediumTopAppBar(),
                    "large" => new LargeTopAppBar(), "mediumFlexible" => new MediumFlexibleTopAppBar(), "largeFlexible" => new LargeFlexibleTopAppBar(),
                    "twoRows" => new TwoRowsTopAppBar(), _ => new TopAppBar() }; Bar = bar;
                bar.ScrollBehavior = new TopAppBarScrollBehavior(config.Behavior switch
                {
                    "pinned" => TopAppBarScrollMode.Pinned,
                    "enter" => TopAppBarScrollMode.EnterAlways,
                    _ => TopAppBarScrollMode.ExitUntilCollapsed,
                }) { State = _top };
                if (config.Behavior is "pinned" or "enter")
                    bar.ScrollBehavior.IsScrollingContentAtStart = false;
                if (config.NoSpecs)
                {
                    bar.ScrollBehavior.SnapAnimationSpec = null;
                    bar.ScrollBehavior.FlingAnimationSpec = null;
                }
                bar.TitleHorizontalAlignment = config.Alignment switch { "center" => HorizontalAlignment.Center, "end" => HorizontalAlignment.Right, _ => HorizontalAlignment.Left };
                bar.WindowInsets = Thickness(config.Insets); bar.ContentPadding = Thickness(config.Padding);
                if (config.ExpandedHeight is { } height) bar.ExpandedHeight = height;
                if (config.CollapsedHeight is { } collapsed) bar.CollapsedHeight = collapsed;
                var twoRows = config.Variant is not ("small" or "center");
                bar.Title = "data";
                bar.TitleTemplate = new FuncDataTemplate<string>((_, _) => Make("title", config.TitleWidth, config.TitleHeight));
                if (twoRows) bar.CollapsedTitleTemplate = new FuncDataTemplate<string>((_, _) => Make("collapsed-title", config.TitleWidth, config.TitleHeight));
                if (config.Subtitle)
                {
                    bar.Subtitle = "data"; bar.SubtitleTemplate = new FuncDataTemplate<string>((_, _) => Make("subtitle", 70, 12));
                    if (twoRows) bar.CollapsedSubtitleTemplate = new FuncDataTemplate<string>((_, _) => Make("collapsed-subtitle", 70, 12));
                }
                if (config.Navigation) bar.NavigationIcon = Make("navigation", 48, 48);
                for (var i = 0; i < config.Actions; i++) bar.Items.Add(Make($"action-{i}", 48, 48));
            }
            Bar.Width = config.Width; Bar.HorizontalAlignment = HorizontalAlignment.Left; Bar.VerticalAlignment = VerticalAlignment.Top;
            var parent = new Border { Padding = Thickness(config.Consumed), Child = Bar };
            MaterialWindowInsets.SetConsumePadding(parent, parent.Padding);
            Root = new Grid { Margin = new Thickness(16), FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { parent } };
            Scope = new AppBarScrollScope { Child = Root, TopScrollBehavior = (Bar as TopAppBar)?.ScrollBehavior, BottomScrollBehavior = (Bar as BottomAppBar)?.ScrollBehavior };
        }
        internal Marker Make(string id, double width, double height)
        {
            var marker = new Marker(width, height) { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }; _markers[id] = marker; return marker;
        }
        internal void SetFraction(float? value = null)
        {
            if (Bar is TopAppBar) _top.HeightOffset = _top.HeightOffsetLimit * (value ?? (float)_config.Fraction);
            else _bottom.HeightOffset = _bottom.HeightOffsetLimit * (value ?? (float)_config.Fraction);
        }
        internal void Scroll(float delta, float body)
        {
            var input = new Vector(0, delta * AvaloniaHost.RenderScale);
            _consumed = Scope.DispatchScroll(input, left => new Vector(0, Math.Sign(left.Y) * Math.Min(Math.Abs(left.Y), Math.Abs(body * AvaloniaHost.RenderScale))));
            _available = input - _consumed;
        }
        internal void Fling(float velocity)
        {
            _ = _host.Window.AnimationRoot.Launch(async token =>
            { _flingTask = Scope.DispatchFlingAsync(new Vector(0, velocity * AvaloniaHost.RenderScale), (_, _) => Task.FromResult(default(Vector)), _host.Window.AnimationRoot, token); _flingResult = await _flingTask; }, "AppBar parity fling");
        }
        internal void SetColors(bool custom)
        {
            var bar = (TopAppBar)Bar;
            if (custom)
            {
                bar.Background = new SolidColorBrush(Color.Parse("#FF336699"));
                bar.ScrolledContainerColor = Color.Parse("#FF228866");
                bar.NavigationIconContentColor = Color.Parse("#FFDD7722");
                bar.TitleContentColor = Color.Parse("#FF8866AA");
                bar.ActionIconContentColor = Color.Parse("#FF008877");
                bar.SubtitleContentColor = Color.Parse("#FF223344");
            }
            else
            {
                foreach (var property in new AvaloniaProperty[] { TopAppBar.BackgroundProperty, TopAppBar.ScrolledContainerColorProperty,
                    TopAppBar.NavigationIconContentColorProperty, TopAppBar.TitleContentColorProperty,
                    TopAppBar.ActionIconContentColorProperty, TopAppBar.SubtitleContentColorProperty }) bar.ClearValue(property);
            }
        }
        internal void Input(string action, Point position)
        {
            var platform = _host.Window.PlatformImpl!;
            var mouse = (IMouseDevice)platform.GetType().GetProperty("MouseDevice")!.GetValue(platform)!;
            var type = action switch { "down" => RawPointerEventType.LeftButtonDown, "up" => RawPointerEventType.LeftButtonUp, _ => RawPointerEventType.Move };
            var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), mouse,
                (ulong)(1000 + Math.Max(0, _host.TimeUs) / 1000), platform.GetType().GetProperty("InputRoot")!.GetValue(platform), type, position,
                action == "move" && _pressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!;
            ((Action<RawInputEventArgs>)platform.GetType().GetProperty("Input")!.GetValue(platform)!)(raw);
            if (action == "down") _pressed = true; if (action == "up") _pressed = false;
        }
        internal ProbePoint[] Measure(bool state = false)
        {
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, _host.Window).Where(m => m.Name is "box" or "size").ToArray();
            var points = new List<ProbePoint> { new("bar", "bar", null, Geometry(Bar)) };
            if (state)
            {
                var s = Bar is TopAppBar ? new[] { _top.HeightOffsetLimit, _top.HeightOffset, _top.ContentOffset, _top.CollapsedFraction } : new[] { _bottom.HeightOffsetLimit, _bottom.HeightOffset, _bottom.ContentOffset, _bottom.CollapsedFraction };
                var names = new[] { "limit", "offset", "content-offset", "fraction", "consumed", "available", "fling-complete", "fling-cancelled", "fling-consumed" };
                var values = s.Select(v => (double)v).Concat([_consumed.Y, _available.Y, _flingTask?.IsCompletedSuccessfully == true ? 1 : 0, _flingTask?.IsCanceled == true ? 1 : 0, _flingResult.Y]).ToArray();
                points.Add(new("scroll-state", "scroll-state", "bar", names.Select((name, i) => Measurement.Numeric(name, values[i], i == 3 || i is 6 or 7 ? "none" : i == 8 ? "px/s" : "px", "none", "motion", $"appbar.{name}", "AppBarState/ScrollScope")).ToArray()));
            }
            foreach (var (id, marker) in _markers.Where(entry => entry.Value.IsAttachedToVisualTree() && entry.Value.IsEffectivelyVisible))
            {
                var color = ((ISolidColorBrush)TextElement.GetForeground(marker)!).Color;
                points.Add(new(id, id, "bar", [.. Geometry(marker), Measurement.Vector("color", "color", [color.R, color.G, color.B, color.A], "srgba8", "none", "properties", "appbar.inherited-content-color", "TextElement.Foreground")]));
            }
            return points.ToArray();
        }
    }
    private sealed class Marker(double width, double height) : Control
    {
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        { base.OnPropertyChanged(change); if (change.Property == TextElement.ForegroundProperty) InvalidateVisual(); }
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
    private sealed class Fill : Grid
    {
        protected override Size MeasureOverride(Size availableSize) { base.MeasureOverride(availableSize); return availableSize; }
    }
}
