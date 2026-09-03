using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

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
