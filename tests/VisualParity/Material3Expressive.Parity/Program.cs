using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Headless;

namespace Material3Expressive.Parity;

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
                if (args[i] is "--refresh-reference" or "--require-equal") continue;
                if (args[i] is not ("--case" or "--family" or "--output" or "--directory"))
                    throw new ArgumentException("Unknown argument: " + args[i]);
                if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("Missing option value.");
            }

            string? Option(string key)
            {
                var i = Array.IndexOf(args, key);
                return i < 0 ? null : args[i + 1];
            }

            if (command == "verify-reference")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required");
                ParityJson.Verify(directory);
                var capture = ParityJson.Read<Capture>(Path.Combine(directory, "observations.json"));
                if (capture.Runner != "compose" ||
                    capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                    throw new InvalidDataException("Reference runner/baseline differs.");
                Console.WriteLine("Reference integrity and baseline verified.");
                return 0;
            }

            if (command == "render")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required");
                var result = Report.Write(directory);
                Console.WriteLine(ResultFiles.Overview(result));
                Console.WriteLine("Overview: " + Path.GetFullPath(Path.Combine(directory, "overview.md")));
                return ExitCode(result.Comparison, args.Contains("--require-equal"));
            }

            if (command is not ("list" or "run" or "self-test" or "capture-actual"))
            {
                Console.WriteLine(
                    "parity list|run|self-test|capture-actual [--case ID|--family FAMILY] [--output NEW-DIRECTORY] [--refresh-reference] [--require-equal]\nparity render|verify-reference --directory DIRECTORY\nRead overview.md and samples/*.yaml directly.");
                return command == "help" ? 0 : 2;
            }

            var scenarios = ScenarioDiscovery.Discover(typeof(Program).Assembly.GetTypes());
            var metadata = ScenarioDiscovery.Expand(scenarios.Values);
            var selected = metadata.Where(m => Option("--case") is not { } id || m.Id == id)
                .Where(m => Option("--family") is not { } family || m.Suite == family).ToArray();
            if (selected.Length == 0) throw new ArgumentException("No cases matched.");
            if (command == "list")
            {
                foreach (var m in selected) Console.WriteLine($"{m.Id,-30} {m.Suite} / {m.Scenario}");
                return 0;
            }

            if (command != "capture-actual")
                ScenarioDiscovery.RequireReferenceScenarios(selected, await ReferenceProducer.DiscoverScenarios(root));

            var output = Path.GetFullPath(Option("--output") ?? Path.Combine(root, "artifacts/visual-parity",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]));
            if (Directory.Exists(output)) throw new IOException("Output must be a new directory.");
            Directory.CreateDirectory(output);
            var outcomes = new List<Comparison>();
            var unstable = false;
            var captureRoot = Path.Combine(root, "artifacts/visual-parity-captures", Guid.NewGuid().ToString("N"));
            // Freeze every selected definition before either producer starts.
            foreach (var m in selected)
                ParityJson.Write(Path.Combine(captureRoot, m.Id, "case.json"), m);
            await using var session = HeadlessUnitTestSession.StartNew(typeof(ParityAppBuilder));
            foreach (var m in selected)
            {
                var directory = Path.Combine(captureRoot, m.Id);
                var resultDirectory = Path.Combine(output, "cases", m.Id);
                JsonNode? repeatability = null;
                Directory.CreateDirectory(directory);
                var input = Path.Combine(directory, "case.json");

                Capture? expected = null;
                Capture? actual = null;
                Comparison comparison;
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
                            var frozen = ParityJson.Read<CaseMetadata>(input);
                            frozen.Validate();
                            var writer = new CaptureWriter(frozen, input, target, root, extra);
                            try
                            {
                                await scenarios[new(frozen.Suite, frozen.Scenario)].Execute(writer);
                            }
                            catch (Exception error)
                            {
                                writer.Errors.Add(error.ToString());
                            }

                            return writer.Finish();
                        }, timeout.Token);
                        ParityJson.Verify(target);
                        return result;
                    }

                    actual = await Actual(Path.Combine(directory, "actual"));
                    comparison = ParityComparison.Run(m, expected, actual, Path.Combine(directory, "expected"),
                        Path.Combine(directory, "actual"), directory);
                    if (command == "self-test")
                    {
                        var repeatReference = await ReferenceProducer.Capture(root, input, true);
                        ReferenceProducer.CopyBundle(repeatReference, Path.Combine(directory, "expected-repeat"));
                        var denseReference = await ReferenceProducer.Capture(root, input, true, true);
                        ReferenceProducer.CopyBundle(denseReference, Path.Combine(directory, "expected-extra-images"));
                        await Actual(Path.Combine(directory, "actual-repeat"));
                        await Actual(Path.Combine(directory, "actual-extra-images"), true);
                        var stability = new Dictionary<string, string[]>
                        {
                            ["compose.repeat"] = Stability(Path.Combine(directory, "expected"), Path.Combine(directory, "expected-repeat")),
                            ["compose.extraImages"] = Stability(Path.Combine(directory, "expected"),
                                Path.Combine(directory, "expected-extra-images"), true),
                            ["avalonia.repeat"] = Stability(Path.Combine(directory, "actual"), Path.Combine(directory, "actual-repeat")),
                            ["avalonia.extraImages"] = Stability(Path.Combine(directory, "actual"),
                                Path.Combine(directory, "actual-extra-images"), true)
                        };
                        unstable |= stability.Values.Any(x => x.Length != 0);
                        repeatability = ResultYaml.Node(new { kind = "repeatability", results = stability });
                        ParityJson.Write(Path.Combine(directory, "repeatability.json"), repeatability);
                        Console.WriteLine($"repeatability {m.Id}: {(stability.Values.All(x => x.Length == 0) ? "stable" : "changed")}");
                    }
                }
                catch (Exception error)
                {
                    comparison = ParityComparison.Run(m, expected, actual, Path.Combine(directory, "expected"),
                        Path.Combine(directory, "actual"), directory, executionError: error.ToString());
                }

                ResultFiles.Write(resultDirectory, m, comparison, expected, actual,
                    Path.Combine(directory, "expected"), Path.Combine(directory, "actual"), directory, repeatability);
                var result = Report.Write(resultDirectory);
                outcomes.Add(comparison);
                Console.WriteLine(ResultFiles.Overview(result));
                Console.WriteLine("Overview: " + Path.Combine(resultDirectory, "overview.md"));
            }

            Report.Suite(output);
            Console.WriteLine("Summary: " + Path.Combine(output, "summary.md"));
            Console.WriteLine("Report: " + Path.Combine(output, "report.html"));
            return unstable || outcomes.Any(x => x.Errors.Length != 0)
                ? 2
                : args.Contains("--require-equal")
                    ? outcomes.Any(x => x.Coverage.Different != 0) ? 1 : outcomes.Any(x => x.Coverage.NotCompared != 0) ? 3 : 0
                    : 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 2;
        }
    }

    static int ExitCode(Comparison comparison, bool requireEqual) => comparison.Errors.Length != 0 ? 2
        : requireEqual ? comparison.Coverage.Different != 0 ? 1 : comparison.Coverage.NotCompared != 0 ? 3 : 0 : 0;

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
        ParityJson.Verify(first);
        ParityJson.Verify(second);
        var a = ParityJson.Read<Capture>(Path.Combine(first, "observations.json"));
        var b = ParityJson.Read<Capture>(Path.Combine(second, "observations.json"));
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
