using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Local default-argument slots; Scaffold has no generated upstream component table.
        public static class Scaffold
        {
            public static readonly MaterialTokenKey<FabPosition> FloatingActionButtonPosition = new("md.comp.scaffold.floating-action-button.position");
            public static readonly MaterialTokenKey<Thickness> ContentWindowInsets = new("md.comp.scaffold.content.window-insets");
        }
    }
}
