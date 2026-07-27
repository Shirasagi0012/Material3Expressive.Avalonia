using System.Text.Json;

namespace Material3Expressive.VisualTesting.Compose;

/// <summary>Machine-written provenance supplied with a directory of Compose captures.</summary>
public sealed record ComposeCaptureManifest(
    int SchemaVersion,
    string ReferenceSetId,
    string ComposeMaterial3Version,
    string SourceArtifact,
    string SourceArtifactSha256,
    string ReferenceManifestSha256,
    ComposeColorContract ColorContract,
    ComposeReferenceEnvironment Environment,
    string CaptureId,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<ComposeCapturedCase> Cases)
{
    public const string FileName = "capture.json";

    public static ComposeCaptureManifest Read(string captureRoot)
    {
        var path = Path.Combine(captureRoot, FileName);
        if (!File.Exists(path))
            throw new ComposeReferenceException($"Compose capture metadata is missing: {path}");

        var capture = JsonSerializer.Deserialize<ComposeCaptureManifest>(
                          File.ReadAllText(path),
                          ComposeReferenceManifest.JsonOptions)
                      ?? throw new ComposeReferenceException(
                          $"Compose capture metadata '{path}' is empty.");
        capture.Validate();
        return capture;
    }

    private void Validate()
    {
        if (SchemaVersion != ComposeReferenceManifest.CurrentSchemaVersion)
            throw new ComposeReferenceException($"Unsupported Compose capture schema {SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(ReferenceSetId)
            || string.IsNullOrWhiteSpace(ComposeMaterial3Version)
            || string.IsNullOrWhiteSpace(SourceArtifact)
            || string.IsNullOrWhiteSpace(SourceArtifactSha256)
            || string.IsNullOrWhiteSpace(ReferenceManifestSha256)
            || string.IsNullOrWhiteSpace(CaptureId))
        {
            throw new ComposeReferenceException("Compose capture provenance is incomplete.");
        }
        if (ReferenceManifestSha256.Length != 64
            || ReferenceManifestSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ComposeReferenceException(
                "Compose reference-manifest SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        ColorContract.Validate();
        Environment.Validate();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var capturedCase in Cases)
        {
            if (string.IsNullOrWhiteSpace(capturedCase.Id)
                || string.IsNullOrWhiteSpace(capturedCase.Image)
                || string.IsNullOrWhiteSpace(capturedCase.Sha256)
                || !ids.Add(capturedCase.Id))
            {
                throw new ComposeReferenceException(
                    $"Compose capture contains an invalid or duplicate case '{capturedCase.Id}'.");
            }

            ComposeReferenceManifest.NormalizeRelativePath(capturedCase.Image);
        }
    }
}

public sealed record ComposeCapturedCase(
    string Id,
    string Image,
    int Width,
    int Height,
    string Sha256);
