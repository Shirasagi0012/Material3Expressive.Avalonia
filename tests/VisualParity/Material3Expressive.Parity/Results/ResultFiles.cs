using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Material3Expressive.Parity;

public sealed record CaseResult(CaseMetadata Metadata, Comparison Comparison, Capture? Expected, Capture? Actual,
    Dictionary<string, string> Samples, JsonNode? Repeatability);

// Public, self-contained case results. The producer's schema-4 bundles stay in the capture cache.
// Every sample owns its facts, descriptors, collection decisions and preceding execution events.
public static class ResultFiles
{
    static readonly string[] Categories = ["geometry", "text", "properties", "motion", "mixed"];
    static JsonNode? N<T>(T value) => ResultYaml.Node(value);
    static T Get<T>(JsonNode? node) => node.Deserialize<T>(ParityJson.Options)!;
    static string Text(JsonNode? node) => node?.GetValue<string>() ?? throw new InvalidDataException("Missing string.");
    static JsonNode? Pair(JsonNode? e, JsonNode? a) => JsonNode.DeepEquals(e, a)
        ? e?.DeepClone() : new JsonObject { ["compose"] = e?.DeepClone(), ["avalonia"] = a?.DeepClone() };
    static JsonNode? Side(JsonNode? node, string side) => node is JsonObject pair && pair.ContainsKey("compose")
        ? pair[side] : node;
    static string Category(FieldDifference f) => f.Expected is { } e && f.Actual is { } a && e.Category != a.Category
        ? "mixed" : (f.Expected ?? f.Actual)!.Category;
    public static Coverage Counts(IEnumerable<string> statuses)
    {
        var all = statuses.ToArray();
        return new(all.Count(s => s == "equal"), all.Count(s => s == "different"), all.Count(s => s == "notCompared"),
            all.Count(s => s == "withinTolerance"));
    }

