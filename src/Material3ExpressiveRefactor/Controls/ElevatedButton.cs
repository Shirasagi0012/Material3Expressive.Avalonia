using System.Collections.Frozen;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

/// <summary>
/// Baseline defaults for the elevated Button slots (upstream ElevatedButtonTokens v0_11_0). The
/// shadow slots alias the baked <c>md.sys.elevation.*-shadow</c> recipes (§7.6): resting/focus/
/// pressed at level 1, hover at level 2, disabled at level 0. Immutable baseline:
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

        [Comp.ElevatedButton.ContainerShadow] = new TokenRef(Sys.Elevation.Level1Shadow),
        [Comp.ElevatedButton.HoveredContainerShadow] = new TokenRef(Sys.Elevation.Level2Shadow),
        [Comp.ElevatedButton.FocusedContainerShadow] = new TokenRef(Sys.Elevation.Level1Shadow),
        [Comp.ElevatedButton.PressedContainerShadow] = new TokenRef(Sys.Elevation.Level1Shadow),
        [Comp.ElevatedButton.DisabledContainerShadow] = new TokenRef(Sys.Elevation.Level0Shadow),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
