using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Local native CommandBar themes use navigation indicator colors and motion.
        public static class CommandBar
        {
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> EffectsAnimation = new("md.comp.command-bar.effects-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> IndicatorSizeAnimation = new("md.comp.command-bar.indicator-size-animation");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.command-bar.container.color");
            public static readonly MaterialTokenKey<double> StackedContainerHeight = new("md.comp.command-bar.stacked-container.height");
            public static readonly MaterialTokenKey<Foundation.Typography.TextStyle> LabelTextStyle = new("md.comp.command-bar.label.text-style");
            public static readonly MaterialTokenKey<double> ActiveIndicatorWidth = new("md.comp.command-bar.active-indicator.width");
            public static readonly MaterialTokenKey<double> ActiveIndicatorHeight = new("md.comp.command-bar.active-indicator.height");
            public static readonly MaterialTokenKey<Thickness> InlineContentPadding = new("md.comp.command-bar.inline-content.padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.command-bar.icon.size");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.command-bar.selected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColor = new("md.comp.command-bar.selected.label.color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.command-bar.indicator.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.command-bar.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.command-bar.unselected.label.color");
            public static readonly MaterialTokenKey<Color> DisabledIconColor = new("md.comp.command-bar.disabled.icon.color");
            public static readonly MaterialTokenKey<Color> DisabledTextColor = new("md.comp.command-bar.disabled.label.color");
        }
    }
}
