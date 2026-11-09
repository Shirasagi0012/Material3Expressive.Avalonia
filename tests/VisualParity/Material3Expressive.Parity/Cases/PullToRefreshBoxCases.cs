using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PullRefreshBoxConfig(string Action = "release", bool Expressive = false, bool Rtl = false);

[ParitySuite("pull-refresh-box")]
public sealed class PullToRefreshBoxCases
{
    public static IEnumerable<CaseData<PullRefreshBoxConfig>> Configurations()
    {
        foreach (var expressive in new[] { false, true })
        foreach (var action in new[] { "release", "refresh", "reverse", "disabled", "body", "detach", "cancel" })
            yield return new($"dynamic.{(expressive ? "expressive" : "circular")}.{action}",
                "Real scrollable content and touch pull", new(action, expressive));
        yield return new("dynamic.circular.rtl", "RTL real touch pull", new(Rtl: true));
    }

    [ParityScenario("touch", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<PullRefreshBoxConfig>();
        await using var host = new AvaloniaHost(writer, 240, 320, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.SurfaceContainerHigh] = Color.Parse("#ECE6F0");
        host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse("#EADDFF");
        host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse("#21005D");
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var state = new PullToRefreshState();
        var viewer = new ScrollViewer
        {
            Content = new Border { Height = 1200, Background = new SolidColorBrush(Color.Parse("#DDEEFF")) },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            IsScrollInertiaEnabled = true
        };
        var box = new PullToRefreshBox { Content = viewer, State = state, RefreshEnabled = c.Action != "disabled" };
        if (c.Expressive) box.Indicator = new PullToRefreshLoadingIndicator { State = state };
        var requests = 0;
        box.RefreshRequested += (_, _) =>
        {
            requests++;
            if (c.Action == "refresh")
            {
                box.IsRefreshing = true;
                if (box.Indicator is PullToRefreshIndicatorBox custom) custom.IsRefreshing = true;
            }
        };
        var root = new Grid { Children = { box } };
        host.Mount(root); host.Frame(0);
        if (c.Action == "body") { viewer.Offset = new Vector(0, 240); host.Draw(); }
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(host.Window)!;
        var inputRoot = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("pull", "pull-refresh-box", null,
                [Measurement.Numeric("fraction", state.DistanceFraction, "fraction"),
                 Measurement.Numeric("body-offset", viewer.Offset.Y, "dip"),
                 Measurement.Numeric("requests", requests, "count"),
                 Measurement.Flag("refreshing", box.IsRefreshing),
                 Measurement.Flag("animating", state.IsAnimating)])], true));
        Sample("initial");
        for (long time = 16000; time <= 1408000; time += 16000)
        {
            host.AdvanceTo(time);
            var y = time switch { 16000 => 30d, 32000 => 100, 48000 => 170, 64000 => 230,
                96000 when c.Action == "reverse" => 180,
                128000 when c.Action == "reverse" => 70,
                160000 => c.Action == "reverse" ? 70 : 230, _ => -1 };
            if (y >= 0)
            {
                var type = time == 16000 ? RawPointerEventType.TouchBegin :
                    time == 160000 ? c.Action == "cancel" ? RawPointerEventType.TouchCancel : RawPointerEventType.TouchEnd
                    : RawPointerEventType.TouchUpdate;
                input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch,
                    (ulong)(1000 + time / 1000), inputRoot, type, new Point(60, y), RawInputModifiers.None, 1L)!);
                writer.Record(time, "input", $"touch {type} (60,{y})");
                // Core runs the input coroutine queue before performFrame. Drain the
                // real compositor input/snapshot/release queue before our frame too;
                // Draw does not advance the animation clock or bypass that FIFO.
                host.Draw();
            }
            if (time == 208000 && c.Action == "detach") { root.Children.Clear(); writer.Record(time, "mutation", "detach box"); }
            if (time == 800000 && c.Action == "refresh")
            {
                box.IsRefreshing = false;
                if (box.Indicator is PullToRefreshIndicatorBox custom) custom.IsRefreshing = false;
                writer.Record(time, "mutation", "complete refresh");
            }
            host.Frame(time);
            if (time is 32000 or 48000 or 64000 or 96000 or 128000 or 160000 or 176000 or 208000 or 240000 or 400000 or 640000 or 800000 or 960000 or 1408000)
                Sample($"time-{time}");
        }
    }
}
