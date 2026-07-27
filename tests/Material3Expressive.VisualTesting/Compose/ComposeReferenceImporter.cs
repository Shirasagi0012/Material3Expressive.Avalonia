using System.Security.Cryptography;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Compose;

/// <summary>
/// Imports only captures whose version, commit, emulator contract, dimensions and hashes all match
/// the checked-in plan. Validation completes before any destination image is changed.
/// </summary>
public sealed class ComposeReferenceImporter(ComposeReferenceStore store)
{
    public IReadOnlyList<string> Import(
        string captureRoot,
        IEnumerable<ComposeReferenceCase> selectedCases)
    {
        var root = Path.GetFullPath(captureRoot);
        var manifest = store.ReadManifest();
        var capture = ComposeCaptureManifest.Read(root);
        EnsureCaptureMatches(manifest, capture);

        var capturesById = capture.Cases.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var validated = new List<ValidatedCapture>();
        foreach (var referenceCase in selectedCases)
        {
            if (!capturesById.TryGetValue(referenceCase.Id, out var captured))
            {
                throw new ComposeReferenceException(
                    $"Capture '{capture.CaptureId}' does not contain '{referenceCase.Id}'.");
            }

            var sourcePath = ComposeReferenceStore.ResolveContainedPath(
                root,
                ComposeReferenceManifest.NormalizeRelativePath(captured.Image));
            if (!File.Exists(sourcePath))
                throw new ComposeReferenceException($"Captured PNG is missing: {sourcePath}");

            var hash = ComposeReferenceStore.HashFile(sourcePath);
            if (!string.Equals(hash, captured.Sha256, StringComparison.Ordinal))
            {
                throw new ComposeReferenceException(
                    $"Captured PNG hash changed for '{referenceCase.Id}'.");
            }

            var png = File.ReadAllBytes(sourcePath);
            var image = RenderedImage.DecodePng(png);
            if (image.Width != referenceCase.Width
                || image.Height != referenceCase.Height
                || captured.Width != image.Width
                || captured.Height != image.Height)
            {
                throw new ComposeReferenceException(
                    $"'{referenceCase.Id}' expected {referenceCase.Width}x{referenceCase.Height}, " +
                    $"capture metadata/image were {captured.Width}x{captured.Height}/" +
                    $"{image.Width}x{image.Height}.");
            }

            validated.Add(new ValidatedCapture(referenceCase, hash, png, image));
        }

        var oldIndex = ComposeReferenceIndex.Read(store.IndexPath);
        var importedCases = string.Equals(
            oldIndex.ReferenceSetId,
            manifest.ReferenceSetId,
            StringComparison.Ordinal)
            ? oldIndex.Cases
            : new SortedDictionary<string, ComposeImportedReference>(StringComparer.Ordinal);

        var captureManifestPath = Path.Combine(root, ComposeCaptureManifest.FileName);
        var captureManifestSha256 = Convert.ToHexString(
            SHA256.HashData(File.ReadAllBytes(captureManifestPath)));
        var importedAtUtc = DateTimeOffset.UtcNow;
        foreach (var item in validated)
        {
            var destination = store.GetImagePath(item.ReferenceCase);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, item.Png);
            importedCases[item.ReferenceCase.Id] = new ComposeImportedReference(
                item.ReferenceCase.Image,
                item.Sha256,
                item.Image.Width,
                item.Image.Height,
                capture.CaptureId,
                captureManifestSha256,
                capture.ColorContract.Sha256,
                importedAtUtc);
        }

        var index = new ComposeReferenceIndex
        {
            ReferenceSetId = manifest.ReferenceSetId,
            CaptureId = capture.CaptureId,
            CaptureManifestSha256 = captureManifestSha256,
            ImportedAtUtc = importedAtUtc,
            Cases = importedCases
        };
        index.Write(store.IndexPath);
        return validated.Select(item => item.ReferenceCase.Id).ToArray();
    }

    private static void EnsureCaptureMatches(
        ComposeReferenceManifest expected,
        ComposeCaptureManifest actual)
    {
        var mismatches = new List<string>();
        Match(nameof(expected.ReferenceSetId), expected.ReferenceSetId, actual.ReferenceSetId);
        Match(
            nameof(expected.ComposeMaterial3Version),
            expected.ComposeMaterial3Version,
            actual.ComposeMaterial3Version);
        Match(nameof(expected.SourceArtifact), expected.SourceArtifact, actual.SourceArtifact);
        Match(
            nameof(expected.SourceArtifactSha256),
            expected.SourceArtifactSha256,
            actual.SourceArtifactSha256);
        Match("ColorContract.Id", expected.ColorContract.Id, actual.ColorContract.Id);
        Match("ColorContract.Seed", expected.ColorContract.Seed, actual.ColorContract.Seed);
        Match("ColorContract.Variant", expected.ColorContract.Variant, actual.ColorContract.Variant);
        Match(
            "ColorContract.SpecVersion",
            expected.ColorContract.SpecVersion,
            actual.ColorContract.SpecVersion);
        Match(
            "ColorContract.Platform",
            expected.ColorContract.Platform,
            actual.ColorContract.Platform);
        Match(
            "ColorContract.Manifest",
            expected.ColorContract.Manifest,
            actual.ColorContract.Manifest);
        Match(
            "ColorContract.Sha256",
            expected.ColorContract.Sha256,
            actual.ColorContract.Sha256);
        Match(
            "ColorContract.MaterialColorUtilitiesCommit",
            expected.ColorContract.MaterialColorUtilitiesCommit,
            actual.ColorContract.MaterialColorUtilitiesCommit);
        Match(
            "AndroidApi",
            expected.Environment.AndroidApi.ToString(),
            actual.Environment.AndroidApi.ToString());
        Match(
            "DensityDpi",
            expected.Environment.DensityDpi.ToString(),
            actual.Environment.DensityDpi.ToString());
        Match(
            "FontScale",
            expected.Environment.FontScale.ToString("R"),
            actual.Environment.FontScale.ToString("R"));
        Match("Locale", expected.Environment.Locale, actual.Environment.Locale);
        Match(
            "LayoutDirection",
            expected.Environment.LayoutDirection,
            actual.Environment.LayoutDirection);
        Match("FontFamily", expected.Environment.FontFamily, actual.Environment.FontFamily);
        Match("FontFileName", expected.Environment.FontFileName, actual.Environment.FontFileName);
        Match(
            "FontFileSha256",
            expected.Environment.FontFileSha256,
            actual.Environment.FontFileSha256);
        Match(
            "FontWeightPolicy",
            expected.Environment.FontWeightPolicy,
            actual.Environment.FontWeightPolicy);
        Match(
            "EmulatorProfile",
            expected.Environment.EmulatorProfile,
            actual.Environment.EmulatorProfile);

        if (mismatches.Count > 0)
        {
            throw new ComposeReferenceException(
                "Compose capture environment does not match the reference contract: "
                + string.Join("; ", mismatches));
        }

        return;

        void Match(string field, string expectedValue, string actualValue)
        {
            if (!string.Equals(expectedValue, actualValue, StringComparison.Ordinal))
                mismatches.Add($"{field} expected '{expectedValue}', got '{actualValue}'");
        }
    }

    private sealed record ValidatedCapture(
        ComposeReferenceCase ReferenceCase,
        string Sha256,
        byte[] Png,
        RenderedImage Image);
}
