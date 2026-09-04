using ComposeAnimationRuntime;
using Xunit;

namespace ComposeAnimationRuntime.Tests;

public sealed class AnimatableTests
{
    [Fact]
    public void TweenReachesTarget()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(10, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        animation.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(5, animation.Value, 3);
        animation.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(10, animation.Value);
        Assert.False(animation.IsRunning);
    }

    [Fact]
    public void SpringRetargetsFromCurrentValue()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        var spec = new SpringAnimationSpec<float>(new ScalarVectorConverter(), 0.8f, 380);
        animation.AnimateTo(1, spec);
        animation.Tick(TimeSpan.FromMilliseconds(100));
        var before = animation.Value;
        animation.AnimateTo(0, spec);
        animation.Tick(TimeSpan.FromMilliseconds(1));
        Assert.True(animation.Value > 0);
        Assert.NotEqual(before, animation.Value);
    }

    [Fact]
    public void SnapStopsImmediately()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateTo(1, new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1)));
        animation.SnapTo(0.25f);
        Assert.Equal(0.25f, animation.Value);
        Assert.False(animation.IsRunning);
    }

    [Fact]
    public void DecaySlowsToRest()
    {
        var animation = new Animatable<float>(0, new ScalarVectorConverter());
        animation.AnimateDecay(10, new DecaySpec<float>(new ScalarVectorConverter(), friction: 4));

        animation.Tick(TimeSpan.FromSeconds(1));

        Assert.True(animation.Value > 2);
        Assert.True(animation.Velocity < 1);
        Assert.True(animation.IsRunning);
    }

    [Fact]
    public void ManualClockAdvancesRegisteredAnimationsTogether()
    {
        var first = new Animatable<float>(0, new ScalarVectorConverter());
        var second = new Animatable<float>(10, new ScalarVectorConverter());
        var spec = new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromSeconds(1));
        first.AnimateTo(1, spec);
        second.AnimateTo(0, spec);
        var clock = new ManualAnimationClock();
        clock.Register(first);
        clock.Register(second);

        Assert.True(clock.AdvanceBy(TimeSpan.FromMilliseconds(250)));
        Assert.Equal(0.25f, first.Value, 3);
        Assert.Equal(7.5f, second.Value, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(250), clock.Time);
    }

    [Fact]
    public void InvalidSpecsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpringAnimationSpec<float>(new ScalarVectorConverter(), stiffness: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TweenSpec<float>(new ScalarVectorConverter(), TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecaySpec<float>(new ScalarVectorConverter(), friction: 0));
    }

    [Fact]
    public void KeyframesUsePerSegmentEasing()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(10, new KeyframesSpec<float>(converter, TimeSpan.FromSeconds(1), new(TimeSpan.Zero, 0), new(TimeSpan.FromMilliseconds(500), 10, Easings.EaseIn), new(TimeSpan.FromSeconds(1), 0)));
        animation.Tick(TimeSpan.FromMilliseconds(250));
        Assert.Equal(2.5f, animation.Value, 2);
    }

    [Fact]
    public void RepeatableSpecFinishesAfterRequestedIterations()
    {
        var converter = new ScalarVectorConverter();
        var animation = new Animatable<float>(0, converter);
        animation.AnimateTo(1, new RepeatableSpec<float>(new TweenSpec<float>(converter, TimeSpan.FromMilliseconds(100)), 2, reverse: true));
        animation.Tick(TimeSpan.FromMilliseconds(200));
        Assert.False(animation.IsRunning);
        Assert.Equal(1, animation.Value);
    }

    [Fact]
    public void TransitionCoordinatesRegisteredTracks()
    {
        var converter = new ScalarVectorConverter();
        var value = new Animatable<float>(0, converter);
        var transition = new MotionTransition<bool>(false);
        transition.Register("opacity", value, state => state ? 1 : 0,
            _ => new TweenSpec<float>(converter, TimeSpan.FromSeconds(1)));

        transition.AnimateTo(true);
        Assert.True(transition.IsRunning);
        transition.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0.5f, value.Value, 3);
        transition.Tick(TimeSpan.FromMilliseconds(500));
        Assert.False(transition.IsRunning);
        Assert.True(transition.CurrentState);
    }

    [Fact]
    public void ArcReachesTarget()
    {
        var converter = VectorConverter.Create(
            value => new AnimationVector2D((float)value.X, (float)value.Y),
            vector => new System.Numerics.Vector2(vector[0], vector[1]),
            new System.Numerics.Vector2(0.001f));
        var animation = new Animatable<System.Numerics.Vector2>(new(0, 0), converter);
        animation.AnimateTo(new(10, 20), new ArcSpec<System.Numerics.Vector2>(converter, TimeSpan.FromSeconds(1)));
        animation.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(new System.Numerics.Vector2(10, 20), animation.Value);
    }

    [Fact]
    public void VisibilityRemainsVisibleUntilExitCompletes()
    {
        var spec = new TweenSpec<double>(new DoubleVectorConverter(), TimeSpan.FromMilliseconds(100));
        var visibility = new VisibilityMotion(true, spec, spec);
        visibility.Hide();
        visibility.Tick(TimeSpan.FromMilliseconds(50));
        Assert.Equal(VisibilityState.Visible, visibility.State);
        visibility.Tick(TimeSpan.FromMilliseconds(50));
        Assert.Equal(VisibilityState.Hidden, visibility.State);
    }

}
