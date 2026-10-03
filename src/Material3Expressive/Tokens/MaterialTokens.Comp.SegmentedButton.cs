using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class SegmentedButton
        {
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
