using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

using Avalonia = global::Avalonia;

public class SplineBasedDecayTest
{
    private static SplineBasedFloatDecayAnimationSpec Spec(double density = 1)
        => new(density);

    [Fact]
    public void FlingPosition_ClampsOutOfRange()
    {
        var start = AndroidFlingSpline.FlingPosition(0f);
        var end = AndroidFlingSpline.FlingPosition(1f);

        Assert.Equal(start, AndroidFlingSpline.FlingPosition(-10f));
        Assert.Equal(start, AndroidFlingSpline.FlingPosition(-1f));
        Assert.Equal(start, AndroidFlingSpline.FlingPosition(-0.06f));
        Assert.Equal(end, AndroidFlingSpline.FlingPosition(1.5f));
        Assert.Equal(end, AndroidFlingSpline.FlingPosition(10f));
    }

    [Fact]
    public void FlingPosition_StartsAtZeroAndEndsAtOne()
    {
        var start = AndroidFlingSpline.FlingPosition(0f);
        var end = AndroidFlingSpline.FlingPosition(1f);

        Assert.InRange(start.DistanceCoefficient, 0, 1e-4f);
        Assert.Equal(1, end.DistanceCoefficient, 5);
        Assert.Equal(0, end.VelocityCoefficient, 5);
        Assert.True(start.VelocityCoefficient > 0);
    }

    [Fact]
    public void FlingPosition_DistanceIsMonotonic()
    {
        var previous = AndroidFlingSpline.FlingPosition(0f).DistanceCoefficient;
        for (var i = 1; i <= 100; i++)
        {
            var current = AndroidFlingSpline.FlingPosition(i / 100f).DistanceCoefficient;
            Assert.True(current >= previous);
            previous = current;
        }
    }

    [Fact]
    public void AtRest_TargetIsInitialAndDurationIsZero()
    {
        var spec = Spec();

        Assert.Equal(42, spec.GetTargetValue(42, 0), 5);
        Assert.Equal(TimeSpan.Zero, spec.GetDuration(42, 0));
        Assert.Equal(42, spec.GetValue(TimeSpan.Zero, 42, 0), 5);
        Assert.Equal(0, spec.GetVelocity(TimeSpan.Zero, 42, 0), 5);
    }

    [Fact]
    public void TargetAndDuration_MatchSampledEnd()
    {
        var spec = Spec();
        var target = spec.GetTargetValue(0, 5000);
        var duration = spec.GetDuration(0, 5000);

        Assert.True(target > 0);
        Assert.True(duration > TimeSpan.Zero);
        Assert.Equal(0, duration.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(target, spec.GetValue(duration, 0, 5000), 5);
        Assert.Equal(0, spec.GetVelocity(duration, 0, 5000), 1);
    }

    [Fact]
    public void NegativeVelocity_TravelsBackward()
    {
        var spec = Spec();

        Assert.True(spec.GetTargetValue(0, -5000) < 0);
        Assert.Equal(
            -spec.GetTargetValue(0, 5000),
            spec.GetTargetValue(0, -5000),
            5);
    }

    [Fact]
    public void SplineIsFastOut_MoreThanHalfDistanceByMidDuration()
    {
        var spec = Spec();
        var target = spec.GetTargetValue(0, 5000);
        var duration = spec.GetDuration(0, 5000);
        var mid = spec.GetValue(TimeSpan.FromTicks(duration.Ticks / 2), 0, 5000);

        Assert.True(mid > target * 0.5);
    }

    [Fact]
    public void SamePixelVelocity_HigherDensityTravelsFewerPixels()
    {
        var mdpi = Spec(1).GetTargetValue(0, 5000);
        var xhdpi = Spec(2).GetTargetValue(0, 5000);

        Assert.True(xhdpi < mdpi);
    }

    [Fact]
    public void VelocityScaledWithDensity_DistanceScalesApproximatelyWithDensity()
    {
        var mdpi = Spec(1).GetTargetValue(0, 5000);
        var xhdpi = Spec(2).GetTargetValue(0, 10000);

        Assert.Equal(2 * mdpi, xhdpi, mdpi * 0.05);
    }

    [Fact]
    public void Factory_UsesSplineBasedFloatSpec()
    {
        var spec = DecayAnimationSpec.SplineBasedDecay<double>(1);
        var floatSpec = Spec();

        Assert.Equal(
            floatSpec.GetTargetValue(0, 5000),
            spec.CalculateTargetValue(0, 5000),
            5);
        Assert.Equal(
            floatSpec.GetDuration(0, 5000),
            spec.Vectorize(new DoubleConverter()).GetDuration([0], [5000]));
    }

    [Fact]
    public void CalculateTargetValue_MatchesDecayAnimation()
    {
        var spec = DecayAnimationSpec.SplineBasedDecay<double>(1);
        var animation = new DecayAnimation<double>(spec, new DoubleConverter(), 10, 5000);

        Assert.Equal(animation.TargetValue, spec.CalculateTargetValue(10, 5000), 8);
        Assert.Equal(animation.TargetValue, animation.GetValue(animation.Duration), 8);
    }

    [Fact]
    public void WorksPerDimensionForVectors()
    {
        var animation = new DecayAnimation<Avalonia.Point>(
            DecayAnimationSpec.SplineBasedDecay<Avalonia.Point>(1),
            new PointConverter(),
            new Avalonia.Point(0, 100),
            new Avalonia.Point(5000, 0));

        Assert.InRange(animation.GetValue(TimeSpan.Zero).X, 0, 1);
        Assert.Equal(100, animation.GetValue(TimeSpan.Zero).Y, 5);
        Assert.Equal(100, animation.GetValue(animation.Duration).Y, 5);
        Assert.True(animation.GetValue(animation.Duration).X > 0);
    }

    [Fact]
    public void RejectsNonPositiveDensity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplineBasedFloatDecayAnimationSpec(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplineBasedFloatDecayAnimationSpec(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DecayAnimationSpec.SplineBasedDecay<double>(0));
    }

    [Fact]
    public void ScrollFriction_MatchesAndroidViewConfiguration()
    {
        Assert.Equal(0.015, DecayAnimationSpec.ScrollFriction);
    }
}
