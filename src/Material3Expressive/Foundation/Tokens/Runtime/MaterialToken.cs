using System.ComponentModel;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A strongly typed resource key in the Material token graph.</summary>
[Obsolete(
    "Legacy descriptor-owned-default token API. Use MaterialDesignTokens.Avalonia.MaterialToken; " +
    "defaults are supplied by the composed catalog.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class MaterialToken
{
    private protected MaterialToken(string path, TokenLayer layer, Type valueType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
        Layer = layer;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }

    /// <summary>Gets the stable, human-readable token path.</summary>
    public string Path { get; }

    /// <summary>Gets the token hierarchy layer.</summary>
    public TokenLayer Layer { get; }

    /// <summary>Gets the type produced when the token is resolved.</summary>
    public Type ValueType { get; }

    internal abstract ITokenDefinition UntypedDefaultDefinition { get; }

    /// <inheritdoc />
    public override string ToString() => Path;
}

/// <summary>A strongly typed resource key and its generated fallback definition.</summary>
/// <typeparam name="T">The resolved token value type.</typeparam>
[Obsolete(
    "Legacy descriptor-owned-default token API. Use MaterialDesignTokens.Avalonia.MaterialToken<T>; " +
    "defaults are supplied by the composed catalog.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MaterialToken<T> : MaterialToken
{
    /// <summary>Creates a token descriptor.</summary>
    public MaterialToken(string path, TokenLayer layer, TokenDefinition<T> defaultDefinition)
        : base(path, layer, typeof(T))
    {
        DefaultDefinition = defaultDefinition ?? throw new ArgumentNullException(nameof(defaultDefinition));
    }

    /// <summary>Gets the definition used only when no resource in scope defines this token.</summary>
    [Obsolete(
        "Descriptor-owned defaults are being removed. Use the composed DefaultRawTokens catalog.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TokenDefinition<T> DefaultDefinition { get; }

    internal override ITokenDefinition UntypedDefaultDefinition => DefaultDefinition;
}
