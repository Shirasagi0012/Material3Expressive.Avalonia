namespace ComposeAnimation.Runtime;

/// <summary>
/// Interpolation mode for arc-based animations. Values are matched to the underlying
/// <see cref="ArcSpline"/> segment modes.
/// </summary>
public enum ArcMode
{
    /// <summary>Forces linear interpolation.</summary>
    ArcLinear = 0,

    /// <summary>Interpolates using a quarter of an ellipse below the straight line.</summary>
    ArcBelow = 4,

    /// <summary>Interpolates using a quarter of an ellipse above the straight line.</summary>
    ArcAbove = 5,
}
