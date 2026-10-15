using Avalonia;
using Avalonia.Layout;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed record EnterExitEffects
{
    internal record FadeEffect(double Alpha, IFiniteAnimationSpec<double> Spec);

    internal record ScaleEffect(double Scale, IFiniteAnimationSpec<double> Spec, RelativePoint Origin);

    internal record SlideEffect(Func<Size, Vector> Offset, IFiniteAnimationSpec<Vector> Spec);

    internal record SizeEffect(Func<Size, Size> Size, IFiniteAnimationSpec<Size> Spec, VisibilityAlignment Alignment, bool Clip);

    internal static EnterExitEffects Read(EnterTransition declaration)
    {
        var builder = new Builder();
        declaration.Apply(builder);
        return builder.Build();
    }

    internal static EnterExitEffects Read(ExitTransition declaration)
    {
        var builder = new Builder();
        declaration.Apply(builder);
        return builder.Build();
    }

    internal EnterExitEffects Overlay(EnterExitEffects other) => new() {
        Fade = other.Fade ?? Fade,
        Scale = other.Scale ?? Scale,
        Slide = other.Slide ?? Slide,
        ChangeSize = other.ChangeSize ?? ChangeSize,
        Hold = Hold || other.Hold
    };

    public FadeEffect? Fade { get; init; }
    public ScaleEffect? Scale { get; init; }
    public SlideEffect? Slide { get; init; }
    public SizeEffect? ChangeSize { get; init; }
    public bool Hold { get; init; }

    internal sealed class Builder
    {
        public FadeEffect? Fade;
        public ScaleEffect? Scale;
        public SlideEffect? Slide;
        public SizeEffect? ChangeSize;
        public bool Hold;
        internal readonly HashSet<object> Groups = new(ReferenceEqualityComparer.Instance);

        internal EnterExitEffects Build() => new() {
            Fade = Fade,
            Scale = Scale,
            Slide = Slide,
            ChangeSize = ChangeSize,
            Hold = Hold
        };
    }

    internal static SpringSpec<T> Spring<T>() => new(stiffness: 400);

    internal static double Finite(double value) =>
        Double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));

    internal static double Alpha(double value) =>
        Finite(value) is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));

    internal static VisibilityAlignment Align(VisibilityHorizontalAlignment value) => value switch {
        VisibilityHorizontalAlignment.Start => VisibilityAlignment.Start,
        VisibilityHorizontalAlignment.Center => VisibilityAlignment.Center,
        VisibilityHorizontalAlignment.End => VisibilityAlignment.End,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    internal static VisibilityAlignment Align(VerticalAlignment value) => value switch {
        VerticalAlignment.Top => VisibilityAlignment.Top,
        VerticalAlignment.Center => VisibilityAlignment.Center,
        VerticalAlignment.Bottom => VisibilityAlignment.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Stretch is not an animation anchor.")
    };

    internal static void ApplyGroup<T>(object group, IEnumerable<T> children, Builder effects)
    {
        if (!effects.Groups.Add(group)) throw new ArgumentException("Transition groups must not contain cycles.");

        try
        {
            foreach (T child in children)
            {
                switch (child)
                {
                    case EnterTransition enter: enter.Apply(effects); break;
                    case ExitTransition exit: exit.Apply(effects); break;
                    default: throw new ArgumentException("Transition groups must not contain null entries.");
                }
            }
        }
        finally
        {
            effects.Groups.Remove(group);
        }
    }
}
