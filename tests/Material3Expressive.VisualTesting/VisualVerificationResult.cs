using Material3Expressive.VisualTesting.Comparison;

namespace Material3Expressive.VisualTesting;

public enum VisualVerificationStatus
{
    Passed,
    Failed,
    AwaitingApproval
}

public sealed record VisualVerificationResult(
    string CaseId,
    VisualVerificationStatus Status,
    string Summary,
    string? ArtifactDirectory,
    PixelComparisonResult? Comparison)
{
    public bool Passed => Status == VisualVerificationStatus.Passed;

    public bool IsEnforcedFailure => Status == VisualVerificationStatus.Failed;

    public bool RequiresApproval => Status == VisualVerificationStatus.AwaitingApproval;
}
