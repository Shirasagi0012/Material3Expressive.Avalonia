using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Parity;

[ParitySuite("motion")]
public sealed class MotionCases
{
    public static IEnumerable<CaseData<EmptyConfig>> Configurations() =>
        [new("interrupted", "Interrupted linear tween", new())];


    [ParityScenario("interrupted", Data = nameof(Configurations))]
    public async Task MotionInterrupted(CaptureWriter writer)
    {
        _ = writer.Metadata.ReadConfig<EmptyConfig>();
        await using var host = new AvaloniaHost(writer);
        var marker = new Border { Width = 18, Height = 18, Background = Brush.Parse("#6750A4") };
        Canvas.SetLeft(marker, 24);
        Canvas.SetTop(marker, 64);
        host.Mount(new Canvas { Children = { marker } });
        host.Frame(-2000);
        host.Frame(-1000);
        var scope = VisualAnimationScope.Create(marker);
        var value = scope.CreateAnimatable(0d);
        scope.SubscribeValue(value, x => Canvas.SetLeft(marker, 24 + x));

        void Target(double target)
        {
            writer.Record(host.TimeUs, "target", target.ToString(System.Globalization.CultureInfo.InvariantCulture),
                new { value = target, unit = "dip" });
            _ = host.Window.AnimationRoot.Launch(ct => value.AnimateToAsync(target,
                    new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: ComposeEasings.Linear), cancellationToken: ct),
                "Motion probe");
        }

        void Sample(string id, bool image) => writer.Submit(host.Snapshot(id, "interrupted-tween", [
            new("marker", "container", null,
            [
                .. AvaloniaMeasurements.Geometry(marker, host.Window), Measurement.Numeric("value", value.Value, "dip", category: "motion"),
                Measurement.Numeric("velocity", value.Velocity, "dip/s", category: "motion"),
                Measurement.Flag("running", value.IsRunning, "motion")
            ])
        ], image));

        Target(100);
        foreach (var time in new long[] { 0, 16000, 40000 })
        {
            host.Frame(time);
            Sample("forward-" + time, time != 16000);
        }

        Target(-20);
        foreach (var time in new long[] { 56000, 80000, 140000, 160000 })
        {
            host.Frame(time);
            Sample("reverse-" + time, time != 80000);
        }
    }
}
