using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.elevation.*</c> — the six elevation levels in DIPs plus their
        /// <see cref="BoxShadows"/> literals (§7.6: the level→shadow recipe is baked at production
        /// time from the M3 web shadow definitions; no multi-input derivation).
        /// </summary>
        public static class Elevation
        {
            public static readonly MaterialTokenKey<double> Level0 = new("md.sys.elevation.level0");
            public static readonly MaterialTokenKey<double> Level1 = new("md.sys.elevation.level1");
            public static readonly MaterialTokenKey<double> Level2 = new("md.sys.elevation.level2");
            public static readonly MaterialTokenKey<double> Level3 = new("md.sys.elevation.level3");
            public static readonly MaterialTokenKey<double> Level4 = new("md.sys.elevation.level4");
            public static readonly MaterialTokenKey<double> Level5 = new("md.sys.elevation.level5");

            public static readonly MaterialTokenKey<BoxShadows> Level0Shadow = new("md.sys.elevation.level0-shadow");
            public static readonly MaterialTokenKey<BoxShadows> Level1Shadow = new("md.sys.elevation.level1-shadow");
            public static readonly MaterialTokenKey<BoxShadows> Level2Shadow = new("md.sys.elevation.level2-shadow");
            public static readonly MaterialTokenKey<BoxShadows> Level3Shadow = new("md.sys.elevation.level3-shadow");
            public static readonly MaterialTokenKey<BoxShadows> Level4Shadow = new("md.sys.elevation.level4-shadow");
            public static readonly MaterialTokenKey<BoxShadows> Level5Shadow = new("md.sys.elevation.level5-shadow");
        }
    }
}
