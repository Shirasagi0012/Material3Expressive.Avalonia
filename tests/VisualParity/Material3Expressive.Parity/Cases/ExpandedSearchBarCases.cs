using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using ComposeAnimation.Runtime;
using Avalonia.Animation.Easings;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ExpandedSearchBarConfig(bool Expanded = true, bool Dark = false, bool Rtl = false,
    int HostWidth = 420, int HostHeight = 600, double ContentHeight = 40, double InputWidth = 360,
    double InputHeight = 56, bool Custom = false, string Action = "none", string Motion = "default", bool Anchor = true,
    double TonalElevation = 0, double ShadowElevation = 0, bool Gap = false, bool NoScrim = false, bool FullScreen = false, bool Insets = false, bool Contained = false, bool FilledInput = false, bool AppBar = false);

[ParitySuite("expanded-search-bar")]
public sealed class ExpandedSearchBarCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Configurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{(expanded ? "expanded" : "collapsed")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", "search surface and anchored popup", new(expanded, dark, rtl));
        yield return new("static.tall-results", "whole popup window maximum", new(ContentHeight: 700));
        yield return new("static.small-window", "height minimum constrained to maximum", new(HostWidth: 240, HostHeight: 240, ContentHeight: 700));
        yield return new("static.input-size", "custom input intrinsic size", new(InputWidth: 280, InputHeight: 72));
        yield return new("static.custom", "custom surface and divider", new(Custom: true));
        yield return new("static.no-anchor", "expanded popup without a collapsed anchor", new(Anchor: false));
        yield return new("static.zero-width", "a zero-width anchor is distinct from no anchor", new(InputWidth: 0));
        yield return new("static.elevation", "custom tonal and shadow elevation", new(TonalElevation: 3, ShadowElevation: 3));
        foreach (var motion in new[] { "tween", "overshoot", "content" })
            yield return new($"dynamic.motion-{motion}", "independent progress and content animation specifications", new(Expanded: false, Action: "open-close", Motion: motion));
        foreach (var action in new[] { "open-close", "reverse", "snap", "anchor", "content", "escape", "outside" })
            yield return new($"dynamic.{action}", "state progress, lifecycle and desktop dismissal", new(Expanded: action is "anchor" or "escape" or "outside", Action: action));
        yield return new("dynamic.remove-anchor", "remaining popup retains the application-owned state", new(Expanded: false, Action: "remove-anchor"));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<ExpandedSearchBarConfig>();
        await using var host = new AvaloniaHost(writer, c.HostWidth, c.HostHeight);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse(c.Dark ? "#2B2930" : "#ECE6F0");
        host.Window.Resources[Sys.Color.Outline] = Color.Parse(c.Dark ? "#938F99" : "#79747E");
        if (c.AppBar) host.Window.Resources[Sys.Color.Surface] = Color.Parse(c.Dark ? "#141218" : "#FEF7FF");
        var initial = c.Expanded ? SearchBarValue.Expanded : SearchBarValue.Collapsed;
        var state = c.Motion switch
        {
            "gap-default" => SearchBarState.CreateWithGap(initial),
            "tween" => new SearchBarState(initial, new TweenSpec<float>(TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(50), new LinearEasing()),
                new TweenSpec<float>(TimeSpan.FromMilliseconds(300), easing: new LinearEasing()),
                new TweenSpec<float>(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100), new LinearEasing()),
                new TweenSpec<float>(TimeSpan.FromMilliseconds(150), easing: new LinearEasing())),
            "overshoot" => new SearchBarState(initial, new SpringSpec<float>(.4f, 120f), new SpringSpec<float>(.4f, 120f)),
            "content" => new SearchBarState(initial, new SnapSpec<float>(), new SnapSpec<float>(),
                new TweenSpec<float>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()),
                new TweenSpec<float>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing())),
            _ => c.Contained ? SearchBarState.CreateContained(initial) : new SearchBarState(initial)
        };
        var collapsed = new InputMarker { PreferredWidth = c.InputWidth, PreferredHeight = c.InputHeight };
        var input = new InputMarker { PreferredWidth = c.InputWidth, PreferredHeight = c.InputHeight, Filled = c.FilledInput };
        var results = new ResultsMarker { PreferredHeight = c.ContentHeight };
        var bar = new SearchBar { State = state, Content = collapsed, Margin = new(16, 24, 0, 0) };
        AppBarWithSearch? appBar = null;
        if (c.AppBar)
        {
            bar.Content = null;
            appBar = new AppBarWithSearch
            {
                State = state, InputField = collapsed, Margin = new(16, 24, 0, 0),
                NavigationIcon = new Border { Width = 48, Height = 48, Background = Brushes.Red }
            };
            appBar.Items.Add(new Border { Width = 48, Height = 48, Background = Brushes.Red });
        }
        ExpandedDockedSearchBar popup = c.Contained ? new ExpandedFullScreenContainedSearchBar() : c.FullScreen ? new ExpandedFullScreenSearchBar() : c.Gap ? new ExpandedDockedSearchBarWithGap() : new ExpandedDockedSearchBar();
        popup.State = state; popup.InputField = input; popup.Content = results;
        if (popup is ExpandedFullScreenSearchBar full && c.Insets) full.WindowInsets = new(12, 20, 24, 16);
        if (popup is ExpandedDockedSearchBarWithGap gap)
        {
            if (c.NoScrim) gap.DropdownScrimColor = Colors.Transparent;
            if (c.Custom) { gap.DropdownGapSize = 8; gap.DropdownCornerRadius = new(4); gap.DropdownScrimColor = Color.Parse("#400000FF"); }
        }
        bar.TonalElevation = popup.TonalElevation = c.TonalElevation;
        bar.ShadowElevation = popup.ShadowElevation = c.ShadowElevation;
        var root = new Grid { FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        // Core's surrounding Box has LocalContentColor's Black default (it is not a Surface).
        // A themed Avalonia Window supplies OnSurface; make the fixture's ambient scope explicit.
        root.SetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty, Brushes.Black);
        if (c.Anchor) root.Children.Add(appBar is not null ? appBar : bar);
        root.Children.Add(popup);
        if (c.Custom)
        {
            bar.Background = popup.Background = Brushes.Yellow;
            bar.CornerRadius = popup.CornerRadius = new(12);
            popup.DividerColor = Colors.Red;
        }
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        if (appBar is not null) bar = appBar.CollapsedBar!;
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("state", "search-state", null, [
                Measurement.Numeric("progress", state.Progress, "fraction", definition: "search-state.progress"),
                Measurement.Numeric("content-progress", state.ContentProgress, "fraction", definition: "search-state.content-progress"),
                Measurement.Text("current", state.CurrentValue.ToString(), definition: "search-state.current"),
                Measurement.Text("target", state.TargetValue.ToString(), definition: "search-state.target"),
                Measurement.Flag("animating", state.IsAnimating, definition: "search-state.animating"),
                Measurement.Flag("presented", popup.IsPresented, definition: "search-state.presented")]),
                new("bar", "collapsed-search-bar", "state", bar.IsAttachedToVisualTree() ? AvaloniaMeasurements.Geometry(bar, host.Window).ToArray() : []) };
            if (appBar is not null)
                points.Add(new("app-bar-state", "search-app-bar-state", "state", [
                    Measurement.Flag("full-screen", state.ExpandsToFullScreen),
                    Measurement.Flag("search-visible", bar.Opacity != 0, definition: "search.visibility.requested",
                        source: "SearchBar.Opacity != 0 (not retained raster alpha)")]));
            if (popup.IsPresented)
            {
                points.Add(new("popup", "expanded-search-bar", "state", AvaloniaMeasurements.Geometry((c.FullScreen ? popup.PresentationLayout : popup.Presentation)!, host.Window).ToArray()));
                points.Add(new("input", "input-slot", "popup", AvaloniaMeasurements.Geometry(input, host.Window).ToArray()));
                if (!c.Gap || results.IsAttachedToVisualTree())
                    points.Add(new("results", "results-slot", "popup", AvaloniaMeasurements.Geometry(results, host.Window).ToArray()));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action == "anchor")
        {
            Change(1000, "move", () => bar.Margin = new(32, 48, 0, 0)); At(16000);
            Change(17000, "resize-input", () => collapsed.PreferredWidth = 280); At(32000); return;
        }
        if (c.Action == "snap")
        {
            foreach (var (time, value) in new[] { (1000L, .02f), (17000L, .021f), (33000L, .5f), (49000L, 1.2f), (65000L, -.1f) })
            { Change(time, $"snap-{time}", () => _ = state.SnapToAsync(value)); At(time + 15000); }
            return;
        }
        if (c.Action is "escape" or "outside")
        {
            Change(1000, c.Action, () =>
            {
                if (c.Action == "escape") host.Window.KeyPress(Key.Escape, 0, PhysicalKey.Escape, null);
                else { host.Input("move", new(c.HostWidth - 4, c.HostHeight - 4)); host.Input("down", new(c.HostWidth - 4, c.HostHeight - 4)); }
            });
            At(16000);
            if (c.Gap && c.Action == "outside") Change(17000, "outside-release", () => host.Input("up", new(c.HostWidth - 4, c.HostHeight - 4)));
            At(32000, 64000, 128000, 256000, 512000, 1024000); return;
        }
        Change(1000, "expand", () => _ = state.AnimateToExpandedAsync());
        At(16000, 32000, 64000, 96000);
        if (c.Action == "reverse") Change(97000, "reverse", () => _ = state.AnimateToCollapsedAsync());
        if (c.Action == "remove-anchor") Change(97000, "remove-anchor", () => root.Children.Remove(bar));
        if (c.Action == "content") Change(97000, "grow-results", () => results.PreferredHeight = 700);
        At(128000, 192000, 320000, 512000, 1024000);
        Change(1025000, "collapse", () => _ = state.AnimateToCollapsedAsync());
        At(1040000, 1088000, 1152000, 1280000, 1536000, 2048000);
    }

    private class InputMarker : Control
    {
        public static readonly StyledProperty<double> PreferredWidthProperty = AvaloniaProperty.Register<InputMarker, double>(nameof(PreferredWidth));
        public double PreferredWidth { get => GetValue(PreferredWidthProperty); set => SetValue(PreferredWidthProperty, value); }
        public double PreferredHeight { get; init; }
        public bool Filled { get; init; }
        static InputMarker() => AffectsMeasure<InputMarker>(PreferredWidthProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(PreferredWidth, availableSize.Width), Math.Min(PreferredHeight, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            if (Filled) context.FillRectangle(Brushes.Green, new Rect(Bounds.Size));
            context.FillRectangle(Brushes.Magenta, new Rect(16, (Bounds.Height - 24) / 2, 24, 24));
        }
    }
    private class ResultsMarker : Control
    {
        public static readonly StyledProperty<double> PreferredHeightProperty = AvaloniaProperty.Register<ResultsMarker, double>(nameof(PreferredHeight));
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        static ResultsMarker() => AffectsMeasure<ResultsMarker>(PreferredHeightProperty);
        protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Math.Min(PreferredHeight, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(Brushes.Blue, new Rect(Bounds.Size));
    }
}
