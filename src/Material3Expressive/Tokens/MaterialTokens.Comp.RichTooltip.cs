using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class RichTooltip
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.rich-tooltip.container-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.rich-tooltip.container-shape");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.rich-tooltip.container-elevation");
            public static readonly MaterialTokenKey<Color> SubheadColor = new("md.comp.rich-tooltip.subhead-color");
            public static readonly MaterialTokenKey<TextStyle> SubheadFont = new("md.comp.rich-tooltip.subhead-font");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.rich-tooltip.supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> SupportingTextFont = new("md.comp.rich-tooltip.supporting-text-font");
            public static readonly MaterialTokenKey<Color> ActionLabelTextColor = new("md.comp.rich-tooltip.action-label-text-color");
            public static readonly MaterialTokenKey<TextStyle> ActionLabelTextFont = new("md.comp.rich-tooltip.action-label-text-font");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.rich-tooltip.content-padding");
            public static readonly MaterialTokenKey<double> MaxWidth = new("md.comp.rich-tooltip.max-width");
            public static readonly MaterialTokenKey<double> SubheadFirstBaselineOffset = new("md.comp.rich-tooltip.subhead-first-baseline-offset");
            public static readonly MaterialTokenKey<double> TextFirstBaselineOffset = new("md.comp.rich-tooltip.text-first-baseline-offset");
            public static readonly MaterialTokenKey<double> TextBottomPadding = new("md.comp.rich-tooltip.text-bottom-padding");
            public static readonly MaterialTokenKey<double> TextVerticalPadding = new("md.comp.rich-tooltip.text-vertical-padding");
            public static readonly MaterialTokenKey<double> ActionMinHeight = new("md.comp.rich-tooltip.action-min-height");
            public static readonly MaterialTokenKey<double> ActionBottomPadding = new("md.comp.rich-tooltip.action-bottom-padding");
        }
    }
}
