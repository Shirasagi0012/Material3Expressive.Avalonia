using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PullToRefreshConfig(string Kind = "circular", double Fraction = 1, bool Refreshing = false,
    bool Dark = false, bool Rtl = false, double? Constraint = null, bool CustomColors = false,
    double MaxDistance = 80, string Action = "none", bool Translucent = false);

[ParitySuite("pull-to-refresh")]
public sealed class PullToRefreshCases
{
    public static IEnumerable<CaseData<PullToRefreshConfig>> Configurations()
    {
        // The arrow's first 40%, threshold, overshoot and tension cap have distinct drawing paths.
        foreach (var kind in new[] { "circular", "loading" })
        {
            foreach (var dark in new[] { false, true })
            foreach (var fraction in new[] { 0, .25, .4, .65, 1, 1.5, 3 })
                yield return new($"static.{kind}.{(dark ? "dark" : "light")}.{fraction.ToString(CultureInfo.InvariantCulture)}",
                    "pull progress and clipping", new(kind, fraction, Dark: dark));
            yield return new($"static.{kind}.rtl", "physical rotation is independent of RTL", new(kind, 1.5, Rtl: true));
            yield return new($"static.{kind}.constrained", "preferred container size under a small parent", new(kind, .65, Constraint: 24));
            yield return new($"static.{kind}.custom", "explicit colors and custom distance", new(kind, 1, CustomColors: true, MaxDistance: 56));
            yield return new($"static.{kind}.translucent", "translucent container and indicator paints", new(kind, .65, Translucent: true));
            yield return new($"dynamic.{kind}.translucent-reverse", "translucent paints during interrupted group opacity", new(kind, 1, Action: "reverse", Translucent: true));
            yield return new($"dynamic.{kind}.conflation", "multiple distance updates between animation frames", new(kind, .8, Action: "conflation"));
            foreach (var action in new[] { "progress", "state", "refresh", "reverse", "detach", "indeterminate" })
                yield return new($"dynamic.{kind}.{action}", "pull state, crossfade and animation lifetime",
                    new(kind, action == "state" ? .3 : 1, Refreshing: action is "detach" or "indeterminate", Action: action));
        }
        yield return new("static.custom-content", "custom indicator box", new("custom", .5));
    }

    [ParityScenario("indicator", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<PullToRefreshConfig>();
        await using var host = new AvaloniaHost(writer, 240, 320, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse(c.Dark ? "#2B2930" : "#ECE6F0");
        host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse(c.Dark ? "#4F378B" : "#EADDFF");
        host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(c.Dark ? "#EADDFF" : "#21005D");
        var state = new PullToRefreshState((float)c.Fraction);
        PullToRefreshIndicatorBox indicator = c.Kind switch
        {
            "loading" => new PullToRefreshLoadingIndicator(),
            "custom" => new PullToRefreshIndicatorBox { Background = Brushes.Yellow,
                Content = new Border { Width = 12, Height = 12, Background = Brushes.Blue } },
            _ => new PullToRefreshIndicator()
        };
        indicator.State = state; indicator.IsRefreshing = c.Refreshing; indicator.MaxDistance = c.MaxDistance;
        if (c.CustomColors)
        {
            indicator.Background = Brushes.Yellow;
            ((PullToRefreshIndicator)indicator).Color = Colors.Blue;
        }
        if (c.Translucent)
        {
            indicator.Background = new SolidColorBrush(Color.Parse("#8000CCAA"));
            ((PullToRefreshIndicator)indicator).Color = Color.Parse("#806750A4");
        }
        var anchor = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 0, 0), FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children = { indicator } };
        if (c.Constraint is { } constraint) { anchor.Width = constraint; anchor.Height = constraint; }
        var root = new Grid { Children = { anchor } };
        host.Mount(root); host.Frame(0);
        void Sample(string id)
        {
            var present = indicator.IsAttachedToVisualTree();
            var points = new List<ProbePoint> { new("state", "pull-state", null, [
                Measurement.Numeric("distanceFraction", state.DistanceFraction, "fraction", definition: "pull.distance-fraction"),
                Measurement.Flag("animating", state.IsAnimating, definition: "pull.animating"),
                Measurement.Flag("refreshing", indicator.IsRefreshing, definition: "pull.refreshing"),
                Measurement.Flag("present", present, definition: "pull.present")]) };
            if (present) points.Add(new("indicator", "indicator-layout", "state", AvaloniaMeasurements.Geometry(indicator, host.Window).ToArray()));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", id); host.Draw();
            // Resolve composition and newly created waiters in both hosts at the same
            // timestamp, as in loading-indicator; these frames do not advance time.
            host.Frame(time); host.Frame(time); Sample(id);
        }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        switch (c.Action)
        {
            case "conflation":
                foreach (var (time, fraction) in new[] { (1000L, 1.2f), (2000L, .7f), (3000L, 1.1f) })
                {
                    Advance(time); await state.SnapToAsync(fraction); writer.Record(time, "mutation", "distance-before-frame");
                    host.Draw(); Sample($"input-{time}");
                }
                At(16000, 32000, 64000, 160000, 640000); break;
            case "progress":
                Change(1000, "below", () => _ = state.SnapToAsync(.8f)); At(16000, 64000, 160000);
                Change(161000, "above", () => _ = state.SnapToAsync(1.2f)); At(176000, 208000);
                Change(209000, "below-again", () => _ = state.SnapToAsync(.7f)); At(224000, 320000, 640000);
                Change(641000, "tension", () => _ = state.SnapToAsync(3)); At(656000, 800000, 1008000); break;
            case "state":
                Change(1000, "threshold", () => _ = state.AnimateToThresholdAsync()); At(16000, 48000, 96000);
                Change(97000, "hide", () => _ = state.AnimateToHiddenAsync()); At(112000, 160000, 240000);
                Change(241000, "snap-overshoot", () => _ = state.SnapToAsync(1.4f));
                Change(257000, "threshold-again", () => _ = state.AnimateToThresholdAsync()); At(272000, 400000, 800000); break;
            case "refresh":
            case "reverse":
                Change(1000, "refresh", () => indicator.IsRefreshing = true); At(16000, 48000, 80000);
                var stop = c.Action == "reverse" ? 81000 : 737000;
                if (c.Action == "refresh") At(160000, 320000, 640000, 736000);
                Change(stop, "stop", () => indicator.IsRefreshing = false);
                At(stop + 16000, stop + 64000, stop + 160000, stop + 640000); break;
            case "detach":
                At(64000, 160000); Change(161000, "detach", () => anchor.Children.Clear()); At(320000);
                Change(321000, "reattach", () => anchor.Children.Add(indicator)); At(336000, 400000, 640000, 1008000); break;
            case "indeterminate": At(16000, 80000, 160000, 320000, 640000, 656000, 736000, 1300000, 2600000, 5200000); break;
        }
    }
}
