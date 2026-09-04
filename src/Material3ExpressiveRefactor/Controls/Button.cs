using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
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


/// <summary>
/// Baseline defaults for the elevated Button slots (upstream ElevatedButtonTokens v0_11_0). The
/// per-state elevations alias the <c>md.sys.elevation.level*</c> DIP levels: resting/focus/pressed
/// at level 1, hover at level 2, disabled at level 0. They are consumed by the template part's
/// <c>MaterialSurface.ShadowElevation</c> and rendered by MaterialSkiaShadow. Immutable baseline:
/// <see cref="Changed"/> is a documented no-op.
/// </summary>
public class ElevatedButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.ElevatedButton.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.ElevatedButton.ContentColor] = new TokenRef(Sys.Color.Primary),
        [Comp.ElevatedButton.StateLayerColor] = new TokenRef(Comp.ElevatedButton.ContentColor),
        [Comp.ElevatedButton.HoverStateLayerColor] = new TokenRef(Comp.ElevatedButton.StateLayerColor),
        [Comp.ElevatedButton.PressedStateLayerColor] = new TokenRef(Comp.ElevatedButton.StateLayerColor),
        [Comp.ElevatedButton.FocusedStateLayerColor] = new TokenRef(Comp.ElevatedButton.StateLayerColor),
        [Comp.ElevatedButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContainerOpacity),
        [Comp.ElevatedButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),

        [Comp.ElevatedButton.ContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedButton.HoveredContainerElevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.ElevatedButton.FocusedContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedButton.PressedContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedButton.DisabledContainerElevation] = new TokenRef(Sys.Elevation.Level0),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the filled Button color slots (design doc §7.5). Component→system
/// mappings are <see cref="TokenRef"/> entries resolved hop-by-hop at the consumption side
/// (§5.1), and the disabled colors are <see cref="WithAlpha"/> derivations over
/// <c>md.sys.color.on-surface</c> (§5.3/§7.4), so a local OnSurface override restyles disabled
/// buttons too. Metrics/shape/typography live in <see cref="ButtonSizesTokenSource"/> — the size
/// axis is orthogonal to the style axis upstream. Immutable baseline: <see cref="Changed"/> is a
/// documented no-op.
/// </summary>
public class FilledButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FilledButton.ContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.FilledButton.ContentColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.FilledButton.StateLayerColor] = new TokenRef(Comp.FilledButton.ContentColor),
        [Comp.FilledButton.HoverStateLayerColor] = new TokenRef(Comp.FilledButton.StateLayerColor),
        [Comp.FilledButton.PressedStateLayerColor] = new TokenRef(Comp.FilledButton.StateLayerColor),
        [Comp.FilledButton.FocusedStateLayerColor] = new TokenRef(Comp.FilledButton.StateLayerColor),
        [Comp.FilledButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContainerOpacity),
        [Comp.FilledButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the outlined Button slots (upstream OutlinedButtonTokens v0_11_0). The
/// container is transparent at rest and when disabled (upstream composable, not a token).
/// DisabledOutlineColor follows the composable rather than the raw token table: it is the resting
/// outline color with the disabled-container opacity applied, not the table's plain
/// OutlineVariant. Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class OutlinedButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.OutlinedButton.ContainerColor] = Colors.Transparent,
        [Comp.OutlinedButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.OutlinedButton.StateLayerColor] = new TokenRef(Comp.OutlinedButton.ContentColor),
        [Comp.OutlinedButton.HoverStateLayerColor] = new TokenRef(Comp.OutlinedButton.StateLayerColor),
        [Comp.OutlinedButton.PressedStateLayerColor] = new TokenRef(Comp.OutlinedButton.StateLayerColor),
        [Comp.OutlinedButton.FocusedStateLayerColor] = new TokenRef(Comp.OutlinedButton.StateLayerColor),
        [Comp.OutlinedButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.OutlinedButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),

        [Comp.OutlinedButton.OutlineColor] = new TokenRef(Sys.Color.OutlineVariant),
        [Comp.OutlinedButton.DisabledOutlineColor] = new WithAlpha(Comp.OutlinedButton.OutlineColor, Sys.State.DisabledContainerOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the text Button slots (upstream TextButtonTokens v0_11_0). ContentColor
/// maps to Primary because the upstream composable overrides its own LabelColor token
/// (OnSurfaceVariant) with a "TODO replace once the token is corrected" comment — the composable
/// is treated as the source of truth. The container is transparent at rest and when disabled.
/// Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class TextButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TextButton.ContainerColor] = Colors.Transparent,
        [Comp.TextButton.ContentColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TextButton.StateLayerColor] = new TokenRef(Comp.TextButton.ContentColor),
        [Comp.TextButton.HoverStateLayerColor] = new TokenRef(Comp.TextButton.StateLayerColor),
        [Comp.TextButton.PressedStateLayerColor] = new TokenRef(Comp.TextButton.StateLayerColor),
        [Comp.TextButton.FocusedStateLayerColor] = new TokenRef(Comp.TextButton.StateLayerColor),
        [Comp.TextButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.TextButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the filled tonal Button slots (upstream TonalButtonTokens v0_11_0).
/// Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class TonalButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TonalButton.ContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.TonalButton.ContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.TonalButton.StateLayerColor] = new TokenRef(Comp.TonalButton.ContentColor),
        [Comp.TonalButton.HoverStateLayerColor] = new TokenRef(Comp.TonalButton.StateLayerColor),
        [Comp.TonalButton.PressedStateLayerColor] = new TokenRef(Comp.TonalButton.StateLayerColor),
        [Comp.TonalButton.FocusedStateLayerColor] = new TokenRef(Comp.TonalButton.StateLayerColor),
        [Comp.TonalButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContainerOpacity),
        [Comp.TonalButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
