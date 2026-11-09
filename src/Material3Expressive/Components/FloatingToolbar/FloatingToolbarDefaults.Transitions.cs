using Avalonia.Layout;
using ComposeAnimation;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

public static partial class FloatingToolbarDefaults
{
    /// <summary>Creates the horizontal expand transition. Supply the current FastSpatial theme spring.</summary>
    public static EnterTransition HorizontalEnterTransition(AnimatedVisibility owner,
        VisibilityHorizontalAlignment expandFrom, MaterialSpring animationSpec) =>
        new ExpandHorizontally { ExpandFrom = expandFrom, Spec = SizeSpec(owner, animationSpec) };

    /// <summary>Creates the vertical expand transition. Supply the current FastSpatial theme spring.</summary>
    public static EnterTransition VerticalEnterTransition(AnimatedVisibility owner,
        VerticalAlignment expandFrom, MaterialSpring animationSpec) =>
        new ExpandVertically { ExpandFrom = expandFrom, Spec = SizeSpec(owner, animationSpec) };

    /// <summary>Creates the horizontal shrink transition. Supply the current FastSpatial theme spring.</summary>
    public static ExitTransition HorizontalExitTransition(AnimatedVisibility owner,
        VisibilityHorizontalAlignment shrinkTowards, MaterialSpring animationSpec) =>
        new ShrinkHorizontally { ShrinkTowards = shrinkTowards, Spec = SizeSpec(owner, animationSpec) };

    /// <summary>Creates the vertical shrink transition. Supply the current FastSpatial theme spring.</summary>
    public static ExitTransition VerticalExitTransition(AnimatedVisibility owner,
        VerticalAlignment shrinkTowards, MaterialSpring animationSpec) =>
        new ShrinkVertically { ShrinkTowards = shrinkTowards, Spec = SizeSpec(owner, animationSpec) };

    private static FloatingToolbarSizeSpec SizeSpec(AnimatedVisibility owner, MaterialSpring spring)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return new FloatingToolbarSizeSpec(spring.DampingRatio, spring.Stiffness, owner);
    }
}
