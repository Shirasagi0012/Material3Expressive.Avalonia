using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Headless;

namespace Material3Expressive.Parity;

public sealed record RunPlan(int SchemaVersion, string Command, Dictionary<string, string> RunnerEnvironment,
    ReferenceSnapshot? Reference, CaseMetadata[] Cases, string? SourceRun);

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var root = FindRoot();
            var command = args.FirstOrDefault() ?? "help";
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--refresh-reference" or "--require-equal" or "--require-match") continue;
                if (args[i] is not ("--case" or "--family" or "--output" or "--directory" or "--reference"))
                    throw new ArgumentException("Unknown argument: " + args[i]);
                if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("Missing option value.");
            }
            string? Option(string key)
            {
                var i = Array.IndexOf(args, key);
                return i < 0 ? null : args[i + 1];
            }
            if (args.Contains("--refresh-reference") && command != "prepare-reference")
                throw new ArgumentException("--refresh-reference belongs to prepare-reference. Ordinary run only consumes prepared references.");
            if (command == "verify-reference")
            {
                var capture = ParityJson.Verify(Option("--directory") ?? throw new ArgumentException("--directory is required"));
                if (capture.Runner != "compose" || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                    throw new InvalidDataException("Reference runner/baseline differs.");
                Console.WriteLine("Reference integrity and baseline verified.");
                return 0;
            }
            if (command is not ("list" or "prepare-reference" or "run" or "self-test" or "capture-actual" or "recompare"))
            {
                Console.WriteLine("parity prepare-reference [--case ID|--family FAMILY] [--refresh-reference]\n" +
                    "parity run [--case ID|--family FAMILY] [--reference SNAPSHOT.json] [--output NEW-DIRECTORY]\n" +
                    "parity list|self-test|capture-actual [--case ID|--family FAMILY] [--output NEW-DIRECTORY]\n" +
                    "parity recompare --directory RUN [--output NEW-DIRECTORY]\n" +
                    "parity verify-reference --directory CAPTURE\n" +
                    "run/recompare support --require-match and --require-equal. Read overview.md and samples/*.yaml directly.");
                return command == "help" ? 0 : 2;
            }
            if (command is "run" or "self-test" or "capture-actual" or "recompare")
                if (await BuildSnapshot.ExecuteFrozen(root, args) is { } exitCode) return exitCode;

            var startup = Stopwatch.StartNew();
            var sourceRun = command == "recompare"
                ? Path.GetFullPath(Option("--directory") ?? throw new ArgumentException("--directory is required")) : null;
            var scenarios = ScenarioDiscovery.Discover(typeof(Program).Assembly.GetTypes());
            var sourcePlan = sourceRun is null ? null : ParityJson.Read<RunPlan>(Path.Combine(sourceRun, "plan.json"));
            if (sourcePlan is not null && sourcePlan.SchemaVersion != 1) throw new InvalidDataException("Run plan schema 1 required.");
            var metadata = sourceRun is null ? ScenarioDiscovery.Expand(scenarios.Values)
                : sourcePlan!.Cases;
            foreach (var m in metadata) m.Validate();
            if (metadata.Select(m => m.Id).Distinct().Count() != metadata.Length) throw new InvalidDataException("Duplicate case in run plan.");
            var selected = metadata.Where(m => Option("--case") is not { } id || m.Id == id)
                .Where(m => Option("--family") is not { } family || m.Suite == family).ToArray();
            if (selected.Length == 0) throw new ArgumentException("No cases matched.");
            if (command == "list")
            {
                foreach (var m in selected) Console.WriteLine($"{m.Id,-30} {m.Suite} / {m.Scenario}");
                return 0;
            }
            if (command == "prepare-reference")
            {
                await ReferenceCatalog.Prepare(root, selected, args.Contains("--refresh-reference"), Option("--reference"));
                return 0;
            }
            ReferenceSnapshot? reference = null;
            if (command == "run")
            {
                var referencePath = Option("--reference") ?? ReferenceCatalog.Current(root);
                if (!File.Exists(referencePath)) throw new FileNotFoundException("Prepare references first: parity prepare-reference", referencePath);
                reference = ReferenceCatalog.Load(root, referencePath);
                var problems = ReferenceCatalog.Problems(root, reference, ReferenceCatalog.ProducerFingerprint(root), selected);
                if (problems.Length != 0) throw new InvalidDataException("Reference preflight failed:\n" + string.Join("\n", problems));
                reference = reference with { Cases = selected.ToDictionary(m => m.Id, m => reference.Cases[m.Id]) };
            }
            var output = Path.GetFullPath(Option("--output") ?? Path.Combine(root, "artifacts/visual-parity/runs",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]));
            if (Directory.Exists(output)) throw new IOException("Output must be a new directory.");
            Directory.CreateDirectory(output);
            var environment = EnvironmentInfo.Read(root);
            ParityJson.Write(Path.Combine(output, "plan.json"), new RunPlan(1, command, environment, reference, selected, sourceRun));
            var run = new RunResults(output, selected.Length, command);
            run.TimingsMs["startup"] = startup.Elapsed.TotalMilliseconds;
            run.Save("running");
            var total = Stopwatch.StartNew();
            var anyErrors = false; var anyDifferent = false; var anyTolerated = false; var anyUncompared = false; var unstable = false;
            var captureRoot = Path.Combine(output, ".work");
            foreach (var m in selected) ParityJson.Write(Path.Combine(captureRoot, m.Id, "case.json"), m);
            await using var producer = command == "self-test" ? await ReferenceProducer.Start(root) : null;
            if (producer is not null) ScenarioDiscovery.RequireReferenceScenarios(selected, producer.Scenarios);
            await using var session = sourceRun is null ? HeadlessUnitTestSession.StartNew(typeof(ParityAppBuilder)) : null;
            var completed = 0;
            foreach (var m in selected)
            {
                var caseTimer = Stopwatch.StartNew();
                var timings = new Dictionary<string, double>();
                T Measure<T>(string phase, Func<T> action)
                {
                    var watch = Stopwatch.StartNew();
                    try { return action(); }
                    finally { timings[phase] = timings.GetValueOrDefault(phase) + watch.Elapsed.TotalMilliseconds; }
                }
                async Task<T> MeasureAsync<T>(string phase, Func<Task<T>> action)
                {
                    var watch = Stopwatch.StartNew();
                    try { return await action(); }
                    finally { timings[phase] = timings.GetValueOrDefault(phase) + watch.Elapsed.TotalMilliseconds; }
                }
                var directory = Path.Combine(captureRoot, m.Id);
                var input = Path.Combine(directory, "case.json");
                var expectedDirectory = Path.Combine(directory, "expected");
                var actualDirectory = Path.Combine(directory, "actual");
                Capture? expected = null; Capture? actual = null;
                JsonNode? repeatability = null;
                var caseUnstable = false;
                string? sourceError = null;
                Comparison comparison;
                async Task<Capture> Actual(string target, bool extra = false)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    var result = await session!.Dispatch(async () =>
                    {
                        var frozen = ParityJson.Read<CaseMetadata>(input);
                        frozen.Validate();
                        var writer = new CaptureWriter(frozen, input, target, root, extra, environment);
                        try { await scenarios[new(frozen.Suite, frozen.Scenario)].Execute(writer); }
                        catch (Exception error) { writer.Errors.Add(error.ToString()); }
                        return writer.Finish();
                    }, timeout.Token);
                    ParityJson.Verify(target, decodePixels: false);
                    return result;
                }
                try
                {
                    if (sourceRun is not null)
                    {
                        var original = Path.Combine(sourceRun, "cases", m.Id);
                        var prior = Measure("readSource", () => ResultFiles.Read(original));
                        expected = prior.Expected; actual = prior.Actual;
                        if (prior.Comparison.Errors.Length != 0) sourceError = "Source execution errors: " + string.Join("\n", prior.Comparison.Errors);
                        expectedDirectory = actualDirectory = original;
                    }
                    else
                    {
                        if (reference is not null)
                        {
                            var entry = reference.Cases[m.Id];
                            expectedDirectory = ReferenceCatalog.CaptureDirectory(root, entry);
                            expected = Measure("referenceRead", () => ReferenceCatalog.ReadCapture(root, m, entry));
                        }
                        else if (producer is not null)
                            expected = await MeasureAsync("referenceCapture", () => producer.Capture(input, expectedDirectory));
                        actual = await MeasureAsync("actualCapture", () => Actual(actualDirectory));
                    }
                    comparison = Measure("comparison", () => ParityComparison.Run(m, expected, actual, expectedDirectory, actualDirectory, directory, executionError: sourceError));
                    if (producer is not null)
                    {
                        repeatability = await MeasureAsync("selfTest", async () =>
                        {
                            var repeatReference = Path.Combine(directory, "expected-repeat");
                            var denseReference = Path.Combine(directory, "expected-extra-images");
                            await producer.Capture(input, repeatReference);
                            await producer.Capture(input, denseReference, true);
                            await Actual(Path.Combine(directory, "actual-repeat"));
                            await Actual(Path.Combine(directory, "actual-extra-images"), true);
                            var stability = new Dictionary<string, string[]>
                            {
                                ["compose.repeat"] = Stability(expectedDirectory, repeatReference),
                                ["compose.extraImages"] = Stability(expectedDirectory, denseReference, true),
                                ["avalonia.repeat"] = Stability(actualDirectory, Path.Combine(directory, "actual-repeat")),
                                ["avalonia.extraImages"] = Stability(actualDirectory, Path.Combine(directory, "actual-extra-images"), true)
                            };
                            caseUnstable = stability.Values.Any(x => x.Length != 0);
                            return ResultYaml.Node(new { kind = "repeatability", results = stability });
                        });
                        unstable |= caseUnstable;
                    }
                }
                catch (Exception error)
                {
                    // Invalid/corrupt capture data must not be retried by the comparator or exported as valid evidence.
                    expected = actual = null;
                    comparison = ParityComparison.Run(m, null, null, expectedDirectory, actualDirectory, directory, executionError: error.ToString());
                }
                var keepDiagnostics = comparison.Errors.Length != 0 || caseUnstable;
                var resultDirectory = Path.Combine(output, "cases", m.Id);
                var result = Measure("export", () =>
                {
                    var staging = Path.Combine(directory, "published");
                    var value = ResultFiles.Write(staging, m, comparison, expected, actual, expectedDirectory, actualDirectory,
                        directory, repeatability, keepDiagnostics ? Path.GetRelativePath(resultDirectory, directory).Replace('\\', '/') : null);
                    Directory.CreateDirectory(Path.GetDirectoryName(resultDirectory)!);
                    Directory.Move(staging, resultDirectory);
                    return value;
                });
                if (!keepDiagnostics)
                {
                    if (Path.GetDirectoryName(Path.GetFullPath(directory)) != Path.GetFullPath(captureRoot))
                        throw new InvalidDataException("Temporary capture outside this run.");
                    Directory.Delete(directory, recursive: true);
                }
                timings["total"] = caseTimer.Elapsed.TotalMilliseconds;
                run.Add(result, timings);
                run.Save("running");
                anyErrors |= comparison.Errors.Length != 0;
                anyDifferent |= comparison.Coverage.Different != 0;
                anyTolerated |= comparison.Coverage.WithinTolerance != 0;
                anyUncompared |= comparison.Coverage.NotCompared != 0;
                Console.WriteLine($"[{++completed}/{selected.Length}] {m.Id}: {comparison.Status}; {caseTimer.Elapsed.TotalSeconds:F2}s; errors={comparison.Errors.Length}" +
                    (producer is null ? "" : $"; repeatability={(repeatability is null ? "notChecked" : caseUnstable ? "unstable" : "stable")}"));
                Console.WriteLine("Overview: " + Path.Combine(resultDirectory, "overview.md"));
            }
            if (!Directory.EnumerateFileSystemEntries(captureRoot).Any()) Directory.Delete(captureRoot);
            run.TimingsMs["wall"] = total.Elapsed.TotalMilliseconds;
            run.Save("completed");
            Console.WriteLine("Summary: " + Path.Combine(output, "summary.md"));
            Console.WriteLine("Run index: " + Path.Combine(output, "run.yaml"));
            Console.WriteLine("Seconds by phase: " + string.Join(", ", run.TimingsMs.Select(p => $"{p.Key}={p.Value / 1000:F2}")));
            return unstable || anyErrors ? 2 : args.Contains("--require-equal") || args.Contains("--require-match")
                ? anyDifferent || args.Contains("--require-equal") && anyTolerated ? 1 : anyUncompared ? 3 : 0 : 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 2; }
    }
    public static string FindRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs/upstream-baseline.json"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Run from inside Material3ExpressiveRefactor.");
    }

    public static string[] Stability(string first, string second, bool extraImages = false)
    {
        var a = ParityJson.Verify(first, decodePixels: false);
        var b = ParityJson.Verify(second, decodePixels: false);
        var failures = new List<string>();
        if (a.Errors.Length != 0 || b.Errors.Length != 0) failures.Add("Runner errors recorded.");
        var aa = a with { Trace = [], Samples = a.Samples.Select(s => s with { Rasters = [] }).ToArray() };
        var bb = b with
        {
            Trace = [], Samples = b.Samples.Select(s => s with
            {
                Rasters = [],
                RasterCollection = s.RasterCollection?.Select(r =>
                {
                    var original = a.Samples.SingleOrDefault(x => x.Id == s.Id)?.RasterCollection?.SingleOrDefault(x => x.Id == r.Id);
                    return extraImages && original?.Status == "notRequested" && r.Status == "captured" ? original : r;
                }).ToArray()
            }).ToArray()
        };
        if (JsonSerializer.Serialize(aa, ParityJson.Options) != JsonSerializer.Serialize(bb, ParityJson.Options))
            failures.Add("Observations or environment changed.");

        TraceEvent[] Trace(Capture c) => c.Trace.Where(t => !extraImages || t.Operation != "capture")
            .Select((t, i) => t with { Sequence = i }).ToArray();

        if (JsonSerializer.Serialize(Trace(a), ParityJson.Options) != JsonSerializer.Serialize(Trace(b), ParityJson.Options))
            failures.Add("Execution trace changed.");
        foreach (var s in a.Samples)
        foreach (var r in s.Rasters)
        {
            var other = b.Samples.SingleOrDefault(x => x.Id == s.Id)?.Rasters.SingleOrDefault(x => x.Id == r.Id);
            if (other is null || (r.Width, r.Height, r.Format, r.Space, r.Contribution) !=
                              (other.Width, other.Height, other.Format, other.Space, other.Contribution)
                              || !PixelData.Read(first, r).SequenceEqual(PixelData.Read(second, other)))
                failures.Add($"Raster changed: {s.Id}/{r.Id}");
        }

        if (!extraImages && !a.Samples.SelectMany(s => s.Rasters.Select(r => s.Id + "/" + r.Id))
                .SequenceEqual(b.Samples.SelectMany(s => s.Rasters.Select(r => s.Id + "/" + r.Id))))
            failures.Add("Raster set changed.");
        return failures.ToArray();
    }
}
