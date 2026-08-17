using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;
using Xunit;
using Xunit.Sdk;

namespace Material3Expressive.Tests.TestUtils;

/// <summary>Small test-only bridge for resolving the shared Raw-token API from a placement.</summary>
internal static class SharedTokenTestHelper
{
    public static T Resolve<T>(
        IResourceHost origin,
        MaterialToken<T> token,
        ThemeVariant? theme = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(token);

        var key = MaterialValue.FromRaw($"Tests.Resolve.{token.Path}", token);
        return Resolve(origin, key, theme);
    }

    public static T Resolve<T>(
        IResourceHost origin,
        MaterialValueKey<T> key,
        ThemeVariant? theme = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(key);

        // Match the shared runtime's normal requester-relative behaviour when callers omit the
        // query theme. Explicit queries remain unchanged so tests can exercise inheritance/fallback.
        theme ??= (origin as StyledElement)?.ActualThemeVariant;
        if (!origin.TryFindResource(key, theme, out var value))
            throw new XunitException($"Resource '{key.Path}' was not found.");

        return Assert.IsType<T>(value);
    }
}