    public static void Write(string directory, CaseMetadata metadata, Comparison comparison, Capture? expected, Capture? actual,
        string expectedDirectory, string actualDirectory, string diffDirectory, JsonNode? repeatability = null, string? diagnostics = null)
    {
        if (Directory.Exists(directory)) throw new IOException("Result directory must be new.");
        Directory.CreateDirectory(directory);
        var index = new JsonObject();
        var tracePositions = new Dictionary<string, int> { ["compose"] = 0, ["avalonia"] = 0 };

        JsonNode? Trace(Capture? capture, string side, string sample)
        {
            if (capture is null) return null;
            var start = tracePositions[side];
            var end = Array.FindIndex(capture.Trace, start, t => t.Operation == "sample" && t.Detail == sample);
            if (end < 0) return N(Array.Empty<TraceEvent>());
            tracePositions[side] = end + 1;
            return N(capture.Trace[start..(end + 1)]);
        }

        string Copy(string sourceDirectory, string relative, string name)
        {
            var source = Inside(sourceDirectory, relative);
            var destination = "images/" + name + Path.GetExtension(relative);
            Directory.CreateDirectory(Path.Combine(directory, "images"));
            File.Copy(source, Inside(directory, destination));
            return destination;
        }

        // Consume trace slices in each capture's original submission order, independently of comparison sorting.
        var traces = new Dictionary<(string Side, string Sample), JsonNode?>();
        foreach (var (side, capture) in new[] { ("compose", expected), ("avalonia", actual) })
            if (capture is not null)
                foreach (var sample in capture.Samples) traces[(side, sample.Id)] = Trace(capture, side, sample.Id);

        foreach (var (s, number) in comparison.Samples.Select((s, i) => (s, i)))
        {
            var stem = $"{number:D4}-{s.Id}";
            var file = "samples/" + stem + ".yaml";
            index[s.Id] = file;
            var e = expected?.Samples.SingleOrDefault(x => x.Id == s.Id);
            var a = actual?.Samples.SingleOrDefault(x => x.Id == s.Id);
            var points = new JsonObject();
            foreach (var p in s.Points)
            {
                var point = new JsonObject
                {
                    ["status"] = p.Status, ["role"] = Pair(N(p.ExpectedRole), N(p.ActualRole)),
                    ["parent"] = Pair(N(p.ExpectedParentId), N(p.ActualParentId))
                };
                if (p.Issue is not null) point["issue"] = N(p.Issue);
                if (p.Reason is not null) point["reason"] = p.Reason;
                foreach (var group in p.Fields.GroupBy(Category))
                {
                    var fields = new JsonObject();
                    foreach (var f in group) fields[f.Name] = Field(f, group.Key);
                    point[group.Key] = fields;
                }
                points[p.Id] = point;
            }

            var rasters = new JsonObject();
            foreach (var r in s.Rasters)
            {
                RasterObservation? Relocate(RasterObservation? raster, string source, string side) => raster is null ? null : raster with
                {
                    Image = Copy(source, raster.Image, stem + "." + side + "." + r.Id)
                };
                var er = Relocate(r.Expected, expectedDirectory, "compose");
                var ar = Relocate(r.Actual, actualDirectory, "avalonia");
                var raster = new JsonObject { ["status"] = r.Status };
                var en = er is null ? null : N(er)!.AsObject();
                var an = ar is null ? null : N(ar)!.AsObject();
                raster["present"] = N(new[] { er is null ? null : "compose", ar is null ? null : "avalonia" }.OfType<string>().ToArray());
                foreach (var key in ParityComparison.Keys(en?.Select(p => p.Key), an?.Select(p => p.Key)).Where(k => k != "id"))
                    raster[key] = Pair(en?[key], an?[key]);
                if (r.Metrics is not null) raster["metrics"] = N(r.Metrics);
                if (r.ExactStatus is not null) raster["exactStatus"] = r.ExactStatus;
                if (r.Tolerance is not null) raster["tolerance"] = N(r.Tolerance);
                if (r.Regions is not null) raster["regions"] = N(r.Regions);
                if (r.Issue is not null) raster["issue"] = N(r.Issue);
                if (r.Reason is not null) raster["reason"] = r.Reason;
                if (r.DifferenceImage is not null) raster["diffImage"] = Copy(diffDirectory, r.DifferenceImage, stem + ".diff." + r.Id);
                rasters[r.Id] = raster;
            }

            var document = new JsonObject
            {
                ["schemaVersion"] = 8, ["case"] = metadata.Id, ["sample"] = s.Id, ["status"] = s.Status,
                ["present"] = N(new[] { e is null ? null : "compose", a is null ? null : "avalonia" }.OfType<string>().ToArray()),
                ["timeUs"] = Pair(N(s.ExpectedTimeUs), N(s.ActualTimeUs)),
                ["frameTimeUs"] = Pair(N(s.ExpectedFrameTimeUs), N(s.ActualFrameTimeUs)),
                ["stage"] = Pair(N(s.ExpectedStage), N(s.ActualStage)),
                ["points"] = points, ["rasters"] = rasters,
                ["rasterCollection"] = Pair(N(e?.RasterCollection), N(a?.RasterCollection)),
                ["trace"] = new JsonObject
                {
                    ["compose"] = traces.GetValueOrDefault(("compose", s.Id))?.DeepClone(),
                    ["avalonia"] = traces.GetValueOrDefault(("avalonia", s.Id))?.DeepClone()
                }
            };
            if (s.Issue is not null) document["issue"] = N(s.Issue);
            if (s.Reason is not null) document["reason"] = s.Reason;
            ResultYaml.Write(Inside(directory, file), document);
        }

        JsonNode? Header(Capture? capture, string side) => capture is null ? null : new JsonObject
        {
            ["runner"] = capture.Runner, ["caseId"] = capture.CaseId,
            ["metadataSha256"] = capture.MetadataSha256, ["baselineSha256"] = capture.BaselineSha256,
            ["errors"] = N(capture.Errors), ["sampleOrder"] = N(capture.Samples.Select(s => s.Id).ToArray()),
            ["traceTail"] = N(capture.Trace[tracePositions[side]..])
        };
        ResultYaml.Write(Path.Combine(directory, "case.yaml"), new JsonObject
        {
            ["schemaVersion"] = 8, ["metadata"] = N(metadata), ["status"] = comparison.Status,
            ["kind"] = comparison.Kind, ["errors"] = N(comparison.Errors), ["samples"] = index,
            ["sampleSummaries"] = N(comparison.Samples.Select(s => new
            {
                s.Id, s.Status, s.ExpectedTimeUs, s.ActualTimeUs, s.Stage,
                categories = Categories.ToDictionary(category => category, category =>
                    Counts(s.Points.SelectMany(p => p.Fields).Where(f => Category(f) == category).Select(f => f.Status))),
                rasters = Counts(s.Rasters.Select(r => r.Status)),
                collection = new { compose = expected?.Samples.SingleOrDefault(x => x.Id == s.Id)?.RasterCollection,
                    avalonia = actual?.Samples.SingleOrDefault(x => x.Id == s.Id)?.RasterCollection }
            }).ToArray()),
            ["captures"] = new JsonObject { ["compose"] = Header(expected, "compose"), ["avalonia"] = Header(actual, "avalonia") },
            ["repeatability"] = repeatability?.DeepClone(), ["diagnostics"] = diagnostics
        });
        ResultYaml.Write(Path.Combine(directory, "environment.yaml"), new JsonObject
        { ["compose"] = N(expected?.Environment), ["avalonia"] = N(actual?.Environment) });
        Seal(directory);
        File.WriteAllText(Path.Combine(directory, "overview.md"), Overview(Read(directory)));
    }

