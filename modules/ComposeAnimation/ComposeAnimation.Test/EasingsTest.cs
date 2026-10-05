using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class EasingsTest
{
    [Fact]
    public void MaterialCurves_HaveComposeControlPoints()
    {
        Assert.Equal(new CubicBezierEasing(0.4, 0.0, 0.2, 1.0), ComposeEasings.FastOutSlowIn);
        Assert.Equal(new CubicBezierEasing(0.0, 0.0, 0.2, 1.0), ComposeEasings.LinearOutSlowIn);
        Assert.Equal(new CubicBezierEasing(0.4, 0.0, 1.0, 1.0), ComposeEasings.FastOutLinearIn);
    }

    [Theory]
    [InlineData(-0.25)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1.25)]
    public void Endpoints_AreIdentity(double fraction)
    {
        Assert.Equal(fraction, ComposeEasings.FastOutSlowIn.Ease(fraction));
        Assert.Equal(fraction, ComposeEasings.LinearOutSlowIn.Ease(fraction));
        Assert.Equal(fraction, ComposeEasings.FastOutLinearIn.Ease(fraction));
        Assert.Equal(fraction, ComposeEasings.Linear.Ease(fraction));
    }

    [Fact]
    public void Linear_IsIdentityOnTheUnitInterval()
    {
        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20.0;
            Assert.Equal(t, ComposeEasings.Linear.Ease(t));
        }
    }

    [Fact]
    public void FastOutSlowIn_PutsMoreTimeOnDeceleration()
    {
        Assert.True(ComposeEasings.FastOutSlowIn.Ease(0.5) > 0.5);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void LinearOutSlowIn_LeadsFastOutSlowIn(double t)
    {
        Assert.True(ComposeEasings.LinearOutSlowIn.Ease(t) > ComposeEasings.FastOutSlowIn.Ease(t));
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void FastOutLinearIn_LagsFastOutSlowIn(double t)
    {
        Assert.True(ComposeEasings.FastOutLinearIn.Ease(t) < ComposeEasings.FastOutSlowIn.Ease(t));
    }

    [Fact]
    public void MaterialCurves_AreMonotonicOnTheUnitInterval()
    {
        AssertMonotonic(ComposeEasings.FastOutSlowIn);
        AssertMonotonic(ComposeEasings.LinearOutSlowIn);
        AssertMonotonic(ComposeEasings.FastOutLinearIn);
    }

    [Fact]
    public void DefaultTweenSpec_SharesFastOutSlowIn()
    {
        var spec = new TweenSpec<double>();
        Assert.Same(ComposeEasings.FastOutSlowIn, spec.Easing);
        Assert.Same(spec.Easing, new TweenSpec<double>().Easing);
        Assert.Same(ComposeEasings.FastOutSlowIn, new ArcAnimationSpec<double>().Easing);
    }

    [Fact]
    public void DefaultTween_SamplesFastOutSlowIn()
    {
        var animation = new TargetBasedAnimation<double>(
            new TweenSpec<double>(TimeSpan.FromMilliseconds(100)),
            new DoubleConverter(),
            0,
            100);

        Assert.Equal(ComposeEasings.FastOutSlowIn.Ease(0.4) * 100,
            animation.GetValue(TimeSpan.FromMilliseconds(40)),
            8);
    }

    [Fact]
    public void OvershootingCurve_CanExceedOne()
    {
        var easeOutBack = new CubicBezierEasing(0.34, 1.56, 0.64, 1.0);
        Assert.True(easeOutBack.Ease(0.6) > 1.0);
    }

    [Fact]
    public void RejectsNaNControlPoints()
    {
        Assert.Throws<ArgumentException>(() => new CubicBezierEasing(double.NaN, 0, 1, 1));
        Assert.Throws<ArgumentException>(() => new CubicBezierEasing(0, double.NaN, 1, 1));
        Assert.Throws<ArgumentException>(() => new CubicBezierEasing(0, 0, double.NaN, 1));
        Assert.Throws<ArgumentException>(() => new CubicBezierEasing(0, 0, 1, double.NaN));
    }

    [Fact]
    public void Equals_ByControlPoints()
    {
        var left = new CubicBezierEasing(0.4, 0, 0.2, 1);
        var right = new CubicBezierEasing(0.4, 0, 0.2, 1);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, new CubicBezierEasing(0, 0, 0.2, 1));
    }

    [Fact]
    public void SharedInstance_IsSafeToEvaluateConcurrently()
    {
        var easing = ComposeEasings.FastOutSlowIn;
        var results = new double[256];
        Parallel.For(0, results.Length, i => results[i] = easing.Ease((i + 1) / 257.0));

        Assert.All(results, value =>
        {
            Assert.InRange(value, 0, 1);
        });
        for (var i = 1; i < results.Length; i++)
            Assert.True(results[i] > results[i - 1]);
    }

    private static void AssertMonotonic(IEasing easing)
    {
        var previous = easing.Ease(0);
        for (var i = 1; i <= 100; i++)
        {
            var current = easing.Ease(i / 100.0);
            Assert.True(current >= previous);
            previous = current;
        }
    }
}
