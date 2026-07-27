using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Storage;

/// <summary>Maps stable case IDs to committed PNG files and one environment fingerprint.</summary>
public sealed class BaselineStore(RepositoryPaths paths)
{
    public string EnvironmentPath => Path.Combine(paths.BaselineRoot, "environment.json");

    public string ApprovalPath => Path.Combine(paths.BaselineRoot, "approval.json");

    public string GetBaselinePath(string caseId) =>
        Path.Combine(paths.BaselineRoot, caseId.Replace('/', Path.DirectorySeparatorChar) + ".png");

    public bool HasBaseline(string caseId) => File.Exists(GetBaselinePath(caseId));

    public RenderedImage Read(string caseId) =>
        RenderedImage.DecodePng(File.ReadAllBytes(GetBaselinePath(caseId)));

    public void Write(string caseId, RenderedImage image)
    {
        var path = GetBaselinePath(caseId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, image.EncodePng());

        var approvals = BaselineApprovalManifest.Read(ApprovalPath);
        approvals.Invalidate(caseId);
        approvals.Write(ApprovalPath);
    }

    public void EnsureEnvironmentMatches(VisualEnvironmentFingerprint current)
    {
        if (!File.Exists(EnvironmentPath))
        {
            throw new VisualBaselineException(
                $"The baseline environment is missing: {EnvironmentPath}");
        }

        var expected = VisualEnvironmentFingerprint.FromJson(File.ReadAllText(EnvironmentPath));
        if (!string.Equals(expected.ToJson(), current.ToJson(), StringComparison.Ordinal))
        {
            throw new VisualBaselineException(
                "The current renderer environment does not match the recorded baseline environment. " +
                $"Compare '{EnvironmentPath}' before recording or verifying images.");
        }
    }

    public void WriteEnvironment(VisualEnvironmentFingerprint current)
    {
        Directory.CreateDirectory(paths.BaselineRoot);
        File.WriteAllText(EnvironmentPath, current.ToJson());
    }

    public BaselineApprovalState GetApprovalState(string caseId)
    {
        var path = GetBaselinePath(caseId);
        if (!File.Exists(path))
            return BaselineApprovalState.AwaitingApproval;

        return BaselineApprovalManifest.Read(ApprovalPath).GetState(caseId, path);
    }

    public void Approve(string caseId)
    {
        var path = GetBaselinePath(caseId);
        if (!File.Exists(path))
            throw new VisualBaselineException($"Cannot approve missing baseline '{caseId}'.");

        EnsureEnvironmentMatches(VisualEnvironmentFingerprint.Capture());
        var approvals = BaselineApprovalManifest.Read(ApprovalPath);
        approvals.Approve(caseId, path);
        approvals.Write(ApprovalPath);
    }

    public IReadOnlyDictionary<string, BaselineApprovalState> GetApprovalStates(
        IEnumerable<string> caseIds)
    {
        return caseIds.ToDictionary(
            id => id,
            GetApprovalState,
            StringComparer.Ordinal);
    }

    public IReadOnlyList<string> FindOrphanedBaselines(IEnumerable<string> caseIds)
    {
        if (!Directory.Exists(paths.BaselineRoot))
            return [];

        var known = caseIds.ToHashSet(StringComparer.Ordinal);
        return Directory.EnumerateFiles(paths.BaselineRoot, "*.png", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(paths.BaselineRoot, path))
            .Select(path => path[..^Path.GetExtension(path).Length].Replace('\\', '/'))
            .Where(id => !known.Contains(id))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}

public sealed class VisualBaselineException(string message) : Exception(message);
