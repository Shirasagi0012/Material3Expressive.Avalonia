using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class PlainTooltip
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.plain-tooltip.container-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.plain-tooltip.container-shape");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.plain-tooltip.supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> SupportingTextFont = new("md.comp.plain-tooltip.supporting-text-font");
        }
    }
}
