using Material3Expressive.VisualTesting.Parity;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

public static class VisualParityCollection
{
    public const string Name = "Visual parity";
}

[CollectionDefinition(VisualParityCollection.Name, DisableParallelization = true)]
public sealed class VisualParityCollectionDefinition : ICollectionFixture<ComposeParityFixture>
{
}

/// <summary>
/// Generates and validates one immutable Compose expected bundle for every parity test in the
/// collection. Avalonia captures remain isolated in their individual theory rows.
/// </summary>
public sealed class ComposeParityFixture
{
    private readonly object _resultsLock = new();
    private readonly Dictionary<string, ParityRunResult> _results = new(StringComparer.Ordinal);

    public ComposeParityFixture()
    {
        var root = RepositoryRoot.Find();
        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        ArtifactRoot = Path.Combine(root, "artifacts", "visual-parity", "xunit", runId);
        Expected = new ComposeExpectedRunner().Generate(ArtifactRoot);
        ParityRun.ValidateCaseCatalog(Expected);
    }

    public string ArtifactRoot { get; }

    public ComposeExpectedBundle Expected { get; }

    public void RecordResult(ParityRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        lock (_resultsLock)
        {
            _results[result.CaseId] = result;
            var orderedResults = Expected.Manifest.Cases
                .Select(item => _results.GetValueOrDefault(item.Id))
                .Where(item => item is not null)
                .Cast<ParityRunResult>()
                .ToArray();
            ParityRun.WriteActualManifest(ArtifactRoot, Expected, orderedResults);
        }
    }
}
