using System.Collections.Frozen;
using Avalonia.Styling;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Fixed IconButtonDefaults toggle colors, retaining the upstream token relationships.</summary>
public sealed class IconToggleButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.StandardIconToggleButton.ContainerColor] = Colors.Transparent,
        [Comp.StandardIconToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.StandardIconToggleButton.CheckedContainerColor] = Colors.Transparent,
        [Comp.StandardIconToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.Primary),
        [Comp.StandardIconToggleButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.StandardIconToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
        [Comp.OutlinedIconToggleButton.ContainerColor] = Colors.Transparent,
        [Comp.OutlinedIconToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.OutlinedIconToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.InverseSurface),
        [Comp.OutlinedIconToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.OutlinedIconToggleButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.OutlinedIconToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
        [Comp.FilledIconToggleButton.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.FilledIconToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.FilledIconToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.FilledIconToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.FilledIconToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.10),
        [Comp.FilledIconToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
        [Comp.TonalIconToggleButton.ContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.TonalIconToggleButton.ContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.TonalIconToggleButton.CheckedContainerColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.TonalIconToggleButton.CheckedContentColor] = new TokenRef(Sys.Color.OnSecondary),
        [Comp.TonalIconToggleButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.10),
        [Comp.TonalIconToggleButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
