using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class ButtonSpinner
        {
            public static readonly MaterialTokenKey<Thickness> RightPadding = new("md.comp.button-spinner.right-padding");
            public static readonly MaterialTokenKey<Thickness> LeftPadding = new("md.comp.button-spinner.left-padding");
        }
    }
}
