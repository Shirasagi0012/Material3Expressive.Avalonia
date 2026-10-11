using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.shape.*</c> — the Expressive corner scale as <see cref="CornerRadius"/> values
        /// (§4.2). Direction variants (Top/End) are the spec-defined closed named set, not free
        /// four-corner combinations (§4.3). <see cref="Full"/> retains the legacy 1000 sentinel;
        /// MaterialSurface resolves it to 50% of its smaller extent before shape interpolation.
        /// </summary>
        public static class Shape
        {
            public static readonly MaterialTokenKey<CornerRadius> None = new("md.sys.shape.corner.none");
            public static readonly MaterialTokenKey<CornerRadius> ExtraSmall = new("md.sys.shape.corner.extra-small");
            public static readonly MaterialTokenKey<CornerRadius> ExtraSmallTop = new("md.sys.shape.corner.extra-small-top");
            public static readonly MaterialTokenKey<CornerRadius> Small = new("md.sys.shape.corner.small");
            public static readonly MaterialTokenKey<CornerRadius> Medium = new("md.sys.shape.corner.medium");
            public static readonly MaterialTokenKey<CornerRadius> Large = new("md.sys.shape.corner.large");
            public static readonly MaterialTokenKey<CornerRadius> LargeTop = new("md.sys.shape.corner.large-top");
            public static readonly MaterialTokenKey<CornerRadius> LargeEnd = new("md.sys.shape.corner.large-end");
            public static readonly MaterialTokenKey<CornerRadius> LargeIncreased = new("md.sys.shape.corner.large-increased");
            public static readonly MaterialTokenKey<CornerRadius> ExtraLarge = new("md.sys.shape.corner.extra-large");
            public static readonly MaterialTokenKey<CornerRadius> ExtraLargeTop = new("md.sys.shape.corner.extra-large-top");
            public static readonly MaterialTokenKey<CornerRadius> ExtraLargeIncreased = new("md.sys.shape.corner.extra-large-increased");
            public static readonly MaterialTokenKey<CornerRadius> ExtraExtraLarge = new("md.sys.shape.corner.extra-extra-large");
            public static readonly MaterialTokenKey<CornerRadius> Full = new("md.sys.shape.corner.full");
        }
    }

    public static partial class Ref
    {
        /// <summary>
        /// <c>md.ref.shape.*</c> — the corner scalars (DIPs) the system scale is composed from
        /// (§4.3). Alignment/advanced tier: overriding a scalar does not retro-edit the composed
        /// <see cref="CornerRadius"/> tokens — that propagation is the future parameterized shape
        /// scheme's job, same read-only contract as <c>ref.palette.*</c> (§5.6).
        /// </summary>
        public static class Shape
        {
            public static readonly MaterialTokenKey<double> CornerNone = new("md.ref.shape.corner.none");
            public static readonly MaterialTokenKey<double> CornerExtraSmall = new("md.ref.shape.corner.extra-small");
            public static readonly MaterialTokenKey<double> CornerSmall = new("md.ref.shape.corner.small");
            public static readonly MaterialTokenKey<double> CornerMedium = new("md.ref.shape.corner.medium");
            public static readonly MaterialTokenKey<double> CornerLarge = new("md.ref.shape.corner.large");
            public static readonly MaterialTokenKey<double> CornerLargeIncreased = new("md.ref.shape.corner.large-increased");
            public static readonly MaterialTokenKey<double> CornerExtraLarge = new("md.ref.shape.corner.extra-large");
            public static readonly MaterialTokenKey<double> CornerExtraLargeIncreased = new("md.ref.shape.corner.extra-large-increased");
            public static readonly MaterialTokenKey<double> CornerExtraExtraLarge = new("md.ref.shape.corner.extra-extra-large");
        }
    }
}
