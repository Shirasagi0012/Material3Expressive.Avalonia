using Avalonia;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public enum ContentSlideDirection { Left, Right, Up, Down, Start, End }

/// <summary>
/// Segment of an AnimatedContent transform. Size properties are read live from the host.
/// </summary>
public interface IAnimatedContentTransitionScope
{
    object? InitialState { get; }
    object? TargetState { get; }
    bool IsTransitioningTo(object? initial, object? target);
    VisibilityAlignment ContentAlignment { get; }
    Size CurrentSize { get; }
    Size TargetContentSize { get; }
    FlowDirection FlowDirection { get; }
    EnterTransition SlideIntoContainer(ContentSlideDirection towards,
        IFiniteAnimationSpec<Vector>? spec = null, Func<double, double>? offset = null);
    ExitTransition SlideOutOfContainer(ContentSlideDirection towards,
        IFiniteAnimationSpec<Vector>? spec = null, Func<double, double>? offset = null);
}
