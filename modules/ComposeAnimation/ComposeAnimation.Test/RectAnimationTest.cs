using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class RectAnimationTest
{
    [Theory]
    [InlineData(10, 20, 30, 40)]
    [InlineData(-10, -20, -30, -40)]
    [InlineData(0.5, 0.5, 0, 0)]
    public void Converter_PreservesEdgesIncludingInvertedAndThresholdRectangles(
        double x, double y, double width, double height)
    {
        var rect = new Rect(x, y, width, height);
        var converter = new RectConverter();
        var vector = new double[4];
        converter.To(rect, vector);

        Assert.Equal(new[] { x, y, x + width, y + height }, vector);
        Assert.Equal(rect, converter.From(vector));
    }

    [Fact]
    public void LinearTween_PreservesGeometryAndTypedVelocity_ButVectorsDescribeEdges()
    {
        var animation = new TargetBasedAnimation<Rect>(
            new TweenSpec<Rect>(TimeSpan.FromSeconds(1), easing: new LinearEasing()),
            new RectConverter(), new Rect(10, 20, 30, 40), new Rect(110, 160, 110, 180));
        var time = TimeSpan.FromMilliseconds(500);

        Assert.Equal(new Rect(60, 90, 70, 110), animation.GetValue(time));
        var typedVelocity = animation.GetVelocity(time);
        Assert.Equal(100, typedVelocity.X, 8);
        Assert.Equal(140, typedVelocity.Y, 8);
        Assert.Equal(80, typedVelocity.Width, 8);
        Assert.Equal(140, typedVelocity.Height, 8);
        var velocity = new double[4];
        animation.GetVelocityVector(time, velocity);
        double[] expectedVelocity = [100, 140, 180, 280];
        for (var i = 0; i < 4; i++) Assert.Equal(expectedVelocity[i], velocity[i], 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArcAndArcKeyframes_AnimateBothCorners(bool useKeyframes)
    {
        var from = new Rect(10, 20, 30, 40);
        var to = new Rect(210, 120, 160, 280);
        IAnimationSpec<T> Spec<T>(T initial, T target)
        {
            if (!useKeyframes)
                return new ArcAnimationSpec<T>(ArcMode.ArcAbove, TimeSpan.FromSeconds(1),
                    easing: new LinearEasing());
            var config = new KeyframesSpecConfig<T> { Duration = TimeSpan.FromSeconds(1) };
            config.At(0, initial).Using(ArcMode.ArcAbove);
            config.At(1000, target);
            return new KeyframesSpec<T>(config);
        }

        var bounds = new TargetBasedAnimation<Rect>(Spec(from, to), new RectConverter(), from, to);
        var topLeft = new TargetBasedAnimation<Point>(Spec(from.TopLeft, to.TopLeft),
            new PointConverter(), from.TopLeft, to.TopLeft);
        var bottomRight = new TargetBasedAnimation<Point>(Spec(from.BottomRight, to.BottomRight),
            new PointConverter(), from.BottomRight, to.BottomRight);

        foreach (var milliseconds in new[] { 0, 100, 500, 900, 1000 })
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
            var actual = bounds.GetValue(time);
            var expectedTopLeft = topLeft.GetValue(time);
            var expectedBottomRight = bottomRight.GetValue(time);
            Assert.Equal(expectedTopLeft.X, actual.Left, 8);
            Assert.Equal(expectedTopLeft.Y, actual.Top, 8);
            Assert.Equal(expectedBottomRight.X, actual.Right, 8);
            Assert.Equal(expectedBottomRight.Y, actual.Bottom, 8);
        }
    }

    [Fact]
    public void Spring_UsesPerEdgeVelocityAndThreshold_ForTrajectoryAndCompletion()
    {
        var animation = new TargetBasedAnimation<Rect>(
            new SpringSpec<Rect>(stiffness: 50, visibilityThreshold: VisibilityThresholds.Rect),
            new RectConverter(), new Rect(10, 20, 30, 40), new Rect(110, 160, 110, 180),
            initialVelocity: new Rect(40, -20, -80, 100));
        // Explicit edge data is independent of the Rect converter under test.
        double[] starts = [10, 20, 40, 60];
        double[] targets = [110, 160, 220, 340];
        double[] velocities = [40, -20, -40, 80];
        var edges = Enumerable.Range(0, 4).Select(i => new TargetBasedAnimation<double>(
            new SpringSpec<double>(stiffness: 50, visibilityThreshold: VisibilityThresholds.Dip),
            new DoubleConverter(), starts[i], targets[i], initialVelocity: velocities[i])).ToArray();

        Assert.Equal(edges.Max(edge => edge.Duration), animation.Duration);
        Assert.Equal(new Rect(40, -20, -80, 100), animation.GetVelocity(TimeSpan.Zero));
        var time = TimeSpan.FromMilliseconds(32);
        var actual = animation.GetValue(time);
        Assert.Equal(edges[0].GetValue(time), actual.Left, 8);
        Assert.Equal(edges[1].GetValue(time), actual.Top, 8);
        Assert.Equal(edges[2].GetValue(time), actual.Right, 8);
        Assert.Equal(edges[3].GetValue(time), actual.Bottom, 8);
        Assert.Equal(new Rect(110, 160, 110, 180), animation.GetValue(animation.Duration));
    }
}
