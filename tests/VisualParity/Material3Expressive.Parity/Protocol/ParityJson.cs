using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.Parity;

public sealed record ArtifactManifest(
    int SchemaVersion,
    string Runner,
    Dictionary<string, string> Files,
    Dictionary<string, string> Environment
);

public static class ParityJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
                                            ?? throw new InvalidDataException($"Empty document: {path}");

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
    }

    public static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    public static void Seal(string directory, string runner, Dictionary<string, string> environment)
    {
        var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetFullPath(x) != Path.Combine(Path.GetFullPath(directory), "manifest.json"))
            .Order().ToDictionary(x => Path.GetRelativePath(directory, x).Replace('\\', '/'), Hash);
        Write(Path.Combine(directory, "manifest.json"), new ArtifactManifest(4, runner, files, environment));
    }

    public static void Verify(string directory)
    {
        var manifest = Read<ArtifactManifest>(Path.Combine(directory, "manifest.json"));
        if (manifest.SchemaVersion != 4 || !manifest.Files.ContainsKey("observations.json"))
            throw new InvalidDataException("Invalid artifact manifest.");
        var actualFiles = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetFullPath(x) != Path.Combine(Path.GetFullPath(directory), "manifest.json"))
            .Select(x => Path.GetRelativePath(directory, x).Replace('\\', '/')).Order();
        if (!actualFiles.SequenceEqual(manifest.Files.Keys.Order()))
            throw new InvalidDataException("Artifact file set differs from manifest.");
        foreach (var (relative, hash) in manifest.Files)
        {
            var path = Path.GetFullPath(Path.Combine(directory, relative));
            if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path) || Hash(path) != hash)
                throw new InvalidDataException($"Artifact missing, modified or outside bundle: {relative}");
        }

        var capture = Read<Capture>(Path.Combine(directory, "observations.json"));
        capture.Validate();
        foreach (var sample in capture.Samples)
        foreach (var raster in sample.Rasters)
            _ = PixelData.Read(directory, raster);

        if (capture.Runner != manifest.Runner ||
            !capture.Environment.OrderBy(x => x.Key).SequenceEqual(manifest.Environment.OrderBy(x => x.Key)))
            throw new InvalidDataException("Manifest and observation identity differ.");
    }
}
