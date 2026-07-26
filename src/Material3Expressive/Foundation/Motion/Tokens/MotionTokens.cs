namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// Spring constants transcribed from androidx <c>tokens/StandardMotionTokens.kt</c> and
/// <c>tokens/ExpressiveMotionTokens.kt</c>, token-DB stamp <c>v0_14_0</c>.
/// </summary>
/// <remarks>
/// Invariant worth preserving on any upstream sync: the three effects springs are identical
/// between the two schemes and all critically damped. Only the spatial springs differ — expressive
/// is softer and slower, which is where its personality lives.
/// </remarks>
public static class MotionTokens
{
    public static class Standard
    {
        public static SpringSpec DefaultSpatial { get; } = new(0.9, 700);
        public static SpringSpec FastSpatial { get; } = new(0.9, 1400);
        public static SpringSpec SlowSpatial { get; } = new(0.9, 300);
        public static SpringSpec DefaultEffects { get; } = new(1.0, 1600);
        public static SpringSpec FastEffects { get; } = new(1.0, 3800);
        public static SpringSpec SlowEffects { get; } = new(1.0, 800);
    }

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
