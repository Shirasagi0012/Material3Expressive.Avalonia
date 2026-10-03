using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Chip.kt defaults and Assist/Filter/Input/SuggestionChipTokens 7_0_1, ChipsTokens 37.2.1.</summary>
public sealed class ChipTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Chip.ContainerHeight] = 32d,
        [Comp.Chip.ContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.Chip.UnselectedShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.Chip.SelectedShape] = new TokenRef(Sys.Shape.Full),
        [Comp.Chip.PressedShape] = new TokenRef(Sys.Shape.Small),
        [Comp.Chip.AvatarShape] = new TokenRef(Sys.Shape.Full),
        [Comp.Chip.AvatarOpacity] = 0.38d,
        [Comp.Chip.IconSize] = 18d,
        [Comp.Chip.AvatarSize] = 24d,
        [Comp.Chip.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.Chip.AssistLabelColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.Chip.LabelColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.Chip.PrimaryIconColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Chip.TonalIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.Chip.SelectedContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.Chip.SelectedContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.Chip.ElevatedContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.Chip.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.12),
        [Comp.Chip.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Chip.OutlineColor] = new TokenRef(Sys.Color.OutlineVariant),
        [Comp.Chip.DisabledOutlineColor] = new WithAlpha(Sys.Color.OnSurface, 0.12),
        [Comp.Chip.OutlineWidth] = 1d,
        [Comp.Chip.FlatElevation] = new TokenRef(Sys.Elevation.Level0),
        // Approved deviation from Chip.kt's Level1: transparent flat Filter backgrounds expose
        // the native Ganesh shadow's inner ring. Keep hover feedback in the state layer.
        [Comp.Chip.FlatFilterHoverElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.Chip.ElevatedElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.Chip.ElevatedHoverElevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.Chip.DisabledElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.Chip.DraggedElevation] = new TokenRef(Sys.Elevation.Level4),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
