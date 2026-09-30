using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.Parity;

public sealed record Scenario
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Component { get; init; }
    public string Variant { get; init; } = "filled";
    public string Content { get; init; } = "slot";
    public string Text { get; init; } = "Save";
    public string Direction { get; init; } = "LTR";
    public bool Dark { get; init; }
    public bool Enabled { get; init; } = true;
    public int ViewportWidth { get; init; } = 240;
    public int ViewportHeight { get; init; } = 160;
    public double Width { get; init; } = 100;
    public double Height { get; init; } = 50;
    public double Radius { get; init; } = 12;
    public double Elevation { get; init; }
    public double FontSize { get; init; } = 14;
    public int FontWeight { get; init; } = 400;
    public double LineHeight { get; init; } = 20;
    public double LetterSpacing { get; init; }
    public bool Wrap { get; init; }
    public double Density { get; init; } = 1;
    public double FontScale { get; init; } = 1;
    public string MotionSpec { get; init; } = "tween";
    public string Background { get; init; } = "#FFFFFF";
    public string Ink { get; init; } = "#6750A4";
    public long[] FramesUs { get; init; } = [0];
    public int[] Checkpoints { get; init; } = [0];
    public InputEvent[] Events { get; init; } = [];
    public string[] Required { get; init; } = ["layout", "raster", "behavior"];

    public void Validate()
    {
        if (SchemaVersion != 1 || !System.Text.RegularExpressions.Regex.IsMatch(Id, "^[a-z0-9][a-z0-9.-]+$"))
            throw new InvalidDataException("Invalid scenario identity/schema.");
        if (!new[] { "surface", "button", "iconbutton", "text", "motion" }.Contains(Component)
            || !new[] { "filled", "outlined", "text", "tonal", "elevated", "standard" }.Contains(Variant)
            || !new[] { "slot", "text" }.Contains(Content) || Direction is not ("LTR" or "RTL") || MotionSpec is not ("tween" or "spring"))
            throw new InvalidDataException($"Unsupported fixture: {Id}.");
        if (ViewportWidth is < 1 or > 2048 || ViewportHeight is < 1 or > 2048
            || new[] { Width, Height, Radius, Elevation, FontSize, LineHeight, Density, FontScale }.Any(x => !double.IsFinite(x) || x < 0)
            || !double.IsFinite(LetterSpacing)
            || Density <= 0 || FontScale <= 0 || FontSize <= 0 || Width > ViewportWidth || Height > ViewportHeight)
            throw new InvalidDataException("Invalid dimensions.");
        if (FramesUs.Length is < 1 or > 1200 || FramesUs[0] != 0 || FramesUs[^1] > 30_000_000
            || FramesUs.Zip(FramesUs.Skip(1)).Any(x => x.First >= x.Second)
            || Checkpoints.Length == 0 || Checkpoints.Distinct().Count() != Checkpoints.Length
            || Checkpoints.Any(x => x < 0 || x >= FramesUs.Length))
            throw new InvalidDataException("Frames must start at zero and strictly increase; checkpoints must address frames.");
        if (Events.Any(x => x.AtUs < 0 || x.AtUs > FramesUs[^1] || !double.IsFinite(x.Value)
            || !new[] { "down", "up", "move", "leave", "target", "disable", "detach" }.Contains(x.Action)))
            throw new InvalidDataException("Invalid input event.");
        if (Events.Zip(Events.Skip(1)).Any(x => x.First.AtUs > x.Second.AtUs))
            throw new InvalidDataException("Events must be ordered; file order breaks ties.");
        if (Required.Any(x => !new[] { "layout", "raster", "behavior", "motion", "text", "textLayers", "resolvedFont", "textCoverage", "virtualDelays" }.Contains(x)))
            throw new InvalidDataException("Unknown required capability.");
        if (FontWeight is < 100 or > 900 || !System.Text.RegularExpressions.Regex.IsMatch(Background, "^#[0-9a-fA-F]{6}$")
            || !System.Text.RegularExpressions.Regex.IsMatch(Ink, "^#[0-9a-fA-F]{6}$")) throw new InvalidDataException("Invalid font weight or sRGB color.");
    }
}
public sealed record InputEvent(long AtUs, string Action, double Value = 0);
public sealed record Bounds(double X, double Y, double Width, double Height);
public sealed record TextLine(double Baseline, double Width, double Height, bool Ellipsized);
public sealed record TextObservation(string Text, double FontSize, int FontWeight, TextLine[] Lines, string[] ResolvedFonts, double LetterSpacing = 0);
public sealed record Node(string Id, Bounds Bounds, TextObservation? Text = null);
public sealed record FrameObservation(int Index, long TimeUs, string? Image, Node[] Nodes,
    Dictionary<string, double> Values, int Clicks, bool Enabled, string? WithoutTextImage = null);
public sealed record Capture(string Runner, string ScenarioSha256, string BaselineSha256,
    Dictionary<string, string> Environment, string[] Capabilities, FrameObservation[] Frames, string[] Errors);
public sealed record Finding(string Status, int Frame, string Subject, string Metric,
    double? Expected = null, double? Actual = null, double? Delta = null, string? Detail = null);
public sealed record Comparison(string Scenario, string Status, Finding[] Findings, string Kind = "parity");
public sealed record ArtifactManifest(int SchemaVersion, string Runner, Dictionary<string, string> Files,
    Dictionary<string, string> Environment);

public static class ParityJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
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
        Write(Path.Combine(directory, "manifest.json"), new ArtifactManifest(1, runner, files, environment));
    }
    public static void Verify(string directory)
    {
        var manifest = Read<ArtifactManifest>(Path.Combine(directory, "manifest.json"));
        if (manifest.SchemaVersion != 1 || !manifest.Files.ContainsKey("observations.json"))
            throw new InvalidDataException("Invalid artifact manifest.");
        var actualFiles = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetFullPath(x) != Path.Combine(Path.GetFullPath(directory), "manifest.json"))
            .Select(x => Path.GetRelativePath(directory, x).Replace('\\', '/')).Order();
        if (!actualFiles.SequenceEqual(manifest.Files.Keys.Order())) throw new InvalidDataException("Artifact file set differs from manifest.");
        foreach (var (relative, hash) in manifest.Files)
        {
            var path = Path.GetFullPath(Path.Combine(directory, relative));
            if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path) || Hash(path) != hash)
                throw new InvalidDataException($"Artifact missing, modified or outside bundle: {relative}");
        }
        var capture = Read<Capture>(Path.Combine(directory, "observations.json"));
        if (capture.Runner != manifest.Runner || !capture.Environment.OrderBy(x => x.Key).SequenceEqual(manifest.Environment.OrderBy(x => x.Key)))
            throw new InvalidDataException("Manifest and observation identity differ.");
    }
}
