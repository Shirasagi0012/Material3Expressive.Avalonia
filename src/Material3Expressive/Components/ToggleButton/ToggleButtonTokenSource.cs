using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Defaults from the fixed Compose baseline. References remain dynamically resolved.</summary>
public sealed class ToggleButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FilledToggleButton.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.FilledToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.FilledToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.FilledToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.FilledToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.10),
        [Comp.FilledToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurfaceVariant, Sys.State.DisabledContentOpacity),
        [Comp.ElevatedToggleButton.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.ElevatedToggleButton.ContentColor] = new TokenRef(Sys.Color.Primary),
        [Comp.ElevatedToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.ElevatedToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.ElevatedToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.10),
        [Comp.ElevatedToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurfaceVariant, Sys.State.DisabledContentOpacity),
        [Comp.TonalToggleButton.ContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.TonalToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.TonalToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.TonalToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.OnSecondary),
        [Comp.TonalToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.10),
        [Comp.TonalToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurfaceVariant, Sys.State.DisabledContentOpacity),
        [Comp.OutlinedToggleButton.ContainerColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.OutlinedToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.OutlinedToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.InverseSurface),
        [Comp.OutlinedToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.OutlinedToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OutlineVariant, 0.10),
        [Comp.OutlinedToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurfaceVariant, Sys.State.DisabledContentOpacity),
        [Comp.ToggleButton.ShapeAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.ToggleButton.BorderWidthAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.ToggleButton.BorderColorAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.ToggleButton.SmallPressedShape] = new CornerRadius(6),
        [Comp.ToggleButton.ExtraSmallIconSpacing] = new TokenRef(Comp.Button.ExtraSmallIconSpacing),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
