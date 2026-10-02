using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Baseline;

/// <summary>
/// The baseline type scale (design doc §6.3 menu): answers <c>md.sys.typescale.*</c> and
/// <c>md.ref.typeface.*</c>. Metrics follow the M3 type scale (transcribed from the Compose M3
/// type-scale tokens). TextUnit entries convert sp to DIPs at the consuming resource scope;
/// TextStyleRef assembles each aggregate from the resolved scalars. Text rendering parity remains
/// unverified. The default typeface resolves by plain name: an app that registers the bundled
/// Roboto collection (MaterialTypography.Roboto) gets it, otherwise the platform falls back —
/// this assembly deliberately does not depend on the font module.
/// </summary>
public class TypographyTokenSource : ITokenSource
{
    private static readonly FontFamily Roboto = new("Roboto");

    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Ref.Typeface.Brand] = Roboto,
        [Sys.TypeScale.FontScale] = 1d,
        [Ref.Typeface.Plain] = Roboto,

        [Sys.TypeScale.DisplayLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayLarge.Style] = new TextStyleRef(Sys.TypeScale.DisplayLarge.Font, Sys.TypeScale.DisplayLarge.Size, Sys.TypeScale.DisplayLarge.Weight, Sys.TypeScale.DisplayLarge.LetterSpacing, Sys.TypeScale.DisplayLarge.LineHeight),
        [Sys.TypeScale.DisplayLarge.Size] = TextUnit.Sp(57),
        [Sys.TypeScale.DisplayLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplayLarge.LetterSpacing] = TextUnit.Sp(-0.2),
        [Sys.TypeScale.DisplayLarge.LineHeight] = TextUnit.Sp(64),

        [Sys.TypeScale.DisplayMedium.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayMedium.Style] = new TextStyleRef(Sys.TypeScale.DisplayMedium.Font, Sys.TypeScale.DisplayMedium.Size, Sys.TypeScale.DisplayMedium.Weight, Sys.TypeScale.DisplayMedium.LetterSpacing, Sys.TypeScale.DisplayMedium.LineHeight),
        [Sys.TypeScale.DisplayMedium.Size] = TextUnit.Sp(45),
        [Sys.TypeScale.DisplayMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplayMedium.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.DisplayMedium.LineHeight] = TextUnit.Sp(52),

        [Sys.TypeScale.DisplaySmall.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplaySmall.Style] = new TextStyleRef(Sys.TypeScale.DisplaySmall.Font, Sys.TypeScale.DisplaySmall.Size, Sys.TypeScale.DisplaySmall.Weight, Sys.TypeScale.DisplaySmall.LetterSpacing, Sys.TypeScale.DisplaySmall.LineHeight),
        [Sys.TypeScale.DisplaySmall.Size] = TextUnit.Sp(36),
        [Sys.TypeScale.DisplaySmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplaySmall.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.DisplaySmall.LineHeight] = TextUnit.Sp(44),

        [Sys.TypeScale.HeadlineLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineLarge.Style] = new TextStyleRef(Sys.TypeScale.HeadlineLarge.Font, Sys.TypeScale.HeadlineLarge.Size, Sys.TypeScale.HeadlineLarge.Weight, Sys.TypeScale.HeadlineLarge.LetterSpacing, Sys.TypeScale.HeadlineLarge.LineHeight),
        [Sys.TypeScale.HeadlineLarge.Size] = TextUnit.Sp(32),
        [Sys.TypeScale.HeadlineLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineLarge.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineLarge.LineHeight] = TextUnit.Sp(40),

        [Sys.TypeScale.HeadlineMedium.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineMedium.Style] = new TextStyleRef(Sys.TypeScale.HeadlineMedium.Font, Sys.TypeScale.HeadlineMedium.Size, Sys.TypeScale.HeadlineMedium.Weight, Sys.TypeScale.HeadlineMedium.LetterSpacing, Sys.TypeScale.HeadlineMedium.LineHeight),
        [Sys.TypeScale.HeadlineMedium.Size] = TextUnit.Sp(28),
        [Sys.TypeScale.HeadlineMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineMedium.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineMedium.LineHeight] = TextUnit.Sp(36),

        [Sys.TypeScale.HeadlineSmall.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineSmall.Style] = new TextStyleRef(Sys.TypeScale.HeadlineSmall.Font, Sys.TypeScale.HeadlineSmall.Size, Sys.TypeScale.HeadlineSmall.Weight, Sys.TypeScale.HeadlineSmall.LetterSpacing, Sys.TypeScale.HeadlineSmall.LineHeight),
        [Sys.TypeScale.HeadlineSmall.Size] = TextUnit.Sp(24),
        [Sys.TypeScale.HeadlineSmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineSmall.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineSmall.LineHeight] = TextUnit.Sp(32),

        [Sys.TypeScale.TitleLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.TitleLarge.Style] = new TextStyleRef(Sys.TypeScale.TitleLarge.Font, Sys.TypeScale.TitleLarge.Size, Sys.TypeScale.TitleLarge.Weight, Sys.TypeScale.TitleLarge.LetterSpacing, Sys.TypeScale.TitleLarge.LineHeight),
        [Sys.TypeScale.TitleLarge.Size] = TextUnit.Sp(22),
        [Sys.TypeScale.TitleLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.TitleLarge.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.TitleLarge.LineHeight] = TextUnit.Sp(28),

        [Sys.TypeScale.TitleMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleMedium.Style] = new TextStyleRef(Sys.TypeScale.TitleMedium.Font, Sys.TypeScale.TitleMedium.Size, Sys.TypeScale.TitleMedium.Weight, Sys.TypeScale.TitleMedium.LetterSpacing, Sys.TypeScale.TitleMedium.LineHeight),
        [Sys.TypeScale.TitleMedium.Size] = TextUnit.Sp(16),
        [Sys.TypeScale.TitleMedium.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleMedium.LetterSpacing] = TextUnit.Sp(0.2),
        [Sys.TypeScale.TitleMedium.LineHeight] = TextUnit.Sp(24),

        [Sys.TypeScale.TitleSmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleSmall.Style] = new TextStyleRef(Sys.TypeScale.TitleSmall.Font, Sys.TypeScale.TitleSmall.Size, Sys.TypeScale.TitleSmall.Weight, Sys.TypeScale.TitleSmall.LetterSpacing, Sys.TypeScale.TitleSmall.LineHeight),
        [Sys.TypeScale.TitleSmall.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.TitleSmall.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleSmall.LetterSpacing] = TextUnit.Sp(0.1),
        [Sys.TypeScale.TitleSmall.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.BodyLarge.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyLarge.Style] = new TextStyleRef(Sys.TypeScale.BodyLarge.Font, Sys.TypeScale.BodyLarge.Size, Sys.TypeScale.BodyLarge.Weight, Sys.TypeScale.BodyLarge.LetterSpacing, Sys.TypeScale.BodyLarge.LineHeight),
        [Sys.TypeScale.BodyLarge.Size] = TextUnit.Sp(16),
        [Sys.TypeScale.BodyLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodyLarge.LetterSpacing] = TextUnit.Sp(0.5),
        [Sys.TypeScale.BodyLarge.LineHeight] = TextUnit.Sp(24),

        [Sys.TypeScale.BodyMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyMedium.Style] = new TextStyleRef(Sys.TypeScale.BodyMedium.Font, Sys.TypeScale.BodyMedium.Size, Sys.TypeScale.BodyMedium.Weight, Sys.TypeScale.BodyMedium.LetterSpacing, Sys.TypeScale.BodyMedium.LineHeight),
        [Sys.TypeScale.BodyMedium.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.BodyMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodyMedium.LetterSpacing] = TextUnit.Sp(0.2),
        [Sys.TypeScale.BodyMedium.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.BodySmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodySmall.Style] = new TextStyleRef(Sys.TypeScale.BodySmall.Font, Sys.TypeScale.BodySmall.Size, Sys.TypeScale.BodySmall.Weight, Sys.TypeScale.BodySmall.LetterSpacing, Sys.TypeScale.BodySmall.LineHeight),
        [Sys.TypeScale.BodySmall.Size] = TextUnit.Sp(12),
        [Sys.TypeScale.BodySmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodySmall.LetterSpacing] = TextUnit.Sp(0.4),
        [Sys.TypeScale.BodySmall.LineHeight] = TextUnit.Sp(16),

        [Sys.TypeScale.LabelLarge.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelLarge.Style] = new TextStyleRef(Sys.TypeScale.LabelLarge.Font, Sys.TypeScale.LabelLarge.Size, Sys.TypeScale.LabelLarge.Weight, Sys.TypeScale.LabelLarge.LetterSpacing, Sys.TypeScale.LabelLarge.LineHeight),
        [Sys.TypeScale.LabelLarge.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.LabelLarge.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelLarge.LetterSpacing] = TextUnit.Sp(0.1),
        [Sys.TypeScale.LabelLarge.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.LabelMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelMedium.Style] = new TextStyleRef(Sys.TypeScale.LabelMedium.Font, Sys.TypeScale.LabelMedium.Size, Sys.TypeScale.LabelMedium.Weight, Sys.TypeScale.LabelMedium.LetterSpacing, Sys.TypeScale.LabelMedium.LineHeight),
        [Sys.TypeScale.LabelMedium.Size] = TextUnit.Sp(12),
        [Sys.TypeScale.LabelMedium.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelMedium.LetterSpacing] = TextUnit.Sp(0.5),
        [Sys.TypeScale.LabelMedium.LineHeight] = TextUnit.Sp(16),

        [Sys.TypeScale.LabelSmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelSmall.Style] = new TextStyleRef(Sys.TypeScale.LabelSmall.Font, Sys.TypeScale.LabelSmall.Size, Sys.TypeScale.LabelSmall.Weight, Sys.TypeScale.LabelSmall.LetterSpacing, Sys.TypeScale.LabelSmall.LineHeight),
        [Sys.TypeScale.LabelSmall.Size] = TextUnit.Sp(11),
        [Sys.TypeScale.LabelSmall.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelSmall.LetterSpacing] = TextUnit.Sp(0.5),
        [Sys.TypeScale.LabelSmall.LineHeight] = TextUnit.Sp(16),

        [Sys.TypeScale.DisplayLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayLargeEmphasized.Style] = new TextStyleRef(Sys.TypeScale.DisplayLargeEmphasized.Font, Sys.TypeScale.DisplayLargeEmphasized.Size, Sys.TypeScale.DisplayLargeEmphasized.Weight, Sys.TypeScale.DisplayLargeEmphasized.LetterSpacing, Sys.TypeScale.DisplayLargeEmphasized.LineHeight),
        [Sys.TypeScale.DisplayLargeEmphasized.Size] = TextUnit.Sp(57),
        [Sys.TypeScale.DisplayLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplayLargeEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.DisplayLargeEmphasized.LineHeight] = TextUnit.Sp(64),

        [Sys.TypeScale.DisplayMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayMediumEmphasized.Style] = new TextStyleRef(Sys.TypeScale.DisplayMediumEmphasized.Font, Sys.TypeScale.DisplayMediumEmphasized.Size, Sys.TypeScale.DisplayMediumEmphasized.Weight, Sys.TypeScale.DisplayMediumEmphasized.LetterSpacing, Sys.TypeScale.DisplayMediumEmphasized.LineHeight),
        [Sys.TypeScale.DisplayMediumEmphasized.Size] = TextUnit.Sp(45),
        [Sys.TypeScale.DisplayMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplayMediumEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.DisplayMediumEmphasized.LineHeight] = TextUnit.Sp(52),

        [Sys.TypeScale.DisplaySmallEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplaySmallEmphasized.Style] = new TextStyleRef(Sys.TypeScale.DisplaySmallEmphasized.Font, Sys.TypeScale.DisplaySmallEmphasized.Size, Sys.TypeScale.DisplaySmallEmphasized.Weight, Sys.TypeScale.DisplaySmallEmphasized.LetterSpacing, Sys.TypeScale.DisplaySmallEmphasized.LineHeight),
        [Sys.TypeScale.DisplaySmallEmphasized.Size] = TextUnit.Sp(36),
        [Sys.TypeScale.DisplaySmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplaySmallEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.DisplaySmallEmphasized.LineHeight] = TextUnit.Sp(44),

        [Sys.TypeScale.HeadlineLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineLargeEmphasized.Style] = new TextStyleRef(Sys.TypeScale.HeadlineLargeEmphasized.Font, Sys.TypeScale.HeadlineLargeEmphasized.Size, Sys.TypeScale.HeadlineLargeEmphasized.Weight, Sys.TypeScale.HeadlineLargeEmphasized.LetterSpacing, Sys.TypeScale.HeadlineLargeEmphasized.LineHeight),
        [Sys.TypeScale.HeadlineLargeEmphasized.Size] = TextUnit.Sp(32),
        [Sys.TypeScale.HeadlineLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineLargeEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineLargeEmphasized.LineHeight] = TextUnit.Sp(40),

        [Sys.TypeScale.HeadlineMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineMediumEmphasized.Style] = new TextStyleRef(Sys.TypeScale.HeadlineMediumEmphasized.Font, Sys.TypeScale.HeadlineMediumEmphasized.Size, Sys.TypeScale.HeadlineMediumEmphasized.Weight, Sys.TypeScale.HeadlineMediumEmphasized.LetterSpacing, Sys.TypeScale.HeadlineMediumEmphasized.LineHeight),
        [Sys.TypeScale.HeadlineMediumEmphasized.Size] = TextUnit.Sp(28),
        [Sys.TypeScale.HeadlineMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineMediumEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineMediumEmphasized.LineHeight] = TextUnit.Sp(36),

        [Sys.TypeScale.HeadlineSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineSmallEmphasized.Style] = new TextStyleRef(Sys.TypeScale.HeadlineSmallEmphasized.Font, Sys.TypeScale.HeadlineSmallEmphasized.Size, Sys.TypeScale.HeadlineSmallEmphasized.Weight, Sys.TypeScale.HeadlineSmallEmphasized.LetterSpacing, Sys.TypeScale.HeadlineSmallEmphasized.LineHeight),
        [Sys.TypeScale.HeadlineSmallEmphasized.Size] = TextUnit.Sp(24),
        [Sys.TypeScale.HeadlineSmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineSmallEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.HeadlineSmallEmphasized.LineHeight] = TextUnit.Sp(32),

        [Sys.TypeScale.TitleLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.TitleLargeEmphasized.Style] = new TextStyleRef(Sys.TypeScale.TitleLargeEmphasized.Font, Sys.TypeScale.TitleLargeEmphasized.Size, Sys.TypeScale.TitleLargeEmphasized.Weight, Sys.TypeScale.TitleLargeEmphasized.LetterSpacing, Sys.TypeScale.TitleLargeEmphasized.LineHeight),
        [Sys.TypeScale.TitleLargeEmphasized.Size] = TextUnit.Sp(22),
        [Sys.TypeScale.TitleLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleLargeEmphasized.LetterSpacing] = TextUnit.Sp(0),
        [Sys.TypeScale.TitleLargeEmphasized.LineHeight] = TextUnit.Sp(28),

        [Sys.TypeScale.TitleMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleMediumEmphasized.Style] = new TextStyleRef(Sys.TypeScale.TitleMediumEmphasized.Font, Sys.TypeScale.TitleMediumEmphasized.Size, Sys.TypeScale.TitleMediumEmphasized.Weight, Sys.TypeScale.TitleMediumEmphasized.LetterSpacing, Sys.TypeScale.TitleMediumEmphasized.LineHeight),
        [Sys.TypeScale.TitleMediumEmphasized.Size] = TextUnit.Sp(16),
        [Sys.TypeScale.TitleMediumEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.TitleMediumEmphasized.LetterSpacing] = TextUnit.Sp(0.15),
        [Sys.TypeScale.TitleMediumEmphasized.LineHeight] = TextUnit.Sp(24),

        [Sys.TypeScale.TitleSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleSmallEmphasized.Style] = new TextStyleRef(Sys.TypeScale.TitleSmallEmphasized.Font, Sys.TypeScale.TitleSmallEmphasized.Size, Sys.TypeScale.TitleSmallEmphasized.Weight, Sys.TypeScale.TitleSmallEmphasized.LetterSpacing, Sys.TypeScale.TitleSmallEmphasized.LineHeight),
        [Sys.TypeScale.TitleSmallEmphasized.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.TitleSmallEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.TitleSmallEmphasized.LetterSpacing] = TextUnit.Sp(0.1),
        [Sys.TypeScale.TitleSmallEmphasized.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.BodyLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyLargeEmphasized.Style] = new TextStyleRef(Sys.TypeScale.BodyLargeEmphasized.Font, Sys.TypeScale.BodyLargeEmphasized.Size, Sys.TypeScale.BodyLargeEmphasized.Weight, Sys.TypeScale.BodyLargeEmphasized.LetterSpacing, Sys.TypeScale.BodyLargeEmphasized.LineHeight),
        [Sys.TypeScale.BodyLargeEmphasized.Size] = TextUnit.Sp(16),
        [Sys.TypeScale.BodyLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodyLargeEmphasized.LetterSpacing] = TextUnit.Sp(0.15),
        [Sys.TypeScale.BodyLargeEmphasized.LineHeight] = TextUnit.Sp(24),

        [Sys.TypeScale.BodyMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyMediumEmphasized.Style] = new TextStyleRef(Sys.TypeScale.BodyMediumEmphasized.Font, Sys.TypeScale.BodyMediumEmphasized.Size, Sys.TypeScale.BodyMediumEmphasized.Weight, Sys.TypeScale.BodyMediumEmphasized.LetterSpacing, Sys.TypeScale.BodyMediumEmphasized.LineHeight),
        [Sys.TypeScale.BodyMediumEmphasized.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.BodyMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodyMediumEmphasized.LetterSpacing] = TextUnit.Sp(0.25),
        [Sys.TypeScale.BodyMediumEmphasized.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.BodySmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodySmallEmphasized.Style] = new TextStyleRef(Sys.TypeScale.BodySmallEmphasized.Font, Sys.TypeScale.BodySmallEmphasized.Size, Sys.TypeScale.BodySmallEmphasized.Weight, Sys.TypeScale.BodySmallEmphasized.LetterSpacing, Sys.TypeScale.BodySmallEmphasized.LineHeight),
        [Sys.TypeScale.BodySmallEmphasized.Size] = TextUnit.Sp(12),
        [Sys.TypeScale.BodySmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodySmallEmphasized.LetterSpacing] = TextUnit.Sp(0.4),
        [Sys.TypeScale.BodySmallEmphasized.LineHeight] = TextUnit.Sp(16),

        [Sys.TypeScale.LabelLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelLargeEmphasized.Style] = new TextStyleRef(Sys.TypeScale.LabelLargeEmphasized.Font, Sys.TypeScale.LabelLargeEmphasized.Size, Sys.TypeScale.LabelLargeEmphasized.Weight, Sys.TypeScale.LabelLargeEmphasized.LetterSpacing, Sys.TypeScale.LabelLargeEmphasized.LineHeight),
        [Sys.TypeScale.LabelLargeEmphasized.Size] = TextUnit.Sp(14),
        [Sys.TypeScale.LabelLargeEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelLargeEmphasized.LetterSpacing] = TextUnit.Sp(0.1),
        [Sys.TypeScale.LabelLargeEmphasized.LineHeight] = TextUnit.Sp(20),

        [Sys.TypeScale.LabelMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelMediumEmphasized.Style] = new TextStyleRef(Sys.TypeScale.LabelMediumEmphasized.Font, Sys.TypeScale.LabelMediumEmphasized.Size, Sys.TypeScale.LabelMediumEmphasized.Weight, Sys.TypeScale.LabelMediumEmphasized.LetterSpacing, Sys.TypeScale.LabelMediumEmphasized.LineHeight),
        [Sys.TypeScale.LabelMediumEmphasized.Size] = TextUnit.Sp(12),
        [Sys.TypeScale.LabelMediumEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelMediumEmphasized.LetterSpacing] = TextUnit.Sp(0.5),
        [Sys.TypeScale.LabelMediumEmphasized.LineHeight] = TextUnit.Sp(16),

        [Sys.TypeScale.LabelSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelSmallEmphasized.Style] = new TextStyleRef(Sys.TypeScale.LabelSmallEmphasized.Font, Sys.TypeScale.LabelSmallEmphasized.Size, Sys.TypeScale.LabelSmallEmphasized.Weight, Sys.TypeScale.LabelSmallEmphasized.LetterSpacing, Sys.TypeScale.LabelSmallEmphasized.LineHeight),
        [Sys.TypeScale.LabelSmallEmphasized.Size] = TextUnit.Sp(11),
        [Sys.TypeScale.LabelSmallEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelSmallEmphasized.LetterSpacing] = TextUnit.Sp(0.5),
        [Sys.TypeScale.LabelSmallEmphasized.LineHeight] = TextUnit.Sp(16),

    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
