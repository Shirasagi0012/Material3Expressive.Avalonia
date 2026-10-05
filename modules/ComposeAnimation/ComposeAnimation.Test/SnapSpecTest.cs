using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SnapSpecTest
{
    [Fact]
    public void SnapsToTargetAfterDelay()
    {
        var animation = Animate(new SnapSpec<double>(TimeSpan.FromMilliseconds(50)), 0, 100);

        Assert.Equal(0, animation.GetValue(TimeSpan.FromMilliseconds(0)));
        Assert.Equal(0, animation.GetValue(TimeSpan.FromMilliseconds(49)));
        Assert.Equal(100, animation.GetValue(TimeSpan.FromMilliseconds(50)));
        Assert.Equal(100, animation.GetValue(TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void VelocityIsInitialVelocity()
    {
        var animation = Animate(new SnapSpec<double>(), 0, 100, initialVelocity: 42);

        Assert.Equal(42, animation.GetVelocity(TimeSpan.FromMilliseconds(0)));
        Assert.Equal(42, animation.GetVelocity(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void DurationEqualsDelay()
    {
        var spec = new SnapSpec<double>(TimeSpan.FromMilliseconds(50));
        var animation = Animate(spec, 0, 100);

        Assert.Equal(TimeSpan.FromMilliseconds(50), animation.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(50), spec.Delay);
    }

    private static TargetBasedAnimation<double> Animate(SnapSpec<double> spec, double from, double to, double initialVelocity = 0)
        => new(spec, new DoubleConverter(), from, to, initialVelocity);
}
