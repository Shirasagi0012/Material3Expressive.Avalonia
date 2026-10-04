using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class AppBarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.AppBar.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.AppBar.ScrolledContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.AppBar.NavigationIconColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.AppBar.TitleColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.AppBar.SubtitleColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.AppBar.ActionIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.AppBar.BottomContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.AppBar.SmallTitleStyle] = new TokenRef(Sys.TypeScale.TitleLarge.Style),
        [Comp.AppBar.SmallSubtitleStyle] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.AppBar.MediumTitleStyle] = new TokenRef(Sys.TypeScale.HeadlineSmall.Style),
        [Comp.AppBar.LargeTitleStyle] = new TokenRef(Sys.TypeScale.HeadlineMedium.Style),
        [Comp.AppBar.MediumFlexibleTitleStyle] = new TokenRef(Sys.TypeScale.HeadlineMedium.Style),
        [Comp.AppBar.MediumFlexibleSubtitleStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.AppBar.LargeFlexibleTitleStyle] = new TokenRef(Sys.TypeScale.DisplaySmall.Style),
        [Comp.AppBar.LargeFlexibleSubtitleStyle] = new TokenRef(Sys.TypeScale.TitleMedium.Style),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
