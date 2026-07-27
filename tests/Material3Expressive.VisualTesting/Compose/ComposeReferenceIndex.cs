using System.Text.Json;

namespace Material3Expressive.VisualTesting.Compose;

/// <summary>Hash index proving which exact Compose PNG bytes were imported and from which capture.</summary>
public sealed class ComposeReferenceIndex
{
    public int SchemaVersion { get; init; } = ComposeReferenceManifest.CurrentSchemaVersion;

    public string ReferenceSetId { get; init; } = "";

    public string CaptureId { get; init; } = "";

    public string CaptureManifestSha256 { get; init; } = "";

    public DateTimeOffset ImportedAtUtc { get; init; }

    public SortedDictionary<string, ComposeImportedReference> Cases { get; init; } =
        new(StringComparer.Ordinal);

    public static ComposeReferenceIndex Read(string path)
    {
        if (!File.Exists(path))
            return new ComposeReferenceIndex();

        return JsonSerializer.Deserialize<ComposeReferenceIndex>(
                   File.ReadAllText(path),
                   ComposeReferenceManifest.JsonOptions)
               ?? throw new ComposeReferenceException($"Compose reference index '{path}' is empty.");
    }

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(this, ComposeReferenceManifest.JsonOptions)
            + System.Environment.NewLine);
    }
}

public sealed record ComposeImportedReference(
    string Image,
    string Sha256,
    int Width,
    int Height,
    string CaptureId,
    string CaptureManifestSha256,
    string ColorContractSha256,
    DateTimeOffset ImportedAtUtc);

public enum ComposeReferenceState
{
    Missing,
    MissingIndex,
    Ready,
    HashMismatch
}
