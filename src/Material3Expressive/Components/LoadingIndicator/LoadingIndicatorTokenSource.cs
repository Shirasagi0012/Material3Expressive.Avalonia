using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class LoadingIndicatorTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.LoadingIndicator.ActiveIndicatorColor)) { value = new TokenRef(Sys.Color.Primary); return true; }
        if (ReferenceEquals(key, Comp.LoadingIndicator.ContainedActiveColor)) { value = new TokenRef(Sys.Color.OnPrimaryContainer); return true; }
        if (ReferenceEquals(key, Comp.LoadingIndicator.ContainedContainerColor)) { value = new TokenRef(Sys.Color.PrimaryContainer); return true; }
        if (ReferenceEquals(key, Comp.LoadingIndicator.ContainerShape)) { value = new TokenRef(Sys.Shape.Full); return true; }
        if (ReferenceEquals(key, Comp.LoadingIndicator.ContainerWidth) || ReferenceEquals(key, Comp.LoadingIndicator.ContainerHeight))
        { value = LoadingIndicatorDefaults.ContainerWidth; return true; }
        if (ReferenceEquals(key, Comp.LoadingIndicator.ActiveSize)) { value = LoadingIndicatorDefaults.IndicatorSize; return true; }
        value = null!; return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
