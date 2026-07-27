using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>The single registry consumed by both xUnit and the baseline command.</summary>
public static class VisualCaseCatalog
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        .. ParityCases.All,
        .. ComponentParityCases.All,
        .. ComposeWaveOneParityCases.All
    ];

    public static VisualCase Get(string id) =>
        All.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));
}
