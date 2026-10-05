using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class ArcAnimationSpecTest
{
    private static TargetBasedAnimation<Point> Animate(IAnimationSpec<Point> spec, Point from, Point to)
        => new(spec, new PointConverter(), from, to);

    [Fact]
    public void ArcAboveBulgesUp()
    {
        var arc = Animate(new ArcAnimationSpec<Point>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()), new Point(0, 0), new Point(200, 200));

        var value = arc.GetValue(TimeSpan.FromMilliseconds(500));
        // X is higher and Y is lower than the linear midpoint.
        Assert.True(value.X > 100);
        Assert.True(value.Y < 100);
    }

    [Fact]
    public void ArcBelowBulgesDown()
    {
        var arc = Animate(new ArcAnimationSpec<Point>(ArcMode.ArcBelow, TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()), new Point(0, 0), new Point(200, 200));

        var value = arc.GetValue(TimeSpan.FromMilliseconds(500));
        // X is lower and Y is higher than the linear midpoint.
        Assert.True(value.X < 100);
        Assert.True(value.Y > 100);
    }

    [Fact]
    public void ArcModeInKeyframesShapesOnlyTheFollowingInterval()
    {
        var config = new KeyframesSpecConfig<Point> { Duration = TimeSpan.FromMilliseconds(1000) };
        config.At(0, new Point(0, 0)).Using(ArcMode.ArcAbove);
        config.At(1000, new Point(200, 200)).Using(ArcMode.ArcBelow);

        var arc = Animate(new KeyframesSpec<Point>(config), new Point(0, 0), new Point(200, 200));
        var linear = Animate(
            new KeyframesSpec<Point>(new KeyframesSpecConfig<Point>
            {
                Duration = TimeSpan.FromMilliseconds(1000),
            }),
            new Point(0, 0),
            new Point(200, 200));

        var arcValue = arc.GetValue(TimeSpan.FromMilliseconds(500));
        var linearValue = linear.GetValue(TimeSpan.FromMilliseconds(500));

        Assert.True(arcValue.X > linearValue.X);
        Assert.True(arcValue.Y < linearValue.Y);
    }

    [Fact]
    public void OneDimensionalValuesStayLinear()
    {
        var spec = new ArcAnimationSpec<double>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(1000), easing: new LinearEasing());
        var animation = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 100);

        Assert.Equal(50, animation.GetValue(TimeSpan.FromMilliseconds(500)), 0.001);
    }

    [Fact]
    public void DelaysBeforeArcing()
    {
        var spec = new ArcAnimationSpec<Point>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(200));
        var animation = Animate(spec, new Point(0, 0), new Point(200, 200));

        Assert.Equal(new Point(0, 0), animation.GetValue(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(TimeSpan.FromMilliseconds(1200), animation.Duration);
    }
}
