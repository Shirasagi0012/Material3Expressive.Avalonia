using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>Chooses a finite rectangle animation from the current and destination bounds.</summary>
/// <remarks>Override Create for geometry-dependent paths. Replacing this policy affects the next
/// target change, not a run already in progress. AnimatedBounds invokes it separately for its
/// outer and inner layout boundaries. Both arguments and any absolute keyframes in the returned
/// spec use the animation's coordinate basis: ReferenceScope coordinates, the selected motion-frame
/// convention, plus accumulated compensation after MoveContent. They are not necessarily raw
/// LookaheadCoordinates positions after a scroll-ancestor change. Derive absolute waypoints from
/// these arguments; changing this basis without translating the spec would change the path.</remarks>
public class BoundsTransform
{
    public IFiniteAnimationSpec<Rect> Spec { get; set; } = new SpringSpec<Rect>(
        stiffness: Spring.StiffnessMediumLow, visibilityThreshold: VisibilityThresholds.Rect);

    public virtual IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds) => Spec;
}
