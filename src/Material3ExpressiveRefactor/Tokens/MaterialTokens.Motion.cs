using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.motion.*</c> — durations as <see cref="TimeSpan"/> and Expressive spring
        /// parameters as scalars (§4.2: no Easing objects are stored; each control builds its own
        /// easing — stock <c>SpringEasing</c> included — from these parameters; spring mass is
        /// fixed at 1.0 upstream, so only damping and stiffness are tokens).
        /// </summary>
        public static class Motion
        {
            /// <summary>Standard durations, in milliseconds (short1–4 50–200, medium1–4 250–400, long1–4 450–600, extra-long1–4 700–1000).</summary>
            public static class Duration
            {
                public static readonly MaterialTokenKey<TimeSpan> Short1 = new("md.sys.motion.duration.short1");
                public static readonly MaterialTokenKey<TimeSpan> Short2 = new("md.sys.motion.duration.short2");
                public static readonly MaterialTokenKey<TimeSpan> Short3 = new("md.sys.motion.duration.short3");
                public static readonly MaterialTokenKey<TimeSpan> Short4 = new("md.sys.motion.duration.short4");
                public static readonly MaterialTokenKey<TimeSpan> Medium1 = new("md.sys.motion.duration.medium1");
                public static readonly MaterialTokenKey<TimeSpan> Medium2 = new("md.sys.motion.duration.medium2");
                public static readonly MaterialTokenKey<TimeSpan> Medium3 = new("md.sys.motion.duration.medium3");
                public static readonly MaterialTokenKey<TimeSpan> Medium4 = new("md.sys.motion.duration.medium4");
                public static readonly MaterialTokenKey<TimeSpan> Long1 = new("md.sys.motion.duration.long1");
                public static readonly MaterialTokenKey<TimeSpan> Long2 = new("md.sys.motion.duration.long2");
                public static readonly MaterialTokenKey<TimeSpan> Long3 = new("md.sys.motion.duration.long3");
                public static readonly MaterialTokenKey<TimeSpan> Long4 = new("md.sys.motion.duration.long4");
                public static readonly MaterialTokenKey<TimeSpan> ExtraLong1 = new("md.sys.motion.duration.extra-long1");
                public static readonly MaterialTokenKey<TimeSpan> ExtraLong2 = new("md.sys.motion.duration.extra-long2");
                public static readonly MaterialTokenKey<TimeSpan> ExtraLong3 = new("md.sys.motion.duration.extra-long3");
                public static readonly MaterialTokenKey<TimeSpan> ExtraLong4 = new("md.sys.motion.duration.extra-long4");
            }

            /// <summary>
            /// Expressive spring parameters: spatial (position/size) and effects (opacity/color)
            /// springs, each in default, fast and slow variants. Values mirror the Compose M3
            /// motion scheme (damping 0.8/0.6/0.8 spatial, 1.0 effects; stiffness 380/800/200
            /// spatial, 1600/3800/800 effects).
            /// </summary>
            public static class Spring
            {
                public static readonly MaterialTokenKey<double> SpatialDefaultDamping = new("md.sys.motion.spring.spatial-default-damping");
                public static readonly MaterialTokenKey<double> SpatialDefaultStiffness = new("md.sys.motion.spring.spatial-default-stiffness");
                public static readonly MaterialTokenKey<double> SpatialFastDamping = new("md.sys.motion.spring.spatial-fast-damping");
                public static readonly MaterialTokenKey<double> SpatialFastStiffness = new("md.sys.motion.spring.spatial-fast-stiffness");
                public static readonly MaterialTokenKey<double> SpatialSlowDamping = new("md.sys.motion.spring.spatial-slow-damping");
                public static readonly MaterialTokenKey<double> SpatialSlowStiffness = new("md.sys.motion.spring.spatial-slow-stiffness");
                public static readonly MaterialTokenKey<double> EffectsDefaultDamping = new("md.sys.motion.spring.effects-default-damping");
                public static readonly MaterialTokenKey<double> EffectsDefaultStiffness = new("md.sys.motion.spring.effects-default-stiffness");
                public static readonly MaterialTokenKey<double> EffectsFastDamping = new("md.sys.motion.spring.effects-fast-damping");
                public static readonly MaterialTokenKey<double> EffectsFastStiffness = new("md.sys.motion.spring.effects-fast-stiffness");
                public static readonly MaterialTokenKey<double> EffectsSlowDamping = new("md.sys.motion.spring.effects-slow-damping");
                public static readonly MaterialTokenKey<double> EffectsSlowStiffness = new("md.sys.motion.spring.effects-slow-stiffness");
            }
        }
    }
}
