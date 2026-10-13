using Avalonia.Threading;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using ComposeAnimation.Runtime;
using System.Text.Json;

namespace ComposeAnimation.Test;

public class ColorConverterTest
{

    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    [Theory]
    [InlineData(0xffff0000u, 1f, 0.6279297f, 0.22485352f, 0.12573242f)]
    [InlineData(0xff00ff00u, 1f, 0.86621094f, -0.23388672f, 0.17944336f)]
    [InlineData(0xff0000ffu, 1f, 0.4519043f, -0.032440186f, -0.31152344f)]
    [InlineData(0x40000000u, 0.2512219f, 0f, 0f, 0f)]
    public void To_UsesAlphaAndOklabComponents(
        uint argb, float alpha, float lightness, float a, float b)
    {
        var vector = new double[4];

        new ColorConverter().To(Color.FromUInt32(argb), vector);

        Assert.Equal((double)alpha, vector[0]);
        Assert.Equal((double)lightness, vector[1]);
        Assert.Equal((double)a, vector[2]);
        Assert.Equal((double)b, vector[3]);
    }

    [Theory]
    [InlineData("to")]
    [InlineData("from")]
    public void Conversion_MatchesFixedComposeColorStorageAndProjection(string kind)
    {
        var converter = new ColorConverter();
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ColorConverter", "compose-reference.jsonl");
        foreach (var line in File.ReadLines(fixture))
        {
            using var document = JsonDocument.Parse(line);
            var record = document.RootElement;
            if (record.GetProperty("kind").GetString() != kind) continue;
            var expectedVector = record.GetProperty("vector").EnumerateArray()
                .Select(component => (double)component.GetSingle()).ToArray();
            var vector = expectedVector;
            if (kind == "to")
            {
                vector = new double[4];
                converter.To(Color.FromUInt32(Convert.ToUInt32(record.GetProperty("argb").GetString(), 16)), vector);
                Assert.True(expectedVector.SequenceEqual(vector), $"Compose input vector differs: {line}; actual [{string.Join(", ", vector)}]");
            }
            var expected = Color.FromUInt32(Convert.ToUInt32(record.GetProperty("output").GetString(), 16));
            Assert.True(expected == converter.From(vector), $"Compose output color differs: {line}; actual {converter.From(vector)}");
        }
    }

    [Fact]
    public void Spring_AtRailReverseSample_MatchesFixedComposeColor()
    {
        var animation = new TargetBasedAnimation<Color>(new SpringSpec<Color>(1, 1600),
            new ColorConverter(), Color.Parse("#49454F"), Color.Parse("#1D192B"));
        Assert.Equal(Color.Parse("#2E2B3A"), animation.GetValue(TimeSpan.FromMilliseconds(49)));
    }

