using System.Security.Cryptography;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Compose;

public sealed class ComposeReferenceStore
{
    public ComposeReferenceStore(RepositoryPaths paths)
    {
        Paths = paths;
    }

    public RepositoryPaths Paths { get; }

    public string ManifestPath => Path.Combine(Paths.ComposeReferenceRoot, "manifest.json");

    public string IndexPath => Path.Combine(Paths.ComposeReferenceRoot, "reference-index.json");

    public ComposeReferenceManifest ReadManifest()
    {
        var manifest = ComposeReferenceManifest.Read(ManifestPath);
        var relative = ComposeReferenceManifest.NormalizeRelativePath(
            manifest.ColorContract.Manifest);
        var colorContractPath = ResolveContainedPath(Paths.Root, relative);
        if (!File.Exists(colorContractPath))
        {
            throw new ComposeReferenceException(
                $"Compose color contract is missing: {colorContractPath}");
        }

        var actualHash = HashFile(colorContractPath);
        if (!string.Equals(
                actualHash,
                manifest.ColorContract.Sha256,
                StringComparison.Ordinal))
        {
            throw new ComposeReferenceException(
                $"Compose color-contract hash mismatch. Expected " +
                $"{manifest.ColorContract.Sha256}, got {actualHash}.");
        }

        return manifest;
    }

    public string GetImagePath(ComposeReferenceCase referenceCase)
    {
        var relative = ComposeReferenceManifest.NormalizeRelativePath(referenceCase.Image);
        return ResolveContainedPath(Paths.ComposeReferenceRoot, relative);
    }

    public RenderedImage ReadImage(ComposeReferenceCase referenceCase) =>
        RenderedImage.DecodePng(File.ReadAllBytes(GetImagePath(referenceCase)));

    public ComposeReferenceState GetState(ComposeReferenceCase referenceCase)
    {
        var imagePath = GetImagePath(referenceCase);
        if (!File.Exists(imagePath))
            return ComposeReferenceState.Missing;

        var index = ComposeReferenceIndex.Read(IndexPath);
        if (!index.Cases.TryGetValue(referenceCase.Id, out var imported))
            return ComposeReferenceState.MissingIndex;
        var manifest = ReadManifest();
        if (!string.Equals(index.ReferenceSetId, manifest.ReferenceSetId, StringComparison.Ordinal)
            || !string.Equals(
                imported.ColorContractSha256,
                manifest.ColorContract.Sha256,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(imported.CaptureId)
            || string.IsNullOrWhiteSpace(imported.CaptureManifestSha256))
        {
            return ComposeReferenceState.MissingIndex;
        }

        return string.Equals(HashFile(imagePath), imported.Sha256, StringComparison.Ordinal)
            ? ComposeReferenceState.Ready
            : ComposeReferenceState.HashMismatch;
    }

    public IReadOnlyDictionary<string, ComposeReferenceState> GetStates(
        IEnumerable<ComposeReferenceCase> cases) =>
        cases.ToDictionary(item => item.Id, GetState, StringComparer.Ordinal);

    internal static string ResolveContainedPath(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new ComposeReferenceException($"Path '{relative}' escapes '{root}'.");
        return fullPath;
    }

    internal static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
