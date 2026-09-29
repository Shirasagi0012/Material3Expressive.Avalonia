namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// A reference to another token key — one of the four closed value forms a token entry may
/// hold (design doc §5.3). Resolved hop-by-hop from the consuming element's position (§5.1),
/// so overriding either the referenced key or any intermediate hop takes effect (R1'/R2').
/// Immutable and public so third parties can author default mappings.
/// </summary>
public sealed class TokenRef
{
    public TokenRef(MaterialTokenKey key)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>The referenced key.</summary>
    public MaterialTokenKey Key { get; }

    public override string ToString() => $"TokenRef({Key})";
}
