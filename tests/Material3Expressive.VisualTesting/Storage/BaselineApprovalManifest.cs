using System.Security.Cryptography;
using System.Text.Json;

namespace Material3Expressive.VisualTesting.Storage;

public enum BaselineApprovalState
{
    AwaitingApproval,
    Approved,
    ApprovedImageChanged
}

/// <summary>
/// User confirmation is bound to the exact PNG bytes, so recording a new candidate can never
/// silently inherit approval from the image it replaced.
/// </summary>
public sealed class BaselineApprovalManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public int Version { get; init; } = 1;

    public SortedDictionary<string, BaselineApproval> Cases { get; init; } =
        new(StringComparer.Ordinal);

    public static BaselineApprovalManifest Read(string path)
    {
        if (!File.Exists(path))
            return new BaselineApprovalManifest();

        return JsonSerializer.Deserialize<BaselineApprovalManifest>(
                   File.ReadAllText(path),
                   JsonOptions)
               ?? throw new InvalidDataException($"Approval manifest '{path}' is empty.");
    }

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions) + Environment.NewLine);
    }

    public BaselineApprovalState GetState(string caseId, string baselinePath)
    {
        if (!Cases.TryGetValue(caseId, out var approval))
            return BaselineApprovalState.AwaitingApproval;

        return string.Equals(
            approval.Sha256,
            HashFile(baselinePath),
            StringComparison.Ordinal)
            ? BaselineApprovalState.Approved
            : BaselineApprovalState.ApprovedImageChanged;
    }

    public void Approve(string caseId, string baselinePath)
    {
        Cases[caseId] = new BaselineApproval(
            HashFile(baselinePath),
            DateTimeOffset.UtcNow,
            Environment.UserName);
    }

    public void Invalidate(string caseId)
    {
        Cases.Remove(caseId);
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

public sealed record BaselineApproval(
    string Sha256,
    DateTimeOffset ApprovedAtUtc,
    string ApprovedBy);
