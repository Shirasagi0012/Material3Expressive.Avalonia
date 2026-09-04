using Avalonia.Controls;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;

namespace Material3ExpressiveRefactor.Foundation.Motion;

/// <summary>
/// Assembles a <see cref="SpringSpec" /> for a <see cref="MotionRole" /> from the
/// <c>md.sys.motion.spring.*</c> scalar token pairs.
/// </summary>
/// <remarks>
/// The tokens store damping and stiffness as independent scalars (design doc §4.2: no spec objects
/// in the token system); this is the single place that pairs them back into a spec. The fallbacks
/// are the Expressive motion scheme values themselves, so a host without the baseline source still
/// animates identically.
/// </remarks>
internal static class MotionSpecs
{
    public static SpringSpec Resolve(IResourceHost? host, MotionRole role, ThemeVariant? theme = null)
    {
        var (dampingKey, stiffnessKey, fallback) = role switch
        {
            MotionRole.DefaultSpatial => (MaterialTokens.Sys.Motion.Spring.SpatialDefaultDamping,
                MaterialTokens.Sys.Motion.Spring.SpatialDefaultStiffness, new SpringSpec(0.8, 380)),
            MotionRole.FastSpatial => (MaterialTokens.Sys.Motion.Spring.SpatialFastDamping,
                MaterialTokens.Sys.Motion.Spring.SpatialFastStiffness, new SpringSpec(0.6, 800)),
            MotionRole.SlowSpatial => (MaterialTokens.Sys.Motion.Spring.SpatialSlowDamping,
                MaterialTokens.Sys.Motion.Spring.SpatialSlowStiffness, new SpringSpec(0.8, 200)),
            MotionRole.DefaultEffects => (MaterialTokens.Sys.Motion.Spring.EffectsDefaultDamping,
                MaterialTokens.Sys.Motion.Spring.EffectsDefaultStiffness, new SpringSpec(1.0, 1600)),
            MotionRole.FastEffects => (MaterialTokens.Sys.Motion.Spring.EffectsFastDamping,
                MaterialTokens.Sys.Motion.Spring.EffectsFastStiffness, new SpringSpec(1.0, 3800)),
            MotionRole.SlowEffects => (MaterialTokens.Sys.Motion.Spring.EffectsSlowDamping,
                MaterialTokens.Sys.Motion.Spring.EffectsSlowStiffness, new SpringSpec(1.0, 800)),
            _ => throw new System.ArgumentOutOfRangeException(nameof(role), role, null)
        };

        var damping = fallback.DampingRatio;
        var stiffness = fallback.Stiffness;

        if (host is not null)
        {
            if (TokenResolver.TryResolve(host, dampingKey, theme, out var d) && d is double resolvedDamping)
                damping = resolvedDamping;
            if (TokenResolver.TryResolve(host, stiffnessKey, theme, out var s) && s is double resolvedStiffness)
                stiffness = resolvedStiffness;
        }

        return new SpringSpec(damping, stiffness);
    }
}
