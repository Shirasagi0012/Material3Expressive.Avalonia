using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class RepeatableSpecTest
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(600);

    private static TargetBasedAnimation<double> Animate(
        IAnimationSpec<double> spec,
        double from = 0,
        double to = 1000)
        => new(spec, new DoubleConverter(), from, to);

    private static TweenSpec<double> Tween(int durationMillis)
        => new(TimeSpan.FromMilliseconds(durationMillis), easing: new LinearEasing());

    [Fact]
    public void RestartCyclesThroughIterations()
    {
        var animation = Animate(new RepeatableSpec<double>(5, Tween(600), RepeatMode.Restart));

        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(150)), 3.0);
        // The second iteration restarts from the initial value.
        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(750)), 3.0);
        Assert.Equal(1000, animation.GetValue(TimeSpan.FromMilliseconds(600 - 1)), 3.0);
    }

    [Fact]
    public void ReverseCyclesBackAndForth()
    {
        var animation = Animate(new RepeatableSpec<double>(5, Tween(600), RepeatMode.Reverse));

        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(150)), 3.0);
        // The second iteration plays backwards.
        Assert.Equal(750, animation.GetValue(TimeSpan.FromMilliseconds(750)), 3.0);
        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(1350)), 3.0);
    }

    [Fact]
    public void DurationIsIterationsTimesIterationDuration()
    {
        var spec = new RepeatableSpec<double>(5, Tween(600));
        var animation = Animate(spec);

        Assert.Equal(TimeSpan.FromMilliseconds(3000), animation.Duration);
        Assert.False(animation.IsInfinite);
    }

    [Fact]
    public void StartOffsetDelayAndFastForward()
    {
        var offset = (int)Duration.TotalMilliseconds / 2;

        var plain = Animate(new RepeatableSpec<double>(5, Tween(600)));
        var delayed = Animate(new RepeatableSpec<double>(5, Tween(600), initialStartOffset: new StartOffset(offset)));
        var fastForwarded = Animate(new RepeatableSpec<double>(5, Tween(600), initialStartOffset: new StartOffset(offset, StartOffsetType.FastForward)));

        Assert.Equal(plain.Duration + TimeSpan.FromMilliseconds(offset), delayed.Duration);
        Assert.Equal(plain.Duration - TimeSpan.FromMilliseconds(offset), fastForwarded.Duration);

        // The delay shifts the whole animation; during the delay the value is unchanged.
        for (var t = 0; t <= offset; t += 10)
            Assert.Equal(plain.GetValue(TimeSpan.Zero), delayed.GetValue(TimeSpan.FromMilliseconds(t)));

        // Fast forward starts the animation right away from the given play time.
        for (var t = 0; t < 3 * 600; t += 17)
        {
            Assert.Equal(
                plain.GetValue(TimeSpan.FromMilliseconds(t)),
                fastForwarded.GetValue(TimeSpan.FromMilliseconds(t - offset)),
                3);
            Assert.Equal(
                plain.GetValue(TimeSpan.FromMilliseconds(t)),
                delayed.GetValue(TimeSpan.FromMilliseconds(t + offset)),
                3);
        }
    }

    [Fact]
    public void InfiniteRepeatableNeverFinishes()
    {
        var animation = Animate(new InfiniteRepeatableSpec<double>(Tween(600)));

        Assert.True(animation.IsInfinite);
        Assert.Equal(TimeSpan.MaxValue, animation.Duration);
        // Values cycle; 750ms lands in the 2nd iteration.
        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(150)), 3.0);
        Assert.Equal(250, animation.GetValue(TimeSpan.FromMilliseconds(750)), 3.0);
    }

    [Fact]
    public void InfiniteRepeatableRejectsZeroDuration()
    {
        Assert.Throws<ArgumentException>(
            () => new InfiniteRepeatableSpec<double>(new SnapSpec<double>()));
    }

    [Fact]
    public void RepeatableRejectsZeroIterations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RepeatableSpec<double>(0, Tween(600)));
    }
}
