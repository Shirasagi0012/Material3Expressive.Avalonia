using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record RippleConfig(double Width = 97, double Height = 53, string Action = "hold");

[ParitySuite("ripple")]
public sealed class RippleCases
{
    public static IEnumerable<CaseData<RippleConfig>> Configurations()
    {
        foreach (var action in new[] { "hold", "quick", "cancel", "overlap", "detach" })
            yield return new($"dynamic.{action}", "Shared ripple on a real clickable surface", new(Action: action));
        yield return new("dynamic.wide", "Non-square ripple geometry", new(213, 49));
    }

    [ParityScenario("press", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<RippleConfig>();
        await using var host = new AvaloniaHost(writer, 280, 160);
        var source = new MaterialInteractionSource();
        var surface = new ClickableSurface
        {
            Width = config.Width, Height = config.Height, Background = Brushes.White,
            Foreground = Brushes.Black, InteractionSource = source,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var root = new Grid { Background = Brushes.White, Children = { surface } };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time)
        {
            while (next <= time) { host.Frame(next); next += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id)
        {
            var controller = MaterialInteraction.GetOrCreate(surface);
            var ripples = (List<RippleAnimation>)typeof(MaterialInteractionController)
                .GetField("_ripples", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(controller)!;
            var points = new List<ProbePoint>
            {
                new("state", "state", null, [Measurement.Numeric("count", ripples.Count, "count")])
            };
            for (var i = 0; i < ripples.Count; i++)
            {
                var ripple = ripples[i];
                points.Add(new($"ripple-{i}", "ripple", null,
                [Measurement.Numeric("alpha", ripple.Value, "ratio", category: "motion"),
                 Measurement.Numeric("radius", ripple.Radius, "dip", category: "motion"),
                 Measurement.Numeric("targetRadius", ripple.TargetRadius, "dip"),
                 Measurement.Numeric("centerX", ripple.Center.X, "dip", category: "motion"),
                 Measurement.Numeric("centerY", ripple.Center.Y, "dip", category: "motion")]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: id != "time-32000"));
        }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "interaction", id); host.Draw(); Sample(id); }
        void Times(params long[] times)
        { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        var press = new MaterialInteractionEvent.Press(new Point(7.25, 11.5));
        Sample("initial");
        Change(1000, "press", () => source.Emit(press));
        if (config.Action == "quick") Change(5000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
        Times(16000, 32000, 64000);
        if (config.Action == "cancel") Change(65000, "cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(press)));
        if (config.Action == "detach") Change(65000, "detach", () => root.Children.Remove(surface));
        MaterialInteractionEvent.Press? second = null;
        if (config.Action == "overlap") Change(65000, "second", () => source.Emit(second = new(new Point(31.75, 19.25))));
        Times(80000, 160000, 240000);
        if (config.Action is "hold" or "overlap")
            Change(241000, "release", () => source.Emit(new MaterialInteractionEvent.Release(second ?? press)));
        Times(256000, 320000, 400000, 640000);
    }
}
