using Avalonia.Styling;

namespace Material3ExpressiveRefactor.Foundation;

public interface ITokenSource
{
    bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value);
    
    event EventHandler? Changed;
}

public static class TokenSourceExtensions
{
    static bool TryGetToken<TValue>(
        this ITokenSource @this,
        MaterialTokenKey<TValue> key,
        ThemeVariant? theme,
        out TValue value)
    {
        if ( @this.TryGetToken(key, theme, out var raw) && raw is TValue typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }
}