    static JsonObject Field(FieldDifference f, string category)
    {
        var result = new JsonObject { ["status"] = f.Status };
        var e = f.Expected;
        var a = f.Actual;
        if (category == "mixed") result["category"] = Pair(N(e?.Category), N(a?.Category));
        result["kind"] = Pair(N(e?.Kind), N(a?.Kind));
        result["unit"] = Pair(N(e?.Unit), N(a?.Unit));
        result["space"] = Pair(N(e?.Space), N(a?.Space));
        result["definition"] = Pair(N(e?.Definition), N(a?.Definition));
        if ((e?.ComponentNames().Length ?? 0) + (a?.ComponentNames().Length ?? 0) > 0)
            result["components"] = Pair(N(e?.ComponentNames()), N(a?.ComponentNames()));
        result["source"] = Pair(N(e?.Source), N(a?.Source));
        if (e is { Status: "measured", Reason: not null } || a is { Status: "measured", Reason: not null })
            result["measurementReason"] = Pair(N(e?.Reason), N(a?.Reason));

        JsonNode? Value(Measurement? m) => m is null ? new JsonObject { ["status"] = "absent" }
            : m.Status != "measured" ? new JsonObject { ["status"] = m.Status, ["reason"] = m.Reason }
            : m.Kind switch
            {
                "number" => N(m.Numbers![0]), "string" => N(m.Strings![0]), "strings" => N(m.Strings),
                "boolean" => N(m.Boolean), _ => N(m.Numbers)
            };
        var ev = Value(e);
        var av = Value(a);
        if (JsonNode.DeepEquals(ev, av)) result["value"] = ev;
        else { result["compose"] = ev; result["avalonia"] = av; }
        if (f.Delta is not null && f.Status != "equal") result["delta"] = f.Delta.Length == 1 ? N(f.Delta[0]) : N(f.Delta);
        if (f.Issue is not null) result["issue"] = N(f.Issue);
        if (f.Reason is not null) result["reason"] = f.Reason;
        return result;
    }

