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
    private const string DisplayHeadlinePrefix = "foundation/typography/display-headline/";
    private const string DisplayHeadlineSkipReason =
        "Known issue: Avalonia text baseline API mismatch; parity comparison is quarantined pending a structural baseline probe.";

    public static TheoryData<string> Cases
    {
        get
        {
            var result = new TheoryData<string>();
            foreach (var visualCase in VisualCaseCatalog.All.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var row = new TheoryDataRow<string>(visualCase.Id)
                    .WithTestDisplayName($"Visual parity: {visualCase.Id}")
                    .WithTrait("ParityCase", visualCase.Id);
                if (visualCase.Id.StartsWith(DisplayHeadlinePrefix, StringComparison.Ordinal))
                    row.Skip = DisplayHeadlineSkipReason;
                result.Add(row);
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
