using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Local theme slots for CMP Foundation ScrollbarStyle, not generated Material3 tokens.
        public static class ScrollBar
        {
            public static readonly MaterialTokenKey<double> Thickness = new("md.comp.scrollbar.thickness");
            public static readonly MaterialTokenKey<double> ThumbMinLength = new("md.comp.scrollbar.thumb-min-length");
            public static readonly MaterialTokenKey<CornerRadius> Shape = new("md.comp.scrollbar.shape");
            public static readonly MaterialTokenKey<Color> UnhoverColor = new("md.comp.scrollbar.unhover-color");
            public static readonly MaterialTokenKey<Color> HoverColor = new("md.comp.scrollbar.hover-color");
            public static readonly MaterialTokenKey<TimeSpan> HoverDuration = new("md.comp.scrollbar.hover-duration");
        }
    }
}
