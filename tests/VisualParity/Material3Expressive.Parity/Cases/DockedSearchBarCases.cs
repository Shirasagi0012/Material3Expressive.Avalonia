using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record DockedSearchBarConfig(bool Expanded = false, bool Dark = false, bool Rtl = false,
    int HostWidth = 420, int HostHeight = 600, double ContentHeight = 40, string Action = "none", string Shape = "none");

[ParitySuite("docked-search-bar")]
public sealed class DockedSearchBarCases
{
    public static IEnumerable<CaseData<DockedSearchBarConfig>> Configurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{(expanded ? "expanded" : "collapsed")}.{(dark ? "dark" : "light")}", "docked search surface and slots", new(expanded, dark));
        yield return new("static.rtl", "RTL slots", new(true, Rtl: true));
        yield return new("static.tall-results", "window maximum height", new(true, ContentHeight: 700));
        yield return new("static.small-window", "minimum coerced to window maximum", new(true, HostWidth: 240, HostHeight: 240, ContentHeight: 700));
        foreach (var action in new[] { "open-close", "reverse", "content", "escape", "detach" })
            yield return new($"dynamic.{action}", "visibility lifecycle and desktop input", new(Action: action, Expanded: action == "escape"));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<DockedSearchBarConfig>();
        await using var host = new AvaloniaHost(writer, c.HostWidth, c.HostHeight);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse(c.Dark ? "#2B2930" : "#ECE6F0");
        host.Window.Resources[Sys.Color.Outline] = Color.Parse(c.Dark ? "#938F99" : "#79747E");
        var input = new Border { Height = 56, Child = new Border { Width = 24, Height = 24, Margin = new Thickness(16, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Magenta } };
        var content = new ResultsMarker { PreferredHeight = c.ContentHeight };
        var bar = new DockedSearchBar { InputField = input, Content = content, IsExpanded = c.Expanded,
            Margin = new Thickness(16), FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var requests = 0;
        bar.CollapseRequested += (_, _) => { requests++; bar.IsExpanded = false; };
        var root = new Grid { Children = { bar } };
        if (c.Shape != "none") bar.Shape = MaterialShapeCases.CreateShape(c.Shape);
        host.Mount(root); host.Frame(0);
        void Sample(string id)
        {
            var attached = bar.IsAttachedToVisualTree();
            var present = content.IsAttachedToVisualTree();
            var points = new List<ProbePoint> { new("state", "docked-search-state", null, [
                Measurement.Flag("attached", attached, definition: "search-bar.attached"),
                Measurement.Flag("expanded", bar.IsExpanded, definition: "search-bar.expanded"),
                Measurement.Flag("contentPresent", present, definition: "search-bar.content-present"),
                Measurement.Numeric("collapseRequests", requests, "count", definition: "search-bar.collapse-requests")]) };
            if (attached)
            {
                points.Add(new("bar", "docked-search-bar", "state", AvaloniaMeasurements.Geometry(bar, host.Window).ToArray()));
                points.Add(new("input", "input-slot", "bar", AvaloniaMeasurements.Geometry(input, host.Window).ToArray()));
                if (present) points.Add(new("content", "results-slot", "bar", AvaloniaMeasurements.Geometry(content, host.Window).ToArray()));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action == "escape")
        {
            Change(1000, "escape-down", () => input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }));
            At(16000, 96000, 112000, 208000, 416000, 640000); return;
        }
        Change(1000, "expand", () => bar.IsExpanded = true);
        At(16000, 96000, 112000, 208000);
        if (c.Action == "reverse") Change(209000, "reverse", () => bar.IsExpanded = false);
        if (c.Action == "content") Change(209000, "tall-results", () => content.PreferredHeight = 700);
        if (c.Action == "detach")
        {
            Change(209000, "detach", () => root.Children.Clear());
            At(224000);
            Change(225000, "reattach", () => root.Children.Add(bar));
        }
        At(320000, 640000, 736000);
        Change(737000, "collapse", () => bar.IsExpanded = false);
        At(752000, 832000, 848000, 944000, 1152000, 1280000);
    }

    // Compose Modifier.height is a preferred height constrained by its parent.
    // Avalonia Height forces the arranged size even when its measured size is clamped;
    // use intrinsic content here, as in the other geometry-only parity cases.
    private sealed class ResultsMarker : Control
    {
        public static readonly StyledProperty<double> PreferredHeightProperty =
            AvaloniaProperty.Register<ResultsMarker, double>(nameof(PreferredHeight));
        public double PreferredHeight { get => GetValue(PreferredHeightProperty); set => SetValue(PreferredHeightProperty, value); }
        static ResultsMarker() => AffectsMeasure<ResultsMarker>(PreferredHeightProperty);
        protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Math.Min(PreferredHeight, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(Brushes.Blue, new Rect(Bounds.Size));
    }
}
