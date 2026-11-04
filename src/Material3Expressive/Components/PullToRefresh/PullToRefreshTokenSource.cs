using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class PullToRefreshTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.PullToRefresh.MaxDistance] = 80d,
        [Comp.PullToRefresh.RefreshThreshold] = 80d,
        [Comp.PullToRefresh.ContainerSize] = 40d,
        [Comp.PullToRefresh.ContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.PullToRefresh.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.PullToRefresh.IndicatorColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.PullToRefresh.Elevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.PullToRefresh.LoadingElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.PullToRefresh.EffectsAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
