namespace Material3ExpressiveRefactor.Foundation.Motion;

/// <summary>
/// The six spring slots of a theme. Mirrors androidx <c>MotionSchemeKeyTokens</c>.
/// </summary>
/// <remarks>
/// The split is not decorative: <b>spatial</b> springs animate bounds, position, size and corner
/// radius, and carry the scheme's personality; <b>effects</b> springs animate colour and opacity
/// and are identical across schemes and always critically damped. Animating a colour with a
/// spatial spring makes it wobble, which is never correct.
/// </remarks>
public enum MotionRole
{
    DefaultSpatial,
    FastSpatial,
    SlowSpatial,
    DefaultEffects,
    FastEffects,
    SlowEffects
}
