using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Slider.kt consumes these SliderTokens members and derives the tick colors
        // from the opposite track colors in SliderDefaults.colors().
        public static class Slider
        {
            public static readonly MaterialTokenKey<bool> VerticalEnableCornerShrinking = new("md.comp.slider.verticalenablecornershrinking");
            public static readonly MaterialTokenKey<bool> EnableCornerShrinking = new("md.comp.slider.enablecornershrinking");
            public static readonly MaterialTokenKey<double> TrackInsideCornerSize = new("md.comp.slider.trackinsidecornersize");
            public static readonly MaterialTokenKey<Color> HandleColor = new("md.comp.slider.handle.color");
            public static readonly MaterialTokenKey<Color> ActiveTrackColor = new("md.comp.slider.active-track.color");
            public static readonly MaterialTokenKey<Color> InactiveTrackColor = new("md.comp.slider.inactive-track.color");
            public static readonly MaterialTokenKey<Color> DisabledHandleColor = new("md.comp.slider.disabled.handle.color");
            public static readonly MaterialTokenKey<Color> DisabledActiveTrackColor = new("md.comp.slider.disabled.active-track.color");
            public static readonly MaterialTokenKey<Color> DisabledInactiveTrackColor = new("md.comp.slider.disabled.inactive-track.color");
            public static readonly MaterialTokenKey<double> HandleWidth = new("md.comp.slider.handle.width");
            public static readonly MaterialTokenKey<double> HandleHeight = new("md.comp.slider.handle.height");
            public static readonly MaterialTokenKey<CornerRadius> HandleShape = new("md.comp.slider.handle.shape");
            public static readonly MaterialTokenKey<double> InactiveTrackHeight = new("md.comp.slider.inactive-track.height");
            public static readonly MaterialTokenKey<double> ActiveHandleLeadingSpace = new("md.comp.slider.active-handle.leading-space");
            public static readonly MaterialTokenKey<double> StopIndicatorSize = new("md.comp.slider.stop-indicator.size");
        }
    }
}
