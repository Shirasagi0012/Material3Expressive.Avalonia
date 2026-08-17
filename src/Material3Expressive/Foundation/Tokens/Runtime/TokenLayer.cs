using System.ComponentModel;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Identifies a token's position in the Material Design token hierarchy.</summary>
[Obsolete(
    "Legacy token layer type. Use MaterialDesignTokens.Avalonia.TokenLayer.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public enum TokenLayer
{
    /// <summary>Primitive design decisions, such as palette tones and typeface families.</summary>
    Reference = 0,

    /// <summary>Semantic application-wide roles derived from reference tokens.</summary>
    System = 1,

    /// <summary>Semantic slots consumed by a particular component.</summary>
    Component = 2,
}
