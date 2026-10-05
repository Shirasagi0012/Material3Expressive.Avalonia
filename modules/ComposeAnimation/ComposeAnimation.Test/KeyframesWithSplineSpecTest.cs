using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

using Avalonia = global::Avalonia;

public class KeyframesWithSplineSpecTest
{
    [Fact]
    public void InterpolatesValuesWithMonotoneSpline()
    {
        var config = new KeyframesWithSplineSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(500) };
        // At half the time, reach the final Y value (expected final value is Point(2, 2)).
        config.At(250, new Point(1, 2));

        var spec = new KeyframesWithSplineSpec<Point>(config);

        var animation = new TargetBasedAnimation<Point>(spec, new PointConverter(), new Point(0, 0), new Point(2, 2));

        // At a quarter of the time.
        var value = animation.GetValue(TimeSpan.FromMilliseconds(125));
        Assert.Equal(0.5, value.X, 0.001);
        Assert.Equal(1.25, value.Y, 0.001);

        var velocity = animation.GetVelocity(TimeSpan.FromMilliseconds(125));
        Assert.Equal(4, velocity.X, 0.001);
        Assert.Equal(10, velocity.Y, 0.001);

        // At three quarters of the time. The change in X is completely linear, so its speed is
        // constant, while Y has no change and its velocity is 0.
        value = animation.GetValue(TimeSpan.FromMilliseconds(375));
        Assert.Equal(1.5, value.X, 0.001);
        Assert.Equal(2, value.Y, 0.001);

        velocity = animation.GetVelocity(TimeSpan.FromMilliseconds(375));
        Assert.Equal(4, velocity.X, 0.001);
        Assert.Equal(0, velocity.Y, 0.001);
    }

    [Fact]
    public void UpdatesValuesWhenInitialAndTargetChange()
    {
        var config = new KeyframesWithSplineSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(500) };
        config.At(250, new Point(1, 2));

        var spec = new KeyframesWithSplineSpec<Point>(config);

        // Start from (2, 2) moving to (0, 0), so the values are mirrored around the keyframe.
        var animation = new TargetBasedAnimation<Point>(spec, new PointConverter(), new Point(2, 2), new Point(0, 0));

        var value = animation.GetValue(TimeSpan.FromMilliseconds(375));
        Assert.Equal(0.5, value.X, 0.001);
        Assert.Equal(1.25, value.Y, 0.001);

        var velocity = animation.GetVelocity(TimeSpan.FromMilliseconds(375));
        Assert.Equal(-4, velocity.X, 0.001);
        Assert.Equal(-10, velocity.Y, 0.001);
    }

    [Fact]
    public void PeriodicBiasMakesStartAndEndVelocityEqual()
    {
        var config = new KeyframesWithSplineSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(1000) };
        config.At(500, 1);

        var regular = Animate(new KeyframesWithSplineSpec<double>(config));
        var balanced = Animate(new KeyframesWithSplineSpec<double>(config, 0.5));
        var startBiased = Animate(new KeyframesWithSplineSpec<double>(config, 0));
        var endBiased = Animate(new KeyframesWithSplineSpec<double>(config, 1));

        // Note that the start velocity is sampled at exactly 0 since any positive play time
        // samples the interpolated velocity inside the first interval (Compose samples at 1ns,
        // which clamps to the same boundary tangent).
        var regularStart = regular.GetVelocity(TimeSpan.Zero);
        var regularEnd = regular.GetVelocity(TimeSpan.FromMilliseconds(1000));

        var balancedStart = balanced.GetVelocity(TimeSpan.Zero);
        var balancedEnd = balanced.GetVelocity(TimeSpan.FromMilliseconds(1000));

        var startBiasedStart = startBiased.GetVelocity(TimeSpan.Zero);
        var startBiasedEnd = startBiased.GetVelocity(TimeSpan.FromMilliseconds(1000));

        var endBiasedStart = endBiased.GetVelocity(TimeSpan.Zero);
        var endBiasedEnd = endBiased.GetVelocity(TimeSpan.FromMilliseconds(1000));

        // On splines with periodic bias, the start and end velocities are the same.
        Assert.Equal(balancedStart, balancedEnd, 0.001);
        Assert.Equal(startBiasedStart, startBiasedEnd, 0.001);
        Assert.Equal(endBiasedStart, endBiasedEnd, 0.001);

        // Balanced bias velocities are the average of the regular spline's start/end velocities.
        var average = (regularStart + regularEnd) / 2;
        Assert.Equal(average, balancedStart, 0.001);
        Assert.Equal(average, balancedEnd, 0.001);

        // Fully biased at the start keeps the end velocity unchanged.
        Assert.Equal(regularEnd, startBiasedEnd, 0.001);
        // Fully biased at the end keeps the start velocity unchanged.
        Assert.Equal(regularStart, endBiasedStart, 0.001);
    }

    [Fact]
    public void AppliesEasingPerInterval()
    {
        var config = new KeyframesWithSplineSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(300) };
        config.At(0, new Point(0, 0)).Using(new Avalonia.Animation.Easings.CubicEaseIn());
        config.At(100, new Point(1, 1)).Using(new Avalonia.Animation.Easings.CubicEaseOut());
        config.At(200, new Point(2, 2)).Using(new Avalonia.Animation.Easings.LinearEasing());
        config.At(300, new Point(3, 3));

        var spec = new KeyframesWithSplineSpec<Point>(config);
        var animation = new TargetBasedAnimation<Point>(spec, new PointConverter(), new Point(0, 0), new Point(3, 3));

        // CubicEaseIn is always below linear, CubicEaseOut always above it.
        var value = animation.GetValue(TimeSpan.FromMilliseconds(50));
        Assert.True(value.X < 0.5);
        Assert.True(value.Y < 0.5);

        value = animation.GetValue(TimeSpan.FromMilliseconds(150));
        Assert.True(value.X > 1.5);
        Assert.True(value.Y > 1.5);

        value = animation.GetValue(TimeSpan.FromMilliseconds(250));
        Assert.Equal(2.5, value.X, 0.001);
        Assert.Equal(2.5, value.Y, 0.001);
    }

    private static TargetBasedAnimation<double> Animate(KeyframesWithSplineSpec<double> spec)
        => new(spec, new DoubleConverter(), 0, 0);
}
