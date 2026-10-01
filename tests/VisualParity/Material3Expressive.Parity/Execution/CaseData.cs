namespace Material3Expressive.Parity;

public sealed record EmptyConfig;

public abstract record CaseData(string Id, string Title)
{
    internal abstract CaseMetadata Expand(string suite, string scenario);
}

public sealed record CaseData<T>(string Id, string Title, T Config) : CaseData(Id, Title) where T : notnull
{
    internal override CaseMetadata Expand(string suite, string scenario) =>
        CaseMetadata.Create($"{suite}.{Id}", Title, suite, scenario, Config);
}
