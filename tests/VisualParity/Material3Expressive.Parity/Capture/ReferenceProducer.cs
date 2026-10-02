using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Material3Expressive.Parity;

public static class ReferenceProducer
{
    public static async Task<ScenarioIdentity[]> DiscoverScenarios(string root)
    {
        var library = Path.Combine(root, "tests/VisualParity/reference/build/install/Material3ParityReference/lib");
        var output = await ProcessOutput("java", ["-Djava.awt.headless=true", "-cp", Path.Combine(library, "*"), "parity.MainKt", "--list-scenarios"], stdoutOnly: true);
        return JsonSerializer.Deserialize<ScenarioIdentity[]>(output, ParityJson.Options)
               ?? throw new InvalidDataException("Empty Compose scenario index.");
    }

    public static async Task<string> Capture(string root, string input, string fresh, bool refresh, bool extraImages = false)
    {
        var library = Path.Combine(root, "tests/VisualParity/reference/build/install/Material3ParityReference/lib");
        if (!Directory.Exists(library))
            throw new DirectoryNotFoundException("Build the Compose producer with tests/VisualParity/parity.ps1 first.");
        var runtime = await ProcessOutput("java", ["-version"]);
        var files = Directory.EnumerateFiles(library).Concat(new[]
        {
            input, Path.Combine(root, "docs/upstream-baseline.json"),
            Path.Combine(root, "tests/VisualParity/reference/gradle.lockfile"),
            Path.Combine(root, "tests/VisualParity/reference/gradle/verification-metadata.xml"),
            Path.Combine(root, "tests/VisualParity/reference/build.gradle.kts"),
            Path.Combine(root, "tests/VisualParity/reference/gradle/wrapper/gradle-wrapper.properties"),
            Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto.ttf"),
            Path.Combine(root, "modules/MaterialSymbols/assets/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf")
        });
        var identity = string.Join("\n", files.Order().Select(x => Path.GetFileName(x) + ":" + ParityJson.Hash(x))) + runtime
            + System.Runtime.InteropServices.RuntimeInformation.OSDescription +
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        identity += extraImages ? "extra-images" : "default-images";
        identity += "\nrender-scale=" + AvaloniaHost.RenderScale;
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var cache = Path.Combine(root, "artifacts/visual-parity/cache/reference", key);
        if (Directory.Exists(cache) && !refresh)
        {
            ParityJson.Verify(cache);
            var capture = ParityJson.Read<Capture>(Path.Combine(cache, "observations.json"));
            if (capture.Runner != "compose" || capture.Errors.Length != 0 || capture.MetadataSha256 != ParityJson.Hash(input)
                || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json"))
                || capture.Environment.GetValueOrDefault("density") != AvaloniaHost.RenderScale.ToString())
                throw new InvalidDataException("Cached reference identity/failure mismatch.");
            return cache;
        }

        if (Directory.Exists(fresh)) throw new IOException("Capture directory must be new.");
        Console.WriteLine("Compose capture: " + ParityJson.Read<CaseMetadata>(input).Id);
        var arguments = new List<string>
            { "-Djava.awt.headless=true", "-cp", Path.Combine(library, "*"), "parity.MainKt", input, fresh, root };
        if (extraImages) arguments.Add("--extra-images");
        await ProcessOutput("java", arguments.ToArray());
        ParityJson.Verify(fresh);
        var result = ParityJson.Read<Capture>(Path.Combine(fresh, "observations.json"));
        if (result.Errors.Length != 0) return fresh; // Failed captures remain reviewable, never cached as successful references.
        if (refresh || Directory.Exists(cache)) return fresh; // Forced captures belong to this run, not the reusable cache.
        Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
        // --output may be on a different volume. Publish a complete cache atomically
        // on the cache's volume; the original stays in this run until export succeeds.
        var staging = cache + "." + Guid.NewGuid().ToString("N");
        try
        {
            foreach (var file in Directory.EnumerateFiles(fresh, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(staging, Path.GetRelativePath(fresh, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            try { Directory.Move(staging, cache); }
            catch (IOException) when (Directory.Exists(cache)) { return fresh; }
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }

        return cache;
    }

    private static async Task<string> ProcessOutput(string executable, string[] arguments, bool stdoutOnly = false)
    {
        var start = new ProcessStartInfo(executable)
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Could not start " + executable);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Reference process timed out.");
        }

        var standardOutput = await stdout;
        var text = standardOutput + await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} exited {process.ExitCode}: {text}");
        return stdoutOnly ? standardOutput : text;
    }
}
