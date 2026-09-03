using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

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
