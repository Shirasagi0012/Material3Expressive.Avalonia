using System.Collections.Frozen;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

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
