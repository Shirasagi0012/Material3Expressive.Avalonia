using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class LoadingIndicator
        {
            public static readonly MaterialTokenKey<Color> ActiveIndicatorColor = new("md.comp.loading-indicator.active-indicator.color");
            public static readonly MaterialTokenKey<Color> ContainedActiveColor = new("md.comp.loading-indicator.contained-active.color");
            public static readonly MaterialTokenKey<Color> ContainedContainerColor = new("md.comp.loading-indicator.contained-container.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.loading-indicator.container.shape");
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.loading-indicator.container.width");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.loading-indicator.container.height");
            public static readonly MaterialTokenKey<double> ActiveSize = new("md.comp.loading-indicator.active.size");
        }
    }
}
