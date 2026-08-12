using Avalonia.Controls;
using Avalonia.Styling;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A short-lived bulk reader which memoizes shared token dependencies.</summary>
public sealed class TokenReader
{
    private readonly MaterialTokenResolver.Session _session;

    internal TokenReader(MaterialTokenResolver.Session session)
    {
        _session = session;
    }

    /// <summary>Starts a bulk token read for a consumer.</summary>
    public static TokenReader Begin(IResourceHost origin, ThemeVariant? theme = null) =>
        new MaterialTokenResolver().BeginRead(origin, theme);

    /// <summary>Gets a resolved token value.</summary>
    public T Get<T>(MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _session.Resolve(token);
    }

    /// <summary>
    /// Resolves <paramref name="token" /> when the resource scope explicitly defines it;
    /// otherwise resolves the component-policy fallback.
    /// </summary>
    /// <remarks>
    /// This is intentionally internal: generated token defaults remain the canonical token
    /// graph, while a component adapter may preserve a narrowly scoped runtime correction
    /// without making that correction part of the generated descriptor.
    /// </remarks>
    internal T GetDefinedOr<T>(MaterialToken<T> token, MaterialToken<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(fallback);
        return _session.IsDefined(token) ? _session.Resolve(token) : _session.Resolve(fallback);
    }
}
