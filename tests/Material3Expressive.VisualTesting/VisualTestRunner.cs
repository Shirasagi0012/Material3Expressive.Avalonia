using Material3Expressive.VisualTesting.Comparison;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Rendering;
using Material3Expressive.VisualTesting.Reporting;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting;

/// <summary>Shared record/verify entry point. Verification never writes approved files.</summary>
public sealed class VisualTestRunner
{
    private readonly VisualRenderer _renderer;
    private readonly PixelComparer _comparer;
    private readonly BaselineStore _baselines;
    private readonly DiffArtifactWriter _artifacts;
    private readonly RepositoryPaths _paths;

    public VisualTestRunner()
    {
        _paths = new RepositoryPaths();
        _renderer = new VisualRenderer();
        _comparer = new PixelComparer();
        _baselines = new BaselineStore(_paths);
        _artifacts = new DiffArtifactWriter(_paths, _comparer);
    }

    public RepositoryPaths Paths => _paths;

    public VisualEnvironmentFingerprint Environment { get; } =
        VisualEnvironmentFingerprint.Capture();

    public void Record(VisualCase visualCase)
    {
        if (File.Exists(_baselines.EnvironmentPath))
            _baselines.EnsureEnvironmentMatches(Environment);
        else
            _baselines.WriteEnvironment(Environment);

        _baselines.Write(visualCase.Id, _renderer.Render(visualCase));
    }

    public void Approve(VisualCase visualCase)
    {
        _baselines.Approve(visualCase.Id);
    }

    public VisualVerificationResult Verify(VisualCase visualCase)
    {
        var approval = _baselines.GetApprovalState(visualCase.Id);
        try
        {
            _baselines.EnsureEnvironmentMatches(Environment);
        }
        catch (VisualBaselineException exception)
        {
            return new VisualVerificationResult(
                visualCase.Id,
                approval == BaselineApprovalState.Approved
                    ? VisualVerificationStatus.Failed
                    : VisualVerificationStatus.AwaitingApproval,
                exception.Message,
                null,
                null);
        }

        var actual = _renderer.Render(visualCase);
        if (!_baselines.HasBaseline(visualCase.Id))
        {
            var summary = $"No approved baseline exists for '{visualCase.Id}'.";
            var artifacts = _artifacts.Write(visualCase, actual, null, null, summary);
            return new VisualVerificationResult(
                visualCase.Id,
                VisualVerificationStatus.AwaitingApproval,
                summary,
                artifacts,
                null);
        }

        var expected = _baselines.Read(visualCase.Id);
        var comparison = _comparer.Compare(expected, actual, visualCase.Comparison);
        if (approval != BaselineApprovalState.Approved)
        {
            var approvalSummary = approval == BaselineApprovalState.ApprovedImageChanged
                ? $"The candidate for '{visualCase.Id}' changed after approval and needs renewed user confirmation."
                : $"The candidate for '{visualCase.Id}' has not been approved by the user.";
            var summary = comparison.Passed
                ? approvalSummary
                : $"{approvalSummary} Current render difference: {comparison.Summary}";
            var artifacts = comparison.Passed
                ? null
                : _artifacts.Write(visualCase, actual, expected, comparison, summary);
            return new VisualVerificationResult(
                visualCase.Id,
                VisualVerificationStatus.AwaitingApproval,
                summary,
                artifacts,
                comparison);
        }

        if (comparison.Passed)
            return new VisualVerificationResult(
                visualCase.Id,
                VisualVerificationStatus.Passed,
                comparison.Summary,
                null,
                comparison);

        var directory = _artifacts.Write(
            visualCase, actual, expected, comparison, comparison.Summary);
        return new VisualVerificationResult(
            visualCase.Id,
            VisualVerificationStatus.Failed,
            comparison.Summary,
            directory,
            comparison);
    }

    public IReadOnlyList<string> FindOrphanedBaselines(IEnumerable<VisualCase> cases) =>
        _baselines.FindOrphanedBaselines(cases.Select(item => item.Id));

    public IReadOnlyDictionary<string, BaselineApprovalState> GetApprovalStates(
        IEnumerable<VisualCase> cases) =>
        _baselines.GetApprovalStates(cases.Select(item => item.Id));
}
