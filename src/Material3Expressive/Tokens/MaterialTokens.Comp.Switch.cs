using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // SwitchDefaults.defaultSwitchColors, fixed SwitchTokens v0_210.
        public static class Switch
        {
            public static readonly MaterialTokenKey<Color> CheckedThumbColor = new("md.comp.switch.checked-thumb-color");
            public static readonly MaterialTokenKey<Color> CheckedTrackColor = new("md.comp.switch.checked-track-color");
            public static readonly MaterialTokenKey<Color> CheckedBorderColor = new("md.comp.switch.checked-border-color");
            public static readonly MaterialTokenKey<Color> CheckedIconColor = new("md.comp.switch.checked-icon-color");
            public static readonly MaterialTokenKey<Color> UncheckedThumbColor = new("md.comp.switch.unchecked-thumb-color");
            public static readonly MaterialTokenKey<Color> UncheckedTrackColor = new("md.comp.switch.unchecked-track-color");
            public static readonly MaterialTokenKey<Color> UncheckedBorderColor = new("md.comp.switch.unchecked-border-color");
            public static readonly MaterialTokenKey<Color> UncheckedIconColor = new("md.comp.switch.unchecked-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckedThumbColor = new("md.comp.switch.disabled-checked-thumb-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckedTrackColor = new("md.comp.switch.disabled-checked-track-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckedBorderColor = new("md.comp.switch.disabled-checked-border-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckedIconColor = new("md.comp.switch.disabled-checked-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedThumbColor = new("md.comp.switch.disabled-unchecked-thumb-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedTrackColor = new("md.comp.switch.disabled-unchecked-track-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedBorderColor = new("md.comp.switch.disabled-unchecked-border-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedIconColor = new("md.comp.switch.disabled-unchecked-icon-color");
        }
    }
}
