using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.elevation.*</c> — the six elevation levels in DIPs (upstream ElevationTokens).
        /// The level→pixels derivation (point-light key/ambient model, shadow color from
        /// <c>md.sys.color.shadow</c>) is performed at render time by MaterialSkiaShadow via
        /// <c>MaterialSurface</c> — exactly like Compose — so no baked shadow recipe tokens exist.
        /// </summary>
        public static class Elevation
        {
            public static readonly MaterialTokenKey<double> Level0 = new("md.sys.elevation.level0");
            public static readonly MaterialTokenKey<double> Level1 = new("md.sys.elevation.level1");
            public static readonly MaterialTokenKey<double> Level2 = new("md.sys.elevation.level2");
            public static readonly MaterialTokenKey<double> Level3 = new("md.sys.elevation.level3");
            public static readonly MaterialTokenKey<double> Level4 = new("md.sys.elevation.level4");
            public static readonly MaterialTokenKey<double> Level5 = new("md.sys.elevation.level5");
        }
    }
}
