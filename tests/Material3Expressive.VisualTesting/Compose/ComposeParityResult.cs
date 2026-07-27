namespace Material3Expressive.VisualTesting.Compose;

public enum ComposeParityStatus
{
    Compared,
    ReferenceMissing,
    ReferenceInvalid,
    AvaloniaCaseMissing
}

public sealed record ComposeParityResult(
    string CaseId,
    ComposeParityStatus Status,
    string Summary,
    string ArtifactDirectory,
    ComposeParityMetrics? Metrics)
{
    public bool HasComparison => Status == ComposeParityStatus.Compared;

    public bool HasInvalidInput =>
        Status is ComposeParityStatus.ReferenceInvalid or ComposeParityStatus.AvaloniaCaseMissing;
}
