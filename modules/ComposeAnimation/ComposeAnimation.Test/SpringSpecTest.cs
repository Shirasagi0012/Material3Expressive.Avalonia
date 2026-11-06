using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SpringSpecTest
{
    // Captured from fixed Core a9e47e8, SpringCases / FloatSpringSpec; no local solver generates these.
    [Theory]
    [InlineData(.9, 700.25, 2, 40, 3, 16, 4.6782450675964355, 290.4378662109375, 380)]
    [InlineData(.9, 700.25, 2, 40, 3, 64, 22.417015075683594, 337.6917419433594, 380)]
    [InlineData(.7, 380, 1, 0, 5, 16, 1.0218394994735718, -1.7974401712417603, 375)]
    [InlineData(.9, 700.25, 16777216, 3, -2, 16, 15609151, -127683312, 926)]
    public void FloatSamplesPreservePinnedCoreArithmetic(double damping, double stiffness, float from, float to,
        float velocity, double milliseconds, double expectedValue, double expectedVelocity, int duration)
    {
        var animation = new TargetBasedAnimation<float>(new SpringSpec<float>(damping, stiffness, .01f),
            new FloatConverter(), from, to, velocity);
        Assert.Equal(expectedValue, (double)animation.GetValue(TimeSpan.FromMilliseconds(milliseconds)));
        Assert.Equal(expectedVelocity, (double)animation.GetVelocity(TimeSpan.FromMilliseconds(milliseconds)));
        Assert.Equal(TimeSpan.FromMilliseconds(duration), animation.Duration);
    }

    [Fact]
    public void FloatUsesWholeMillisecondsWhileDoubleKeepsSubmillisecondTimeAndPrecision()
    {
        var single = new TargetBasedAnimation<float>(new SpringSpec<float>(.9, 700.25, .01f), new FloatConverter(), 2, 40, 3);
        var wide = Animate(new SpringSpec<double>(.9, 700.25, .01), 2, 40, 3);
        var fraction = TimeSpan.FromTicks(9990);
        Assert.Equal(single.GetValue(TimeSpan.Zero), single.GetValue(fraction));
        Assert.Equal(single.GetVelocity(TimeSpan.Zero), single.GetVelocity(fraction));
        Assert.NotEqual(wide.GetValue(TimeSpan.Zero), wide.GetValue(fraction));
        var precise = Animate(new SpringSpec<double>(), 16777217, 16777218);
        Assert.Equal(16777217d, precise.GetValue(TimeSpan.Zero));
    }

    private static TargetBasedAnimation<double> Animate(
        SpringSpec<double> spec,
        double from = 0,
        double to = 100,
        double initialVelocity = 0)
        => new(spec, new DoubleConverter(), from, to, initialVelocity);

    [Fact]
    public void StartsAtInitialValue()
    {
        var animation = Animate(new SpringSpec<double>());

        Assert.Equal(0, animation.GetValue(TimeSpan.Zero));
    }

    [Fact]
    public void SettlesAtTargetWithinDuration()
    {
        var animation = Animate(new SpringSpec<double>());

        Assert.Equal(100, animation.GetValue(animation.Duration), 2.0);
    }

    [Fact]
    public void EndVelocityIsZero()
    {
        var animation = Animate(new SpringSpec<double>());

        Assert.Equal(0, animation.GetEndVelocity());
    }

    [Fact]
    public void CriticallyDampedSpringNeverOvershoots()
    {
        var animation = Animate(new SpringSpec<double>(Spring.DampingRatioNoBouncy, Spring.StiffnessMedium));

        for (var t = 0; t <= animation.Duration.TotalMilliseconds; t += 10)
            Assert.True(animation.GetValue(TimeSpan.FromMilliseconds(t)) <= 100);
    }

    [Fact]
    public void UnderDampedSpringOvershoots()
    {
        var animation = Animate(new SpringSpec<double>(Spring.DampingRatioMediumBouncy, Spring.StiffnessMedium));

        var overshoots = false;
        for (var t = 0; t <= animation.Duration.TotalMilliseconds; t += 5)
            overshoots |= animation.GetValue(TimeSpan.FromMilliseconds(t)) > 100;
        Assert.True(overshoots);
    }

    [Fact]
    public void AtRestDurationIsZero()
    {
        var animation = Animate(new SpringSpec<double>(), from: 100, to: 100);

        Assert.Equal(TimeSpan.Zero, animation.Duration);
    }

    [Fact]
    public void LargerVisibilityThresholdShortensDuration()
    {
        var tight = Animate(new SpringSpec<double>(visibilityThreshold: 0.01));
        var loose = Animate(new SpringSpec<double>(visibilityThreshold: 1.0));

        Assert.True(loose.Duration < tight.Duration);
    }

    [Fact]
    public void VelocityTowardTargetSettlesNoLater()
    {
        var spec = new SpringSpec<float>(Spring.DampingRatioNoBouncy, Spring.StiffnessMedium, 0.01f);
        var converter = new FloatConverter();
        var toward = new TargetBasedAnimation<float>(spec, converter, 0, 100, 50).Duration;
        var away = new TargetBasedAnimation<float>(spec, converter, 0, 100, -50).Duration;

        // Compose rounds both durations down to whole milliseconds; a sub-ms
        // advantage can therefore produce equal durations.
        Assert.True(toward <= away);
        Assert.True(new TargetBasedAnimation<float>(spec, converter, 0, 100, 50).GetValue(TimeSpan.FromMilliseconds(16)) >
            new TargetBasedAnimation<float>(spec, converter, 0, 100, -50).GetValue(TimeSpan.FromMilliseconds(16)));
    }

    [Fact]
    public void RejectsInvalidSpringConstants()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpringSpec<double>(dampingRatio: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpringSpec<double>(stiffness: 0));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void DurationUsesComposeWholeMilliseconds(double from, double to)
    {
        // Fixed Compose FloatSpringSpec(1f, 800f, 1f): 80 ms. An 80.9382 ms
        // local duration delays the final snap at a 16 ms frame grid.
        var animation = Animate(new SpringSpec<double>(1, 800, 1), from, to);
        Assert.Equal(TimeSpan.FromMilliseconds(80), animation.Duration);
        Assert.Equal(to, animation.GetValue(TimeSpan.FromMilliseconds(80)));
    }
}
