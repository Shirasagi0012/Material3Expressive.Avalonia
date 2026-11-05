using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class EasingsTest
{
    // Captured from the pinned Core TargetBasedAnimation / CubicBezierEasing at 48ms,
    // in animation-tween (125ms, 2f -> 40f, initial velocity 3f). Values are exact Float
    // results, not tolerances or values calculated by the implementation under test.
    [Theory]
    [InlineData(.4, 0, .2, 1, 0, .5798073410987854, 24.032678604125977, 676.7291870117188)]
    [InlineData(.4, 0, .6, 1, 0, .3122538924217224, 13.86564826965332, 460.43206787109375)]
    [InlineData(1f / 3f, 1f / 3f, 2f / 3f, 2f / 3f, 0, .3840000033378601, 16.59200096130371, 304.00274658203125)]
    [InlineData(0, 0, 1f / 3f, 1, 0, .6760877966880798, 27.69133758544922, 349.80963134765625)]
    [InlineData(0, 0, 0, 1, 0, .8169252276420593, 33.04315948486328, 231.42623901367188)]
    [InlineData(1, 0, 1, 1, 0, .06009047478437424, 4.283438205718994, 105.03101348876953)]
    [InlineData(.34, 1.56, .64, 1, 0, 1.015377163887024, 40.58433151245117, 306.08367919921875)]
    [InlineData(.36, -.56, .66, 0, 0, -.1938953697681427, -5.368023872375488, 162.17327880859375)]
    [InlineData(.4, 0, .2, 1, 17, .5798073410987854, 10.809252738952637, 195.94454956054688)]
    public void FloatTweenAndEasing_MatchPinnedComposeSamples(double a, double b, double c, double d,
        int delay, double expectedEasing, double expectedValue, double expectedVelocity)
    {
        var easing = new CubicBezierEasing(a, b, c, d);
        var animation = new TargetBasedAnimation<float>(
            new TweenSpec<float>(TimeSpan.FromMilliseconds(125), TimeSpan.FromMilliseconds(delay), easing),
            new FloatConverter(), 2, 40, 3);
        Assert.Equal(expectedEasing, easing.Ease(48000f / 125000f));
        Assert.Equal(expectedValue, (double)animation.GetValue(TimeSpan.FromMilliseconds(48)));
        Assert.Equal(expectedVelocity, (double)animation.GetVelocity(TimeSpan.FromMilliseconds(48)));
        Assert.Equal(40, animation.GetValue(animation.Duration));
    }

    [Fact]
    public void SmallFractionsUseOneUlpTranslationAndNaNPassesThrough()
    {
        var easing = new CubicBezierEasing(0, 0, 0, 1);
        Assert.Equal(easing.Ease(MathF.BitIncrement(1f) - 1f), easing.Ease(float.Epsilon));
        Assert.True(double.IsNaN(easing.Ease(double.NaN)));
    }

    [Fact]
    public void DoubleTweenRetainsDoubleEndpointsAndInterpolation()
    {
        var animation = new TargetBasedAnimation<double>(
            new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: ComposeEasings.Linear),
            new DoubleConverter(), 16777216, 16777218);
        Assert.Equal(16777217, animation.GetValue(TimeSpan.FromMilliseconds(50)));
    }

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
