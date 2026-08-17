using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>A strongly typed, consumer-ready Material resource endpoint.</summary>
public abstract class MaterialValueKey
{
    private protected MaterialValueKey(string path, Type valueType, IMaterialValuePlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    /// <summary>Gets the stable diagnostic path of this endpoint.</summary>
    public string Path { get; }

    /// <summary>Gets the type produced when this endpoint is resolved.</summary>
    public Type ValueType { get; }

    internal IMaterialValuePlan Plan { get; }

    internal abstract object Resolve(MaterialResolutionSession session);

    /// <inheritdoc />
    public override string ToString() => Path;
}

/// <summary>A strongly typed, consumer-ready Material resource endpoint.</summary>
/// <typeparam name="T">The final value type.</typeparam>
public sealed class MaterialValueKey<T> : MaterialValueKey
{
    internal MaterialValueKey(string path, IMaterialValuePlan<T> plan)
        : base(path, typeof(T), plan)
    {
    }

    internal override object Resolve(MaterialResolutionSession session) => session.Resolve(this)!;
}

internal interface IMaterialValuePlan
{
    Type ValueType { get; }

    IReadOnlyList<MaterialToken> Dependencies { get; }

    object? Evaluate(MaterialValueContext context);
}

internal interface IMaterialValuePlan<out T> : IMaterialValuePlan
{
    new T Evaluate(MaterialValueContext context);
}

internal abstract class MaterialValueContext
{
    public abstract T Get<T>(MaterialToken<T> token);
}
