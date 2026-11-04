using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class PullToRefresh
        {
            public static readonly MaterialTokenKey<double> MaxDistance = new("md.comp.pull-to-refresh.max-distance");
            public static readonly MaterialTokenKey<double> RefreshThreshold = new("md.comp.pull-to-refresh.refresh-threshold");
            public static readonly MaterialTokenKey<double> ContainerSize = new("md.comp.pull-to-refresh.container.size");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.pull-to-refresh.container.shape");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.pull-to-refresh.container.color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.pull-to-refresh.indicator.color");
            public static readonly MaterialTokenKey<double> Elevation = new("md.comp.pull-to-refresh.elevation");
            public static readonly MaterialTokenKey<double> LoadingElevation = new("md.comp.pull-to-refresh.loading.elevation");
            public static readonly MaterialTokenKey<SpringSpec> EffectsAnimation = new("md.comp.pull-to-refresh.effects-animation");
        }
    }
}
