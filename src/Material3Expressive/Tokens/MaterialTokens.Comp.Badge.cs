using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class Badge
        {
            public static readonly MaterialTokenKey<Color> Color = new("md.comp.badge.color");
            public static readonly MaterialTokenKey<Color> LargeColor = new("md.comp.badge.large-color");
            public static readonly MaterialTokenKey<Color> LargeLabelTextColor = new("md.comp.badge.large-label-text-color");
            public static readonly MaterialTokenKey<TextStyle> LargeLabelTextFont = new("md.comp.badge.large-label-text-font");
            public static readonly MaterialTokenKey<CornerRadius> LargeShape = new("md.comp.badge.large-shape");
            public static readonly MaterialTokenKey<double> LargeSize = new("md.comp.badge.large-size");
            public static readonly MaterialTokenKey<CornerRadius> Shape = new("md.comp.badge.shape");
            public static readonly MaterialTokenKey<double> Size = new("md.comp.badge.size");
        }
    }
}
