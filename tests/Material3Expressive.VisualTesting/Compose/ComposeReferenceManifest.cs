using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.VisualTesting.Compose;

/// <summary>
/// Declares the exact Compose build, emulator contract and stable case IDs accepted as references.
/// Image hashes live in the generated reference index so this file remains a reviewable test plan.
/// </summary>
public sealed record ComposeReferenceManifest(
    int SchemaVersion,
    string ReferenceSetId,
    string ComposeMaterial3Version,
    string SourceArtifact,
    string SourceArtifactSha256,
    ComposeColorContract ColorContract,
    ComposeReferenceEnvironment Environment,
    IReadOnlyList<ComposeReferenceCase> Cases)
{
    public const int CurrentSchemaVersion = 2;

    internal static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    public static ComposeReferenceManifest Read(string path)
    {
        if (!File.Exists(path))
            throw new ComposeReferenceException($"Compose reference manifest is missing: {path}");

        var manifest = JsonSerializer.Deserialize<ComposeReferenceManifest>(
                           File.ReadAllText(path),
                           JsonOptions)
                       ?? throw new ComposeReferenceException(
                           $"Compose reference manifest '{path}' is empty.");
        manifest.Validate();
        return manifest;
    }

    public ComposeReferenceCase GetCase(string id) =>
        Cases.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new ComposeReferenceException(
                $"Unsupported Compose reference schema {SchemaVersion}; " +
                $"expected {CurrentSchemaVersion}.");
        }

        RequireValue(ReferenceSetId, nameof(ReferenceSetId));
        RequireValue(ComposeMaterial3Version, nameof(ComposeMaterial3Version));
        RequireValue(SourceArtifact, nameof(SourceArtifact));
        RequireValue(SourceArtifactSha256, nameof(SourceArtifactSha256));
        if (SourceArtifactSha256.Length != 64
            || SourceArtifactSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ComposeReferenceException(
                "Compose source artifact SHA-256 must contain exactly 64 hexadecimal characters.");
        }
        ArgumentNullException.ThrowIfNull(ColorContract);
        ArgumentNullException.ThrowIfNull(Environment);
        ArgumentNullException.ThrowIfNull(Cases);
        ColorContract.Validate();
        Environment.Validate();

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var visualCase in Cases)
        {
            visualCase.Validate();
            if (!ids.Add(visualCase.Id))
                throw new ComposeReferenceException($"Duplicate Compose case ID '{visualCase.Id}'.");
            if (!images.Add(NormalizeRelativePath(visualCase.Image)))
            {
                throw new ComposeReferenceException(
                    $"Multiple Compose cases use image path '{visualCase.Image}'.");
            }
        }
    }

    internal static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || Path.IsPathRooted(path)
            || path.Split(['/', '\\']).Any(part => part is "" or "." or ".."))
        {
            throw new ComposeReferenceException(
                $"'{path}' is not a safe relative Compose reference path.");
        }

        return path.Replace('\\', '/');
    }

    private static void RequireValue(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ComposeReferenceException($"Compose reference field '{name}' is required.");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

public sealed record ComposeColorContract(
    string Id,
    string Seed,
    string Variant,
    string SpecVersion,
    string Platform,
    string Manifest,
    string Sha256,
    string MaterialColorUtilitiesCommit)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)
            || string.IsNullOrWhiteSpace(Seed)
            || string.IsNullOrWhiteSpace(Variant)
            || string.IsNullOrWhiteSpace(SpecVersion)
            || string.IsNullOrWhiteSpace(Platform)
            || string.IsNullOrWhiteSpace(MaterialColorUtilitiesCommit))
        {
            throw new ComposeReferenceException("Compose color-contract provenance is incomplete.");
        }

        ComposeReferenceManifest.NormalizeRelativePath(Manifest);
        if (Sha256.Length != 64 || Sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ComposeReferenceException(
                "Compose color-contract SHA-256 must contain exactly 64 hexadecimal characters.");
        }
    }
}

public sealed record ComposeReferenceEnvironment(
    int AndroidApi,
    int DensityDpi,
    double FontScale,
    string Locale,
    string LayoutDirection,
    string FontFamily,
    string FontFileName,
    string FontFileSha256,
    string FontWeightPolicy,
    string EmulatorProfile)
{
    internal void Validate()
    {
        if (AndroidApi <= 0 || DensityDpi <= 0 || FontScale <= 0)
            throw new ComposeReferenceException("Compose API, density and font scale must be positive.");
        if (string.IsNullOrWhiteSpace(Locale)
            || string.IsNullOrWhiteSpace(LayoutDirection)
            || string.IsNullOrWhiteSpace(FontFamily)
            || string.IsNullOrWhiteSpace(FontFileName)
            || string.IsNullOrWhiteSpace(FontWeightPolicy)
            || string.IsNullOrWhiteSpace(EmulatorProfile))
        {
            throw new ComposeReferenceException(
                "Compose locale, layout direction, font and emulator profile are required.");
        }
        if (FontFileSha256.Length != 64
            || FontFileSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ComposeReferenceException(
                "Compose font SHA-256 must contain exactly 64 hexadecimal characters.");
        }
    }
}

public sealed record ComposeReferenceCase(
    string Id,
    string Image,
    string Theme,
    int Width,
    int Height,
    string SourceTest,
    string SourceGolden,
    IReadOnlyList<ComposeParityRegion>? Regions = null)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)
            || Id.Contains("..", StringComparison.Ordinal)
            || Id.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ComposeReferenceException($"Invalid Compose case ID '{Id}'.");
        }

        ComposeReferenceManifest.NormalizeRelativePath(Image);
        if (Width <= 0 || Height <= 0)
            throw new ComposeReferenceException($"Compose case '{Id}' has an invalid viewport.");
        if (Theme is not ("light" or "dark"))
            throw new ComposeReferenceException($"Compose case '{Id}' has unknown theme '{Theme}'.");
        if (string.IsNullOrWhiteSpace(SourceTest) || string.IsNullOrWhiteSpace(SourceGolden))
            throw new ComposeReferenceException($"Compose case '{Id}' needs source-test provenance.");

        foreach (var region in Regions ?? [])
            region.Validate(Id, Width, Height);
    }
}

public enum ComposeParityRegionKind
{
    Container,
    Text,
    Icon,
    Shadow,
    Other
}

public sealed record ComposeParityRegion(
    string Id,
    ComposeParityRegionKind Kind,
    int X,
    int Y,
    int Width,
    int Height)
{
    internal void Validate(string caseId, int imageWidth, int imageHeight)
    {
        if (string.IsNullOrWhiteSpace(Id)
            || X < 0
            || Y < 0
            || Width <= 0
            || Height <= 0
            || X + Width > imageWidth
            || Y + Height > imageHeight)
        {
            throw new ComposeReferenceException(
                $"Compose parity region '{Id}' is outside case '{caseId}'.");
        }
    }
}

public sealed class ComposeReferenceException(string message) : Exception(message);
