using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Parity;

public sealed record TweenConfig(double A, double B, double C, double D, int Duration = 125, int Delay = 0);

[ParitySuite("animation-tween")]
public sealed class TweenCases
{
    public static IEnumerable<CaseData<TweenConfig>> Configurations()
    {
        yield return new("dynamic.standard", "Material enter", new(.4, 0, .2, 1));
        yield return new("dynamic.exit", "Material exit", new(.4, 0, .6, 1));
        yield return new("dynamic.linear-cubic", "Degenerate cubic", new(1f / 3f, 1f / 3f, 2f / 3f, 2f / 3f));
        yield return new("dynamic.quadratic", "Quadratic root branch", new(0, 0, 1f / 3f, 1));
        yield return new("dynamic.flat-start", "Zero initial X derivative", new(0, 0, 0, 1));
        yield return new("dynamic.flat-end", "Zero final X derivative", new(1, 0, 1, 1));
        yield return new("dynamic.overshoot", "Vertical extrema above one", new(.34, 1.56, .64, 1));
        yield return new("dynamic.undershoot", "Vertical extrema below zero", new(.36, -.56, .66, 0));
        yield return new("dynamic.delay", "Delayed tween velocity", new(.4, 0, .2, 1, Delay: 17));
        yield return new("dynamic.zero-duration", "Zero duration with delay", new(.4, 0, .2, 1, Duration: 0, Delay: 17));
    }

    [ParityScenario("sample", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TweenConfig>();
        var easing = new CubicBezierEasing(c.A, c.B, c.C, c.D);
        var animation = new TargetBasedAnimation<float>(
            new TweenSpec<float>(TimeSpan.FromMilliseconds(c.Duration), TimeSpan.FromMilliseconds(c.Delay), easing),
            new FloatConverter(), 2, 40, 3);
        await using var host = new AvaloniaHost(writer, 240, 96);
        var marker = new TweenMarker();
        host.Mount(marker);
        foreach (var time in new long[] { 0, 1, 1000, 16000, 17000, 18000, 32000, 48000, 64000, 96000, 124999, 125000, 141999, 142000, 160000 })
        {
            host.AdvanceTo(time);
            var playTime = TimeSpan.FromTicks(time * 10);
            marker.Value = animation.GetValue(playTime);
            marker.InvalidateVisual();
            host.Draw();
            writer.Submit(host.Snapshot($"time-{time}", $"time-{time}", [new("animation", "tween", null, [
                Measurement.Numeric("value", marker.Value, category: "motion", definition: "tween.value", source: "TargetBasedAnimation<float>.GetValue"),
                Measurement.Numeric("velocity", animation.GetVelocity(playTime), "dip/s", category: "motion", definition: "tween.velocity", source: "TargetBasedAnimation<float>.GetVelocity"),
                Measurement.Numeric("easing", easing.Ease((float)time / 125000f), "fraction", category: "motion", definition: "tween.easing", source: "CubicBezierEasing.Ease"),
                Measurement.Flag("finished", animation.IsFinished(playTime), definition: "tween.finished")])]));
        }
    }

    private sealed class TweenMarker : Control
    {
        public float Value { get; set; }
        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));
            context.DrawRectangle(Brushes.Blue, null, new Rect(24f + Value * 3f, 36, 12, 24));
        }
    }
}
