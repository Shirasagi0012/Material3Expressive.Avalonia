namespace Material3Expressive.Parity;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ParitySuiteAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ParityScenarioAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Data { get; init; } = "";
}