    public static CaseResult Read(string directory)
    {
        Verify(directory);
        var document = ResultYaml.Read(Path.Combine(directory, "case.yaml"));
        if (Get<int>(document["schemaVersion"]) != 8) throw new InvalidDataException("Result schema 8 required.");
        var metadata = Get<CaseMetadata>(document["metadata"]);
        metadata.Validate();
        var environment = ResultYaml.Read(Path.Combine(directory, "environment.yaml"));
        var files = Get<Dictionary<string, string>>(document["samples"]);
        var samples = new List<SampleDifference>();
        var captures = new Dictionary<string, List<Sample>> { ["compose"] = [], ["avalonia"] = [] };
        var traces = new Dictionary<string, List<TraceEvent>> { ["compose"] = [], ["avalonia"] = [] };

        foreach (var (id, file) in files)
        {
            var s = ResultYaml.Read(Inside(directory, file));
            if (Get<int>(s["schemaVersion"]) != 8 || Text(s["sample"]) != id || Text(s["case"]) != metadata.Id)
                throw new InvalidDataException("Sample identity differs from case index.");
            var points = new List<PointDifference>();
            foreach (var (pid, node) in s["points"]!.AsObject())
            {
                var p = node!.AsObject();
                var fields = new List<FieldDifference>();
                foreach (var category in Categories)
                {
                    if (p[category] is not JsonObject group) continue;
                    foreach (var (name, fieldNode) in group)
                    {
                        var field = fieldNode!.AsObject();
                        Measurement? ReadField(string side)
                        {
                            var value = field.ContainsKey("value") ? field["value"] : field[side];
                            var status = value is JsonObject unavailable ? Text(unavailable["status"]) : "measured";
                            if (status == "absent") return null;
                            string Property(string key) => Text(Side(field[key], side));
                            var kind = Property("kind");
                            var numbers = status != "measured" || kind is "string" or "strings" or "boolean" ? null
                                : kind == "number" ? new[] { Get<double>(value) } : Get<double[]>(value);
                            return new(name, category == "mixed" ? Property("category") : category, kind,
                                Property("unit"), Property("space"), Property("definition"), Property("source"), status,
                                Numbers: numbers, Strings: status != "measured" ? null : kind == "string" ? [Text(value)]
                                    : kind == "strings" ? Get<string[]>(value) : null,
                                Reason: status == "measured" ? Side(field["measurementReason"], side)?.GetValue<string>() : Text(value!["reason"]),
                                Boolean: status == "measured" && kind == "boolean" ? Get<bool>(value) : null,
                                Components: Side(field["components"], side) is { } components ? Get<string[]>(components) : null);
                        }
                        var e = ReadField("compose");
                        var a = ReadField("avalonia");
                        var status = Text(field["status"]);
                        double[]? delta = field["delta"] is { } d ? d is JsonArray ? Get<double[]>(d) : [Get<double>(d)]
                            : status == "equal" && e?.Numbers is { } numbers ? new double[numbers.Length] : null;
                        fields.Add(new(name, status, e, a, delta, field["reason"]?.GetValue<string>(),
                            Get<ComparisonIssue?>(field["issue"])));
                    }
                }
                var er = Side(p["role"], "compose")?.GetValue<string>();
                var ar = Side(p["role"], "avalonia")?.GetValue<string>();
                var ep = Side(p["parent"], "compose")?.GetValue<string>();
                var ap = Side(p["parent"], "avalonia")?.GetValue<string>();
                points.Add(new(pid, er ?? ar, ep ?? ap, Text(p["status"]), fields.OrderBy(f => f.Name).ToArray(),
                    p["reason"]?.GetValue<string>(), er, ar, ep, ap, Get<ComparisonIssue?>(p["issue"])));
            }
            var rasters = new List<RasterDifference>();
            foreach (var (rid, node) in s["rasters"]!.AsObject())
            {
                var r = node!.AsObject();
                RasterObservation? Raster(string side)
                {
                    if (!Get<string[]>(r["present"]).Contains(side)) return null;
                    return new(rid, Text(Side(r["image"], side)),
                        Get<int>(Side(r["width"], side)), Get<int>(Side(r["height"], side)),
                        Text(Side(r["format"], side)), Text(Side(r["space"], side)), Text(Side(r["contribution"], side)));
                }
                rasters.Add(new(rid, Text(r["status"]), Raster("compose"), Raster("avalonia"),
                    Get<PixelMetrics?>(r["metrics"]), Get<RasterRegion[]?>(r["regions"]), r["diffImage"]?.GetValue<string>(),
                    r["reason"]?.GetValue<string>(), Get<ComparisonIssue?>(r["issue"]),
                    r["exactStatus"]?.GetValue<string>(), Get<RasterTolerance?>(r["tolerance"])));
            }
            var sample = new SampleDifference(id, Get<long?>(Side(s["timeUs"], "compose")), Get<long?>(Side(s["timeUs"], "avalonia")),
                (Side(s["stage"], "compose") ?? Side(s["stage"], "avalonia"))?.GetValue<string>(), Text(s["status"]),
                points.ToArray(), rasters.ToArray(), s["reason"]?.GetValue<string>(),
                Side(s["stage"], "compose")?.GetValue<string>(), Side(s["stage"], "avalonia")?.GetValue<string>(),
                Get<long?>(Side(s["frameTimeUs"], "compose")), Get<long?>(Side(s["frameTimeUs"], "avalonia")),
                Get<ComparisonIssue?>(s["issue"]));
            samples.Add(sample);
            foreach (var side in new[] { "compose", "avalonia" })
            {
                traces[side].AddRange(Get<TraceEvent[]?>(s["trace"]?[side]) ?? []);
                if (!Get<string[]>(s["present"]).Contains(side)) continue;
                var expected = side == "compose";
                captures[side].Add(new(id, Get<long>(Side(s["timeUs"], side)), Text(Side(s["stage"], side)),
                    points.Where(p => (expected ? p.ExpectedRole : p.ActualRole) is not null).Select(p => new ProbePoint(p.Id,
                        (expected ? p.ExpectedRole : p.ActualRole)!, expected ? p.ExpectedParentId : p.ActualParentId,
                        p.Fields.Select(f => expected ? f.Expected : f.Actual).OfType<Measurement>().ToArray())).ToArray(),
                    rasters.Select(r => expected ? r.Expected : r.Actual).OfType<RasterObservation>().ToArray(),
                    Get<long?>(Side(s["frameTimeUs"], side)), Get<RasterCollection[]?>(Side(s["rasterCollection"], side))));
            }
        }

        Capture? Capture(string side)
        {
            if (document["captures"]?[side] is not JsonObject header) return null;
            var order = Get<string[]>(header["sampleOrder"]);
            var capture = new Capture(4, Text(header["runner"]), Text(header["caseId"]),
                Text(header["metadataSha256"]), Text(header["baselineSha256"]), Get<Dictionary<string, string>>(environment[side]),
                order.Select(id => captures[side].Single(s => s.Id == id)).ToArray(),
                traces[side].Concat(Get<TraceEvent[]>(header["traceTail"])).OrderBy(t => t.Sequence).ToArray(), Get<string[]>(header["errors"]));
            capture.Validate();
            foreach (var raster in capture.Samples.SelectMany(s => s.Rasters))
                _ = PixelData.Read(directory, raster);
            return capture;
        }
        var expectedCapture = Capture("compose");
        var actualCapture = Capture("avalonia");
        var environments = ParityComparison.Keys(expectedCapture?.Environment.Keys, actualCapture?.Environment.Keys)
            .Select(key => ParityComparison.Field(key,
                expectedCapture?.Environment.TryGetValue(key, out var e) == true ? Measurement.Text(key, e) : null,
                actualCapture?.Environment.TryGetValue(key, out var a) == true ? Measurement.Text(key, a) : null)).ToArray();
        var statuses = samples.SelectMany(s => s.Points.Length == 0 && s.Rasters.Length == 0 ? ["notCompared"]
            : s.Points.SelectMany(p => p.Fields.Length == 0 ? ["notCompared"] : p.Fields.Select(f => f.Status))
                .Concat(s.Rasters.Select(r => r.Status)));
        var comparison = new Comparison(2, metadata.Id, Text(document["status"]), Counts(statuses), samples.ToArray(),
            Get<string[]>(document["errors"]), environments, Text(document["kind"]));
        return new(metadata, comparison, expectedCapture, actualCapture, files, document["repeatability"]?.DeepClone());
    }

