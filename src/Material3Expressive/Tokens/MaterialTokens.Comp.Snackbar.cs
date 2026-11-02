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
        public static class Snackbar
        {
            public static readonly MaterialTokenKey<double> MaxWidth = new("md.comp.snackbar.max-width");
            public static readonly MaterialTokenKey<double> ContentStartPadding = new("md.comp.snackbar.content-start-padding");
            public static readonly MaterialTokenKey<double> ContentEndPadding = new("md.comp.snackbar.content-end-padding");
            public static readonly MaterialTokenKey<double> TextVerticalPadding = new("md.comp.snackbar.text-vertical-padding");
            public static readonly MaterialTokenKey<double> TextEndSpacing = new("md.comp.snackbar.text-end-spacing");
            public static readonly MaterialTokenKey<double> FirstBaselineOffset = new("md.comp.snackbar.first-baseline-offset");
            public static readonly MaterialTokenKey<double> NewLineTextEndSpacing = new("md.comp.snackbar.new-line-text-end-spacing");
            public static readonly MaterialTokenKey<double> NewLineLastBaselineOffset = new("md.comp.snackbar.new-line-last-baseline-offset");
            public static readonly MaterialTokenKey<double> NewLineBottomPadding = new("md.comp.snackbar.new-line-bottom-padding");
            public static readonly MaterialTokenKey<Thickness> DataMargin = new("md.comp.snackbar.data-margin");
            public static readonly MaterialTokenKey<double> DismissContainerSize = new("md.comp.snackbar.dismiss-container-size");

            public static readonly MaterialTokenKey<Thickness> ActionContentPadding = new("md.comp.snackbar.action-content-padding");
            public static readonly MaterialTokenKey<SpringSpec> ScaleAnimation = new("md.comp.snackbar.scale-animation");
            public static readonly MaterialTokenKey<SpringSpec> OpacityAnimation = new("md.comp.snackbar.opacity-animation");

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
