using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Fixed IconButtonDefaults filled/tonal toggle colors, retaining the upstream token relationships.</summary>
public sealed class IconToggleButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
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
