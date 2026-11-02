using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class Expander
        {
            public static readonly MaterialTokenKey<double> HeaderMinHeight = new("md.comp.expander.header-min-height");
            public static readonly MaterialTokenKey<Thickness> HeaderPadding = new("md.comp.expander.header-padding");
            public static readonly MaterialTokenKey<double> HeaderIconSpacing = new("md.comp.expander.header-icon-spacing");
            public static readonly MaterialTokenKey<double> HeaderIconSize = new("md.comp.expander.header-icon-size");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.expander.content-padding");
        }
    }
}
