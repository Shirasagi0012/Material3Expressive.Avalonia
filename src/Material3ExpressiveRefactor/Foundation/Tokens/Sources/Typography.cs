using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Foundation.Tokens.Sources;

/// <summary>
/// The baseline type scale (design doc §6.3 menu): answers <c>md.sys.typescale.*</c> and
/// <c>md.ref.typeface.*</c>. Metrics follow the M3 type scale (transcribed from the Compose M3
/// type-scale tokens; flagged for re-verification once the parity harness lands — tracking
/// especially). The default typeface resolves by plain name: an app that registers the bundled
/// Roboto collection (MaterialTypography.Roboto) gets it, otherwise the platform falls back —
/// this assembly deliberately does not depend on the font module.
/// </summary>
public class TypographyTokenSource : ITokenSource
{
    private static readonly FontFamily Roboto = new("Roboto");

    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Ref.Typeface.Brand] = Roboto,
        [Ref.Typeface.Plain] = Roboto,

        [Sys.TypeScale.DisplayLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayLarge.Size] = 57d,
        [Sys.TypeScale.DisplayLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplayLarge.LetterSpacing] = -0.2d,
        [Sys.TypeScale.DisplayLarge.LineHeight] = 64d,

        [Sys.TypeScale.DisplayMedium.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayMedium.Size] = 45d,
        [Sys.TypeScale.DisplayMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplayMedium.LetterSpacing] = 0d,
        [Sys.TypeScale.DisplayMedium.LineHeight] = 52d,

        [Sys.TypeScale.DisplaySmall.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplaySmall.Size] = 36d,
        [Sys.TypeScale.DisplaySmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.DisplaySmall.LetterSpacing] = 0d,
        [Sys.TypeScale.DisplaySmall.LineHeight] = 44d,

        [Sys.TypeScale.HeadlineLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineLarge.Size] = 32d,
        [Sys.TypeScale.HeadlineLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineLarge.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineLarge.LineHeight] = 40d,

        [Sys.TypeScale.HeadlineMedium.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineMedium.Size] = 28d,
        [Sys.TypeScale.HeadlineMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineMedium.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineMedium.LineHeight] = 36d,

        [Sys.TypeScale.HeadlineSmall.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineSmall.Size] = 24d,
        [Sys.TypeScale.HeadlineSmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.HeadlineSmall.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineSmall.LineHeight] = 32d,

        [Sys.TypeScale.TitleLarge.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.TitleLarge.Size] = 22d,
        [Sys.TypeScale.TitleLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.TitleLarge.LetterSpacing] = 0d,
        [Sys.TypeScale.TitleLarge.LineHeight] = 28d,

        [Sys.TypeScale.TitleMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleMedium.Size] = 16d,
        [Sys.TypeScale.TitleMedium.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleMedium.LetterSpacing] = 0.2d,
        [Sys.TypeScale.TitleMedium.LineHeight] = 24d,

        [Sys.TypeScale.TitleSmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleSmall.Size] = 14d,
        [Sys.TypeScale.TitleSmall.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleSmall.LetterSpacing] = 0.1d,
        [Sys.TypeScale.TitleSmall.LineHeight] = 20d,

        [Sys.TypeScale.BodyLarge.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyLarge.Size] = 16d,
        [Sys.TypeScale.BodyLarge.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodyLarge.LetterSpacing] = 0.5d,
        [Sys.TypeScale.BodyLarge.LineHeight] = 24d,

        [Sys.TypeScale.BodyMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyMedium.Size] = 14d,
        [Sys.TypeScale.BodyMedium.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodyMedium.LetterSpacing] = 0.2d,
        [Sys.TypeScale.BodyMedium.LineHeight] = 20d,

        [Sys.TypeScale.BodySmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodySmall.Size] = 12d,
        [Sys.TypeScale.BodySmall.Weight] = FontWeight.Regular,
        [Sys.TypeScale.BodySmall.LetterSpacing] = 0.4d,
        [Sys.TypeScale.BodySmall.LineHeight] = 16d,

        [Sys.TypeScale.LabelLarge.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelLarge.Size] = 14d,
        [Sys.TypeScale.LabelLarge.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelLarge.LetterSpacing] = 0.1d,
        [Sys.TypeScale.LabelLarge.LineHeight] = 20d,

        [Sys.TypeScale.LabelMedium.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelMedium.Size] = 12d,
        [Sys.TypeScale.LabelMedium.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelMedium.LetterSpacing] = 0.5d,
        [Sys.TypeScale.LabelMedium.LineHeight] = 16d,

        [Sys.TypeScale.LabelSmall.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelSmall.Size] = 11d,
        [Sys.TypeScale.LabelSmall.Weight] = FontWeight.Medium,
        [Sys.TypeScale.LabelSmall.LetterSpacing] = 0.5d,
        [Sys.TypeScale.LabelSmall.LineHeight] = 16d,

        [Sys.TypeScale.DisplayLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayLargeEmphasized.Size] = 57d,
        [Sys.TypeScale.DisplayLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplayLargeEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.DisplayLargeEmphasized.LineHeight] = 64d,

        [Sys.TypeScale.DisplayMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplayMediumEmphasized.Size] = 45d,
        [Sys.TypeScale.DisplayMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplayMediumEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.DisplayMediumEmphasized.LineHeight] = 52d,

        [Sys.TypeScale.DisplaySmallEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.DisplaySmallEmphasized.Size] = 36d,
        [Sys.TypeScale.DisplaySmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.DisplaySmallEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.DisplaySmallEmphasized.LineHeight] = 44d,

        [Sys.TypeScale.HeadlineLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineLargeEmphasized.Size] = 32d,
        [Sys.TypeScale.HeadlineLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineLargeEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineLargeEmphasized.LineHeight] = 40d,

        [Sys.TypeScale.HeadlineMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineMediumEmphasized.Size] = 28d,
        [Sys.TypeScale.HeadlineMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineMediumEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineMediumEmphasized.LineHeight] = 36d,

        [Sys.TypeScale.HeadlineSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.HeadlineSmallEmphasized.Size] = 24d,
        [Sys.TypeScale.HeadlineSmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.HeadlineSmallEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.HeadlineSmallEmphasized.LineHeight] = 32d,

        [Sys.TypeScale.TitleLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Brand),
        [Sys.TypeScale.TitleLargeEmphasized.Size] = 22d,
        [Sys.TypeScale.TitleLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.TitleLargeEmphasized.LetterSpacing] = 0d,
        [Sys.TypeScale.TitleLargeEmphasized.LineHeight] = 28d,

        [Sys.TypeScale.TitleMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleMediumEmphasized.Size] = 16d,
        [Sys.TypeScale.TitleMediumEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.TitleMediumEmphasized.LetterSpacing] = 0.15d,
        [Sys.TypeScale.TitleMediumEmphasized.LineHeight] = 24d,

        [Sys.TypeScale.TitleSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.TitleSmallEmphasized.Size] = 14d,
        [Sys.TypeScale.TitleSmallEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.TitleSmallEmphasized.LetterSpacing] = 0.1d,
        [Sys.TypeScale.TitleSmallEmphasized.LineHeight] = 20d,

        [Sys.TypeScale.BodyLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyLargeEmphasized.Size] = 16d,
        [Sys.TypeScale.BodyLargeEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodyLargeEmphasized.LetterSpacing] = 0.15d,
        [Sys.TypeScale.BodyLargeEmphasized.LineHeight] = 24d,

        [Sys.TypeScale.BodyMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodyMediumEmphasized.Size] = 14d,
        [Sys.TypeScale.BodyMediumEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodyMediumEmphasized.LetterSpacing] = 0.25d,
        [Sys.TypeScale.BodyMediumEmphasized.LineHeight] = 20d,

        [Sys.TypeScale.BodySmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.BodySmallEmphasized.Size] = 12d,
        [Sys.TypeScale.BodySmallEmphasized.Weight] = FontWeight.Medium,
        [Sys.TypeScale.BodySmallEmphasized.LetterSpacing] = 0.4d,
        [Sys.TypeScale.BodySmallEmphasized.LineHeight] = 16d,

        [Sys.TypeScale.LabelLargeEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelLargeEmphasized.Size] = 14d,
        [Sys.TypeScale.LabelLargeEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelLargeEmphasized.LetterSpacing] = 0.1d,
        [Sys.TypeScale.LabelLargeEmphasized.LineHeight] = 20d,

        [Sys.TypeScale.LabelMediumEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelMediumEmphasized.Size] = 12d,
        [Sys.TypeScale.LabelMediumEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelMediumEmphasized.LetterSpacing] = 0.5d,
        [Sys.TypeScale.LabelMediumEmphasized.LineHeight] = 16d,

        [Sys.TypeScale.LabelSmallEmphasized.Font] = new TokenRef(Ref.Typeface.Plain),
        [Sys.TypeScale.LabelSmallEmphasized.Size] = 11d,
        [Sys.TypeScale.LabelSmallEmphasized.Weight] = FontWeight.Bold,
        [Sys.TypeScale.LabelSmallEmphasized.LetterSpacing] = 0.5d,
        [Sys.TypeScale.LabelSmallEmphasized.LineHeight] = 16d,

    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
