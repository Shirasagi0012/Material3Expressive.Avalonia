using System;
using System.Collections.Generic;
using System.Linq;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>The design-system layer a table belongs to.</summary>
internal enum TokenTableLayer
{
    Reference,
    System,
    Component
}

/// <summary>Whether a table is source data or the assembled/defaulted surface consumed at runtime.</summary>
internal enum TokenTableArtifact
{
    Raw,
    Defaults
}

/// <summary>
/// Common metadata and read-only views for the four token table models. The explicit source
/// <c>kind</c> remains available for diagnostics; emitters use <see cref="IsTokenSet"/> and the
/// layer/artifact properties instead of inferring semantics from that string.
/// </summary>
internal abstract class TokenTable
{
    protected TokenTable(ParsedTokenTable parsed, TokenTableCategory category, TokenTableLayer layer,
        TokenTableArtifact artifact)
    {
        Kind = parsed.DeclaredKind;
        Category = category;
        Layer = layer;
        Artifact = artifact;
        Name = parsed.Name;
        Namespace = parsed.Namespace;
        Version = parsed.Version;
        Source = parsed.Source;
        Summary = parsed.Summary;
        Provenance = parsed.Provenance;
        OverridesName = parsed.OverridesName;
    }

    /// <summary>The explicit root <c>kind</c> from the source file.</summary>
    public string Kind { get; }

    /// <summary>Compatibility category used by dependency diagnostics during migration.</summary>
    public TokenTableCategory Category { get; }

    public TokenTableLayer Layer { get; }

    public TokenTableArtifact Artifact { get; }

    public string Name { get; }

    public string Namespace { get; }

    public string? Version { get; }

    public string? Source { get; }

    public string? Summary { get; }

    public TokenProvenance Provenance { get; }

    /// <summary>Optional compatibility name for the generated public override bag.</summary>
    public string? OverridesName { get; }

    public abstract bool IsTokenSet { get; }

    public virtual IReadOnlyList<TokenEntry> Tokens => Array.Empty<TokenEntry>();

    public virtual IReadOnlyList<TokenProperty> Properties => Array.Empty<TokenProperty>();

    public virtual IReadOnlyList<TokenSet> Sets => Array.Empty<TokenSet>();

    public virtual string? VariantEnum => null;

    public virtual string? VariantEnumSummary => null;

    public virtual bool Overridable => false;

    public virtual string? ResolvedName => null;

    public IReadOnlyList<TokenProperty> ColorProperties =>
        Properties.Where(p => p.Type == TokenType.Color).ToArray();

    public static TokenTable Parse(string json)
    {
        var parsed = ParsedTokenTable.Parse(json);

        return parsed.Category switch
        {
            TokenTableCategory.ReferenceTokens => new ReferenceTokenTable(parsed),
            TokenTableCategory.SystemTokens => new SystemTokenTable(parsed),
            TokenTableCategory.ComponentTokens => new ComponentTokenTable(parsed),
            TokenTableCategory.ComponentDefaults => new ComponentDefaultsTable(parsed),
            _ => throw new InvalidTableException($"Unsupported token table category '{parsed.Category}'.")
        };
    }
}

/// <summary>Verbatim reference-layer data such as a palette or other design primitives.</summary>
internal sealed class ReferenceTokenTable : TokenTable
{
    private readonly IReadOnlyList<TokenEntry> _tokens;

    public ReferenceTokenTable(ParsedTokenTable parsed)
        : base(parsed, TokenTableCategory.ReferenceTokens, TokenTableLayer.Reference, TokenTableArtifact.Raw)
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

    public ComponentTokenTable(ParsedTokenTable parsed,
        TokenTableCategory category = TokenTableCategory.ComponentTokens)
        : base(parsed, category, TokenTableLayer.Component, TokenTableArtifact.Raw)
    {
        _tokens = parsed.Tokens;
    }

    public override bool IsTokenSet => true;

    public override IReadOnlyList<TokenEntry> Tokens => _tokens;
}

/// <summary>
/// System-layer data. Most system tables are raw token sets; TypeScale is intentionally a
/// scheme-shaped table with properties and sets, so both legal system payloads live in this one
/// typed owner instead of being mistaken for a component default.
/// </summary>
internal sealed class SystemTokenTable : TokenTable
{
    private readonly ParsedTokenTable _parsed;

    public SystemTokenTable(ParsedTokenTable parsed)
        : base(parsed, TokenTableCategory.SystemTokens, TokenTableLayer.System, TokenTableArtifact.Raw)
    {
        _parsed = parsed;
    }

    public override bool IsTokenSet => _parsed.Kind == "tokenSet";

    public override IReadOnlyList<TokenEntry> Tokens => _parsed.Tokens;

    public override IReadOnlyList<TokenProperty> Properties => _parsed.Properties;

    public override IReadOnlyList<TokenSet> Sets => _parsed.Sets;

    public override string? VariantEnum => _parsed.VariantEnum;

    public override string? VariantEnumSummary => _parsed.VariantEnumSummary;

    public override bool Overridable => _parsed.Overridable;

    public override string? ResolvedName => _parsed.ResolvedName;
}

/// <summary>Component-owned defaults, variants, resolution and sparse override metadata.</summary>
internal sealed class ComponentDefaultsTable : TokenTable
{
    private readonly ParsedTokenTable _parsed;

    public ComponentDefaultsTable(ParsedTokenTable parsed,
        TokenTableCategory category = TokenTableCategory.ComponentDefaults)
        : base(parsed, category, TokenTableLayer.Component, TokenTableArtifact.Defaults)
    {
        _parsed = parsed;
    }

    public override bool IsTokenSet => false;

    public override IReadOnlyList<TokenProperty> Properties => _parsed.Properties;

    public override IReadOnlyList<TokenSet> Sets => _parsed.Sets;

    public override string? VariantEnum => _parsed.VariantEnum;

    public override string? VariantEnumSummary => _parsed.VariantEnumSummary;

    public override bool Overridable => _parsed.Overridable;

    public override string? ResolvedName => _parsed.ResolvedName;
}
