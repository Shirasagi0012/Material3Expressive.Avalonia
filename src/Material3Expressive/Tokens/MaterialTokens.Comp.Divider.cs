using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Fixed DividerTokens v0_117; both orientations share these defaults.
        public static class Divider
        {
            public static readonly MaterialTokenKey<Color> Color = new("md.comp.divider.color");
            public static readonly MaterialTokenKey<double> Thickness = new("md.comp.divider.thickness");
        }
    }
}
