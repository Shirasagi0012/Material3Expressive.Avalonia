using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SearchAppBarConfig(bool Navigation = true, bool Actions = true, bool Dark = false, bool Rtl = false,
    int Width = 600, int InputWidth = 360, int InputHeight = 56, bool Transparent = false, bool Insets = false, bool Consumed = false,
    bool Padding = false, bool Scrolled = false, float Offset = 0, string Action = "none", bool Reverse = false, bool Enabled = true,
    double Elevation = 0, bool Custom = false);

[ParitySuite("search-app-bar")]
public sealed class AppBarWithSearchCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> ExpansionConfigurations()
    {
        foreach (var contained in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "open-close", "reverse" })
            yield return new($"expansion.{(contained ? "contained" : "full-screen")}.{(rtl ? "rtl" : "ltr")}.{action}",
                "search app bar visibility during full-screen expansion and interruption",
                new(Expanded: false, Rtl: rtl, Action: action, FullScreen: true, Contained: contained, AppBar: true));
    }

    [ParityScenario("expansion", Data = nameof(ExpansionConfigurations))]
    public Task Expansion(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);

    public static IEnumerable<CaseData<SearchAppBarConfig>> Configurations()
    {
        foreach (var nav in new[] { false, true })
        foreach (var actions in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{(nav ? "nav" : "no-nav")}.{(actions ? "actions" : "no-actions")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", "search and icon slots", new(nav, actions, dark, rtl));
        foreach (var width in new[] { 180, 280, 1000 })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.width-{width}.{(rtl ? "rtl" : "ltr")}", "weighted search constraints", new(Width: width, Rtl: rtl));
        foreach (var transparent in new[] { false, true })
        foreach (var scrolled in new[] { false, true })
            yield return new($"static.{(transparent ? "transparent" : "opaque")}.{(scrolled ? "scrolled" : "elevated")}", "surface elevation routing and scrolled palette", new(Transparent: transparent, Scrolled: scrolled, Elevation: 3));
        foreach (var rtl in new[] { false, true })
        foreach (var consumed in new[] { false, true })
            yield return new($"static.insets.{(rtl ? "rtl" : "ltr")}.{(consumed ? "consumed" : "plain")}", "physical insets and logical content padding", new(Rtl: rtl, Insets: true, Consumed: consumed, Padding: true));
        yield return new("static.custom", "custom colors and input intrinsic size", new(Custom: true, InputWidth: 440, InputHeight: 72));
        yield return new("static.partial", "clipped scroll with unscrolled search coordinates", new(Offset: -24));
        yield return new("static.hidden", "fully scrolled layout", new(Offset: -64));
        foreach (var action in new[] { "scroll", "snap", "fling-up", "fling-down", "color-reverse", "resize", "drag", "disable-settle", "disable-drag", "disable-drag-settle" })
            yield return new($"dynamic.{action}", "scroll consumption, geometry and motion", new(Action: action));
        yield return new("dynamic.reverse-layout", "reverse nested-scroll branches", new(Action: "scroll", Reverse: true));
        yield return new("dynamic.disabled", "disabled scroll policy", new(Action: "scroll", Enabled: false));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SearchAppBarConfig>();
        await using var host = new AvaloniaHost(writer, c.Width, 240);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(c.Dark ? "#D0BCFF" : "#6750A4");
        host.Window.Resources[Sys.Color.Surface] = Color.Parse(c.Dark ? "#141218" : "#FEF7FF");
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(c.Dark ? "#211F26" : "#F3EDF7");
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse(c.Dark ? "#2B2930" : "#ECE6F0");
        host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse(c.Dark ? "#36343B" : "#E6E0E9");
        var state = new SearchBarScrollState(-64, c.Offset, c.Scrolled ? -64 : 0);
        var behavior = new SearchBarScrollBehavior(state) { ReverseLayout = c.Reverse, CanScroll = c.Enabled };
        var nav = new Marker(48, 48); var action = new Marker(48, 48); var input = new Marker(c.InputWidth, c.InputHeight, true);
        var bar = new AppBarWithSearch { State = new SearchBarState(), InputField = input,
            NavigationIcon = c.Navigation ? nav : null, ScrollBehavior = behavior, TonalElevation = c.Elevation, ShadowElevation = c.Elevation };
        if (c.Actions) bar.Items.Add(action);
        if (c.Transparent) bar.Background = Brushes.Transparent;
        if (c.Insets) bar.WindowInsets = new(12, 20, 24, 16);
        if (c.Padding) bar.ContentPadding = new(7, 3, 11, 5);
        if (c.Custom) { bar.Background = Brushes.Yellow; bar.SearchBarContainerColor = Colors.Green; bar.NavigationIconContentColor = Colors.Red; bar.ActionIconContentColor = Colors.Blue; bar.CornerRadius = new(12); }
        var root = new Grid { FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { bar } };
        if (c.Consumed) MaterialWindowInsets.SetConsumePadding(root, new(4, 8, 6, 10));
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        var consumed = 0f; var remaining = 0f;
        void Sample(string id)
        {
            var points = new List<ProbePoint> {
                new("app-bar", "search-app-bar", null, AvaloniaMeasurements.Geometry(bar, host.Window).ToArray()),
                new("search", "collapsed-search-bar", "app-bar", AvaloniaMeasurements.Geometry(bar.CollapsedBar!, host.Window).ToArray()),
                new("input", "input-slot", "search", AvaloniaMeasurements.Geometry(input, host.Window).ToArray()),
                new("scroll", "scroll-state", null, [Measurement.Numeric("offset", state.ScrollOffset, "dip"), Measurement.Numeric("limit", state.ScrollOffsetLimit, "dip"), Measurement.Numeric("content", state.ContentOffset, "dip"), Measurement.Numeric("consumed", consumed, "dip"), Measurement.Numeric("remaining", remaining, "dip/s")]) };
            if (c.Navigation) points.Add(new("navigation", "navigation-slot", "app-bar", AvaloniaMeasurements.Geometry(nav, host.Window).ToArray()));
            if (c.Actions) points.Add(new("action", "action-slot", "app-bar", AvaloniaMeasurements.Geometry(action, host.Window).ToArray()));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action == "scroll")
        {
            Change(1000, "pre-up", () => consumed = behavior.OnPreScroll(-20)); At(16000);
            Change(17000, "post-up", () => consumed = behavior.OnPostScroll(-15, -5)); At(32000, 64000, 128000, 320000);
            Change(321000, "pre-down", () => consumed = behavior.OnPreScroll(12)); At(336000);
            Change(337000, "post-down", () => consumed = behavior.OnPostScroll(0, 80)); At(352000, 512000); return;
        }
        if (c.Action == "resize")
        {
            Change(1000, "offset", () => state.ScrollOffset = -32); At(16000);
            Change(17000, "resize", () => input.PreferredHeight = 96); At(32000, 64000); return;
        }
        if (c.Action == "color-reverse")
        {
            Change(1000, "overlap", () => state.ContentOffset = -64); At(16000, 32000, 64000);
            Change(65000, "reverse-color", () => state.ContentOffset = 0); At(80000, 96000, 128000, 256000, 512000); return;
        }
        if (c.Action is "drag" or "disable-drag" or "disable-drag-settle")
        {
            Change(1000, "down", () => { host.Input("move", new(300, 32)); host.Input("down", new(300, 32)); });
            Change(17000, "drag", () => host.Input("move", new(300, 12)));
            if (c.Action == "disable-drag") Change(18000, "disable", () => behavior.CanScroll = false);
            At(32000);
            Change(33000, "up", () => host.Input("up", new(300, 12))); At(48000);
            if (c.Action == "disable-drag-settle") Change(49000, "disable", () => behavior.CanScroll = false);
            At(64000, 96000, 160000, 320000, 640000); return;
        }
        Change(1000, "offset", () => state.ScrollOffset = -24); At(16000);
        Change(17000, "settle", () => { _ = Settle(); });
        async Task Settle() { remaining = await behavior.OnPostFlingAsync(c.Action == "fling-up" ? -600 : c.Action == "fling-down" ? 600 : 0); }
        if (c.Action == "disable-settle")
        {
            At(32000, 48000);
            Change(49000, "disable", () => behavior.CanScroll = false);
            At(64000, 96000, 160000, 320000, 640000, 1024000);
            return;
        }
        At(32000, 48000, 64000, 96000, 160000, 320000, 640000, 1024000);
    }
    private sealed class Marker(double width, double height, bool input = false) : Control
    {
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<Marker, double>(nameof(PreferredHeight));
        public double PreferredHeight { get => IsSet(PreferredHeightProperty) ? GetValue(PreferredHeightProperty) : height; set => SetValue(PreferredHeightProperty, value); }
        static Marker() => AffectsMeasure<Marker>(PreferredHeightProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(input ? Brushes.Magenta : GetValue(TextElement.ForegroundProperty)!,
            new Rect(input ? 16 : (Bounds.Width - 24) / 2, (Bounds.Height - 24) / 2, 24, 24));
    }
}
