using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class ReferenceCatalogTests
{
    static string Root() => Path.Combine(Path.GetTempPath(), "parity-references", Guid.NewGuid().ToString("N"));
    static CaseMetadata Metadata(int value = 42) => CaseMetadata.Create("test.fixture", "Fixture", "test", "direct", new NumericConfig(value));

    [Fact]
    public void FingerprintDependsOnReferenceInputsButNotAvaloniaLibrary()
    {
        var root = Root();
        var paths = new[]
        {
            "tests/VisualParity/reference/src/main/kotlin/Main.kt",
            "tests/VisualParity/reference/build/install/Material3ParityReference/lib/producer.jar",
            "tests/VisualParity/reference/build.gradle.kts", "tests/VisualParity/reference/settings.gradle.kts",
            "tests/VisualParity/reference/gradle.properties", "tests/VisualParity/reference/gradle.lockfile",
            "tests/VisualParity/reference/gradle/verification-metadata.xml",
            "tests/VisualParity/reference/gradle/wrapper/gradle-wrapper.properties",
            "docs/upstream-baseline.json", "modules/MaterialTypography.Roboto/Assets/Roboto-Regular.ttf",
            "modules/MaterialTypography.Roboto/Assets/Roboto-Medium.ttf", "modules/MaterialTypography.Roboto/Assets/Roboto-Bold.ttf",
            "modules/MaterialSymbols/assets/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf"
        };
        foreach (var path in paths)
        {
            var file = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(file)!); File.WriteAllText(file, "original");
        }
        var first = ReferenceCatalog.ProducerFingerprint(root);
        Directory.CreateDirectory(Path.Combine(root, "src/Material3Expressive"));
        File.WriteAllText(Path.Combine(root, "src/Material3Expressive/Button.cs"), "changed actual implementation");
        Assert.Equal(first, ReferenceCatalog.ProducerFingerprint(root));
        File.AppendAllText(Path.Combine(root, paths[0]), "scenario change");
        Assert.NotEqual(first, ReferenceCatalog.ProducerFingerprint(root));
        File.WriteAllText(Path.Combine(root, paths[0]), "original");
        File.AppendAllText(Path.Combine(root, paths[1]), "producer change");
        Assert.NotEqual(first, ReferenceCatalog.ProducerFingerprint(root));
        File.WriteAllText(Path.Combine(root, paths[1]), "original");
        foreach (var path in paths.Where(p => p.EndsWith(".ttf") && p.Contains("MaterialTypography.Roboto")))
        {
            File.AppendAllText(Path.Combine(root, path), "font change");
            Assert.NotEqual(first, ReferenceCatalog.ProducerFingerprint(root));
            File.WriteAllText(Path.Combine(root, path), "original");
        }
    }

    [Fact]
    public void PreflightReportsMissingChangedAndModifiedReferencesBeforeCapture()
    {
        var root = Root(); var metadata = Metadata();
        var capture = Path.Combine(ReferenceCatalog.DirectoryFor(root), "captures/first");
        Directory.CreateDirectory(capture);
        File.WriteAllText(Path.Combine(capture, "manifest.json"), "original");
        var entry = new ReferenceEntry(ParityJson.HashValue(metadata), "captures/first", ParityJson.Hash(Path.Combine(capture, "manifest.json")));
        var snapshot = new ReferenceSnapshot(1, "snapshot", "producer", [], new() { [metadata.Id] = entry });
        Assert.Empty(ReferenceCatalog.Problems(root, snapshot, "producer", [metadata]));
        Assert.Contains("configuration changed", Assert.Single(ReferenceCatalog.Problems(root, snapshot, "producer", [Metadata(43)])));
        Assert.Contains("reference missing", Assert.Single(ReferenceCatalog.Problems(root, snapshot with { Cases = [] }, "producer", [metadata])));
        Assert.Contains("producer/source/environment changed", Assert.Single(ReferenceCatalog.Problems(root, snapshot, "new-producer", [metadata])));
        File.AppendAllText(Path.Combine(capture, "manifest.json"), "tampered");
        Assert.Contains("manifest missing or modified", Assert.Single(ReferenceCatalog.Problems(root, snapshot, "producer", [metadata])));
        Assert.Throws<InvalidDataException>(() => ReferenceCatalog.CaptureDirectory(root, entry with { Directory = "../escape" }));
    }

    [Fact]
    public void MetadataIdentityMatchesFrozenUtf8File()
    {
        var path = Path.Combine(Root(), "case.json"); var metadata = Metadata();
        ParityJson.Write(path, metadata);
        Assert.Equal(ParityJson.Hash(path), ParityJson.HashValue(metadata));
    }

    [Fact]
    public void BuildSnapshotSurvivesSourceRebuild()
    {
        var root = Root(); var source = Path.Combine(root, "bin"); Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "producer.dll"), "first");
        var first = BuildSnapshot.Create(root, source, "test");
        File.WriteAllText(Path.Combine(source, "producer.dll"), "second");
        var second = BuildSnapshot.Create(root, source, "test");
        Assert.NotEqual(first, second);
        Assert.Equal("first", File.ReadAllText(Path.Combine(first, "producer.dll")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(second, "producer.dll")));
        Assert.Equal(second, BuildSnapshot.Create(root, source, "test"));
    }
}
