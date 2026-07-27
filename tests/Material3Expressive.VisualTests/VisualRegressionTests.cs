using Avalonia.Headless.XUnit;
using Material3Expressive.VisualTesting;
using Material3Expressive.VisualTesting.Cases;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class VisualRegressionTests
{
    public static IEnumerable<object[]> Cases =>
        VisualCaseCatalog.All.Select(item => new object[] { item.Id });

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void ApprovedVisualsHaveNotChanged(string caseId)
    {
        var visualCase = VisualCaseCatalog.Get(caseId);
        var result = new VisualTestRunner().Verify(visualCase);

        if (result.RequiresApproval)
            Assert.Skip(result.Summary);

        Assert.True(
            result.Passed,
            result.ArtifactDirectory is null
                ? result.Summary
                : $"{result.Summary}{Environment.NewLine}Artifacts: {result.ArtifactDirectory}");
    }

    [Fact]
    public void EveryApprovedBaselineStillHasACase()
    {
        var runner = new VisualTestRunner();
        var orphans = runner.FindOrphanedBaselines(VisualCaseCatalog.All);
        Assert.Empty(orphans);
    }
}
