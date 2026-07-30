using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>The single registry consumed by both xUnit and the baseline command.</summary>
public static class VisualCaseCatalog
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        .. ParityCases.All,
        .. ElevationParityCases.All,
        .. MotionParityCases.All,
        .. ComponentParityCases.All,
        .. ComponentMotionParityCases.All,
        .. ComposeWaveOneParityCases.All,
        .. DividerParityCases.All,
        .. SegmentedButtonParityCases.All,
        .. BottomSheetParityCases.All,
        .. SnackbarParityCases.All,
        .. AppBarParityCases.All
    ];

    public static VisualCase Get(string id) =>
        All.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));
}
