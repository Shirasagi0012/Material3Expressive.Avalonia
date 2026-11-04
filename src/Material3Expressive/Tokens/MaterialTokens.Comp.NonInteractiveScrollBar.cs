using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Theme slots for NonInteractiveScrollbarDefaults, not generated upstream tokens.
        public static class NonInteractiveScrollBar
        {
            public static readonly MaterialTokenKey<double> Thickness = new("md.comp.non-interactive-scrollbar.Thickness");
            public static readonly MaterialTokenKey<double> ThumbMinLength = new("md.comp.non-interactive-scrollbar.ThumbMinLength");
            public static readonly MaterialTokenKey<double> ThumbMaxLengthFraction = new("md.comp.non-interactive-scrollbar.ThumbMaxLengthFraction");
            public static readonly MaterialTokenKey<double> MainAxisTrackInset = new("md.comp.non-interactive-scrollbar.MainAxisTrackInset");
            public static readonly MaterialTokenKey<double> CrossAxisTrackInset = new("md.comp.non-interactive-scrollbar.CrossAxisTrackInset");
            public static readonly MaterialTokenKey<Color> ThumbColor = new("md.comp.non-interactive-scrollbar.ThumbColor");
            public static readonly MaterialTokenKey<double> ThumbOpacity = new("md.comp.non-interactive-scrollbar.ThumbOpacity");
            public static readonly MaterialTokenKey<Color> TrackColor = new("md.comp.non-interactive-scrollbar.TrackColor");
            public static readonly MaterialTokenKey<TimeSpan> FadeDuration = new("md.comp.non-interactive-scrollbar.FadeDuration");
            public static readonly MaterialTokenKey<TimeSpan> FadeDelay = new("md.comp.non-interactive-scrollbar.FadeDelay");
        }
    }
}
