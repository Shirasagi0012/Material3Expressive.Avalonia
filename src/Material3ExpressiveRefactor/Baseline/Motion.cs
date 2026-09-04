using System.Collections.Frozen;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Baseline;

/// <summary>
/// The baseline motion tokens (design doc §4.2): standard durations as <see cref="TimeSpan"/> and
/// the Expressive spring parameters as scalar damping/stiffness pairs. No Easing objects are
/// stored — each control builds its own easing from these (stock <c>SpringEasing</c> takes
/// Mass/Stiffness/Damping; mass is fixed at 1.0 upstream). Spring values mirror the Compose M3
/// motion scheme. Theme-independent.
/// </summary>
public class MotionTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Sys.Motion.Duration.Short1] = TimeSpan.FromMilliseconds(50),
        [Sys.Motion.Duration.Short2] = TimeSpan.FromMilliseconds(100),
        [Sys.Motion.Duration.Short3] = TimeSpan.FromMilliseconds(150),
        [Sys.Motion.Duration.Short4] = TimeSpan.FromMilliseconds(200),
        [Sys.Motion.Duration.Medium1] = TimeSpan.FromMilliseconds(250),
        [Sys.Motion.Duration.Medium2] = TimeSpan.FromMilliseconds(300),
        [Sys.Motion.Duration.Medium3] = TimeSpan.FromMilliseconds(350),
        [Sys.Motion.Duration.Medium4] = TimeSpan.FromMilliseconds(400),
        [Sys.Motion.Duration.Long1] = TimeSpan.FromMilliseconds(450),
        [Sys.Motion.Duration.Long2] = TimeSpan.FromMilliseconds(500),
        [Sys.Motion.Duration.Long3] = TimeSpan.FromMilliseconds(550),
        [Sys.Motion.Duration.Long4] = TimeSpan.FromMilliseconds(600),
        [Sys.Motion.Duration.ExtraLong1] = TimeSpan.FromMilliseconds(700),
        [Sys.Motion.Duration.ExtraLong2] = TimeSpan.FromMilliseconds(800),
        [Sys.Motion.Duration.ExtraLong3] = TimeSpan.FromMilliseconds(900),
        [Sys.Motion.Duration.ExtraLong4] = TimeSpan.FromMilliseconds(1000),

        [Sys.Motion.Spring.SpatialDefaultDamping] = 0.8d,
        [Sys.Motion.Spring.SpatialDefaultStiffness] = 380d,
        [Sys.Motion.Spring.SpatialFastDamping] = 0.6d,
        [Sys.Motion.Spring.SpatialFastStiffness] = 800d,
        [Sys.Motion.Spring.SpatialSlowDamping] = 0.8d,
        [Sys.Motion.Spring.SpatialSlowStiffness] = 200d,
        [Sys.Motion.Spring.EffectsDefaultDamping] = 1.0d,
        [Sys.Motion.Spring.EffectsDefaultStiffness] = 1600d,
        [Sys.Motion.Spring.EffectsFastDamping] = 1.0d,
        [Sys.Motion.Spring.EffectsFastStiffness] = 3800d,
        [Sys.Motion.Spring.EffectsSlowDamping] = 1.0d,
        [Sys.Motion.Spring.EffectsSlowStiffness] = 800d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
