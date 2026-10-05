using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Material3Expressive.Parity;

public sealed record ReferenceEntry(string MetadataSha256, string Directory, string ManifestSha256);
public sealed record ReferenceSnapshot(int SchemaVersion, string Id, string ProducerSha256,
    Dictionary<string, string> Environment, Dictionary<string, ReferenceEntry> Cases);

// A reference snapshot pins capture bundles. Preparing another snapshot never overwrites an existing bundle.
public static class ReferenceCatalog
{
    public static string DirectoryFor(string root) => Path.Combine(root, "artifacts/visual-parity/references");
    public static string Current(string root) => Path.Combine(DirectoryFor(root), "current.json");

    public static string ProducerFingerprint(string root)
    {
        var reference = Path.Combine(root, "tests/VisualParity/reference");
        var library = Path.Combine(reference, "build/install/Material3ParityReference/lib");
        if (!System.IO.Directory.Exists(library)) throw new DirectoryNotFoundException("Prepare the Compose reference first: parity prepare-reference");
        var files = System.IO.Directory.EnumerateFiles(Path.Combine(reference, "src"), "*", SearchOption.AllDirectories)
            .Concat(System.IO.Directory.EnumerateFiles(library, "*.jar"))
            .Concat(new[] { "build.gradle.kts", "settings.gradle.kts", "gradle.properties", "gradle.lockfile",
                "gradle/verification-metadata.xml", "gradle/wrapper/gradle-wrapper.properties" }.Select(p => Path.Combine(reference, p)))
            .Concat(new[] { "docs/upstream-baseline.json", "modules/MaterialTypography.Roboto/Assets/Roboto-Regular.ttf",
                "modules/MaterialTypography.Roboto/Assets/Roboto-Medium.ttf", "modules/MaterialTypography.Roboto/Assets/Roboto-Bold.ttf" }.Select(p => Path.Combine(root, p)))
            .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), ParityJson.Hash);
        return ParityJson.HashValue(new { files, density = AvaloniaHost.RenderScale,
            platform = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString() });
    }

    public static ReferenceSnapshot Load(string root, string? path = null) =>
        ParityJson.Read<ReferenceSnapshot>(path ?? Current(root));

    public static string CaptureDirectory(string root, ReferenceEntry entry)
    {
        var parent = Path.GetFullPath(DirectoryFor(root));
        var path = Path.GetFullPath(Path.Combine(parent, entry.Directory));
        if (!path.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Reference capture outside reference library.");
        return path;
    }

    public static string[] Problems(string root, ReferenceSnapshot snapshot, string producer, IEnumerable<CaseMetadata> selected)
    {
        if (snapshot.SchemaVersion != 1 || snapshot.ProducerSha256 != producer)
            return ["Reference producer/source/environment changed. Run prepare-reference before testing."];
        return selected.Select(m =>
        {
            if (!snapshot.Cases.TryGetValue(m.Id, out var entry)) return m.Id + ": reference missing";
            if (entry.MetadataSha256 != ParityJson.HashValue(m)) return m.Id + ": configuration changed";
            var manifest = Path.Combine(CaptureDirectory(root, entry), "manifest.json");
            if (!File.Exists(manifest) || ParityJson.Hash(manifest) != entry.ManifestSha256)
                return m.Id + ": reference manifest missing or modified";
            return null;
        }).OfType<string>().ToArray();
    }

    public static Capture ReadCapture(string root, CaseMetadata metadata, ReferenceEntry entry)
    {
        var directory = CaptureDirectory(root, entry);
        if (entry.MetadataSha256 != ParityJson.HashValue(metadata) ||
            ParityJson.Hash(Path.Combine(directory, "manifest.json")) != entry.ManifestSha256)
            throw new InvalidDataException("Reference identity changed: " + metadata.Id);
        var capture = ParityJson.Verify(directory, decodePixels: false);
        if (capture.Runner != "compose" || capture.CaseId != metadata.Id || capture.Errors.Length != 0 ||
            capture.MetadataSha256 != entry.MetadataSha256 ||
            capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")) ||
            capture.Environment.GetValueOrDefault("density") != AvaloniaHost.RenderScale.ToString())
            throw new InvalidDataException("Reference identity/failure mismatch: " + metadata.Id);
        return capture;
    }

    public static async Task<string> Prepare(string root, CaseMetadata[] selected, bool refresh, string? previousPath = null)
    {
        var timer = Stopwatch.StartNew();
        var producerHash = ProducerFingerprint(root);
        var library = DirectoryFor(root);
        System.IO.Directory.CreateDirectory(library);
        var previous = File.Exists(previousPath ?? Current(root)) ? Load(root, previousPath) : null;
        await using var producer = await ReferenceProducer.Start(root);
        ScenarioDiscovery.RequireReferenceScenarios(selected, producer.Scenarios);
        var entries = previous is { SchemaVersion: 1 } && previous.ProducerSha256 == producerHash &&
            previous.Environment.OrderBy(p => p.Key).SequenceEqual(producer.Environment.OrderBy(p => p.Key))
            ? new Dictionary<string, ReferenceEntry>(previous.Cases) : [];
        var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
        var work = Path.Combine(library, ".work", id);
        foreach (var metadata in selected) ParityJson.Write(Path.Combine(work, metadata.Id + ".json"), metadata);
        var reused = 0;
        foreach (var metadata in selected)
        {
            var elapsed = Stopwatch.StartNew();
            if (!refresh && entries.TryGetValue(metadata.Id, out var existing) && existing.MetadataSha256 == ParityJson.HashValue(metadata))
            {
                ReadCapture(root, metadata, existing);
                reused++;
                Console.WriteLine($"reference reuse {metadata.Id} ({elapsed.Elapsed.TotalSeconds:F2}s)");
                continue;
            }
            var staging = Path.Combine(work, metadata.Id);
            await producer.Capture(Path.Combine(work, metadata.Id + ".json"), staging);
            var relative = "captures/" + id + "/" + metadata.Id;
            var destination = Path.Combine(library, relative);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            System.IO.Directory.Move(staging, destination);
            entries[metadata.Id] = new(ParityJson.HashValue(metadata), relative, ParityJson.Hash(Path.Combine(destination, "manifest.json")));
            Console.WriteLine($"reference captured {metadata.Id} ({elapsed.Elapsed.TotalSeconds:F2}s)");
        }
        // Source edits during preparation must not be published under an obsolete source fingerprint.
        if (ProducerFingerprint(root) != producerHash) throw new IOException("Reference inputs changed during preparation; captures retained, snapshot not published.");
        var snapshot = new ReferenceSnapshot(1, id, producerHash, producer.Environment, entries);
        var path = Path.Combine(library, "snapshots", id + ".json");
        ParityJson.Write(path, snapshot);
        var temporary = Path.Combine(library, "current." + id + ".tmp");
        File.Copy(path, temporary);
        File.Move(temporary, Current(root), overwrite: true);
        // Only this successful preparation's scratch directory is removed.
        System.IO.Directory.Delete(work, recursive: true);
        Console.WriteLine($"Reference ready: {selected.Length - reused} captured, {reused} reused; {timer.Elapsed.TotalSeconds:F2}s\nReference snapshot: {path}");
        return path;
    }
}
