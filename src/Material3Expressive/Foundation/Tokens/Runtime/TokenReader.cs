using Avalonia.Controls;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using System.ComponentModel;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A short-lived bulk reader which memoizes shared token dependencies.</summary>
[Obsolete(
    "Requester-relative TokenReader is legacy. Consume typed MaterialValueKey<T> resources " +
    "through the nearest MaterialTheme placement.",
    DiagnosticId = "M3E1002")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TokenReader
{
    private readonly MaterialTokenResolver.Session? _legacySession;
    private readonly MaterialResolutionSession? _sharedSession;

    internal TokenReader(MaterialTokenResolver.Session session)
    {
        _legacySession = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal TokenReader(IResourceHost origin, ThemeVariant? theme)
    {
        ArgumentNullException.ThrowIfNull(origin);
        _sharedSession = new MaterialResolutionSession(origin, theme, DefaultRawTokens.Catalog);
    }

    /// <summary>Starts a bulk token read for a consumer.</summary>
    public static TokenReader Begin(IResourceHost origin, ThemeVariant? theme = null) =>
        new(
            origin,
            theme ?? (origin as Avalonia.StyledElement)?.ActualThemeVariant);

    /// <summary>Gets a resolved token value.</summary>
    public T Get<T>(MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return (_legacySession ?? throw LegacySessionRequired()).Resolve(token);
    }

    /// <summary>Gets a resolved shared Raw-token value.</summary>
    public T Get<T>(MaterialDesignTokens.Avalonia.MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return (_sharedSession ?? throw SharedSessionRequired()).ResolveRaw(token);
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
        var session = _legacySession ?? throw LegacySessionRequired();
        return session.IsDefined(token) ? session.Resolve(token) : session.Resolve(fallback);
    }

    /// <summary>
    /// Resolves a shared Raw token when the resource scope explicitly defines it; otherwise
    /// resolves the supplied shared Raw fallback.
    /// </summary>
    /// <remarks>
    /// Visibility is checked by the same shared resolution session used for the value. An
    /// invalid resource hit therefore remains a hit and is surfaced by <see cref="Get{T}" />
    /// rather than silently selecting the fallback.
    /// </remarks>
    internal T GetDefinedOr<T>(
        MaterialDesignTokens.Avalonia.MaterialToken<T> token,
        MaterialDesignTokens.Avalonia.MaterialToken<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(fallback);
        var session = _sharedSession ?? throw SharedSessionRequired();
        return session.HasVisibleResourceDefinition(token)
            ? session.ResolveRaw(token)
            : session.ResolveRaw(fallback);
    }

    private static InvalidOperationException LegacySessionRequired() => new(
        "This TokenReader was created for the shared Raw-token runtime; the legacy token API " +
        "is not available on this instance.");

    private static InvalidOperationException SharedSessionRequired() => new(
        "This TokenReader was created for the legacy token runtime; the shared Raw-token API " +
        "is available from TokenReader.Begin only.");
}
