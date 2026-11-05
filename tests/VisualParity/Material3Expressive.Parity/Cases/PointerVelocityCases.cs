using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record PointerVelocityConfig(float[] Positions, long[] Times, long Release, bool MoveContent = false);

[ParitySuite("pointer-velocity")]
public sealed class PointerVelocityCases
{
    public static IEnumerable<CaseData<PointerVelocityConfig>> Configurations()
    {
        yield return new("dynamic.two-points", "down and one slop-crossing move", new([100, 80], [1000, 17000], 33000));
        yield return new("dynamic.three-points", "constant velocity with two moves", new([100, 84, 68], [1000, 17000, 33000], 49000));
        yield return new("dynamic.paused", "release after the 40ms stationary cutoff", new([100, 84, 68], [1000, 17000, 33000], 80000));
        yield return new("dynamic.reverse", "reversal in the measured velocity window", new([100, 84, 76, 84], [1000, 17000, 33000, 49000], 65000));
        yield return new("dynamic.same-time", "two samples with the same event timestamp", new([100, 80], [1000, 1000], 17000));
        yield return new("dynamic.moving-content", "content layout moves beneath the captured pointer", new([100, 80], [1000, 17000], 33000, true));
    }

    [ParityScenario("drag", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<PointerVelocityConfig>();
        await using var host = new AvaloniaHost(writer, 120, 160);
        float delta = 0, velocity = 0; var stops = 0;
        var target = new Border { Width = 120, Height = 160, Background = Brushes.Black };
        target.AddHandler(Avalonia.Input.InputElement.PointerMovedEvent, (_, e) =>
            writer.Record(host.TimeUs, "pointer-observation", "move", new { timestampMs = e.Timestamp,
                sceneY = e.GetPosition(host.Window).Y, localY = e.GetPosition(target).Y }), Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        target.GestureRecognizers.Add(new AppBarVerticalGesture(() => true, () => { },
            (y, _) => { delta += y; if (c.MoveContent) Canvas.SetTop(target, delta); return new Vector(0, y); },
            (y, _, _) => { velocity = y; stops++; }, directDrag: true));
        host.Mount(new Canvas { Children = { target } }); host.Frame(0);
        for (var i = 0; i < c.Times.Length; i++)
        {
            host.AdvanceTo(c.Times[i]);
            host.Input(i == 0 ? "down" : "move", new Point(60, c.Positions[i]));
            if (c.MoveContent) host.Draw();
        }
        host.AdvanceTo(c.Release); host.Input("up", new Point(60, c.Positions[^1]));
        writer.Submit(host.Snapshot("release", "release", [new("gesture", "vertical-drag", null,
            [Measurement.Numeric("delta", delta, "dip"), Measurement.Numeric("velocity", velocity, "dip/s"),
                Measurement.Numeric("stops", stops, "count")])], image: false));
    }
}
