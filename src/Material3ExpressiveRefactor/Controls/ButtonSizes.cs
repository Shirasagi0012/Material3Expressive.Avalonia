using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

/// <summary>
/// Baseline defaults for the five Button size tables (upstream ButtonXSmall/Small/Medium/Large/
/// XLargeTokens v0_11_0), shared by every button style. Shapes hop into the sys corner scale and
/// typography into the sys type scale, following the composable's textStyleFor mapping (the size
/// token files themselves carry no typography upstream). ContentPadding is horizontal-only: the
/// template centers content vertically. One deviation from rendered Compose: the extra-small
/// horizontal padding is the token value 16, whereas the composable hardcodes 12 with a TODO
/// noting the token will be corrected — the token table is treated as canonical here.
/// Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class ButtonSizesTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.ButtonXSmall.ContainerHeight] = 32d,
        [Comp.ButtonXSmall.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.ButtonXSmall.ContainerShapeSquare] = new TokenRef(Sys.Shape.Medium),
        [Comp.ButtonXSmall.PressedContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.ButtonXSmall.ContentPadding] = new Thickness(16, 0, 16, 0),
        [Comp.ButtonXSmall.IconSize] = 20d,
        [Comp.ButtonXSmall.IconLabelSpace] = 8d,
        [Comp.ButtonXSmall.OutlinedOutlineWidth] = new Thickness(1),
        [Comp.ButtonXSmall.LabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Font),
        [Comp.ButtonXSmall.LabelTextSize] = new TokenRef(Sys.TypeScale.LabelLarge.Size),
        [Comp.ButtonXSmall.LabelTextWeight] = new TokenRef(Sys.TypeScale.LabelLarge.Weight),
        [Comp.ButtonXSmall.LabelTextTracking] = new TokenRef(Sys.TypeScale.LabelLarge.LetterSpacing),
        [Comp.ButtonXSmall.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.LabelLarge.LineHeight),

        [Comp.ButtonSmall.ContainerHeight] = 40d,
        [Comp.ButtonSmall.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.ButtonSmall.ContainerShapeSquare] = new TokenRef(Sys.Shape.Medium),
        [Comp.ButtonSmall.PressedContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.ButtonSmall.ContentPadding] = new Thickness(16, 0, 16, 0),
        [Comp.ButtonSmall.IconSize] = 20d,
        [Comp.ButtonSmall.IconLabelSpace] = 8d,
        [Comp.ButtonSmall.OutlinedOutlineWidth] = new Thickness(1),
        [Comp.ButtonSmall.LabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Font),
        [Comp.ButtonSmall.LabelTextSize] = new TokenRef(Sys.TypeScale.LabelLarge.Size),
        [Comp.ButtonSmall.LabelTextWeight] = new TokenRef(Sys.TypeScale.LabelLarge.Weight),
        [Comp.ButtonSmall.LabelTextTracking] = new TokenRef(Sys.TypeScale.LabelLarge.LetterSpacing),
        [Comp.ButtonSmall.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.LabelLarge.LineHeight),

        [Comp.ButtonMedium.ContainerHeight] = 56d,
        [Comp.ButtonMedium.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.ButtonMedium.ContainerShapeSquare] = new TokenRef(Sys.Shape.Large),
        [Comp.ButtonMedium.PressedContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.ButtonMedium.ContentPadding] = new Thickness(24, 0, 24, 0),
        [Comp.ButtonMedium.IconSize] = 24d,
        [Comp.ButtonMedium.IconLabelSpace] = 8d,
        [Comp.ButtonMedium.OutlinedOutlineWidth] = new Thickness(1),
        [Comp.ButtonMedium.LabelTextFont] = new TokenRef(Sys.TypeScale.TitleMedium.Font),
        [Comp.ButtonMedium.LabelTextSize] = new TokenRef(Sys.TypeScale.TitleMedium.Size),
        [Comp.ButtonMedium.LabelTextWeight] = new TokenRef(Sys.TypeScale.TitleMedium.Weight),
        [Comp.ButtonMedium.LabelTextTracking] = new TokenRef(Sys.TypeScale.TitleMedium.LetterSpacing),
        [Comp.ButtonMedium.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.TitleMedium.LineHeight),

        [Comp.ButtonLarge.ContainerHeight] = 96d,
        [Comp.ButtonLarge.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.ButtonLarge.ContainerShapeSquare] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.ButtonLarge.PressedContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ButtonLarge.ContentPadding] = new Thickness(48, 0, 48, 0),
        [Comp.ButtonLarge.IconSize] = 32d,
        [Comp.ButtonLarge.IconLabelSpace] = 12d,
        [Comp.ButtonLarge.OutlinedOutlineWidth] = new Thickness(2),
        [Comp.ButtonLarge.LabelTextFont] = new TokenRef(Sys.TypeScale.HeadlineSmall.Font),
        [Comp.ButtonLarge.LabelTextSize] = new TokenRef(Sys.TypeScale.HeadlineSmall.Size),
        [Comp.ButtonLarge.LabelTextWeight] = new TokenRef(Sys.TypeScale.HeadlineSmall.Weight),
        [Comp.ButtonLarge.LabelTextTracking] = new TokenRef(Sys.TypeScale.HeadlineSmall.LetterSpacing),
        [Comp.ButtonLarge.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.HeadlineSmall.LineHeight),

        [Comp.ButtonXLarge.ContainerHeight] = 136d,
        [Comp.ButtonXLarge.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.ButtonXLarge.ContainerShapeSquare] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.ButtonXLarge.PressedContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ButtonXLarge.ContentPadding] = new Thickness(64, 0, 64, 0),
        [Comp.ButtonXLarge.IconSize] = 40d,
        [Comp.ButtonXLarge.IconLabelSpace] = 16d,
        [Comp.ButtonXLarge.OutlinedOutlineWidth] = new Thickness(3),
        [Comp.ButtonXLarge.LabelTextFont] = new TokenRef(Sys.TypeScale.HeadlineLarge.Font),
        [Comp.ButtonXLarge.LabelTextSize] = new TokenRef(Sys.TypeScale.HeadlineLarge.Size),
        [Comp.ButtonXLarge.LabelTextWeight] = new TokenRef(Sys.TypeScale.HeadlineLarge.Weight),
        [Comp.ButtonXLarge.LabelTextTracking] = new TokenRef(Sys.TypeScale.HeadlineLarge.LetterSpacing),
        [Comp.ButtonXLarge.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.HeadlineLarge.LineHeight),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
