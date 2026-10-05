using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class TabRow
        {
            public static readonly MaterialTokenKey<double> EdgePadding = new("md.comp.tab-row.edge-padding");
            public static readonly MaterialTokenKey<double> MinTabWidth = new("md.comp.tab-row.min-tab-width");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> SelectedContentAnimation = new("md.comp.tab-row.selected-content-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> UnselectedContentAnimation = new("md.comp.tab-row.unselected-content-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> IndicatorAnimation = new("md.comp.tab-row.indicator-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ScrollAnimation = new("md.comp.tab-row.scroll-animation");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.tab-row.container.color");
            public static readonly MaterialTokenKey<Color> PrimaryContentColor = new("md.comp.tab-row.primary.content-color");
            public static readonly MaterialTokenKey<Color> SecondaryContentColor = new("md.comp.tab-row.secondary.content-color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.tab-row.indicator.color");
            public static readonly MaterialTokenKey<double> IndicatorHeight = new("md.comp.tab-row.indicator.height");
            public static readonly MaterialTokenKey<CornerRadius> IndicatorShape = new("md.comp.tab-row.indicator.shape");
            public static readonly MaterialTokenKey<double> LargeContainerHeight = new("md.comp.tab-row.large-container.height");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.tab-row.container.height");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.tab-row.label.text-style");
        }
    }
}
