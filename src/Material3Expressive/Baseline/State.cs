using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Baseline;

/// <summary>
/// The shared state-layer opacities (design doc §7.4): interactive templates drive
/// <c>PART_StateLayer.Opacity</c> from these in pseudo-class styles, and disabled appearances are
/// authored as <c>WithAlpha(Sys.Color.OnSurface, …)</c> over the two disabled entries. Values are
/// the Expressive-era spec — pressed/focus are 0.10, not the legacy 0.12. Theme-independent.
/// </summary>
public class StateTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Sys.State.HoverStateLayerOpacity] = 0.08d,
        [Sys.State.FocusStateLayerOpacity] = 0.10d,
        [Sys.State.PressedStateLayerOpacity] = 0.10d,
        [Sys.State.DraggedStateLayerOpacity] = 0.16d,
        [Sys.State.DisabledContainerOpacity] = 0.12d,
        [Sys.State.DisabledContentOpacity] = 0.38d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
