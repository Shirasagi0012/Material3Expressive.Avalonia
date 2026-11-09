using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record SearchAppBarScrollConfig(string Action, bool Rtl = false, bool Reverse = false, bool Stretch = false);

[ParitySuite("search-app-bar-scroll")]
public sealed class SearchAppBarScrollCases
{
    public static IEnumerable<CaseData<SearchAppBarScrollConfig>> Configurations()
    {
        // Keep explicit-width inputs as a regression for minimum constraints on caller
        // controls. Stretch inputs isolate the scroll integration without fixing its size.
        foreach (var stretch in new[] { false, true })
        {
            var suffix = stretch ? ".stretch" : "";
            foreach (var rtl in new[] { false, true })
            foreach (var action in new[] { "scroll", "snap", "disable", "replace" })
                yield return new($"{action}.{(rtl ? "rtl" : "ltr")}{suffix}", "Search app bar with a real Scaffold main scroll viewport", new(action, rtl, Stretch: stretch));
            foreach (var rtl in new[] { false, true })
                yield return new($"scroll.reverse.{(rtl ? "rtl" : "ltr")}{suffix}", "Reverse search policy pre/post consumption", new("scroll", rtl, true, stretch));
        }
    }

    [ParityScenario("scroll", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SearchAppBarScrollConfig>();
        await using var host = new AvaloniaHost(writer, 400, 300);
        CaseDefaults.SetThemeColors(host, false);
        var behavior = new SearchBarScrollBehavior { ReverseLayout = c.Reverse };
        Border input = c.Stretch ? new FillWidthMarker() : new Border();
        input.Height = 56; input.Background = Brushes.Magenta;
        if (!c.Stretch) input.Width = 240;
        var bar = new AppBarWithSearch { InputField = input, ScrollBehavior = behavior };
        var rows = new StackPanel();
        foreach (var i in Enumerable.Range(0, 20))
            rows.Children.Add(new Border { Height = 40, Background = i % 2 == 0 ? Brushes.Cyan : Brushes.Yellow });
        var viewer = new ScrollViewer { Content = rows, IsScrollInertiaEnabled = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        var screen = new Scaffold { TopBar = bar, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var body = new Border { Child = viewer };
        body.Bind(Border.WidthProperty, new Binding("Bounds.Width") { Source = screen });
        body.Bind(Border.HeightProperty, new Binding("Bounds.Height") { Source = screen });
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        screen.Content = body;
        host.Mount(screen); host.Frame(-1000); host.Frame(0);
        AppBarSmoothScrollConnection Connection()
        {
            var scope = screen.GetVisualDescendants().OfType<AppBarScrollScope>().Single();
            return (AppBarSmoothScrollConnection?)typeof(AppBarScrollScope)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(scope)
                ?? throw new InvalidOperationException("The search main viewport did not connect.");
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [
            new ProbePoint("bar", "search-app-bar", null, AvaloniaMeasurements.Geometry(bar, host.Window).ToArray()),
            new ProbePoint("viewport", "scroll-viewport", null, AvaloniaMeasurements.Geometry(viewer, host.Window).ToArray()),
            new ProbePoint("scroll", "scroll-state", null,
            [Measurement.Numeric("offset", behavior.State.ScrollOffset, "dip"), Measurement.Numeric("limit", behavior.State.ScrollOffsetLimit, "dip"),
                Measurement.Numeric("content", behavior.State.ContentOffset, "dip"), Measurement.Numeric("body", viewer.Offset.Y, "dip")]) ]));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "scroll", id); host.Draw(); Sample(id); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Delta(double value) => Connection().Tracker!.ApplyVerticalScrollDelta(value);
        Sample("initial");
        Change(1000, "up-20", () => Delta(20)); At(16000);
        if (c.Action == "snap")
        {
            // The caller's explicit post-fling uses the component's frame clock while the
            // connected participant mirrors live state and updates the real viewport.
            Change(17000, "settle", () => _ = behavior.OnPostFlingAsync(0));
            At(32000, 48000, 64000, 96000, 160000, 320000, 640000); return;
        }
        if (c.Action == "disable") Change(17000, "disable", () => behavior.CanScroll = false);
        if (c.Action == "replace") Change(17000, "replace", () => bar.ScrollBehavior = behavior = new SearchBarScrollBehavior());
        Change(33000, "cross-limit", () => Delta(100)); At(48000);
        Change(65000, "body-scroll", () => Delta(40)); At(80000);
        Change(97000, "down-12", () => Delta(-12)); At(112000);
        Change(129000, "down-to-start", () => Delta(-180)); At(144000, 320000);
    }

    // Compose fillMaxWidth consumes the maximum during measure. Avalonia's Stretch
    // alignment alone only stretches within the width its parent finally allocates.
    private sealed class FillWidthMarker : Border
    {
        protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, 56);
    }
}
