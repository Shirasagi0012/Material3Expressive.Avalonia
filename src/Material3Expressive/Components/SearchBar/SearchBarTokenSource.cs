using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Fixed SearchBar / SearchView tokens and DockedSearchBar default arguments.</summary>
public sealed class SearchBarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object> {
        [Comp.SearchBar.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.SearchBar.DockedContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.SearchBar.DividerColor] = new TokenRef(Sys.Color.Outline),
        [Comp.SearchBar.TonalElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.SearchBar.ShadowElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.SearchBar.DefaultWidth] = 360d,
        [Comp.SearchBar.InputMaximumWidth] = 720d,
        [Comp.SearchBar.InputContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.SearchBar.InputTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.SearchBar.LeadingIconColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.SearchBar.TrailingIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.SearchBar.SupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.SearchBar.MinimumResultsHeight] = 240d,
        [Comp.SearchBar.MaximumWindowHeightFraction] = (double)(2f / 3f),
        [Comp.SearchBar.EnterDuration] = new TokenRef(Sys.Motion.Duration.Long4),
        [Comp.SearchBar.ExitDuration] = new TokenRef(Sys.Motion.Duration.Medium3),
        [Comp.SearchBar.AnimationDelay] = new TokenRef(Sys.Motion.Duration.Short2),
        [Comp.SearchBar.ExpandAnimation] = new TokenRef(Sys.Motion.Spring.SpatialSlow),
        [Comp.SearchBar.CollapseAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault)
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
