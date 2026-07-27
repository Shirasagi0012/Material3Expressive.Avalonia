using System.Security.Cryptography;
using System.Text.Json;
using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ComposeReferenceImporterTests
{
    [Fact]
    public void ImportAcceptsOnlyAHashedCaptureMatchingTheContract()
    {
        using var directory = new TemporaryDirectory();
        var paths = CreatePaths(directory.Path);
        var store = new ComposeReferenceStore(paths);
        var referenceCase = WriteReferenceManifest(store, androidApi: 35);
        var captureRoot = WriteCapture(directory.Path, referenceCase, androidApi: 35);

        var imported = new ComposeReferenceImporter(store).Import(captureRoot, [referenceCase]);

        Assert.Equal([referenceCase.Id], imported);
        Assert.Equal(ComposeReferenceState.Ready, store.GetState(referenceCase));
        Assert.True(File.Exists(store.GetImagePath(referenceCase)));
        Assert.True(File.Exists(store.IndexPath));
    }

    [Fact]
    public void EnvironmentMismatchIsRejectedBeforeWritingAnyReference()
    {
        using var directory = new TemporaryDirectory();
        var paths = CreatePaths(directory.Path);
        var store = new ComposeReferenceStore(paths);
        var referenceCase = WriteReferenceManifest(store, androidApi: 35);
        var captureRoot = WriteCapture(directory.Path, referenceCase, androidApi: 36);

        var exception = Assert.Throws<ComposeReferenceException>(
            () => new ComposeReferenceImporter(store).Import(captureRoot, [referenceCase]));

        Assert.Contains("AndroidApi", exception.Message);
        Assert.False(File.Exists(store.GetImagePath(referenceCase)));
        Assert.False(File.Exists(store.IndexPath));
    }

    [Fact]
    public void TamperedCaptureIsRejectedBeforeWritingAnyReference()
    {
        using var directory = new TemporaryDirectory();
        var paths = CreatePaths(directory.Path);
        var store = new ComposeReferenceStore(paths);
        var referenceCase = WriteReferenceManifest(store, androidApi: 35);
        var captureRoot = WriteCapture(directory.Path, referenceCase, androidApi: 35);
        File.AppendAllText(Path.Combine(captureRoot, "case.png"), "tampered");

        var exception = Assert.Throws<ComposeReferenceException>(
            () => new ComposeReferenceImporter(store).Import(captureRoot, [referenceCase]));

        Assert.Contains("hash changed", exception.Message);
        Assert.False(File.Exists(store.GetImagePath(referenceCase)));
    }

    private static RepositoryPaths CreatePaths(string root) =>
        new(
            root,
            "test",
            Path.Combine(root, "baselines"),
            Path.Combine(root, "artifacts"));

    private static ComposeReferenceCase WriteReferenceManifest(
        ComposeReferenceStore store,
        int androidApi)
    {
        var referenceCase = new ComposeReferenceCase(
            "button/filled/medium/enabled/light",
            "cases/button.png",
            "light",
            4,
            3,
            "ButtonScreenshotTest",
            "button_light_theme");
        var manifest = new ComposeReferenceManifest(
            ComposeReferenceManifest.CurrentSchemaVersion,
            "test-reference",
            "1.5.0-alpha24",
            "test:artifact:1.5.0-alpha24",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            ColorContract(),
            Environment(androidApi),
            [referenceCase]);
        var colorContractPath = Path.Combine(
            store.Paths.Root,
            manifest.ColorContract.Manifest.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(colorContractPath)!);
        File.WriteAllText(colorContractPath, "test-color-contract");
        manifest = manifest with
        {
            ColorContract = manifest.ColorContract with
            {
                Sha256 = Convert.ToHexString(
                    SHA256.HashData(File.ReadAllBytes(colorContractPath)))
            }
        };
        Directory.CreateDirectory(Path.GetDirectoryName(store.ManifestPath)!);
        File.WriteAllText(
            store.ManifestPath,
            JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
        return referenceCase;
    }

    private static string WriteCapture(
        string root,
        ComposeReferenceCase referenceCase,
        int androidApi)
    {
        var captureRoot = Path.Combine(root, "capture");
        Directory.CreateDirectory(captureRoot);
        var png = Solid(referenceCase.Width, referenceCase.Height).EncodePng();
        var image = Path.Combine(captureRoot, "case.png");
        File.WriteAllBytes(image, png);
        var capture = new ComposeCaptureManifest(
            ComposeReferenceManifest.CurrentSchemaVersion,
            "test-reference",
            "1.5.0-alpha24",
            "test:artifact:1.5.0-alpha24",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            ColorContract(),
            Environment(androidApi),
            "capture-1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            [
                new ComposeCapturedCase(
                    referenceCase.Id,
                    "case.png",
                    referenceCase.Width,
                    referenceCase.Height,
                    Convert.ToHexString(SHA256.HashData(png)))
            ]);
        File.WriteAllText(
            Path.Combine(captureRoot, ComposeCaptureManifest.FileName),
            JsonSerializer.Serialize(
                capture,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
        return captureRoot;
    }

    private static ComposeReferenceEnvironment Environment(int androidApi) =>
        new(
            androidApi,
            160,
            1,
            "en-US",
            "ltr",
            "Roboto",
            "Roboto-VariableFont_wdth,wght.ttf",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "preset-weights-only",
            "test-emulator");

    private static ComposeColorContract ColorContract() =>
        new(
            "test-color-contract",
            "#6750A4",
            "TonalSpot",
            "Spec2021",
            "Phone",
            "visual-references/color-contracts/test.json",
            Convert.ToHexString(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test-color-contract"))),
            "05f04c94555fc8fbaa3f85ea3c899ad948e955cd");

    private static RenderedImage Solid(int width, int height)
    {
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        return new RenderedImage(width, height, pixels);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Material3Expressive.VisualTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
