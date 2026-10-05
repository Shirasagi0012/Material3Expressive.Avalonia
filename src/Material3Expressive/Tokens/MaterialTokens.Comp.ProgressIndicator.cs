using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class ProgressIndicator
        {
            public static readonly MaterialTokenKey<PenLineCap> StrokeCap = new("md.comp.progress-indicator.stroke-cap");
            public static readonly MaterialTokenKey<Color> ActiveIndicatorColor = new("md.comp.progress-indicator.active-indicator.color");
            public static readonly MaterialTokenKey<Color> TrackColor = new("md.comp.progress-indicator.track.color");
        }
        public static class LinearProgressIndicator
        {
            public static readonly MaterialTokenKey<double> Width = new("md.comp.linear-progress-indicator.width");
            public static readonly MaterialTokenKey<double> Height = new("md.comp.linear-progress-indicator.height");
            public static readonly MaterialTokenKey<double> WaveHeight = new("md.comp.linear-progress-indicator.wave.height");
            public static readonly MaterialTokenKey<double> ActiveThickness = new("md.comp.linear-progress-indicator.active.thickness");
            public static readonly MaterialTokenKey<double> TrackThickness = new("md.comp.linear-progress-indicator.track.thickness");
            public static readonly MaterialTokenKey<double> TrackActiveSpace = new("md.comp.linear-progress-indicator.track-active.space");
            public static readonly MaterialTokenKey<double> StopSize = new("md.comp.linear-progress-indicator.stop.size");
            public static readonly MaterialTokenKey<double> ActiveWaveWavelength = new("md.comp.linear-progress-indicator.active-wave.wavelength");
            public static readonly MaterialTokenKey<double> IndeterminateActiveWaveWavelength = new("md.comp.linear-progress-indicator.indeterminate-active-wave.wavelength");
        }
        public static class CircularProgressIndicator
        {
            public static readonly MaterialTokenKey<Color> IndeterminateTrackColor = new("md.comp.circular-progress-indicator.indeterminate-track.color");
            public static readonly MaterialTokenKey<double> Size = new("md.comp.circular-progress-indicator.size");
            public static readonly MaterialTokenKey<double> WaveSize = new("md.comp.circular-progress-indicator.wave.size");
            public static readonly MaterialTokenKey<double> ActiveThickness = new("md.comp.circular-progress-indicator.active.thickness");
            public static readonly MaterialTokenKey<double> TrackThickness = new("md.comp.circular-progress-indicator.track.thickness");
            public static readonly MaterialTokenKey<double> TrackActiveSpace = new("md.comp.circular-progress-indicator.track-active.space");
            public static readonly MaterialTokenKey<double> ActiveWaveWavelength = new("md.comp.circular-progress-indicator.active-wave.wavelength");
        }
    }
}
