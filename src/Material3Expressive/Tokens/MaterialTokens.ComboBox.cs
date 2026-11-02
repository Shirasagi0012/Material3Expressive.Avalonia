using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class ComboBox
        {
            public static readonly MaterialTokenKey<Thickness> ItemPadding = new("md.comp.combo-box.item-padding");
            public static readonly MaterialTokenKey<double> ItemLeadingSpacing = new("md.comp.combo-box.item-leading-spacing");
        }
    }
}