    public static string Overview(CaseResult result)
    {
        var c = result.Comparison;
        var b = new StringBuilder($"# {Escape(result.Metadata.Id)} — {Escape(result.Metadata.Title)}\n\n");
        var repeatability = result.Repeatability?["results"]?.AsObject();
        var stability = repeatability is null ? "not checked" : repeatability.Any(p => p.Value!.AsArray().Count != 0) ? "changed" : "stable";
        b.AppendLine($"Status: {c.Status}; execution errors: {c.Errors.Length}; repeatability: {stability}.");
        b.AppendLine("Category cells: different / notCompared. Counts are observations, not defects or completion percentages.");
        b.AppendLine("Raster: = exact, ≈ within tolerance (1x box average; max ≤20 and mean ≤0.2 in full image and every non-empty point region), ≠ different, ? not compared. Raw 4x differences remain in sample files.");
        b.AppendLine("\n| Sample | Time µs (C / A if different) | Status | Geometry | Text | Properties | Motion | Mixed | Raster | Detail |");
        b.AppendLine("|---|---:|---|---:|---:|---:|---:|---:|---|---|");
        foreach (var s in c.Samples)
        {
            string Count(string category)
            {
                var n = Counts(s.Points.SelectMany(p => p.Fields).Where(f => Category(f) == category).Select(f => f.Status));
                return n.Different + " / " + n.NotCompared;
            }
            string RasterState()
            {
                if (s.Rasters.Length != 0)
                {
                    var counts = Counts(s.Rasters.Select(r => r.Status));
                    return $"{counts.Equal} = / {counts.WithinTolerance} ≈ / {counts.Different} ≠ / {counts.NotCompared} ?";
                }
                string SideState(Capture? capture) => capture?.Samples.SingleOrDefault(x => x.Id == s.Id)?.RasterCollection is { Length: > 0 } collection
                    ? string.Join(", ", collection.Select(r => r.Id + ":" + r.Status)) : "unrecorded";
                var e = SideState(result.Expected); var a = SideState(result.Actual);
                return e == a ? e : $"C {e}; A {a}";
            }
            string Time(long? t) => t?.ToString(CultureInfo.InvariantCulture) ?? "absent";
            var time = s.ExpectedTimeUs == s.ActualTimeUs ? Time(s.ExpectedTimeUs) : Time(s.ExpectedTimeUs) + " / " + Time(s.ActualTimeUs);
            b.AppendLine($"| {s.Id} | {time} | {s.Status} | {string.Join(" | ", Categories.Select(Count))} | {Escape(RasterState())} | [{s.Id}]({result.Samples[s.Id]}) |");
        }
        if (c.Errors.Length != 0)
        {
            b.AppendLine("\nExecution errors:");
            foreach (var error in c.Errors) b.AppendLine("- " + Escape(error));
        }
        var empty = c.Samples.Sum(s => s.Points.Count(p => p.Fields.Length == 0)) + c.Samples.Count(s => s.Points.Length == 0 && s.Rasters.Length == 0);
        if (empty != 0) b.AppendLine($"\nEmpty points/samples: {empty}; their missing coverage is not represented by category field counts.");
        b.AppendLine("\nSample files include both values, measurement definitions/sources, collection decisions and preceding trace events.");
        b.AppendLine("[Case metadata, cleanup trace and repeatability](case.yaml) · [Environment](environment.yaml)");
        return b.ToString();
    }

