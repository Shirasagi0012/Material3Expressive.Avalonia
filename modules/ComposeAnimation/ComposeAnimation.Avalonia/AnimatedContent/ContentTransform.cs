using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// How incoming content enters and outgoing content leaves <see cref="AnimatedContent"/>.
/// Omitting <see cref="SizeTransform"/> uses a clipped medium-low spring; pass null to disable
/// container size animation.
/// </summary>
public sealed class ContentTransform
{
    public ContentTransform()
    {
        Enter = EnterTransition.None;
        Exit = ExitTransition.None;
        SizeTransform = SizeTransform.CreateDefault();
    }

    /// <summary>Omitting size transform uses <see cref="SizeTransform.CreateDefault"/>.</summary>
    public ContentTransform(EnterTransition enter, ExitTransition exit, float targetContentZIndex = 0f)
        : this(enter, exit, targetContentZIndex, SizeTransform.CreateDefault()) { }

    /// <summary><paramref name="sizeTransform"/> null means no container size animation.</summary>
    public ContentTransform(EnterTransition enter, ExitTransition exit,
        float targetContentZIndex, SizeTransform? sizeTransform)
    {
        Enter = enter ?? throw new ArgumentNullException(nameof(enter));
        Exit = exit ?? throw new ArgumentNullException(nameof(exit));
        TargetContentZIndex = targetContentZIndex;
        SizeTransform = sizeTransform;
    }

    public EnterTransition Enter
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ExitTransition Exit
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    public float TargetContentZIndex { get; set; }

    public SizeTransform? SizeTransform { get; set; }

    /// <summary>Returns a copy with a different size transform. Does not mutate this instance.</summary>
    public ContentTransform Using(SizeTransform? sizeTransform)
        => new(Enter, Exit, TargetContentZIndex, sizeTransform);

    public static ContentTransform CreateFadeThrough()
    {
        var enterSpec = new TweenSpec<double>(
            TimeSpan.FromMilliseconds(220), TimeSpan.FromMilliseconds(90));
        var exitSpec = new TweenSpec<double>(TimeSpan.FromMilliseconds(90));
        return (new FadeIn { Spec = enterSpec }
                + new ScaleIn { InitialScale = 0.92, Spec = enterSpec })
            .TogetherWith(new FadeOut { Spec = exitSpec });
    }
}

/// <summary>Container size animation between outgoing and incoming full sizes.</summary>
public sealed class SizeTransform
{
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size>? Spec { get; set; }
    public Func<Size, Size, IFiniteAnimationSpec<Size>>? CreateSpec { get; set; }

    public static SizeTransform CreateDefault() => new();

    public IFiniteAnimationSpec<Size> CreateAnimationSpec(Size initial, Size target)
        => CreateSpec?.Invoke(initial, target)
           ?? Spec
           ?? new SpringSpec<Size>(
               stiffness: Spring.StiffnessMediumLow,
               visibilityThreshold: VisibilityThresholds.Size);
}

public static class ContentTransformExtensions
{
    public static ContentTransform TogetherWith(this EnterTransition enter, ExitTransition exit)
        => new(enter, exit);
}
