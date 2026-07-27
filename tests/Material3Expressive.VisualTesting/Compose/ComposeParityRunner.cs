using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Rendering;
using Material3Expressive.VisualTesting.Reporting;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Compose;

public sealed class ComposeParityRunner
{
    private readonly VisualRenderer _renderer = new();
    private readonly ComposeParityComparer _comparer = new();
    private readonly ComposeReferenceStore _references;
    private readonly ComposeParityReportWriter _reports;

    public ComposeParityRunner()
        : this(new RepositoryPaths())
    {
    }

    public ComposeParityRunner(RepositoryPaths paths)
    {
        Paths = paths;
        _references = new ComposeReferenceStore(paths);
        _reports = new ComposeParityReportWriter(paths, _comparer);
    }

    public RepositoryPaths Paths { get; }

    public ComposeReferenceManifest Manifest => _references.ReadManifest();

    public ComposeParityResult Compare(ComposeReferenceCase referenceCase)
    {
        var avaloniaCase = VisualCaseCatalog.All.FirstOrDefault(
            item => string.Equals(item.Id, referenceCase.Id, StringComparison.Ordinal));
        if (avaloniaCase is null)
        {
            return _reports.Write(
                referenceCase,
                null,
                null,
                null,
                ComposeParityStatus.AvaloniaCaseMissing,
                $"No Avalonia visual case has ID '{referenceCase.Id}'.");
        }

        if (avaloniaCase.Viewport.Width != referenceCase.Width
            || avaloniaCase.Viewport.Height != referenceCase.Height)
        {
            return _reports.Write(
                referenceCase,
                null,
                null,
                null,
                ComposeParityStatus.AvaloniaCaseMissing,
                $"Avalonia viewport is {avaloniaCase.Viewport.Width}x{avaloniaCase.Viewport.Height}; " +
                $"Compose contract requires {referenceCase.Width}x{referenceCase.Height}.");
        }

        var actual = _renderer.Render(avaloniaCase);
        var state = _references.GetState(referenceCase);
        if (state == ComposeReferenceState.Missing)
        {
            return _reports.Write(
                referenceCase,
                actual,
                null,
                null,
                ComposeParityStatus.ReferenceMissing,
                "Compose reference has not been captured and imported yet.");
        }

        if (state != ComposeReferenceState.Ready)
        {
            return _reports.Write(
                referenceCase,
                actual,
                null,
                null,
                ComposeParityStatus.ReferenceInvalid,
                $"Compose reference is not trusted: {state}. Re-import a verified capture.");
        }

        var reference = _references.ReadImage(referenceCase);
        var metrics = _comparer.Compare(reference, actual, referenceCase.Regions);
        return _reports.Write(
            referenceCase,
            actual,
            reference,
            metrics,
            ComposeParityStatus.Compared,
            metrics.Summary);
    }

    public string WriteIndex(
        IReadOnlyList<ComposeParityResult> results,
        ComposeReferenceManifest manifest) =>
        _reports.WriteIndex(results, manifest);
}
