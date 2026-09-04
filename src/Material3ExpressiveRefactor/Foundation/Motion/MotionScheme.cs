namespace Material3ExpressiveRefactor.Foundation.Motion;

/// <summary>
/// The Compose M3 motion schemes as plain data: the fallback specs used when no token source is
/// in scope, and the reference values the baseline <c>md.sys.motion.spring.*</c> tokens mirror.
/// </summary>
/// <remarks>
/// Consumers normally resolve the composed spec tokens (<c>md.sys.motion.spring.*</c>, composed
/// by <c>SpringSpecRef</c> from the scalar damping/stiffness pairs) and fall back to these
/// constants when resolution fails. A test pins the two representations to equal each other.
/// </remarks>
public static class MotionScheme
{
    /// <summary>
    /// The Expressive scheme: damping 0.8/0.6/0.8 spatial and 1.0 effects; stiffness 380/800/200
    /// spatial, 1600/3800/800 effects. Effects springs are critically damped by design — bounce
    /// belongs to spatial motion only.
    /// </summary>
    public static class Expressive
    {
        public static SpringSpec DefaultSpatial { get; } = new(0.8, 380);
        public static SpringSpec FastSpatial { get; } = new(0.6, 800);
        public static SpringSpec SlowSpatial { get; } = new(0.8, 200);
        public static SpringSpec DefaultEffects { get; } = new(1.0, 1600);
        public static SpringSpec FastEffects { get; } = new(1.0, 3800);
        public static SpringSpec SlowEffects { get; } = new(1.0, 800);
    }
}
