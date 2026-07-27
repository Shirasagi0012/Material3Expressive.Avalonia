using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>The single registry consumed by both xUnit and the baseline command.</summary>
public static class VisualCaseCatalog
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        .. FoundationCases.All,
        .. ButtonCases.All,
        .. FirstWaveCases.All,
        .. ParityCases.All,
        .. ComponentParityCases.All,
        .. SelectionAndProgressCases.All,
        .. InputAndChipCases.All,
        .. NavigationCases.All,
        .. OverlayAndPickerCases.All,
        .. CrossCuttingCases.All,
        .. InteractionCases.All,
        .. MotionCases.All
    ];

    public static VisualCase Get(string id) =>
        All.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<VisualCase> Filter(string? pattern, string? set)
    {
        IEnumerable<VisualCase> cases = All;
        if (!string.IsNullOrWhiteSpace(set))
            cases = cases.Where(item => item.HasTag(set));
        if (!string.IsNullOrWhiteSpace(pattern))
            cases = cases.Where(item => Matches(item.Id, pattern));

        return cases.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    private static bool Matches(string id, string pattern)
    {
        if (pattern.EndsWith('*'))
            return id.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        return id.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
