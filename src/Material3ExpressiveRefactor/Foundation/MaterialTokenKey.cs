using System.Runtime.CompilerServices;

namespace Material3ExpressiveRefactor.Foundation;

/// <summary>
/// Token identity. The object reference IS the identity — equality and hashing keep the
/// default reference semantics (design doc §3.1). Names exist only for diagnostics and
/// serialization and never participate in identity.
/// </summary>
public abstract class MaterialTokenKey
{
    // Sealed hierarchy: keys are created only through MaterialTokenKey<TValue>.
    private protected MaterialTokenKey() { }

    /// <summary>Declared value type, used by container validation and consumption diagnostics.</summary>
    internal abstract Type ValueType { get; }

    /// <summary>Diagnostic/serialization name (e.g. the upstream "md.sys.color.primary" path).</summary>
    internal abstract string DebugName { get; }

    public sealed override string ToString() => DebugName;
}

/// <summary>
/// The generic leaf carrying the value type. Every <c>new</c> produces a new identity;
/// hold keys in static facades or family objects and never recreate them by name.
/// </summary>
public sealed class MaterialTokenKey<TValue> : MaterialTokenKey
{
    private readonly string _debugName;

    /// <param name="debugName">
    /// Diagnostic/serialization name. Source-generated keys always pass the canonical spec name
    /// explicitly; third-party static fields get the field name for free.
    /// </param>
    public MaterialTokenKey([CallerMemberName] string? debugName = null)
    {
        _debugName = debugName ?? string.Empty;
    }

    internal override Type ValueType => typeof(TValue);

    internal override string DebugName => _debugName;
}
