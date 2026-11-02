using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class GridSplitter
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.grid-splitter.minimum-width");
            public static readonly MaterialTokenKey<double> MinimumHeight = new("md.comp.grid-splitter.minimum-height");
        }
    }
}
