using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public static class CrossfadeTransforms
{
    public static ContentTransform CreateTransform(IFiniteAnimationSpec<double>? spec = null)
    {
        spec ??= new TweenSpec<double>();
        return new FadeIn { Spec = spec }.TogetherWith(new FadeOut { Spec = spec }).Using(null);
    }
}

/// <summary>
/// AnimatedContent preset: simultaneous fade in/out with no container size animation.
/// ContentTransform and TransitionSpec on this type have no effect; set <see cref="Spec"/> instead.
/// </summary>
public class Crossfade : AnimatedContent
{
    public static readonly StyledProperty<IFiniteAnimationSpec<double>> SpecProperty =
        AvaloniaProperty.Register<Crossfade, IFiniteAnimationSpec<double>>(
            nameof(Spec), new TweenSpec<double>(), validate: v => v is not null);

    public IFiniteAnimationSpec<double> Spec
    {
        get => GetValue(SpecProperty);
        set => SetValue(SpecProperty, value);
    }

    protected override ContentTransform ResolveCurrentTransform(IAnimatedContentTransitionScope scope)
        => CrossfadeTransforms.CreateTransform(Spec);
}
