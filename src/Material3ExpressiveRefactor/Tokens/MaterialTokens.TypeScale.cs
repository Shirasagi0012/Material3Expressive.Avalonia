using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.typescale.*</c> — the fifteen type-scale roles plus their Expressive
        /// emphasized counterparts, each fanned out into font / size / weight / letter-spacing /
        /// line-height keys (§4.1: composite concepts fan out to flat scalars; §4.2 typography
        /// row). Values stay plain stock types so stock property setters consume them directly.
        /// </summary>
        public static class TypeScale
        {
            public static class DisplayLarge
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-large.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-large.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-large.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-large.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-large.line-height");
            }

            public static class DisplayMedium
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-medium.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-medium.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-medium.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-medium.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-medium.line-height");
            }

            public static class DisplaySmall
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-small.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-small.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-small.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-small.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-small.line-height");
            }

            public static class HeadlineLarge
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-large.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-large.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-large.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-large.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-large.line-height");
            }

            public static class HeadlineMedium
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-medium.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-medium.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-medium.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-medium.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-medium.line-height");
            }

            public static class HeadlineSmall
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-small.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-small.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-small.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-small.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-small.line-height");
            }

            public static class TitleLarge
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-large.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-large.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-large.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-large.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-large.line-height");
            }

            public static class TitleMedium
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-medium.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-medium.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-medium.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-medium.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-medium.line-height");
            }

            public static class TitleSmall
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-small.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-small.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-small.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-small.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-small.line-height");
            }

            public static class BodyLarge
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-large.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-large.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-large.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-large.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-large.line-height");
            }

            public static class BodyMedium
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-medium.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-medium.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-medium.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-medium.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-medium.line-height");
            }

            public static class BodySmall
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-small.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-small.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-small.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-small.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-small.line-height");
            }

            public static class LabelLarge
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-large.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-large.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-large.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-large.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-large.line-height");
            }

            public static class LabelMedium
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-medium.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-medium.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-medium.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-medium.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-medium.line-height");
            }

            public static class LabelSmall
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-small.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-small.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-small.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-small.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-small.line-height");
            }

            public static class DisplayLargeEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-large-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-large-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-large-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-large-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-large-emphasized.line-height");
            }

            public static class DisplayMediumEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-medium-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-medium-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-medium-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-medium-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-medium-emphasized.line-height");
            }

            public static class DisplaySmallEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.display-small-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.display-small-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.display-small-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.display-small-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.display-small-emphasized.line-height");
            }

            public static class HeadlineLargeEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-large-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-large-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-large-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-large-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-large-emphasized.line-height");
            }

            public static class HeadlineMediumEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-medium-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-medium-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-medium-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-medium-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-medium-emphasized.line-height");
            }

            public static class HeadlineSmallEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.headline-small-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.headline-small-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.headline-small-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.headline-small-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.headline-small-emphasized.line-height");
            }

            public static class TitleLargeEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-large-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-large-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-large-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-large-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-large-emphasized.line-height");
            }

            public static class TitleMediumEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-medium-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-medium-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-medium-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-medium-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-medium-emphasized.line-height");
            }

            public static class TitleSmallEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.title-small-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.title-small-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.title-small-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.title-small-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.title-small-emphasized.line-height");
            }

            public static class BodyLargeEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-large-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-large-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-large-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-large-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-large-emphasized.line-height");
            }

            public static class BodyMediumEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-medium-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-medium-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-medium-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-medium-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-medium-emphasized.line-height");
            }

            public static class BodySmallEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.body-small-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.body-small-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.body-small-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.body-small-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.body-small-emphasized.line-height");
            }

            public static class LabelLargeEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-large-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-large-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-large-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-large-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-large-emphasized.line-height");
            }

            public static class LabelMediumEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-medium-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-medium-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-medium-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-medium-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-medium-emphasized.line-height");
            }

            public static class LabelSmallEmphasized
            {
                public static readonly MaterialTokenKey<FontFamily> Font = new("md.sys.typescale.label-small-emphasized.font");
                public static readonly MaterialTokenKey<double> Size = new("md.sys.typescale.label-small-emphasized.size");
                public static readonly MaterialTokenKey<FontWeight> Weight = new("md.sys.typescale.label-small-emphasized.weight");
                public static readonly MaterialTokenKey<double> LetterSpacing = new("md.sys.typescale.label-small-emphasized.letter-spacing");
                public static readonly MaterialTokenKey<double> LineHeight = new("md.sys.typescale.label-small-emphasized.line-height");
            }

        }
    }

    public static partial class Ref
    {
        /// <summary>
        /// <c>md.ref.typeface.*</c> — the brand and plain typefaces the type scale draws on.
        /// System font slots alias these (<c>TokenRef</c>), so retargeting the family here
        /// restyles every role (R2').
        /// </summary>
        public static class Typeface
        {
            public static readonly MaterialTokenKey<FontFamily> Brand = new("md.ref.typeface.brand");
            public static readonly MaterialTokenKey<FontFamily> Plain = new("md.ref.typeface.plain");
        }
    }
}
