using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Headless;

namespace Material3Expressive.Parity;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var root = FindRoot();
            var parity = Path.Combine(root, "tests/VisualParity");
            var command = args.FirstOrDefault() ?? "help";
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] == "--refresh-reference") continue;
                if (args[i] is not ("--case" or "--family" or "--output" or "--directory"))
                    throw new ArgumentException($"Unknown argument: {args[i]}");
                if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("Missing option value.");
            }
            string? Option(string key)
            {
                var index = Array.IndexOf(args, key);
                if (index < 0) return null;
                if (index + 1 >= args.Length || args[index + 1].StartsWith("--")) throw new ArgumentException($"Missing value for {key}");
                return args[index + 1];
            }
            if (command == "verify-reference")
            {
                var directory = Option("--directory") ?? throw new ArgumentException("--directory is required");
                ParityJson.Verify(directory);
                var capture = ParityJson.Read<Capture>(Path.Combine(directory, "observations.json"));
                if (capture.Runner != "compose" || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                    throw new InvalidDataException("Reference runner/baseline mismatch.");
                Console.WriteLine("Reference integrity and baseline verified."); return 0;
            }
            if (command == "inspect")
            {
                var report = Path.Combine(Option("--directory") ?? throw new ArgumentException("--directory is required"), "report.html");
                if (!File.Exists(report)) throw new FileNotFoundException(report);
                Console.WriteLine(Path.GetFullPath(report)); return 0;
            }
            var inputs = Directory.EnumerateFiles(Path.Combine(parity, "scenarios"), "*.json").Order()
                .Select(path => (Path: path, Scenario: ParityJson.Read<Scenario>(path)))
                .Where(x => Option("--case") is not { } id || x.Scenario.Id == id)
                .Where(x => Option("--family") is not { } family || x.Scenario.Component == family).ToArray();
            foreach (var (_, scenario) in inputs) scenario.Validate();
            if (inputs.Select(x => x.Scenario.Id).Distinct().Count() != inputs.Length)
                throw new InvalidDataException("Scenario ids must be unique.");
            if (command == "list")
            {
                foreach (var (_, s) in inputs) Console.WriteLine($"{s.Id,-42} {s.Component,-12} {s.FramesUs.Length} frames / {s.Checkpoints.Length} captures");
                return inputs.Length == 0 ? 2 : 0;
            }
            if (command is not ("run" or "self-test" or "capture-actual"))
            {
                Console.WriteLine("parity list|run|self-test|capture-actual [--case ID|--family FAMILY] [--output DIRECTORY] [--refresh-reference]\nparity verify-reference|inspect --directory DIRECTORY");
                return command == "help" ? 0 : 2;
            }
            if (inputs.Length == 0) throw new ArgumentException("No scenarios matched.");
            var output = Path.GetFullPath(Option("--output") ?? Path.Combine(root, "artifacts/visual-parity",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]));
            if (Directory.Exists(output)) throw new IOException("Output must be a new directory.");
            Directory.CreateDirectory(output);
            var profile = ParityJson.Read<ComparisonProfile>(Path.Combine(parity, "profiles.json"));
            profile.Validate();
            var outcomes = new List<Comparison>();
            await using var session = HeadlessUnitTestSession.StartNew(typeof(ParityAppBuilder));
            foreach (var (input, scenario) in inputs)
            {
                var directory = Path.Combine(output, scenario.Id);
                Directory.CreateDirectory(directory);
                Comparison comparison;
                try
                {
                    var captureInput = Path.Combine(directory, "scenario.json");
                    ParityJson.Write(captureInput, scenario);
                    File.Copy(Path.Combine(parity, "profiles.json"), Path.Combine(directory, "profile.json"));
                    if (scenario.Density != 1 || scenario.FontScale != 1)
                    {
                        comparison = new(scenario.Id, "UNVERIFIED", [new("UNVERIFIED", -1, "environment", "scaling",
                            Detail: "The current paired runners support density=1 and fontScale=1 only.")]);
                        Report.Write(directory, scenario, comparison, null, null);
                        ParityJson.Write(Path.Combine(directory, "comparison.json"), comparison);
                        Console.WriteLine($"UNVERIFIED {scenario.Id} (unsupported scaling)");
                        outcomes.Add(comparison); continue;
                    }
                    if (command == "capture-actual")
                    {
                        using var captureTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        var captured = await session.Dispatch(() => AvaloniaCapture.RunAsync(scenario, captureInput,
                            Path.Combine(directory, "actual"), root), captureTimeout.Token);
                        ParityJson.Verify(Path.Combine(directory, "actual"));
                        comparison = new(scenario.Id, captured.Errors.Length == 0 ? "UNVERIFIED" : "ERROR",
                            captured.Errors.Length == 0 ? [new("UNVERIFIED", -1, "reference", "notCaptured", Detail: "Actual-only capture; no Compose parity verdict.")]
                                : captured.Errors.Select(x => new Finding("ERROR", -1, "runner", "failure", Detail: x)).ToArray());
                        Report.Write(directory, scenario, comparison, null, captured);
                        ParityJson.Write(Path.Combine(directory, "comparison.json"), comparison);
                        outcomes.Add(comparison); Console.WriteLine($"{comparison.Status,-10} {scenario.Id} (actual captured)"); continue;
                    }
                    var reference = await Reference(root, captureInput, args.Contains("--refresh-reference"));
                    CopyBundle(reference, Path.Combine(directory, "expected"));
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    var actual = await session.Dispatch(() => AvaloniaCapture.RunAsync(scenario, captureInput,
                        Path.Combine(directory, "actual"), root), timeout.Token);
                    ParityJson.Verify(Path.Combine(directory, "actual"));
                    var expected = ParityJson.Read<Capture>(Path.Combine(reference, "observations.json"));
                    if (command == "self-test")
                    {
                        var freshReference = await Reference(root, captureInput, true);
                        CopyBundle(freshReference, Path.Combine(directory, "expected-repeat"));
                        using var repeatTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        var repeatActualDirectory = Path.Combine(directory, "actual-repeat");
                        await session.Dispatch(() => AvaloniaCapture.RunAsync(scenario, captureInput, repeatActualDirectory, root), repeatTimeout.Token);
                        var dense = scenario with { Checkpoints = Enumerable.Range(0, scenario.FramesUs.Length).ToArray() };
                        var denseInput = Path.Combine(directory, "scenario-dense.json");
                        ParityJson.Write(denseInput, dense);
                        var denseReference = await Reference(root, denseInput, false);
                        CopyBundle(denseReference, Path.Combine(directory, "expected-dense"));
                        using var denseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        var denseActualDirectory = Path.Combine(directory, "actual-dense");
                        await session.Dispatch(() => AvaloniaCapture.RunAsync(dense, denseInput, denseActualDirectory, root), denseTimeout.Token);
                        var findings = Repeatability(Path.Combine(directory, "expected"), freshReference)
                            .Concat(Repeatability(Path.Combine(directory, "actual"), repeatActualDirectory))
                            .Concat(CheckpointStability(Path.Combine(directory, "expected"), denseReference))
                            .Concat(CheckpointStability(Path.Combine(directory, "actual"), denseActualDirectory)).ToArray();
                        comparison = new(scenario.Id, findings.Length == 0 ? "PASS" : "ERROR", findings, "repeatability");
                    }
                    else
                    {
                        comparison = ParityComparison.Run(scenario, expected, actual, profile,
                            Path.Combine(directory, "expected"), Path.Combine(directory, "actual"), directory);
                        // Animated captures must be stable independently of renderer scheduling
                        // and checkpoint density before their differences can be trusted.
                        if (scenario.Events.Length != 0)
                        {
                            using var repeatTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                            var repeated = Path.Combine(directory, "actual-repeat");
                            await session.Dispatch(() => AvaloniaCapture.RunAsync(scenario, captureInput, repeated, root), repeatTimeout.Token);
                            var dense = scenario with { Checkpoints = Enumerable.Range(0, scenario.FramesUs.Length).ToArray() };
                            var denseInput = Path.Combine(directory, "scenario-dense.json");
                            ParityJson.Write(denseInput, dense);
                            using var denseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                            var denseDirectory = Path.Combine(directory, "actual-dense");
                            await session.Dispatch(() => AvaloniaCapture.RunAsync(dense, denseInput, denseDirectory, root), denseTimeout.Token);
                            var stability = Repeatability(Path.Combine(directory, "actual"), repeated)
                                .Concat(CheckpointStability(Path.Combine(directory, "actual"), denseDirectory)).ToArray();
                            if (stability.Length != 0)
                                comparison = comparison with { Status = "ERROR", Findings = [.. comparison.Findings, .. stability] };
                        }
                    }
                    Report.Write(directory, scenario, comparison, expected, actual);
                }
                catch (Exception error)
                {
                    comparison = new(scenario.Id, "ERROR", [new("ERROR", -1, "infrastructure", "exception", Detail: error.ToString())],
                        command == "self-test" ? "repeatability" : "parity");
                    Report.Write(directory, scenario, comparison, null, null);
                }
                ParityJson.Write(Path.Combine(directory, "comparison.json"), comparison);
                Console.WriteLine($"{comparison.Status,-10} {scenario.Id} ({comparison.Findings.Length} findings)");
                outcomes.Add(comparison);
            }
            ParityJson.Write(Path.Combine(output, "summary.json"), outcomes);
            Report.Suite(output, outcomes);
            Console.WriteLine($"Report: {Path.Combine(output, "report.html")}");
            return outcomes.Any(x => x.Status == "ERROR") ? 2 : outcomes.Any(x => x.Status == "FAIL") ? 1
                : outcomes.Any(x => x.Status == "UNVERIFIED") ? 3 : 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 2; }
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
    public static Finding[] Repeatability(string first, string second)
    {
        ParityJson.Verify(first); ParityJson.Verify(second);
        var a = ParityJson.Read<Capture>(Path.Combine(first, "observations.json"));
        var b = ParityJson.Read<Capture>(Path.Combine(second, "observations.json"));
        var failures = new List<Finding>();
        if (a.Errors.Length != 0 || b.Errors.Length != 0) failures.Add(new("ERROR", -1, "repeatability", "runnerErrors"));
        if (ParityJson.Hash(Path.Combine(first, "observations.json")) != ParityJson.Hash(Path.Combine(second, "observations.json")))
            failures.Add(new("ERROR", -1, "repeatability", "observations", Detail: "Identical input changed observations or environment."));
        foreach (var frame in a.Frames)
        foreach (var path in new[] { frame.Image, frame.WithoutTextImage }.OfType<string>())
        {
            using var e = SkiaSharp.SKBitmap.Decode(Path.Combine(first, path));
            using var actual = SkiaSharp.SKBitmap.Decode(Path.Combine(second, path));
            if (e is null || actual is null || e.Width != actual.Width || e.Height != actual.Height
                || ImageComparison.Compare(e, actual, SkiaSharp.SKColors.White, 0).DifferentPixels != 0)
                failures.Add(new("ERROR", frame.Index, "repeatability", "pixels", Detail: $"{a.Runner}: decoded pixels changed without input changes ({path})."));
        }
        return failures.ToArray();
    }
    public static Finding[] CheckpointStability(string sparse, string dense)
    {
        ParityJson.Verify(sparse); ParityJson.Verify(dense);
        var a = ParityJson.Read<Capture>(Path.Combine(sparse, "observations.json"));
        var b = ParityJson.Read<Capture>(Path.Combine(dense, "observations.json"));
        var failures = new List<Finding>();
        if (a.Errors.Length != 0 || b.Errors.Length != 0 || a.Frames.Length != b.Frames.Length)
            return [new("ERROR", -1, "checkpointStability", "capture", Detail: "Dense capture failed or changed frame count.")];
        for (var i = 0; i < a.Frames.Length; i++)
        {
            var x = a.Frames[i] with { Image = null, WithoutTextImage = null };
            var y = b.Frames[i] with { Image = null, WithoutTextImage = null };
            if (System.Text.Json.JsonSerializer.Serialize(x, ParityJson.Options) != System.Text.Json.JsonSerializer.Serialize(y, ParityJson.Options))
                failures.Add(new("ERROR", i, "checkpointStability", "observations", Detail: $"{a.Runner}: adding screenshots changed samples/layout/behavior."));
            foreach (var path in new[] { a.Frames[i].Image, a.Frames[i].WithoutTextImage }.OfType<string>())
            {
                using var e = SkiaSharp.SKBitmap.Decode(Path.Combine(sparse, path));
                using var actual = SkiaSharp.SKBitmap.Decode(Path.Combine(dense, path));
                if (e is null || actual is null || e.Width != actual.Width || e.Height != actual.Height
                    || ImageComparison.Compare(e, actual, SkiaSharp.SKColors.White, 0).DifferentPixels != 0)
                    failures.Add(new("ERROR", i, "checkpointStability", "pixels", Detail: $"{a.Runner}: adding screenshots changed a shared checkpoint."));
            }
        }
        return failures.ToArray();
    }
    private static async Task<string> Reference(string root, string input, bool refresh)
    {
        var library = Path.Combine(root, "tests/VisualParity/reference/build/install/Material3ParityReference/lib");
        if (!Directory.Exists(library)) throw new DirectoryNotFoundException("Build the Compose producer with tests/VisualParity/parity.ps1 first.");
        var runtime = await ProcessOutput("java", ["-version"]);
        var files = Directory.EnumerateFiles(library).Concat(new[] { input, Path.Combine(root, "docs/upstream-baseline.json"),
            Path.Combine(root, "tests/VisualParity/reference/gradle.lockfile"),
            Path.Combine(root, "tests/VisualParity/reference/gradle/verification-metadata.xml"),
            Path.Combine(root, "tests/VisualParity/reference/build.gradle.kts"),
            Path.Combine(root, "tests/VisualParity/reference/gradle/wrapper/gradle-wrapper.properties"),
            Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto.ttf"),
            Path.Combine(root, "modules/MaterialSymbols/assets/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf") });
        var identity = string.Join("\n", files.Order().Select(x => Path.GetFileName(x) + ":" + ParityJson.Hash(x))) + runtime
            + System.Runtime.InteropServices.RuntimeInformation.OSDescription + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var cache = Path.Combine(root, "artifacts/visual-parity-reference", key);
        if (Directory.Exists(cache) && !refresh)
        {
            ParityJson.Verify(cache);
            var capture = ParityJson.Read<Capture>(Path.Combine(cache, "observations.json"));
            if (capture.Runner != "compose" || capture.Errors.Length != 0 || capture.ScenarioSha256 != ParityJson.Hash(input)
                || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                throw new InvalidDataException("Cached reference identity/failure mismatch.");
            return cache;
        }
        var fresh = cache + "." + Guid.NewGuid().ToString("N");
        Console.WriteLine("Compose capture: " + ParityJson.Read<Scenario>(input).Id);
        await ProcessOutput("java", ["-Djava.awt.headless=true", "-cp", Path.Combine(library, "*"), "parity.MainKt", input, fresh, root]);
        ParityJson.Verify(fresh);
        var result = ParityJson.Read<Capture>(Path.Combine(fresh, "observations.json"));
        if (result.Errors.Length != 0) throw new InvalidDataException("Compose runner failed: " + string.Join("\n", result.Errors));
        if (Directory.Exists(cache)) return fresh; // Refresh is preserved independently for repeatability review.
        try { Directory.Move(fresh, cache); }
        catch (IOException) when (Directory.Exists(cache)) { return fresh; }
        return cache;
    }
    private static async Task<string> ProcessOutput(string executable, string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Could not start " + executable);
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Reference process timed out."); }
        var text = await stdout + await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} exited {process.ExitCode}: {text}");
        return text;
    }
    private static void CopyBundle(string source, string target)
    {
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination);
        }
    }
}
