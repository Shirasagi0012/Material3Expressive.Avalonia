using Material3Expressive.Foundation.Motion;
using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class TooltipBox
        {
            public static readonly MaterialTokenKey<double> Spacing = new("md.comp.tooltip-box.spacing");
            public static readonly MaterialTokenKey<SpringSpec> ScaleAnimation = new("md.comp.tooltip-box.scale-animation");
            public static readonly MaterialTokenKey<SpringSpec> OpacityAnimation = new("md.comp.tooltip-box.opacity-animation");
        }

        public static class PlainTooltip
        {
            public static readonly MaterialTokenKey<Avalonia.Thickness> ContentPadding = new("md.comp.plain-tooltip.contentpadding");
            public static readonly MaterialTokenKey<double> MaxWidth = new("md.comp.plain-tooltip.maxwidth");
            public static readonly MaterialTokenKey<double> MinHeight = new("md.comp.plain-tooltip.minheight");
            public static readonly MaterialTokenKey<double> MinWidth = new("md.comp.plain-tooltip.minwidth");

            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.plain-tooltip.container-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.plain-tooltip.container-shape");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.plain-tooltip.supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> SupportingTextFont = new("md.comp.plain-tooltip.supporting-text-font");
        }
    }
}
