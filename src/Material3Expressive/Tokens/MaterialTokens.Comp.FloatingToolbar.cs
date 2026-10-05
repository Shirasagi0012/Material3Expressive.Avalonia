using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class FloatingToolbar
        {
            public static readonly MaterialTokenKey<double> ExpandedShadowElevation = new("md.comp.floating-toolbar.expanded-shadow-elevation");
            public static readonly MaterialTokenKey<double> ExpandedShadowElevationWithFab = new("md.comp.floating-toolbar.expanded-shadow-elevation-with-fab");
            public static readonly MaterialTokenKey<double> CollapsedShadowElevation = new("md.comp.floating-toolbar.collapsed-shadow-elevation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ExpansionAnimation = new("md.comp.floating-toolbar.expansion-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ElevationAnimation = new("md.comp.floating-toolbar.elevation-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> SideContentAnimation = new("md.comp.floating-toolbar.side-content-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> BalancedPaddingAnimation = new("md.comp.floating-toolbar.balanced-padding-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ScrollSnapAnimation = new("md.comp.floating-toolbar.scroll-snap-animation");
            public static readonly MaterialTokenKey<double> ContainerBetweenSpace = new("md.comp.floating-toolbar.container-between-space");
            public static readonly MaterialTokenKey<double> ContainerExternalPadding = new("md.comp.floating-toolbar.container-external-padding");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.floating-toolbar.container-height");
            public static readonly MaterialTokenKey<double> ContainerLeadingSpace = new("md.comp.floating-toolbar.container-leading-space");
            public static readonly MaterialTokenKey<double> ContainerTrailingSpace = new("md.comp.floating-toolbar.container-trailing-space");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.floating-toolbar.container-shape");
            public static readonly MaterialTokenKey<Color> StandardContainerColor = new("md.comp.floating-toolbar.standard-container-color");
            public static readonly MaterialTokenKey<Color> VibrantContainerColor = new("md.comp.floating-toolbar.vibrant-container-color");
            public static readonly MaterialTokenKey<Color> VibrantButtonSelectedContainerColor = new("md.comp.floating-toolbar.vibrant-button-selected-container-color");
            public static readonly MaterialTokenKey<Color> VibrantButtonSelectedIconColor = new("md.comp.floating-toolbar.vibrant-button-selected-icon-color");
            public static readonly MaterialTokenKey<Color> VibrantButtonSelectedTextColor = new("md.comp.floating-toolbar.vibrant-button-selected-text-color");
            public static readonly MaterialTokenKey<Color> VibrantButtonUnselectedIconColor = new("md.comp.floating-toolbar.vibrant-button-unselected-icon-color");
            public static readonly MaterialTokenKey<Color> VibrantButtonUnselectedTextColor = new("md.comp.floating-toolbar.vibrant-button-unselected-text-color");
        }
    }
}
