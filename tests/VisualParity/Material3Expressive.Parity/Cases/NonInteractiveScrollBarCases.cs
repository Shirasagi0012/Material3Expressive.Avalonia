using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record NonInteractiveScrollBarConfig(bool Horizontal = false, bool Rtl = false, bool Dark = false,
    double Content = 600, double Viewport = 120, double Offset = 90, double Width = 160, double Height = 120,
    double Thickness = 4, double Minimum = 24, double MaximumFraction = .9, double MainInset = 2, double CrossInset = 0,
    string? ThumbColor = null, string? TrackColor = null, bool Fade = false, string Action = "none", int Delay = 400);

[ParitySuite("non-interactive-scrollbar")]
public sealed class NonInteractiveScrollBarCases
{
    public static IEnumerable<CaseData<NonInteractiveScrollBarConfig>> StaticConfigurations()
    {
        foreach (var horizontal in new[] { false, true })
        {
            var axis = horizontal ? "horizontal" : "vertical";
            foreach (var rtl in new[] { false, true })
            foreach (var dark in new[] { false, true })
                yield return Data($"static.{axis}.{(rtl ? "rtl" : "ltr")}.{(dark ? "dark" : "light")}", new(horizontal, rtl, dark));
            foreach (var (id, content, viewport, offset) in new[] {
                ("start", 600d, 120d, 0d), ("end", 600d, 120d, 480d), ("minimum", 6000d, 120d, 3000d),
                ("maximum", 121d, 120d, 1d), ("fits", 120d, 120d, 0d), ("empty", 0d, 120d, 0d),
                ("zero-viewport", 600d, 0d, 50d), ("overscroll", 600d, 120d, -40d) })
                yield return Data($"static.{axis}.{id}", new(horizontal, Content: content, Viewport: viewport, Offset: offset));
            yield return Data($"static.{axis}.insets", new(horizontal, Thickness: 7.3, MainInset: 10.2, CrossInset: 3.7,
                ThumbColor: "#70336699", TrackColor: "#40224466"));
            yield return Data($"static.{axis}.short", new(horizontal, Width: 22, Height: 22));
            yield return Data($"static.{axis}.tight", new(horizontal, Width: 32, Height: 32));
            yield return Data($"static.{axis}.initial-hidden", new(horizontal, Fade: true));
        }
    }

    public static IEnumerable<CaseData<NonInteractiveScrollBarConfig>> DynamicConfigurations()
    {
        // Same fade algorithm on both axes; horizontal RTL also checks reversed moving geometry.
        foreach (var horizontal in new[] { false, true })
        foreach (var action in new[] { "idle", "restart", "configuration", "fits", "reattach" })
            yield return Data($"dynamic.{(horizontal ? "horizontal-rtl" : "vertical")}.{action}",
                new(horizontal, Rtl: horizontal, Fade: true, Action: action, TrackColor: "#40224466"));
        // Zero timeout is an immediate branch, independent of the scheduled idle expiry above.
        foreach (var horizontal in new[] { false, true })
        foreach (var action in new[] { "idle", "quick-restart", "configuration", "reattach" })
            yield return Data($"dynamic.zero-delay.{(horizontal ? "horizontal-rtl" : "vertical")}.{action}",
                new(horizontal, Rtl: horizontal, Fade: true, Action: action, TrackColor: "#40224466", Delay: 0));
    }

    private static CaseData<NonInteractiveScrollBarConfig> Data(string id, NonInteractiveScrollBarConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer);

    private static async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NonInteractiveScrollBarConfig>();
        await using var host = new AvaloniaHost(writer, 200, 160, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, config.Dark);
        // CaseDefaults does not provide Outline; supply the same ColorScheme role as the reference.
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Outline] = Color.Parse(config.Dark ? "#938F99" : "#79747E");
        var bar = new NonInteractiveScrollBar {
            ContentSize = config.Content, ViewportSize = config.Viewport, ScrollOffset = config.Offset,
            Orientation = config.Horizontal ? Orientation.Horizontal : Orientation.Vertical,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Thickness = config.Thickness, ThumbMinLength = config.Minimum, ThumbMaxLengthFraction = config.MaximumFraction,
            MainAxisTrackInset = config.MainInset, CrossAxisTrackInset = config.CrossInset, IsFadeEnabled = config.Fade,
            FadeDelay = TimeSpan.FromMilliseconds(config.Delay) };
        if (config.ThumbColor is { } thumb) bar.ThumbColor = Color.Parse(thumb);
        if (config.TrackColor is { } track) bar.TrackColor = Color.Parse(track);
        var box = new Grid { Width = config.Width, Height = config.Height, Margin = new Thickness(16, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { bar } };
        host.Mount(new Grid { Children = { box } }); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("viewport", "viewport", null,
            AvaloniaMeasurements.Geometry(box, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
        Sample("initial");
        if (config.Action == "none") return;
        // Continuous 16ms grid. State belongs to the scrolling owner; the product observes it during draw.
        for (var ms = 16; ms <= 1120; ms += 16)
        {
            host.AdvanceTo(ms * 1000L);
            if (ms is 32 or 160) bar.ScrollOffset += 40;
            if (ms == 640 && config.Action == "restart") bar.ScrollOffset += 80;
            if (ms == 224 && config.Action == "quick-restart") bar.ScrollOffset += 40;
            if (ms == 320 && config.Action == "configuration") bar.Thickness = 8;
            if (ms == 352 && config.Action == "configuration") bar.ScrollOffset += 40;
            if (ms == 320 && config.Action == "fits") { bar.ContentSize = 100; bar.ScrollOffset += 20; }
            if (ms == 448 && config.Action == "fits") bar.ContentSize = 600;
            if (ms == 640 && config.Action == "reattach") box.Children.Remove(bar);
            if (ms == 672 && config.Action == "reattach") box.Children.Add(bar);
            if (ms == 704 && config.Action == "reattach") bar.ScrollOffset += 40;
            host.Draw(); host.Frame(ms * 1000L);
            if (ms is 32 or 160 or 320 or 352 or 448 or 544 or 560 or 592 or 640 or 672 or 704 or 768 or 832 or 960 or 1120)
                Sample($"time-{ms}");
        }
    }
}
