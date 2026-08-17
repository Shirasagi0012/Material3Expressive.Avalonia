using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>The immutable shared terminal Raw-token graph used by every resolution request.</summary>
internal static class DefaultRawTokens
{
    internal static CompositeRawTokenCatalog Catalog { get; } =
        CompositeRawTokenCatalog.Create(
            GeneratedM3DefaultRawTokens.Instance,
            McuColorDefaultTokens.Instance,
            FoundationThemeDefaultRawTokens.Instance);
}
