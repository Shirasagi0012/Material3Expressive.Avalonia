using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Filled/ElevatedCardTokens v0_210 and OutlinedCardTokens v0_192 at the fixed baseline.</summary>
public sealed class CardTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FilledCard.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.FilledCard.ContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.FilledCard.ContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FilledCard.PressedContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FilledCard.FocusContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FilledCard.HoverContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.FilledCard.DraggedContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.FilledCard.DisabledContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FilledCard.DisabledContainerColor] = new TokenRef(Sys.Color.SurfaceVariant),
        [Comp.FilledCard.DisabledContainerOpacity] = 0.38d,
        [Comp.ElevatedCard.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.ElevatedCard.ContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.ElevatedCard.ContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedCard.PressedContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedCard.FocusContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedCard.HoverContainerElevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.ElevatedCard.DraggedContainerElevation] = new TokenRef(Sys.Elevation.Level4),
        [Comp.ElevatedCard.DisabledContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.ElevatedCard.DisabledContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.ElevatedCard.DisabledContainerOpacity] = 0.38d,
        [Comp.OutlinedCard.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.OutlinedCard.ContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.OutlinedCard.ContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.OutlinedCard.PressedContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.OutlinedCard.FocusContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.OutlinedCard.HoverContainerElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.OutlinedCard.DraggedContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.OutlinedCard.DisabledContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.OutlinedCard.OutlineColor] = new TokenRef(Sys.Color.OutlineVariant),
        [Comp.OutlinedCard.OutlineWidth] = 1d,
        [Comp.OutlinedCard.DisabledOutlineColor] = new TokenRef(Sys.Color.Outline),
        [Comp.OutlinedCard.DisabledOutlineOpacity] = 0.12d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
