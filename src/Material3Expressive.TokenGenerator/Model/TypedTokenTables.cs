using System;
using System.Collections.Generic;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>The design-system layer a table belongs to.</summary>
internal enum TokenTableLayer
{
    Reference,
    System,
    Component
}

internal static class TokenDescriptorPaths
{
    public static string Create(TokenTableLayer layer, string tableName, string memberName) =>
        layer + "." + tableName + "." + memberName;
}

/// <summary>
/// Common metadata and read-only views for the three token table models. The explicit source
/// <c>kind</c> remains available for diagnostics; emitters use <see cref="IsTokenSet"/> and the
/// layer property instead of inferring semantics from that string.
/// </summary>
internal abstract class TokenTable
{
    protected TokenTable(ParsedTokenTable parsed, TokenTableCategory category, TokenTableLayer layer)
    {
        Kind = parsed.DeclaredKind;
        Category = category;
        Layer = layer;
        Name = parsed.Name;
        Namespace = parsed.Namespace;
        Version = parsed.Version;
        Source = parsed.Source;
        Summary = parsed.Summary;
        Provenance = parsed.Provenance;
    }

    /// <summary>The explicit root <c>kind</c> from the source file.</summary>
    public string Kind { get; }

    /// <summary>Schema category used by diagnostics and cross-table validation.</summary>
    public TokenTableCategory Category { get; }

    public TokenTableLayer Layer { get; }

    public string Name { get; }

    public string Namespace { get; }

    public string? Version { get; }

    public string? Source { get; }

    public string? Summary { get; }

    public TokenProvenance Provenance { get; }

    public abstract bool IsTokenSet { get; }

    public virtual IReadOnlyList<TokenEntry> Tokens => Array.Empty<TokenEntry>();

    public virtual IReadOnlyList<TokenProperty> Properties => Array.Empty<TokenProperty>();

    public virtual IReadOnlyList<TokenSet> Sets => Array.Empty<TokenSet>();

    public virtual string? VariantEnum => null;

    public virtual string? VariantEnumSummary => null;

    /// <summary>Gets the stable public descriptor path emitted for one token-set member.</summary>
    public string DescriptorPath(string memberName) => TokenDescriptorPaths.Create(Layer, Name, memberName);

    public static TokenTable Parse(string json)
    {
        var parsed = ParsedTokenTable.Parse(json);

        return parsed.Category switch
        {
            TokenTableCategory.ReferenceTokens => new ReferenceTokenTable(parsed),
            TokenTableCategory.SystemTokens => new SystemTokenTable(parsed),
            TokenTableCategory.ComponentTokens => new ComponentTokenTable(parsed),
            _ => throw new InvalidTableException($"Unsupported token table category '{parsed.Category}'.")
        };
    }
}

/// <summary>Verbatim reference-layer data such as a palette or other design primitives.</summary>
internal sealed class ReferenceTokenTable : TokenTable
{
    private readonly IReadOnlyList<TokenEntry> _tokens;

    public ReferenceTokenTable(ParsedTokenTable parsed)
        : base(parsed, TokenTableCategory.ReferenceTokens, TokenTableLayer.Reference)
    {
        _tokens = parsed.Tokens;
    }

    public override bool IsTokenSet => true;

    public override IReadOnlyList<TokenEntry> Tokens => _tokens;
}

/// <summary>Verbatim component-layer transcriptions of upstream <c>*Tokens.kt</c> objects.</summary>
internal sealed class ComponentTokenTable : TokenTable
{
    private readonly IReadOnlyList<TokenEntry> _tokens;

    public ComponentTokenTable(ParsedTokenTable parsed)
        : base(parsed, TokenTableCategory.ComponentTokens, TokenTableLayer.Component)
    {
        _tokens = parsed.Tokens;
    }

    public override bool IsTokenSet => true;

    public override IReadOnlyList<TokenEntry> Tokens => _tokens;
}

/// <summary>
/// System-layer data. Most system tables are raw token sets; TypeScale is intentionally a
/// scheme-shaped table with properties and sets, so both legal system payloads live in this one
/// typed owner.
/// </summary>
internal sealed class SystemTokenTable : TokenTable
{
    private readonly ParsedTokenTable _parsed;

    public SystemTokenTable(ParsedTokenTable parsed)
        : base(parsed, TokenTableCategory.SystemTokens, TokenTableLayer.System)
    {
        _parsed = parsed;
    }

    public override bool IsTokenSet => _parsed.Kind == "tokenSet";

    public override IReadOnlyList<TokenEntry> Tokens => _parsed.Tokens;

    public override IReadOnlyList<TokenProperty> Properties => _parsed.Properties;

    public override IReadOnlyList<TokenSet> Sets => _parsed.Sets;

    public override string? VariantEnum => _parsed.VariantEnum;

    public override string? VariantEnumSummary => _parsed.VariantEnumSummary;
}
