using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

using Avalonia = global::Avalonia;

public sealed class TargetBasedAnimationTest
{
    private static TargetBasedAnimation<double> Tween(
        TimeSpan? duration = null,
        TimeSpan? delay = null,
        double initial = 0,
        double target = 100,
        double initialVelocity = 0)
        => new(
            new TweenSpec<double>(duration, delay, new LinearEasing()),
            new DoubleConverter(),
            initial,
            target,
            initialVelocity);

    [Fact]
    public void TweenReturnsInitialValueDuringDelay()
    {
        var animation = Tween(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50));

        Assert.Equal(0, animation.GetValue(TimeSpan.Zero));
        Assert.Equal(0, animation.GetValue(TimeSpan.FromMilliseconds(49)));
        Assert.Equal(0, animation.GetValue(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void TweenReturnsTargetAfterDuration()
    {
        var animation = Tween(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50));

        Assert.Equal(100, animation.GetValue(TimeSpan.FromMilliseconds(150)));
        Assert.Equal(100, animation.GetValue(TimeSpan.FromSeconds(1)));
        Assert.True(animation.IsFinished(TimeSpan.FromMilliseconds(150)));
    }

    [Fact]
    public void TweenVelocityOutsideActiveIntervalFollowsClampedBoundary()
    {
        var animation = Tween(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), initialVelocity: 25);

        // During the delay the velocity is the initial velocity.
        Assert.Equal(25, animation.GetVelocity(TimeSpan.FromMilliseconds(50)));
        // At and after the end, the velocity is the 1 ms boundary difference (linear easing).
        Assert.Equal(1000, animation.GetVelocity(TimeSpan.FromMilliseconds(150)));
        Assert.Equal(1000, animation.GetVelocity(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void SampleUsesOneTimeAndReportsCompletion()
    {
        var animation = Tween(TimeSpan.FromMilliseconds(100));

        var sample = animation.Sample(TimeSpan.FromMilliseconds(25));
        Assert.Equal(TimeSpan.FromMilliseconds(25), sample.PlayTime);
        Assert.Equal(25, sample.Value, precision: 8);
        Assert.False(sample.IsFinished);

        var completed = animation.Sample(animation.Duration);
        Assert.Equal(100, completed.Value);
        Assert.Equal(1000, completed.Velocity);
        Assert.True(completed.IsFinished);
    }

    [Fact]
    public void SampleNormalizesNegativeTime()
    {
        var animation = Tween();

        var sample = animation.Sample(TimeSpan.FromMilliseconds(-10));

        Assert.Equal(TimeSpan.Zero, sample.PlayTime);
        Assert.Equal(0, sample.Value);
        Assert.False(sample.IsFinished);
    }

    [Fact]
    public void SpringCompletesOnlyAfterAllVectorDimensionsSettle()
    {
        var spec = new SpringSpec<Avalonia.Point>(visibilityThreshold: new Avalonia.Point(0.01, 0.01));
        var animation = new TargetBasedAnimation<Avalonia.Point>(
            spec,
            new PointConverter(),
            new Avalonia.Point(0, 0),
            new Avalonia.Point(100, 100));

        var sample = animation.Sample(animation.Duration);

        Assert.True(sample.IsFinished);
        Assert.Equal(100, sample.Value.X, 1);
        Assert.Equal(100, sample.Value.Y, 1);
    }

    [Fact]
    public void ZeroInitialVelocityIsPreserved()
    {
        var animation = Tween(initialVelocity: 0);

        Assert.Equal(0, animation.InitialVelocity);
    }

    [Fact]
    public void LossyConverter_DoesNotProjectEndpointPropertiesOrCompletedValue()
    {
        var animation = new TargetBasedAnimation<double>(
            new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()),
            new QuantizingConverter(), 0.25, 1.75);
        Assert.Equal(0.25, animation.InitialValue);
        Assert.Equal(1.75, animation.TargetValue);
        Assert.Equal(1.75, animation.GetValue(animation.Duration));
        Assert.Equal(1.75, animation.Sample(TimeSpan.FromSeconds(1)).Value);
    }

    private sealed class QuantizingConverter : ITwoWayConverter<double>
    {
        public int Dimension => 1;
        public void To(double value, Span<double> destination) => destination[0] = value;
        public double From(ReadOnlySpan<double> vector) => Math.Floor(vector[0]);
    }

    [Fact]
    public void RejectsNonPositiveConverterDimension()
    {
        var converter = new InvalidConverter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetBasedAnimation<double>(new TweenSpec<double>(), converter, 0, 1));
    }

    private sealed class InvalidConverter : ITwoWayConverter<double>
    {
        public int Dimension => 0;
        public void To(double value, Span<double> destination) { }
        public double From(ReadOnlySpan<double> vector) => 0;
    }
}
