using Avalonia.Headless.XUnit;
using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Parity;
using Xunit;

namespace Material3Expressive.VisualTests;

/// <summary>
/// Cross-process conformance test. The expected images/layout are freshly produced by CMP for this
/// run; a mismatch is a test failure and is never promotable to an Avalonia baseline.
/// </summary>
[Collection(VisualParityCollection.Name)]
[Trait("Category", "VisualParity")]
public sealed class ComposeVisualParityTests(ComposeParityFixture fixture)
{
    public static TheoryData<string> Cases
    {
        get
        {
            var result = new TheoryData<string>();
            foreach (var visualCase in VisualCaseCatalog.All.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                result.Add(new TheoryDataRow<string>(visualCase.Id)
                    .WithTestDisplayName($"Visual parity: {visualCase.Id}")
                    .WithTrait("ParityCase", visualCase.Id));
            }
            return result;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void RegisteredSceneMatchesTheFreshComposeReference(string caseId)
    {
        var result = new ParityRun().CompareCase(fixture.Expected, caseId, fixture.ArtifactRoot);
        fixture.RecordResult(result);

        Assert.True(
            result.Comparison.IsMatch,
            $"Visual parity mismatch for '{caseId}'." + Environment.NewLine +
            $"Artifacts: {result.ArtifactDirectory}" + Environment.NewLine +
            result.Comparison.Summary);
    }
}