    [Fact]
    public Task Animatable_EndsAtOriginalColor_AndSnapKeepsItsExactValue() => OnUi(async () =>
    {
        var converter = new ColorConverter();
        var target = Color.FromUInt32(0xFF00FE01);
        var vector = new double[4];
        converter.To(target, vector);
        // The actual fixed Compose color round trip loses this blue byte.
        Assert.Equal(Color.FromUInt32(0xFF00FE00), converter.From(vector));
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animation = new Animatable<Color>(converter, Colors.Black);
        var task = animation.AnimateToAsync(scope, target, new SpringSpec<Color>(1, 1600));
        clock.Pump(Frame);
        clock.Pump(TimeSpan.FromSeconds(2));
        await task;
        Assert.Equal(target, animation.Value);
        Assert.Equal(target, animation.TargetValue);
        Assert.False(animation.IsRunning);
        var next = animation.AnimateToAsync(scope, Colors.Red);
        await animation.SnapToAsync(target);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next);
        Assert.Equal(target, animation.Value);
        Assert.Equal(target, animation.TargetValue);
        window.Close();
    });

    [Fact]
    public void ColorSprings_MatchFixedComposeAtIntegerFrameSamples()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ColorConverter", "compose-reference.jsonl");
        foreach (var line in File.ReadLines(fixture))
        {
            using var document = JsonDocument.Parse(line);
            var record = document.RootElement;
            if (record.GetProperty("kind").GetString() != "animation") continue;
            Color ReadColor(string property) => Color.FromUInt32(Convert.ToUInt32(record.GetProperty(property).GetString(), 16));
            var animation = new TargetBasedAnimation<Color>(
                new SpringSpec<Color>(record.GetProperty("damping").GetSingle(), record.GetProperty("stiffness").GetSingle()),
                new ColorConverter(), ReadColor("initial"), ReadColor("target"));
            var actual = animation.GetValue(TimeSpan.FromMilliseconds(record.GetProperty("timeMillis").GetInt64()));
            Assert.True(ReadColor("output") == actual, $"Compose spring color differs: {line}; actual {actual}");
        }
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x00ff0000u)]
    [InlineData(0x7f123456u)]
    [InlineData(0xff00ff00u)]
    [InlineData(0xffffffffu)]
    public void Conversion_RoundTripsByteSrgb(uint argb)
    {
        var converter = new ColorConverter();
        var vector = new double[4];
        var expected = Color.FromUInt32(argb);

        converter.To(expected, vector);
        var actual = converter.From(vector);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void From_ClampsOklabComponentsBeforeConvertingToSrgb()
    {
        var converter = new ColorConverter();

        var outside = converter.From([2, 2, 1, -1]);
        var clamped = converter.From([1, 1, 0.5, -0.5]);
        var below = converter.From([-1, -1, -1, 1]);
        var lowerBounds = converter.From([0, 0, -0.5, 0.5]);

        Assert.Equal(clamped, outside);
        Assert.Equal(lowerBounds, below);
        Assert.Equal(byte.MaxValue, outside.A);
        Assert.Equal(byte.MinValue, below.A);
    }

    [Fact]
    public void Tween_MidpointInterpolatesOklabInsteadOfArgbBytes()
    {
        var animation = new TargetBasedAnimation<Color>(
            new TweenSpec<Color>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()),
            new ColorConverter(), Colors.Black, Colors.White);

        var midpoint = animation.GetValue(TimeSpan.FromMilliseconds(50));

        Assert.Equal(Color.FromRgb(99, 99, 99), midpoint);
        Assert.NotEqual(Color.FromRgb(128, 128, 128), midpoint);
    }

    [Fact]
    public void Tween_InterpolatesAlphaIndependentlyFromOklabChannels()
    {
        var animation = new TargetBasedAnimation<Color>(
            new TweenSpec<Color>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()),
            new ColorConverter(), Color.FromArgb(0, 255, 0, 0), Colors.Red);

        var midpoint = animation.GetValue(TimeSpan.FromMilliseconds(50));

        Assert.Equal(Color.FromArgb(128, 255, 0, 0), midpoint);
    }

    [Fact]
    public void SpringOvershoot_IsClampedWhenProjectedToColor()
    {
        var converter = new ColorConverter();
        var spec = new SpringSpec<Color>(dampingRatio: 0.1, stiffness: 50);
        var vectorized = spec.Vectorize(converter);
        var initial = new double[4];
        var target = new double[4];
        converter.To(Color.FromArgb(0, 255, 0, 0), initial);
        converter.To(Colors.Red, target);
        var velocity = new double[] { 100, 0, 0, 0 };
        var sampled = new double[4];
        vectorized.GetValue(Frame, initial, target, velocity, sampled);
        var animation = new TargetBasedAnimation<Color>(
            spec, converter, Color.FromArgb(0, 255, 0, 0), Colors.Red,
            initialVelocityVector: AnimationVector.From(velocity));

        var color = animation.GetValue(Frame);

        Assert.True(sampled[0] > 1);
        Assert.Equal(byte.MaxValue, color.A);
        Assert.Equal(Colors.Red, color);
    }

    [Fact]
    public void AnimateColor_UsesColorConverterAndOklabSampling()
    {
        using var transition = new Transition<bool>(false);
        var color = transition.AnimateColor(
            state => state ? Colors.White : Colors.Black,
            _ => new TweenSpec<Color>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()));

        transition.UpdateTarget(true);
        transition.OnFrame(TimeSpan.Zero);
        transition.OnFrame(TimeSpan.FromMilliseconds(50));

        Assert.IsType<ColorConverter>(color.TypeConverter);
        Assert.Equal("ColorAnimation", color.Label);
        Assert.Equal(Color.FromRgb(99, 99, 99), color.Value);
    }

    [Fact]
    public void TransitionInterruption_PreservesUnprojectedOklabVelocity()
    {
        var converter = new ColorConverter();
        using var transition = new Transition<int>(0);
        var color = transition.AnimateColor(
            state => state switch { 0 => Colors.Red, 1 => Colors.Blue, _ => Colors.Lime },
            _ => new TweenSpec<Color>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()));
        transition.UpdateTarget(1);
        transition.OnFrame(TimeSpan.Zero);
        transition.OnFrame(TimeSpan.FromMilliseconds(400));
        var initialValue = color.Value;
        var initialVelocity = AnimationVector.From(color.VelocityVector);

        transition.UpdateTarget(2);
        AssertVectorsEqual(initialVelocity.AsReadOnlySpan(), color.VelocityVector);
        var expected = new TargetBasedAnimation<Color>(
            new SpringSpec<Color>(), converter, initialValue, Colors.Lime,
            initialVelocityVector: initialVelocity);
        transition.OnFrame(TimeSpan.FromMilliseconds(416));
        var expectedVelocity = new double[4];
        expected.GetVelocityVector(Frame, expectedVelocity);

        Assert.Equal(new TransitionSegment<int>(1, 2), transition.Segment);
        AssertVectorsEqual(expectedVelocity, color.VelocityVector);
    }

    [Fact]
    public Task AnimatableInterruption_PreservesUnprojectedOklabVelocity() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var converter = new ColorConverter();
        var animatable = new Animatable<Color>(converter, Colors.Red);
        var first = animatable.AnimateToAsync(
            scope,
            Colors.Blue,
            new TweenSpec<Color>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()));
        clock.Pump(Frame);
        clock.Pump(TimeSpan.FromMilliseconds(400));
        var initialValue = animatable.Value;
        var initialVelocity = AnimationVector.From(animatable.State.VelocityVector);

        var second = animatable.AnimateToAsync(scope, Colors.Lime, new SpringSpec<Color>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        AssertVectorsEqual(initialVelocity.AsReadOnlySpan(), animatable.State.VelocityVector);
        var expected = new TargetBasedAnimation<Color>(
            new SpringSpec<Color>(), converter, initialValue, Colors.Lime,
            initialVelocityVector: initialVelocity);
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Frame);
        var expectedVelocity = new double[4];
        expected.GetVelocityVector(Frame, expectedVelocity);

        AssertVectorsEqual(expectedVelocity, animatable.State.VelocityVector);
        animatable.Stop();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        window.Close();
    });

    private static void AssertVectorsEqual(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], actual[i], 8);
    }
}
