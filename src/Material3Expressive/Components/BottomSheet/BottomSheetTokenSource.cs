using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class BottomSheetTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.BottomSheet.ScrimColor] = new TokenRef(Comp.Scrim.ContainerColor),
        [Comp.BottomSheet.ScrimAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.BottomSheet.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.BottomSheet.ContainerShape] = new TokenRef(Sys.Shape.ExtraLargeTop),
        [Comp.BottomSheet.TonalElevation] = new TokenRef(Sys.Elevation.Level1),
        [Comp.BottomSheet.MaxWidth] = 640d,
        [Comp.BottomSheet.PeekHeight] = 56d,
        [Comp.BottomSheet.WindowInsets] = default(Thickness),
        [Comp.BottomSheet.ShowAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault),
        [Comp.BottomSheet.HideAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.BottomSheet.DragHandleWidth] = 32d,
        [Comp.BottomSheet.DragHandleHeight] = 4d,
        [Comp.BottomSheet.DragHandleColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.BottomSheet.DragHandleShape] = new TokenRef(Sys.Shape.ExtraLarge),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
