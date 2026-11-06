using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Parity;

public sealed record SpringConfig(double Damping, double Stiffness, float Initial = 2, float Target = 40, float Velocity = 3, float Threshold = .01f);

[ParitySuite("animation-spring")]
public sealed class SpringCases
{
    public static IEnumerable<CaseData<SpringConfig>> Configurations()
    {
        yield return new("dynamic.under", "Underdamped Float boundaries", new(.9, 700.25));
        yield return new("dynamic.critical", "Critical damping", new(1, 1600));
        yield return new("dynamic.over", "Over damping", new(1.7, 700.25));
        yield return new("dynamic.reverse", "Initial velocity away from target", new(.7, 380, 1, 0, 5));
        yield return new("dynamic.large", "Float displacement cancellation", new(.9, 700.25, 16777216, 3, -2));
        yield return new("dynamic.rest", "At rest", new(.9, 700.25, 4, 4, 0));
    }

    [ParityScenario("sample", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SpringConfig>();
        var animation = new TargetBasedAnimation<float>(new SpringSpec<float>(c.Damping, c.Stiffness, c.Threshold),
            new FloatConverter(), c.Initial, c.Target, c.Velocity);
        await using var host = new AvaloniaHost(writer, 240, 96);
        var marker = new SpringMarker(c.Initial, c.Target); host.Mount(marker);
        foreach (var time in new long[] { 0, 1, 999, 1000, 1001, 15999, 16000, 16001, 32000, 64000, 96000, 160000, 320000, 640000, 2000000 })
        {
            host.AdvanceTo(time); var playTime = TimeSpan.FromTicks(time * 10);
            marker.Value = animation.GetValue(playTime); marker.InvalidateVisual(); host.Draw();
            writer.Submit(host.Snapshot($"time-{time}", $"time-{time}", [new("animation", "spring", null, [
                Measurement.Numeric("value", marker.Value, category: "motion", definition: "spring.value"),
                Measurement.Numeric("velocity", animation.GetVelocity(playTime), "dip/s", category: "motion", definition: "spring.velocity"),
                Measurement.Numeric("duration", animation.Duration.TotalMilliseconds, "ms", category: "motion", definition: "spring.duration"),
                Measurement.Flag("finished", animation.IsFinished(playTime), definition: "spring.finished")])]));
        }
    }

    private sealed class SpringMarker(float initial, float target) : Control
    {
        public float Value { get; set; }
        public override void Render(DrawingContext context)
        {
            var fraction = initial == target ? 1f : (Value - initial) / (target - initial);
            context.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));
            context.DrawRectangle(Brushes.Blue, null, new Rect(24f + fraction * 160f, 36, 12, 24));
        }
    }
}
