using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class KeyframesSpecTest
{
    private static TargetBasedAnimation<double> Animate(KeyframesSpec<double> spec, double from = 0, double to = 100)
        => new(spec, new DoubleConverter(), from, to);

    [Fact]
    public void InterpolatesLinearlyByDefault()
    {
        var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
        config.At(0, 0);
        config.At(400, 100);

        var animation = Animate(new KeyframesSpec<double>(config));

        Assert.Equal(0, animation.GetValue(TimeSpan.Zero));
        Assert.Equal(50, animation.GetValue(TimeSpan.FromMilliseconds(200)), 0.001);
        Assert.Equal(100, animation.GetValue(TimeSpan.FromMilliseconds(400)));
        // Slope of 100 per 400ms = 250 per second.
        Assert.Equal(250, animation.GetVelocity(TimeSpan.FromMilliseconds(200)), 0.001);
    }

    [Fact]
    public void ReturnsExactValuesAtKeyframeTimestamps()
    {
        var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
        config.At(150, 80);

        var animation = Animate(new KeyframesSpec<double>(config));

        Assert.Equal(80, animation.GetValue(TimeSpan.FromMilliseconds(150)));
        // Before the first keyframe the value moves from the initial value.
        Assert.Equal(40, animation.GetValue(TimeSpan.FromMilliseconds(75)), 0.001);
        // After the keyframe it moves towards the target.
        Assert.Equal(90, animation.GetValue(TimeSpan.FromMilliseconds(275)), 0.001);
    }

    [Fact]
    public void AppliesCustomEasingPerInterval()
    {
        var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
        config.At(0, 0).Using(new CubicEaseIn());
        config.At(200, 100);
        config.At(400, 200);

        var animation = Animate(new KeyframesSpec<double>(config));

        // CubicEaseIn stays below linear for the first half of the interval.
        Assert.True(animation.GetValue(TimeSpan.FromMilliseconds(100)) < 50);
        // The second interval keeps the default linear easing.
        Assert.Equal(150, animation.GetValue(TimeSpan.FromMilliseconds(300)), 0.001);
    }

    [Fact]
    public void KeyframeAtZeroOverridesInitialValueDuringDelay()
    {
        var config = new KeyframesSpecConfig<double>
        {
            Duration = TimeSpan.FromMilliseconds(400),
            Delay = TimeSpan.FromMilliseconds(100),
        };
        config.At(0, 42);

        var animation = Animate(new KeyframesSpec<double>(config));

        // During the delay the animation holds the keyframe value at time 0.
        Assert.Equal(42, animation.GetValue(TimeSpan.Zero));
        Assert.Equal(42, animation.GetValue(TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public void AtFractionPlacesKeyframeAtFractionOfDuration()
    {
        var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
        config.AtFraction(0.5, 50);

        var animation = Animate(new KeyframesSpec<double>(config));

        Assert.Equal(50, animation.GetValue(TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void TwoDimensionalValuesInterpolatePerDimension()
    {
        var config = new KeyframesSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(400) };
        config.At(0, new Point(0, 0));
        config.At(400, new Point(100, 200));

        var animation = new TargetBasedAnimation<Point>(
            new KeyframesSpec<Point>(config), new PointConverter(), new Point(0, 0), new Point(100, 200));

        var value = animation.GetValue(TimeSpan.FromMilliseconds(200));
        Assert.Equal(50, value.X, 0.001);
        Assert.Equal(100, value.Y, 0.001);
    }

    [Fact]
    public void VelocityHasPositiveSignForIncreasingValues()
    {
        var config = new KeyframesSpecConfig<double> { Duration = TimeSpan.FromMilliseconds(400) };
        config.At(0, 0);
        config.At(400, 100);

        var animation = Animate(new KeyframesSpec<double>(config));

        Assert.True(animation.GetVelocity(TimeSpan.FromMilliseconds(200)) > 0);
    }
}
