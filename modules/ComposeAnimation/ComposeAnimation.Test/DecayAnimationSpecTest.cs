using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

using Avalonia = global::Avalonia;

public class DecayAnimationSpecTest
{
    [Fact]
    public void StartsAtInitialValueAndVelocity()
    {
        var animation = new DecayAnimation<double>(
            DecayAnimationSpec.ExponentialDecay<double>(),
            new DoubleConverter(),
            0,
            100);

        Assert.Equal(0, animation.GetValue(TimeSpan.Zero));
        Assert.Equal(100, animation.GetVelocity(TimeSpan.Zero), 0.001);
    }

    [Fact]
    public void VelocityDecaysExponentially()
    {
        var animation = new DecayAnimation<double>(
            DecayAnimationSpec.ExponentialDecay<double>(),
            new DoubleConverter(),
            0,
            100);

        var earlier = animation.GetVelocity(TimeSpan.FromMilliseconds(100));
        var later = animation.GetVelocity(TimeSpan.FromMilliseconds(500));

        Assert.True(Math.Abs(later) < Math.Abs(earlier));
        Assert.True(Math.Abs(earlier) < 100);
    }

    [Fact]
    public void TargetValueIsWhereTheAnimationStops()
    {
        var spec = DecayAnimationSpec.ExponentialDecay<double>();
        var animation = new DecayAnimation<double>(spec, new DoubleConverter(), 0, 100);

        // target = initial - velocity / friction, with friction = -4.2.
        var expected = -100 / -4.2;
        Assert.Equal(expected, animation.TargetValue, 0.05);
        // The value approaches the target as time progresses.
        Assert.Equal(animation.TargetValue, animation.GetValue(animation.Duration), 0.05);
    }

    [Fact]
    public void DurationIsTimeUntilVelocityReachesThreshold()
    {
        var spec = DecayAnimationSpec.ExponentialDecay<double>();
        var animation = new DecayAnimation<double>(spec, new DoubleConverter(), 0, 100);

        // duration = ln(threshold / velocity) / friction * 1000.
        var expected = 1000.0 * Math.Log(0.1 / 100) / -4.2;
        Assert.Equal(expected, animation.Duration.TotalMilliseconds, 0.001);
        Assert.Equal(0.1, spec.Vectorize(new DoubleConverter()).AbsVelocityThreshold, 9);
    }

    [Fact]
    public void AtRestTheTargetIsTheInitialValue()
    {
        var spec = DecayAnimationSpec.ExponentialDecay<double>();
        var animation = new DecayAnimation<double>(spec, new DoubleConverter(), 42, 0.05);

        Assert.Equal(42, animation.TargetValue, 0.001);
    }

    [Fact]
    public void HigherFrictionMultiplierStopsSooner()
    {
        var gentle = new DecayAnimation<double>(DecayAnimationSpec.ExponentialDecay<double>(1), new DoubleConverter(), 0, 100);
        var strong = new DecayAnimation<double>(DecayAnimationSpec.ExponentialDecay<double>(4), new DoubleConverter(), 0, 100);

        Assert.True(strong.Duration < gentle.Duration);
    }

    [Fact]
    public void WorksPerDimensionForVectors()
    {
        var animation = new DecayAnimation<Avalonia.Point>(
            DecayAnimationSpec.ExponentialDecay<Avalonia.Point>(),
            new PointConverter(),
            new Avalonia.Point(0, 100),
            new Avalonia.Point(100, 0));

        // X decays from 0 towards 100/-4.2; Y stays at 100.
        Assert.Equal(0, animation.GetValue(TimeSpan.Zero).X, 0.001);
        Assert.Equal(100, animation.GetValue(TimeSpan.Zero).Y, 0.001);
        Assert.Equal(100, animation.GetValue(animation.Duration).Y, 0.001);
    }

    [Fact]
    public void CalculateTargetValue_MatchesDecayAnimation()
    {
        var spec = DecayAnimationSpec.ExponentialDecay<double>();
        var converter = new DoubleConverter();
        var animation = new DecayAnimation<double>(spec, converter, 10, 80);

        Assert.Equal(animation.TargetValue, spec.CalculateTargetValue(converter, 10, 80), 8);
        Assert.Equal(animation.TargetValue, spec.CalculateTargetValue(10, 80), 8);
    }

    [Fact]
    public void CalculateTargetValue_WorksForFloatOverload()
    {
        var spec = DecayAnimationSpec.ExponentialDecay<float>();
        var converter = new FloatConverter();
        var animation = new DecayAnimation<float>(spec, converter, 10f, 80f);

        Assert.Equal(animation.TargetValue, spec.CalculateTargetValue(10f, 80f), 5);
    }

    [Fact]
    public void GenerateDecayAnimationSpec_AppliesFloatSpecPerDimension()
    {
        var spec = new ConstantFloatDecaySpec().GenerateDecayAnimationSpec<Avalonia.Point>();
        var converter = new PointConverter();

        Assert.Equal(new Avalonia.Point(7, 8),
            spec.CalculateTargetValue(converter, new Avalonia.Point(2, 3), new Avalonia.Point(9, 1)));
        Assert.Equal(TimeSpan.FromMilliseconds(40),
            spec.Vectorize(converter).GetDuration([2, 3], [9, 1]));
    }

    [Fact]
    public void GenerateDecayAnimationSpec_RejectsNull()
    {
        IFloatDecayAnimationSpec spec = null!;
        Assert.Throws<ArgumentNullException>(() => spec.GenerateDecayAnimationSpec<double>());
    }

    private sealed class ConstantFloatDecaySpec : IFloatDecayAnimationSpec
    {
        public double AbsVelocityThreshold => 0.1;

        public double GetValue(TimeSpan playTime, double initialValue, double initialVelocity)
            => initialValue + 5;

        public double GetVelocity(TimeSpan playTime, double initialValue, double initialVelocity)
            => 0;

        public TimeSpan GetDuration(double initialValue, double initialVelocity)
            => TimeSpan.FromMilliseconds(40);

        public double GetTargetValue(double initialValue, double initialVelocity)
            => initialValue + 5;
    }
}
