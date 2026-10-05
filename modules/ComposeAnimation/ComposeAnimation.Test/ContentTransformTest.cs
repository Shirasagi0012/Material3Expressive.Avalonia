using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class ContentTransformTest
{
    [Fact]
    public void ThreeArgConstructor_UsesDefaultSizeTransform()
    {
        var transform = new FadeIn().TogetherWith(new FadeOut());
        Assert.NotNull(transform.SizeTransform);
        Assert.True(transform.SizeTransform!.Clip);
        var spec = transform.SizeTransform.CreateAnimationSpec(new Size(10, 10), new Size(20, 20));
        var spring = Assert.IsType<SpringSpec<Size>>(spec);
        Assert.Equal(Spring.StiffnessMediumLow, spring.Stiffness);
        Assert.Equal(VisibilityThresholds.Size, spring.VisibilityThreshold);
    }

    [Fact]
    public void FourArgConstructor_KeepsNullSizeTransform()
    {
        var transform = new ContentTransform(new FadeIn(), new FadeOut(), 0f, null);
        Assert.Null(transform.SizeTransform);
    }

    [Fact]
    public void UsingNull_DoesNotMutateAnotherFadeThrough()
    {
        var fade = ContentTransform.CreateFadeThrough();
        var cross = fade.Using(null);
        Assert.Null(cross.SizeTransform);
        Assert.NotNull(fade.SizeTransform);
        Assert.NotSame(fade, cross);
        Assert.Same(fade.Enter, cross.Enter);
        Assert.Same(fade.Exit, cross.Exit);
    }

    [Fact]
    public void CreateFadeThrough_MatchesMaterialTimings()
    {
        var transform = ContentTransform.CreateFadeThrough();
        var enter = new VisibilityEffects();
        transform.Enter.Apply(enter);
        Assert.NotNull(enter.Fade);
        var fadeTween = Assert.IsType<TweenSpec<double>>(enter.Fade.Spec);
        Assert.Equal(TimeSpan.FromMilliseconds(220), fadeTween.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(90), fadeTween.Delay);
        Assert.Equal(0, enter.Fade.Alpha);

        Assert.NotNull(enter.Scale);
        Assert.Equal(0.92, enter.Scale.Scale);
        var scaleTween = Assert.IsType<TweenSpec<double>>(enter.Scale.Spec);
        Assert.Equal(TimeSpan.FromMilliseconds(220), scaleTween.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(90), scaleTween.Delay);

        var exit = new VisibilityEffects();
        transform.Exit.Apply(exit);
        Assert.NotNull(exit.Fade);
        var outTween = Assert.IsType<TweenSpec<double>>(exit.Fade.Spec);
        Assert.Equal(TimeSpan.FromMilliseconds(90), outTween.Duration);
        Assert.Equal(TimeSpan.Zero, outTween.Delay);

        Assert.NotNull(transform.SizeTransform);
        Assert.True(transform.SizeTransform!.Clip);
        var size = transform.SizeTransform.CreateAnimationSpec(default, new Size(1, 1));
        Assert.Equal(Spring.StiffnessMediumLow, Assert.IsType<SpringSpec<Size>>(size).Stiffness);
    }

    [Fact]
    public void ParameterlessConstructor_DefaultsToNoneAndDefaultSize()
    {
        var transform = new ContentTransform();
        Assert.Same(EnterTransition.None, transform.Enter);
        Assert.Same(ExitTransition.None, transform.Exit);
        Assert.Equal(0f, transform.TargetContentZIndex);
        Assert.NotNull(transform.SizeTransform);
    }

    [Fact]
    public void KeepUntilTransitionsFinished_AppliesHoldAlongsideFade()
    {
        var exit = new FadeOut() + ExitTransition.KeepUntilTransitionsFinished;
        var effects = new VisibilityEffects();
        exit.Apply(effects);
        Assert.True(effects.Hold);
        Assert.NotNull(effects.Fade);
    }
}
