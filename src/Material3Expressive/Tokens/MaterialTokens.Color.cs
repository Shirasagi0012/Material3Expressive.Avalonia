using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

/// <summary>
/// Static facade of the fixed token keys (design doc §3.2). Hand-written for now; the shape
/// matches what the source generator will emit later, so call sites won't change when generation
/// lands. Debug names are the canonical upstream paths and exist only for diagnostics/parity —
/// identity is the object reference (§3.1).
/// </summary>
public static partial class MaterialTokens
{
    /// <summary>System layer — the primary public face for global/local theming.</summary>
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.color.*</c> — the 49 classic roles plus the four spec-2025 <c>*-dim</c>
        /// roles (which fall back to their base role under older spec versions).
        /// </summary>
        public static class Color
        {
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Background = new("md.sys.color.background");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnBackground = new("md.sys.color.on-background");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Surface = new("md.sys.color.surface");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceDim = new("md.sys.color.surface-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceBright = new("md.sys.color.surface-bright");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceContainerLowest = new("md.sys.color.surface-container-lowest");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceContainerLow = new("md.sys.color.surface-container-low");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceContainer = new("md.sys.color.surface-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceContainerHigh = new("md.sys.color.surface-container-high");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceContainerHighest = new("md.sys.color.surface-container-highest");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSurface = new("md.sys.color.on-surface");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceVariant = new("md.sys.color.surface-variant");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSurfaceVariant = new("md.sys.color.on-surface-variant");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> InverseSurface = new("md.sys.color.inverse-surface");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> InverseOnSurface = new("md.sys.color.inverse-on-surface");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Outline = new("md.sys.color.outline");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OutlineVariant = new("md.sys.color.outline-variant");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Shadow = new("md.sys.color.shadow");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Scrim = new("md.sys.color.scrim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SurfaceTint = new("md.sys.color.surface-tint");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Primary = new("md.sys.color.primary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> PrimaryDim = new("md.sys.color.primary-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnPrimary = new("md.sys.color.on-primary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> PrimaryContainer = new("md.sys.color.primary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnPrimaryContainer = new("md.sys.color.on-primary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> InversePrimary = new("md.sys.color.inverse-primary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Secondary = new("md.sys.color.secondary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SecondaryDim = new("md.sys.color.secondary-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSecondary = new("md.sys.color.on-secondary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SecondaryContainer = new("md.sys.color.secondary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSecondaryContainer = new("md.sys.color.on-secondary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Tertiary = new("md.sys.color.tertiary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> TertiaryDim = new("md.sys.color.tertiary-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnTertiary = new("md.sys.color.on-tertiary");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> TertiaryContainer = new("md.sys.color.tertiary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnTertiaryContainer = new("md.sys.color.on-tertiary-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> Error = new("md.sys.color.error");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> ErrorDim = new("md.sys.color.error-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnError = new("md.sys.color.on-error");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> ErrorContainer = new("md.sys.color.error-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnErrorContainer = new("md.sys.color.on-error-container");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> PrimaryFixed = new("md.sys.color.primary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> PrimaryFixedDim = new("md.sys.color.primary-fixed-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnPrimaryFixed = new("md.sys.color.on-primary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnPrimaryFixedVariant = new("md.sys.color.on-primary-fixed-variant");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SecondaryFixed = new("md.sys.color.secondary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> SecondaryFixedDim = new("md.sys.color.secondary-fixed-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSecondaryFixed = new("md.sys.color.on-secondary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnSecondaryFixedVariant = new("md.sys.color.on-secondary-fixed-variant");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> TertiaryFixed = new("md.sys.color.tertiary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> TertiaryFixedDim = new("md.sys.color.tertiary-fixed-dim");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnTertiaryFixed = new("md.sys.color.on-tertiary-fixed");
            public static readonly MaterialTokenKey<Avalonia.Media.Color> OnTertiaryFixedVariant = new("md.sys.color.on-tertiary-fixed-variant");
        }
    }

    /// <summary>Reference layer — spec alignment and the advanced tier.</summary>
    public static partial class Ref
    {
        /// <summary>
        /// <c>md.ref.palette.*</c> — six tonal palettes over the closed
        /// <c>TonalPalette.CommonTones</c> set (13 tones). Read-only parity/diagnostic output
        /// (§5.6): overriding these does not feed back into the roles.
        /// </summary>
        public static class Palette
        {
            public static readonly MaterialTokenKey<Color> Primary0 = new("md.ref.palette.primary0");
            public static readonly MaterialTokenKey<Color> Primary10 = new("md.ref.palette.primary10");
            public static readonly MaterialTokenKey<Color> Primary20 = new("md.ref.palette.primary20");
            public static readonly MaterialTokenKey<Color> Primary30 = new("md.ref.palette.primary30");
            public static readonly MaterialTokenKey<Color> Primary40 = new("md.ref.palette.primary40");
            public static readonly MaterialTokenKey<Color> Primary50 = new("md.ref.palette.primary50");
            public static readonly MaterialTokenKey<Color> Primary60 = new("md.ref.palette.primary60");
            public static readonly MaterialTokenKey<Color> Primary70 = new("md.ref.palette.primary70");
            public static readonly MaterialTokenKey<Color> Primary80 = new("md.ref.palette.primary80");
            public static readonly MaterialTokenKey<Color> Primary90 = new("md.ref.palette.primary90");
            public static readonly MaterialTokenKey<Color> Primary95 = new("md.ref.palette.primary95");
            public static readonly MaterialTokenKey<Color> Primary99 = new("md.ref.palette.primary99");
            public static readonly MaterialTokenKey<Color> Primary100 = new("md.ref.palette.primary100");
            public static readonly MaterialTokenKey<Color> Secondary0 = new("md.ref.palette.secondary0");
            public static readonly MaterialTokenKey<Color> Secondary10 = new("md.ref.palette.secondary10");
            public static readonly MaterialTokenKey<Color> Secondary20 = new("md.ref.palette.secondary20");
            public static readonly MaterialTokenKey<Color> Secondary30 = new("md.ref.palette.secondary30");
            public static readonly MaterialTokenKey<Color> Secondary40 = new("md.ref.palette.secondary40");
            public static readonly MaterialTokenKey<Color> Secondary50 = new("md.ref.palette.secondary50");
            public static readonly MaterialTokenKey<Color> Secondary60 = new("md.ref.palette.secondary60");
            public static readonly MaterialTokenKey<Color> Secondary70 = new("md.ref.palette.secondary70");
            public static readonly MaterialTokenKey<Color> Secondary80 = new("md.ref.palette.secondary80");
            public static readonly MaterialTokenKey<Color> Secondary90 = new("md.ref.palette.secondary90");
            public static readonly MaterialTokenKey<Color> Secondary95 = new("md.ref.palette.secondary95");
            public static readonly MaterialTokenKey<Color> Secondary99 = new("md.ref.palette.secondary99");
            public static readonly MaterialTokenKey<Color> Secondary100 = new("md.ref.palette.secondary100");
            public static readonly MaterialTokenKey<Color> Tertiary0 = new("md.ref.palette.tertiary0");
            public static readonly MaterialTokenKey<Color> Tertiary10 = new("md.ref.palette.tertiary10");
            public static readonly MaterialTokenKey<Color> Tertiary20 = new("md.ref.palette.tertiary20");
            public static readonly MaterialTokenKey<Color> Tertiary30 = new("md.ref.palette.tertiary30");
            public static readonly MaterialTokenKey<Color> Tertiary40 = new("md.ref.palette.tertiary40");
            public static readonly MaterialTokenKey<Color> Tertiary50 = new("md.ref.palette.tertiary50");
            public static readonly MaterialTokenKey<Color> Tertiary60 = new("md.ref.palette.tertiary60");
            public static readonly MaterialTokenKey<Color> Tertiary70 = new("md.ref.palette.tertiary70");
            public static readonly MaterialTokenKey<Color> Tertiary80 = new("md.ref.palette.tertiary80");
            public static readonly MaterialTokenKey<Color> Tertiary90 = new("md.ref.palette.tertiary90");
            public static readonly MaterialTokenKey<Color> Tertiary95 = new("md.ref.palette.tertiary95");
            public static readonly MaterialTokenKey<Color> Tertiary99 = new("md.ref.palette.tertiary99");
            public static readonly MaterialTokenKey<Color> Tertiary100 = new("md.ref.palette.tertiary100");
            public static readonly MaterialTokenKey<Color> Neutral0 = new("md.ref.palette.neutral0");
            public static readonly MaterialTokenKey<Color> Neutral10 = new("md.ref.palette.neutral10");
            public static readonly MaterialTokenKey<Color> Neutral20 = new("md.ref.palette.neutral20");
            public static readonly MaterialTokenKey<Color> Neutral30 = new("md.ref.palette.neutral30");
            public static readonly MaterialTokenKey<Color> Neutral40 = new("md.ref.palette.neutral40");
            public static readonly MaterialTokenKey<Color> Neutral50 = new("md.ref.palette.neutral50");
            public static readonly MaterialTokenKey<Color> Neutral60 = new("md.ref.palette.neutral60");
            public static readonly MaterialTokenKey<Color> Neutral70 = new("md.ref.palette.neutral70");
            public static readonly MaterialTokenKey<Color> Neutral80 = new("md.ref.palette.neutral80");
            public static readonly MaterialTokenKey<Color> Neutral90 = new("md.ref.palette.neutral90");
            public static readonly MaterialTokenKey<Color> Neutral95 = new("md.ref.palette.neutral95");
            public static readonly MaterialTokenKey<Color> Neutral99 = new("md.ref.palette.neutral99");
            public static readonly MaterialTokenKey<Color> Neutral100 = new("md.ref.palette.neutral100");
            public static readonly MaterialTokenKey<Color> NeutralVariant0 = new("md.ref.palette.neutral-variant0");
            public static readonly MaterialTokenKey<Color> NeutralVariant10 = new("md.ref.palette.neutral-variant10");
            public static readonly MaterialTokenKey<Color> NeutralVariant20 = new("md.ref.palette.neutral-variant20");
            public static readonly MaterialTokenKey<Color> NeutralVariant30 = new("md.ref.palette.neutral-variant30");
            public static readonly MaterialTokenKey<Color> NeutralVariant40 = new("md.ref.palette.neutral-variant40");
            public static readonly MaterialTokenKey<Color> NeutralVariant50 = new("md.ref.palette.neutral-variant50");
            public static readonly MaterialTokenKey<Color> NeutralVariant60 = new("md.ref.palette.neutral-variant60");
            public static readonly MaterialTokenKey<Color> NeutralVariant70 = new("md.ref.palette.neutral-variant70");
            public static readonly MaterialTokenKey<Color> NeutralVariant80 = new("md.ref.palette.neutral-variant80");
            public static readonly MaterialTokenKey<Color> NeutralVariant90 = new("md.ref.palette.neutral-variant90");
            public static readonly MaterialTokenKey<Color> NeutralVariant95 = new("md.ref.palette.neutral-variant95");
            public static readonly MaterialTokenKey<Color> NeutralVariant99 = new("md.ref.palette.neutral-variant99");
            public static readonly MaterialTokenKey<Color> NeutralVariant100 = new("md.ref.palette.neutral-variant100");
            public static readonly MaterialTokenKey<Color> Error0 = new("md.ref.palette.error0");
            public static readonly MaterialTokenKey<Color> Error10 = new("md.ref.palette.error10");
            public static readonly MaterialTokenKey<Color> Error20 = new("md.ref.palette.error20");
            public static readonly MaterialTokenKey<Color> Error30 = new("md.ref.palette.error30");
            public static readonly MaterialTokenKey<Color> Error40 = new("md.ref.palette.error40");
            public static readonly MaterialTokenKey<Color> Error50 = new("md.ref.palette.error50");
            public static readonly MaterialTokenKey<Color> Error60 = new("md.ref.palette.error60");
            public static readonly MaterialTokenKey<Color> Error70 = new("md.ref.palette.error70");
            public static readonly MaterialTokenKey<Color> Error80 = new("md.ref.palette.error80");
            public static readonly MaterialTokenKey<Color> Error90 = new("md.ref.palette.error90");
            public static readonly MaterialTokenKey<Color> Error95 = new("md.ref.palette.error95");
            public static readonly MaterialTokenKey<Color> Error99 = new("md.ref.palette.error99");
            public static readonly MaterialTokenKey<Color> Error100 = new("md.ref.palette.error100");
        }
    }
}