    public static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("|", "&#124;").Replace("\r", "").Replace("\n", "<br>");

    static string Inside(string directory, string relative)
    {
        var root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(directory, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path outside result directory.");
        return path;
    }

    static IEnumerable<string> Files(string directory) => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
        .Select(p => Path.GetRelativePath(directory, p).Replace('\\', '/'))
        .Where(p => p is not ("manifest.json" or "overview.md")).Order();

    public static void Seal(string directory) => ParityJson.Write(Path.Combine(directory, "manifest.json"),
        new { schemaVersion = 8, files = Files(directory).ToDictionary(p => p, p => ParityJson.Hash(Inside(directory, p))) });

    public static void Verify(string directory)
    {
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")))!.AsObject();
        if (Get<int>(manifest["schemaVersion"]) != 8) throw new InvalidDataException("Result manifest schema 8 required.");
        var files = Get<Dictionary<string, string>>(manifest["files"]);
        if (!files.ContainsKey("case.yaml") || !files.ContainsKey("environment.yaml") || !Files(directory).SequenceEqual(files.Keys.Order()))
            throw new InvalidDataException("Result file set differs from manifest.");
        foreach (var (file, hash) in files)
            if (ParityJson.Hash(Inside(directory, file)) != hash) throw new InvalidDataException("Result changed: " + file);
    }
}
