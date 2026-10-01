using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.Parity;

public sealed record CaseMetadata(
    int SchemaVersion,
    string Id,
    string Title,
    string Family,
    string ComposeEntry,
    string AvaloniaEntry
)
{
    public void Validate()
    {
        if (SchemaVersion != 2 || !System.Text.RegularExpressions.Regex.IsMatch(Id, "^[a-z0-9][a-z0-9.-]+$")
                               || new[] { Title, Family, ComposeEntry, AvaloniaEntry }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Invalid case metadata (schema 2 required).");
    }
}

// Values are snapshots. A field's definition identifies its meaning, independently of reader APIs.
public sealed record Measurement(
    string Name,
    string Category,
    string Kind,
    string Unit,
    string Space,
    string Definition,
    string Source,
    string Status = "measured",
    double[]? Numbers = null,
    string[]? Strings = null,
    string? Reason = null,
    bool? Boolean = null,
    string[]? Components = null
)
{
    public static Measurement Numeric(
        string name,
        double value,
        string unit = "dip",
        string space = "none",
        string category = "properties",
        string? definition = null,
        string source = "runtime"
    ) =>
        new(name, category, "number", unit, space, definition ?? name, source, Numbers: [value]);

    public static Measurement Vector(
        string name,
        string kind,
        double[] values,
        string unit,
        string space,
        string category,
        string? definition = null,
        string source = "runtime",
        string[]? components = null
    ) =>
        new(name, category, kind, unit, space, definition ?? name, source, Numbers: values, Components: components);

    // Defaults are protocol conventions, not guesses based on field names. Override e.g. size with width/height.
    public string[] ComponentNames() => Components ?? (Definition == "layout.box.local-size" && Kind == "point"
        ? ["width", "height"]
        : Kind switch
        {
            "number" => ["value"], "point" => ["x", "y"], "range" => ["start", "endExclusive"],
            "rect" => ["x", "y", "width", "height"], "color" => ["r", "g", "b", "a"],
            "quad" => ["p0.x", "p0.y", "p1.x", "p1.y", "p2.x", "p2.y", "p3.x", "p3.y"],
            "matrix" => ["m11", "m12", "m21", "m22", "m31", "m32"], _ => []
        });

    public static Measurement Text(
        string name,
        string value,
        string category = "properties",
        string? definition = null,
        string source = "runtime"
    ) => new(name, category, "string", "none", "none", definition ?? name, source, Strings: [value]);

    public static Measurement Flag(
        string name,
        bool value,
        string category = "properties",
        string? definition = null,
        string source = "runtime"
    ) => new(name, category, "boolean", "none", "none", definition ?? name, source, Boolean: value);

    public static Measurement Unavailable(
        string name,
        string category,
        string kind,
        string unit,
        string space,
        string reason,
        string? definition = null,
        string status = "unsupported"
    ) =>
        new(name, category, kind, unit, space, definition ?? name, "reader", status, Reason: reason);

    public void Validate()
    {
        if (new[] { Name, Category, Unit, Space, Definition, Source }.Any(string.IsNullOrWhiteSpace)
            || Category is not ("geometry" or "text" or "properties" or "motion")
            || Status is not ("measured" or "missing" or "unsupported" or "error"))
            throw new InvalidDataException("Invalid measurement descriptor.");
        var arity = Kind switch
        {
            "number" => 1,
            "point" or "range" => 2,
            "rect" or "color" => 4,
            "matrix" => 6,
            "quad" => 8,
            "string" => -1,
            "strings" => -2,
            "boolean" => -3,
            _ => 0
        };
        if (arity == 0) throw new InvalidDataException("Unknown measurement kind.");
        if (Components is not null && (arity < 1 || Components.Length != arity
                                                 || Components.Any(string.IsNullOrWhiteSpace) || Components.Distinct().Count() != arity))
            throw new InvalidDataException("Numeric component names must match the arity and be unique.");
        if (Status != "measured")
        {
            if (string.IsNullOrWhiteSpace(Reason) || Numbers is not null || Strings is not null || Boolean is not null)
                throw new InvalidDataException("Unavailable facts require a reason and no value.");
        }
        else if (arity == -3)
        {
            if (Boolean is null || Numbers is not null || Strings is not null)
                throw new InvalidDataException("Invalid boolean measurement.");
        }
        else if (Boolean is not null || (arity > 0
                     ? Numbers is null || Numbers.Length != arity || Numbers.Any(x => !double.IsFinite(x)) || Strings is not null
                     : Strings is null || arity == -1 && Strings.Length != 1 || Numbers is not null))
            throw new InvalidDataException("Invalid measurement value.");
    }
}

public sealed record ProbePoint(string Id, string Role, string? ParentId, Measurement[] Measurements);

public sealed record RasterObservation(
    string Id,
    string Image,
    string Pixels,
    int Width,
    int Height,
    string Format = "straight-srgba8",
    string Space = "scene-px",
    string Contribution = "full"
);

public sealed record RasterCollection(string Id, string Status, string? Reason = null);

public sealed record Sample(
    string Id,
    long TimeUs,
    string Stage,
    ProbePoint[] Points,
    RasterObservation[] Rasters,
    long? FrameTimeUs = null,
    RasterCollection[]? RasterCollection = null
);

public sealed record TraceEvent(
    int Sequence,
    long TimeUs,
    string Operation,
    string Detail,
    JsonElement? Data = null
);

public sealed record Capture(
    int SchemaVersion,
    string Runner,
    string CaseId,
    string MetadataSha256,
    string BaselineSha256,
    Dictionary<string, string> Environment,
    Sample[] Samples,
    TraceEvent[] Trace,
    string[] Errors
)
{
    public void Validate()
    {
        if (SchemaVersion != 2 || Runner is not ("compose" or "avalonia") || string.IsNullOrWhiteSpace(CaseId))
            throw new InvalidDataException("Invalid capture identity/schema.");
        if (Samples.Length == 0 && Errors.Length == 0)
            throw new InvalidDataException("A successful entry must submit at least one sample.");
        Unique(Samples.Select(x => x.Id));
        if (Samples.Zip(Samples.Skip(1)).Any(x => x.First.TimeUs > x.Second.TimeUs))
            throw new InvalidDataException("Sample timestamps must not move backwards.");
        foreach (var sample in Samples)
        {
            if (string.IsNullOrWhiteSpace(sample.Stage)) throw new InvalidDataException("A sample needs a stage.");
            Unique(sample.Points.Select(x => x.Id));
            Unique(sample.Rasters.Select(x => x.Id));
            if (sample.RasterCollection is { } collection)
            {
                Unique(collection.Select(x => x.Id));
                if (!collection.Where(x => x.Status == "captured").Select(x => x.Id).Order()
                        .SequenceEqual(sample.Rasters.Select(x => x.Id).Order())
                    || collection.Any(x => x.Status is not ("captured" or "notRequested" or "unsupported" or "missing" or "error")
                                           || x.Status != "captured" && string.IsNullOrWhiteSpace(x.Reason)))
                    throw new InvalidDataException("Raster collection records must describe the captured rasters and explain omissions.");
            }

            foreach (var point in sample.Points)
            {
                if (string.IsNullOrWhiteSpace(point.Role) || point.ParentId is not null && !sample.Points.Any(x => x.Id == point.ParentId))
                    throw new InvalidDataException("Invalid point role/parent.");
                var parents = new HashSet<string> { point.Id };
                var parent = point.ParentId;
                while (parent is not null)
                {
                    if (!parents.Add(parent)) throw new InvalidDataException("Point parent cycle.");
                    parent = sample.Points.Single(x => x.Id == parent).ParentId;
                }

                Unique(point.Measurements.Select(x => x.Name));
                foreach (var field in point.Measurements) field.Validate();
            }

            foreach (var raster in sample.Rasters)
                if (raster.Width < 1 || raster.Height < 1 || raster.Format != "straight-srgba8"
                    || raster.Space != "scene-px" || string.IsNullOrWhiteSpace(raster.Contribution))
                    throw new InvalidDataException("Invalid raster descriptor.");
        }

        if (!Trace.Select(x => x.Sequence).SequenceEqual(Enumerable.Range(0, Trace.Length)))
            throw new InvalidDataException("Trace sequence must be contiguous.");
    }

    static void Unique(IEnumerable<string> values)
    {
        var array = values.ToArray();
        if (array.Any(x => !System.Text.RegularExpressions.Regex.IsMatch(x, "^[a-zA-Z0-9][a-zA-Z0-9._-]*$")) ||
            array.Distinct().Count() != array.Length)
            throw new InvalidDataException("Observation ids must be nonempty, safe and unique in their scope.");
    }
}

public sealed record ComparisonIssue(string Code, string[] Members);

public sealed record FieldDifference(
    string Name,
    string Status,
    Measurement? Expected,
    Measurement? Actual,
    double[]? Delta = null,
    string? Reason = null,
    ComparisonIssue? Issue = null
);

public sealed record PointDifference(
    string Id,
    string? Role,
    string? ParentId,
    string Status,
    FieldDifference[] Fields,
    string? Reason = null,
    string? ExpectedRole = null,
    string? ActualRole = null,
    string? ExpectedParentId = null,
    string? ActualParentId = null,
    ComparisonIssue? Issue = null
);

public sealed record PixelMetrics(long DifferentPixels, int[] MaximumChannelDelta, double[] MeanChannelDelta, int[]? DifferenceBounds);

public sealed record RasterRegion(
    string PointId,
    int[] Bounds,
    PixelMetrics Metrics,
    string Basis = "union of expected/actual layout.box.aabb.unclipped; outward rounded and canvas clipped",
    string Space = "scene-px",
    string Contribution = "full"
);

public sealed record RasterDifference(
    string Id,
    string Status,
    RasterObservation? Expected,
    RasterObservation? Actual,
    PixelMetrics? Metrics = null,
    RasterRegion[]? Regions = null,
    string? DifferenceImage = null,
    string? DifferencePixels = null,
    string? Reason = null,
    ComparisonIssue? Issue = null
);

public sealed record SampleDifference(
    string Id,
    long? ExpectedTimeUs,
    long? ActualTimeUs,
    string? Stage,
    string Status,
    PointDifference[] Points,
    RasterDifference[] Rasters,
    string? Reason = null,
    string? ExpectedStage = null,
    string? ActualStage = null,
    long? ExpectedFrameTimeUs = null,
    long? ActualFrameTimeUs = null,
    ComparisonIssue? Issue = null
);

public sealed record Coverage(int Equal, int Different, int NotCompared);

public sealed record Comparison(
    int SchemaVersion,
    string CaseId,
    string Status,
    Coverage Coverage,
    SampleDifference[] Samples,
    string[] Errors,
    FieldDifference[] Environment,
    string Kind = "parity"
);

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
        Write(Path.Combine(directory, "manifest.json"), new ArtifactManifest(2, runner, files, environment));
    }

    public static void Verify(string directory)
    {
        var manifest = Read<ArtifactManifest>(Path.Combine(directory, "manifest.json"));
        if (manifest.SchemaVersion != 2 || !manifest.Files.ContainsKey("observations.json"))
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
        {
            var pixels = PixelData.Read(directory, raster);
            var imagePath = Path.GetFullPath(Path.Combine(directory, raster.Image));
            if (!imagePath.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Image path outside bundle.");
            var decoded = PixelData.DecodePng(imagePath);
            if (decoded.Width != raster.Width || decoded.Height != raster.Height)
                throw new InvalidDataException("PNG dimensions differ from observations.");
            if (!pixels.SequenceEqual(decoded.Bytes)) throw new InvalidDataException("Canonical bytes differ from decoded PNG pixels.");
        }

        if (capture.Runner != manifest.Runner ||
            !capture.Environment.OrderBy(x => x.Key).SequenceEqual(manifest.Environment.OrderBy(x => x.Key)))
            throw new InvalidDataException("Manifest and observation identity differ.");
    }
}