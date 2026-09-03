using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;

using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Foundation;

/// <summary>
/// Baseline defaults for the filled Button slots (design doc §7.5). Component→system mappings
/// are <see cref="TokenRef"/> entries resolved hop-by-hop at the consumption side (§5.1), and
/// the disabled colors are <see cref="WithAlpha"/> derivations over <c>md.sys.color.on-surface</c>
/// (§5.3/§7.4), so a local OnSurface override restyles disabled buttons too. Immutable baseline:
/// <see cref="Changed"/> is a documented no-op.
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

        [Comp.FilledButton.ContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.FilledButton.ContainerHeight] = 40d,
        [Comp.FilledButton.ContentPadding] = new Thickness(24, 0, 24, 0),

        [Comp.FilledButton.LabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Font),
        [Comp.FilledButton.LabelTextSize] = new TokenRef(Sys.TypeScale.LabelLarge.Size),
        [Comp.FilledButton.LabelTextWeight] = new TokenRef(Sys.TypeScale.LabelLarge.Weight),
        [Comp.FilledButton.LabelTextTracking] = new TokenRef(Sys.TypeScale.LabelLarge.LetterSpacing),
        [Comp.FilledButton.LabelTextLineHeight] = new TokenRef(Sys.TypeScale.LabelLarge.LineHeight),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
