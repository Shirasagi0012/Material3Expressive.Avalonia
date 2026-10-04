using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class Snackbar
        {
            public static readonly MaterialTokenKey<Color> ActionFocusLabelTextColor = new("md.comp.snackbar.action-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionHoverLabelTextColor = new("md.comp.snackbar.action-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionLabelTextColor = new("md.comp.snackbar.action-label-text-color");
            public static readonly MaterialTokenKey<Color> ActionPressedLabelTextColor = new("md.comp.snackbar.action-pressed-label-text-color");
            public static readonly MaterialTokenKey<TextStyle> ActionLabelTextFont = new("md.comp.snackbar.action-label-text-font");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.snackbar.container-color");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.snackbar.container-elevation");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.snackbar.container-shape");
            public static readonly MaterialTokenKey<Color> IconColor = new("md.comp.snackbar.icon-color");
            public static readonly MaterialTokenKey<Color> FocusIconColor = new("md.comp.snackbar.focus-icon-color");
            public static readonly MaterialTokenKey<Color> HoverIconColor = new("md.comp.snackbar.hover-icon-color");
            public static readonly MaterialTokenKey<Color> PressedIconColor = new("md.comp.snackbar.pressed-icon-color");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.snackbar.icon-size");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.snackbar.supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> SupportingTextFont = new("md.comp.snackbar.supporting-text-font");
            public static readonly MaterialTokenKey<double> SingleLineContainerHeight = new("md.comp.snackbar.single-line-container-height");
            public static readonly MaterialTokenKey<double> TwoLinesContainerHeight = new("md.comp.snackbar.two-lines-container-height");
        }
    }
}
