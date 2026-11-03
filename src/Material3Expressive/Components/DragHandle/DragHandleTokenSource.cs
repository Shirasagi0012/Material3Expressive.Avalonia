using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>The fixed DragHandleTokens baseline.</summary>
public sealed class DragHandleTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.DragHandle.Size] = new Size(4, 48),
        [Comp.DragHandle.PressedSize] = new Size(12, 52),
        [Comp.DragHandle.DraggedSize] = new Size(12, 52),
        [Comp.DragHandle.Color] = new TokenRef(Sys.Color.Outline),
        [Comp.DragHandle.PressedColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.DragHandle.DraggedColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.DragHandle.Shape] = new TokenRef(Sys.Shape.Full),
        [Comp.DragHandle.PressedShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.DragHandle.DraggedShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.DragHandle.MinimumInteractiveSize] = 48d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
