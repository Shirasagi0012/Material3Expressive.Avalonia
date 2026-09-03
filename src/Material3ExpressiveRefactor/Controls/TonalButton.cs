using System.Collections.Frozen;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

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
