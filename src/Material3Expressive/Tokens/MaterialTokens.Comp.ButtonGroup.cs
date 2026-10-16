using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class ButtonGroup
        {
            public static readonly MaterialTokenKey<double> ExpandedRatio = new("md.comp.button-group.expanded-ratio");
            public static readonly MaterialTokenKey<double> Spacing = new("md.comp.button-group.spacing");
            public static readonly MaterialTokenKey<SpringSpec> WidthAnimation = new("md.comp.button-group.width.animation");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-group.content.padding");
            public static readonly MaterialTokenKey<Thickness> IconContentPadding = new("md.comp.button-group.icon.content.padding");
            public static readonly MaterialTokenKey<double> CompressionLimit = new("md.comp.button-group.compression-limit");
            public static readonly MaterialTokenKey<ButtonGroupArrangement> HorizontalArrangement = new("md.comp.button-group.horizontal-arrangement");
        }

        public static class ConnectedButtonGroup
        {
            public static readonly MaterialTokenKey<double> Spacing = new("md.comp.connected-button-group.spacing");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.connected-button-group.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.connected-button-group.pressed.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> OuterCornerRadius = new("md.comp.connected-button-group.outer.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> CheckedCornerRadius = new("md.comp.connected-button-group.checked.corner.radius");
        }
    }
}
