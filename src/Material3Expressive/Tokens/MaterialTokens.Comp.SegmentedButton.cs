using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class SegmentedButton
        {
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.outlined-segmented-button.content-padding");
            public static readonly MaterialTokenKey<double> IconSpacing = new("md.comp.outlined-segmented-button.icon-spacing");
            public static readonly MaterialTokenKey<SpringSpec> IconEffectsAnimation = new("md.comp.outlined-segmented-button.icon-effects-animation");
            public static readonly MaterialTokenKey<SpringSpec> IconSpatialAnimation = new("md.comp.outlined-segmented-button.icon-spatial-animation");
            public static readonly MaterialTokenKey<SpringSpec> LabelOffsetAnimation = new("md.comp.outlined-segmented-button.label-offset-animation");
            public static readonly MaterialTokenKey<Color> InactiveContainerColor = new("md.comp.outlined-segmented-button.inactive-container-color");
            public static readonly MaterialTokenKey<Color> DisabledActiveContainerColor = new("md.comp.outlined-segmented-button.disabled-active-container-color");
            public static readonly MaterialTokenKey<Color> DisabledInactiveContainerColor = new("md.comp.outlined-segmented-button.disabled-inactive-container-color");
            public static readonly MaterialTokenKey<Color> DisabledActiveContentColor = new("md.comp.outlined-segmented-button.disabled-active-content-color");
            public static readonly MaterialTokenKey<Color> DisabledInactiveContentColor = new("md.comp.outlined-segmented-button.disabled-inactive-content-color");
            public static readonly MaterialTokenKey<Color> ActiveBorderColor = new("md.comp.outlined-segmented-button.active-border-color");
            public static readonly MaterialTokenKey<Color> InactiveBorderColor = new("md.comp.outlined-segmented-button.inactive-border-color");
            public static readonly MaterialTokenKey<Color> DisabledActiveBorderColor = new("md.comp.outlined-segmented-button.disabled-active-border-color");
            public static readonly MaterialTokenKey<Color> DisabledInactiveBorderColor = new("md.comp.outlined-segmented-button.disabled-inactive-border-color");

            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.outlined-segmented-button.container.height");
            public static readonly MaterialTokenKey<CornerRadius> Shape = new("md.comp.outlined-segmented-button.shape");
            public static readonly MaterialTokenKey<double> OutlineWidth = new("md.comp.outlined-segmented-button.outline.width");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.outlined-segmented-button.icon.size");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.outlined-segmented-button.label.text-style");
            public static readonly MaterialTokenKey<Color> ActiveContainerColor = new("md.comp.outlined-segmented-button.active.container.color");
            public static readonly MaterialTokenKey<Color> ActiveContentColor = new("md.comp.outlined-segmented-button.active.content.color");
            public static readonly MaterialTokenKey<Color> InactiveContentColor = new("md.comp.outlined-segmented-button.inactive.content.color");
            public static readonly MaterialTokenKey<Color> OutlineColor = new("md.comp.outlined-segmented-button.outline.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.outlined-segmented-button.disabled.content.color");
            public static readonly MaterialTokenKey<Color> DisabledOutlineColor = new("md.comp.outlined-segmented-button.disabled.outline.color");
        }
    }
}
