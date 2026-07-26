namespace Material3Expressive.Foundation;

/// <summary>
/// A physics-based animation specification: no duration, no easing curve.
/// </summary>
/// <param name="DampingRatio">
/// Below 1 the spring overshoots and settles back; at 1 it is critically damped and never
/// overshoots. Material's effects springs are all critically damped by design — bounce belongs to
/// spatial motion only.
/// </param>
/// <param name="Stiffness">How hard the spring pulls toward the target. Higher is faster.</param>
/// <param name="VisibilityThreshold">
/// How close to the target counts as arrived, in the animated value's own units.
/// </param>
/// <remarks>
/// Phase 1 ships this as data only — the animator that consumes it lands in Phase 3.
/// </remarks>
public readonly record struct SpringSpec(
    double DampingRatio,
    double Stiffness,
    double VisibilityThreshold = SpringSpec.DefaultVisibilityThreshold
)
{
    public const double DefaultVisibilityThreshold = 0.01;

    /// <summary>True when the spring cannot overshoot its target.</summary>
    public bool IsCriticallyDamped => DampingRatio >= 1.0;
}
