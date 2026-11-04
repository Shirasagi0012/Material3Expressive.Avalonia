using Avalonia;
using Avalonia.Controls;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PullRefreshScrollConfig(string Action = "release", float Drag = 161, float Velocity = 200, double Threshold = 80);

[ParitySuite("pull-refresh-scroll")]
public sealed class PullToRefreshScrollCases
{
    public static IEnumerable<CaseData<PullRefreshScrollConfig>> Configurations()
    {
        foreach (var drag in new[] { 0, 159, 160, 161, 480, 600 })
            yield return new($"release.{drag}", "strict threshold and tension", new(Drag: drag));
        yield return new("upward-release", "upward velocity passes to child", new(Velocity: -200));
        yield return new("fractional-threshold", "threshold rounds at actual density", new(Drag: 121, Threshold: 60.3));
        foreach (var action in new[] { "pre-post", "side-effect", "disabled", "callback", "programmatic", "detach" })
            yield return new(action, action, new(action));
    }
    [ParityScenario("scroll", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<PullRefreshScrollConfig>();
        await using var host = new AvaloniaHost(writer, 240, 320, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Avalonia.Media.Color.Parse("#ECE6F0");
        var state = new PullToRefreshState(2);
        var behavior = new PullToRefreshScrollBehavior();
        var indicator = new PullToRefreshIndicator { State = state, ScrollBehavior = behavior,
            RefreshThreshold = c.Threshold, Margin = new Thickness(0, 16, 0, 0) };
        var root = new Grid { Children = { indicator } };
        var requests = 0; var consumed = 0f; var velocity = 0f; var completed = false; var canceled = false;
        behavior.RefreshRequested += (_, _) => { requests++; if (c.Action == "callback") indicator.IsRefreshing = true; };
        host.Mount(root); host.Frame(0);
        async Task Release()
        {
            try { velocity = await behavior.OnPreFlingAsync(c.Velocity); completed = true; }
            catch (OperationCanceledException) { canceled = true; }
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new ProbePoint("state", "scroll-state", null, [
            Measurement.Numeric("fraction", state.DistanceFraction, "fraction"),
            Measurement.Numeric("consumed", consumed, "dip"), Measurement.Numeric("velocity", velocity, "dip/s"),
            Measurement.Numeric("requests", requests, "count"), Measurement.Flag("animating", state.IsAnimating),
            Measurement.Flag("completed", completed), Measurement.Flag("canceled", canceled),
            Measurement.Flag("refreshing", indicator.IsRefreshing)])]));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); host.Frame(time); Sample(id);
        }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "programmatic")
        {
            Change(1000, "start", () => indicator.IsRefreshing = true); At(16000, 48000, 160000, 640000);
            Change(641000, "stop", () => indicator.IsRefreshing = false); At(656000, 720000, 1008000); return;
        }
        Change(1000, "pull", () => consumed = behavior.OnPostScroll(c.Drag, c.Action != "side-effect")); At(16000);
        if (c.Action == "pre-post")
        {
            Change(17000, "pre", () => consumed = behavior.OnPreScroll(-30));
            Change(33000, "post", () => consumed = behavior.OnPostScroll(0));
            Change(49000, "cap", () => consumed = behavior.OnPostScroll(600)); At(64000);
        }
        if (c.Action == "disabled")
        {
            Change(17000, "disable", () => behavior.IsEnabled = false);
            Change(33000, "disabled-input", () => consumed = behavior.OnPostScroll(20));
        }
        Change(161000, "release", () => _ = Release()); At(176000, 208000);
        if (c.Action == "detach") Change(209000, "detach", () => root.Children.Clear());
        At(240000, 400000, 800000);
        if (c.Action == "callback")
        {
            Change(801000, "complete-refresh", () => indicator.IsRefreshing = false); At(816000, 960000, 1408000);
        }
    }
}
