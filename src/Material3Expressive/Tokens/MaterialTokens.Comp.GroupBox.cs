using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class GroupBox
        {
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.group-box.content-padding");
            public static readonly MaterialTokenKey<double> HeaderBottomSpacing = new("md.comp.group-box.header-bottom-spacing");
        }
    }
}
