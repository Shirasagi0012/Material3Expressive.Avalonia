using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class Dialog
        {
            public static readonly MaterialTokenKey<double> MaxWidth = new("md.comp.dialog.maxwidth");
            public static readonly MaterialTokenKey<double> MinWidth = new("md.comp.dialog.minwidth");
            public static readonly MaterialTokenKey<Color> ActionLabelTextColor = new("md.comp.dialog.action-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionFocusLabelTextColor = new("md.comp.dialog.action-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionHoverLabelTextColor = new("md.comp.dialog.action-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionPressedLabelTextColor = new("md.comp.dialog.action-pressed-label-text-color");
            public static readonly MaterialTokenKey<TextStyle> ActionLabelTextFont = new("md.comp.dialog.action-label-text-font");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.dialog.container-color");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.dialog.container-elevation");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.dialog.container-shape");
            public static readonly MaterialTokenKey<Color> HeadlineColor = new("md.comp.dialog.headline-color");
            public static readonly MaterialTokenKey<TextStyle> HeadlineFont = new("md.comp.dialog.headline-font");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.dialog.supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> SupportingTextFont = new("md.comp.dialog.supporting-text-font");
            public static readonly MaterialTokenKey<Color> IconColor = new("md.comp.dialog.icon-color");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.dialog.icon-size");
        }
    }
}
