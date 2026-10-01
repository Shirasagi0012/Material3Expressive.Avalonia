using System.Text.Json;
using Avalonia.Headless;
namespace Material3Expressive.Parity;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var root = FindRoot(); var parity = Path.Combine(root, "tests/VisualParity"); var command = args.FirstOrDefault() ?? "help";
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--refresh-reference" or "--require-equal") continue;
                if (args[i] is not ("--case" or "--family" or "--output" or "--directory" or "--sample" or "--raster" or "--x" or "--y"))
                    throw new ArgumentException("Unknown argument: " + args[i]);
                if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("Missing option value.");
            }
            string? Option(string key) { var i = Array.IndexOf(args, key); return i < 0 ? null : args[i + 1]; }
            if (command == "verify-reference")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required"); ParityJson.Verify(directory);
                var capture = ParityJson.Read<Capture>(Path.Combine(directory, "observations.json"));
                if (capture.Runner != "compose" || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                    throw new InvalidDataException("Reference runner/baseline differs.");
                Console.WriteLine("Reference integrity and baseline verified."); return 0;
            }
            if (command == "compare")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required");
                var input = Path.Combine(directory, "case.json");
                var caseMetadata = ParityJson.Read<CaseMetadata>(input); caseMetadata.Validate();
                Capture? Read(string side)
                {
                    var path = Path.Combine(directory, side);
                    if (!Directory.Exists(path)) return null;
                    ParityJson.Verify(path);
                    var capture = ParityJson.Read<Capture>(Path.Combine(path, "observations.json"));
                    if (capture.MetadataSha256 != ParityJson.Hash(input)) throw new InvalidDataException("Capture metadata hash differs from case.json.");
                    return capture;
                }
                var expected = Read("expected"); var actual = Read("actual");
                if (expected is null && actual is null) throw new InvalidDataException("No capture bundles found.");
                var comparison = ParityComparison.Run(caseMetadata, expected, actual, Path.Combine(directory, "expected"), Path.Combine(directory, "actual"), directory);
                ParityJson.Write(Path.Combine(directory, "comparison.json"), comparison);
                Report.Write(directory, caseMetadata, comparison, expected, actual);
                Console.WriteLine(Path.GetFullPath(Path.Combine(directory, "report.html")));
                return comparison.Errors.Length != 0 ? 2 : args.Contains("--require-equal")
                    ? comparison.Coverage.Different != 0 ? 1 : comparison.Coverage.NotCompared != 0 ? 3 : 0 : 0;
            }
            if (command == "inspect")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required");
                if (Option("--x") is { } x)
                {
                    var sample = Option("--sample") ?? throw new ArgumentException("--sample is required");
                    var px = int.Parse(x); var py = int.Parse(Option("--y") ?? throw new ArgumentException("--y is required"));
                    var rasterId = Option("--raster") ?? "full";
                    int[]? ReadPixel(string side)
                    {
                        var path = Path.Combine(directory, side);
                        if (!Directory.Exists(path)) return null;
                        ParityJson.Verify(path);
                        var capture = ParityJson.Read<Capture>(Path.Combine(path, "observations.json"));
                        var raster = capture.Samples.SingleOrDefault(s => s.Id == sample)?.Rasters.SingleOrDefault(r => r.Id == rasterId);
                        if (raster is null || px < 0 || py < 0 || px >= raster.Width || py >= raster.Height) return null;
                        return PixelData.Read(path, raster).Skip((py * raster.Width + px) * 4).Take(4).Select(v => (int)v).ToArray();
                    }
                    var expected = ReadPixel("expected"); var actual = ReadPixel("actual");
                    var comparison = ParityJson.Read<Comparison>(Path.Combine(directory, "comparison.json"));
                    var paired = comparison.Samples.SingleOrDefault(s => s.Id == sample)?.Rasters.SingleOrDefault(r => r.Id == rasterId)?.Status is "equal" or "different";
                    var delta = paired && expected is not null && actual is not null ? actual.Zip(expected, (a, e) => a - e).ToArray() : null;
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        sample,
                        raster = rasterId,
                        x = px,
                        y = py,
                        space = "scene-px",
                        format = "straight-srgba8",
                        expected,
                        actual,
                        status = delta is null ? "notCompared" : delta.All(x => x == 0) ? "equal" : "different",
                        delta,
                        absoluteDelta = delta?.Select(Math.Abs).ToArray()
                    }, ParityJson.Options));
                }
                else
                {
                    var report = Path.Combine(directory, "report.html");
                    if (!File.Exists(report)) throw new FileNotFoundException(report);
                    Console.WriteLine(Path.GetFullPath(report));
                }
                return 0;
            }
            if (command is not ("list" or "run" or "self-test" or "capture-actual"))
            {
                Console.WriteLine("parity list|run|self-test|capture-actual [--case ID|--family FAMILY] [--output NEW-DIRECTORY] [--refresh-reference] [--require-equal]\nparity compare|verify-reference|inspect --directory DIRECTORY [--sample ID --x X --y Y]");
                return command == "help" ? 0 : 2;
            }
            var metadata = Directory.EnumerateFiles(Path.Combine(parity, "cases"), "*.json").Order().Select(ParityJson.Read<CaseMetadata>).ToArray();
            foreach (var m in metadata) { m.Validate(); if (!Cases.Entries.ContainsKey(m.AvaloniaEntry)) throw new InvalidDataException("Unknown Avalonia entry: " + m.AvaloniaEntry); }
            if (metadata.Select(m => m.Id).Distinct().Count() != metadata.Length) throw new InvalidDataException("Duplicate case ids.");
            var selected = metadata.Where(m => Option("--case") is not { } id || m.Id == id).Where(m => Option("--family") is not { } family || m.Family == family).ToArray();
            if (selected.Length == 0) throw new ArgumentException("No cases matched.");
            if (command == "list") { foreach (var m in selected) Console.WriteLine($"{m.Id,-30} {m.ComposeEntry} / {m.AvaloniaEntry}"); return 0; }
            var output = Path.GetFullPath(Option("--output") ?? Path.Combine(root, "artifacts/visual-parity", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]));
            if (Directory.Exists(output)) throw new IOException("Output must be a new directory."); Directory.CreateDirectory(output);
            var outcomes = new List<Comparison>(); var unstable = false;
            await using var session = HeadlessUnitTestSession.StartNew(typeof(ParityAppBuilder));
            foreach (var m in selected)
            {
                var directory = Path.Combine(output, m.Id); Directory.CreateDirectory(directory); var input = Path.Combine(directory, "case.json"); ParityJson.Write(input, m);
                Capture? expected = null; Capture? actual = null; Comparison comparison;
                try
                {
                    if (command != "capture-actual")
                    {
                        var reference = await ReferenceProducer.Capture(root, input, args.Contains("--refresh-reference"));
                        ReferenceProducer.CopyBundle(reference, Path.Combine(directory, "expected"));
                        expected = ParityJson.Read<Capture>(Path.Combine(directory, "expected/observations.json"));
                    }
                    async Task<Capture> Actual(string target, bool extra = false)
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        var result = await session.Dispatch(async () =>
                        {
                            var writer = new CaptureWriter(m, input, target, root, extra);
                            try { await Cases.Entries[m.AvaloniaEntry](writer); } catch (Exception error) { writer.Errors.Add(error.ToString()); }
                            return writer.Finish();
                        }, timeout.Token);
                        ParityJson.Verify(target); return result;
                    }
                    actual = await Actual(Path.Combine(directory, "actual"));
                    comparison = ParityComparison.Run(m, expected, actual, Path.Combine(directory, "expected"), Path.Combine(directory, "actual"), directory);
                    if (command == "self-test")
                    {
                        var repeatReference = await ReferenceProducer.Capture(root, input, true);
                        ReferenceProducer.CopyBundle(repeatReference, Path.Combine(directory, "expected-repeat"));
                        var denseReference = await ReferenceProducer.Capture(root, input, true, true);
                        ReferenceProducer.CopyBundle(denseReference, Path.Combine(directory, "expected-extra-images"));
                        await Actual(Path.Combine(directory, "actual-repeat")); await Actual(Path.Combine(directory, "actual-extra-images"), true);
                        var stability = new Dictionary<string, string[]>
                        {
                            ["compose.repeat"] = Stability(Path.Combine(directory, "expected"), Path.Combine(directory, "expected-repeat")),
                            ["compose.extraImages"] = Stability(Path.Combine(directory, "expected"), Path.Combine(directory, "expected-extra-images"), true),
                            ["avalonia.repeat"] = Stability(Path.Combine(directory, "actual"), Path.Combine(directory, "actual-repeat")),
                            ["avalonia.extraImages"] = Stability(Path.Combine(directory, "actual"), Path.Combine(directory, "actual-extra-images"), true)
                        };
                        unstable |= stability.Values.Any(x => x.Length != 0);
                        ParityJson.Write(Path.Combine(directory, "repeatability.json"), new { kind = "repeatability", results = stability });
                        Console.WriteLine($"repeatability {m.Id}: {(stability.Values.All(x => x.Length == 0) ? "stable" : "changed")}");
                    }
                }
                catch (Exception error) { comparison = new(2, m.Id, "error", new(0, 0, 0), [], [error.ToString()], []); }
                ParityJson.Write(Path.Combine(directory, "comparison.json"), comparison); Report.Write(directory, m, comparison, expected, actual);
                outcomes.Add(comparison); Console.WriteLine($"{m.Id}: {comparison.Coverage.Equal} equal / {comparison.Coverage.Different} different / {comparison.Coverage.NotCompared} not compared; errors={comparison.Errors.Length}");
            }
            ParityJson.Write(Path.Combine(output, "summary.json"), outcomes); Report.Suite(output, outcomes); Console.WriteLine("Report: " + Path.Combine(output, "report.html"));
            return unstable || outcomes.Any(x => x.Errors.Length != 0) ? 2 : args.Contains("--require-equal")
                ? outcomes.Any(x => x.Coverage.Different != 0) ? 1 : outcomes.Any(x => x.Coverage.NotCompared != 0) ? 3 : 0 : 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 2; }
    }
    public static string FindRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null) { if (File.Exists(Path.Combine(directory.FullName, "docs/upstream-baseline.json"))) return directory.FullName; directory = directory.Parent; }
        throw new DirectoryNotFoundException("Run from inside Material3ExpressiveRefactor.");
    }
    public static string[] Stability(string first, string second, bool extraImages = false)
    {
        ParityJson.Verify(first); ParityJson.Verify(second);
        var a = ParityJson.Read<Capture>(Path.Combine(first, "observations.json")); var b = ParityJson.Read<Capture>(Path.Combine(second, "observations.json"));
        var failures = new List<string>();
        if (a.Errors.Length != 0 || b.Errors.Length != 0) failures.Add("Runner errors recorded.");
        var aa = a with { Trace = [], Samples = a.Samples.Select(s => s with { Rasters = [] }).ToArray() };
        var bb = b with { Trace = [], Samples = b.Samples.Select(s => s with { Rasters = [] }).ToArray() };
        if (JsonSerializer.Serialize(aa, ParityJson.Options) != JsonSerializer.Serialize(bb, ParityJson.Options)) failures.Add("Observations or environment changed.");
        TraceEvent[] Trace(Capture c) => c.Trace.Where(t => !extraImages || t.Operation != "capture").Select((t, i) => t with { Sequence = i }).ToArray();
        if (JsonSerializer.Serialize(Trace(a), ParityJson.Options) != JsonSerializer.Serialize(Trace(b), ParityJson.Options)) failures.Add("Execution trace changed.");
        foreach (var s in a.Samples) foreach (var r in s.Rasters)
        {
            var other = b.Samples.SingleOrDefault(x => x.Id == s.Id)?.Rasters.SingleOrDefault(x => x.Id == r.Id);
            if (other is null || (r.Width, r.Height, r.Format, r.Space, r.Contribution) != (other.Width, other.Height, other.Format, other.Space, other.Contribution)
                || !PixelData.Read(first, r).SequenceEqual(PixelData.Read(second, other))) failures.Add($"Raster changed: {s.Id}/{r.Id}");
        }
        if (!extraImages && !a.Samples.SelectMany(s => s.Rasters.Select(r => s.Id + "/" + r.Id)).SequenceEqual(b.Samples.SelectMany(s => s.Rasters.Select(r => s.Id + "/" + r.Id))))
            failures.Add("Raster set changed.");
        return failures.ToArray();
    }
}
