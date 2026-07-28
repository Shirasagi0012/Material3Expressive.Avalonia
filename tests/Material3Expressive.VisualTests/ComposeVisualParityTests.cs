using Avalonia.Headless.XUnit;
using Material3Expressive.VisualTesting.Parity;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

/// <summary>
/// Cross-process conformance test. The expected images/layout are freshly produced by CMP for this
/// run; a mismatch is a test failure and is never promotable to an Avalonia baseline.
/// </summary>
[Trait("Category", "VisualParity")]
public sealed class ComposeVisualParityTests
{
    [AvaloniaFact]
    public void AllRegisteredScenesMatchTheFreshComposeReference()
    {
        var root = RepositoryRoot.Find();
        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var artifactRoot = Path.Combine(root, "artifacts", "visual-parity", "xunit", runId);
        var expected = new ComposeExpectedRunner().Generate(artifactRoot);
        ParityRun.ValidateCaseCatalog(expected);

        var runner = new ParityRun();
        var results = expected.Manifest.Cases
            .Select(item => runner.CompareCase(expected, item.Id, artifactRoot))
            .ToArray();
        ParityRun.WriteActualManifest(artifactRoot, expected, results);
        var failed = results.Where(item => !item.Comparison.IsMatch).ToArray();

        Assert.True(
            failed.Length == 0,
            "Visual parity mismatch(es):" + Environment.NewLine + string.Join(
                Environment.NewLine,
                failed.Select(item => $"- {item.CaseId}: {item.ArtifactDirectory}{Environment.NewLine}{item.Comparison.Summary}")));
    }
}
