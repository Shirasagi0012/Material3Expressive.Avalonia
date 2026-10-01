using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Material3Expressive.Parity;

public static class ReferenceProducer
{
    public static async Task<string> Capture(string root, string input, bool refresh, bool extraImages = false)
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
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var cache = Path.Combine(root, "artifacts/visual-parity-reference", key);
        if (Directory.Exists(cache) && !refresh)
        {
            ParityJson.Verify(cache);
            var capture = ParityJson.Read<Capture>(Path.Combine(cache, "observations.json"));
            if (capture.Runner != "compose" || capture.Errors.Length != 0 || capture.MetadataSha256 != ParityJson.Hash(input)
                || capture.BaselineSha256 != ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")))
                throw new InvalidDataException("Cached reference identity/failure mismatch.");
            return cache;
        }

        var fresh = cache + "." + Guid.NewGuid().ToString("N");
        Console.WriteLine("Compose capture: " + ParityJson.Read<CaseMetadata>(input).Id);
        var arguments = new List<string>
            { "-Djava.awt.headless=true", "-cp", Path.Combine(library, "*"), "parity.MainKt", input, fresh, root };
        if (extraImages) arguments.Add("--extra-images");
        await ProcessOutput("java", arguments.ToArray());
        ParityJson.Verify(fresh);
        var result = ParityJson.Read<Capture>(Path.Combine(fresh, "observations.json"));
        if (result.Errors.Length != 0) return fresh; // Failed captures remain reviewable, never cached as successful references.
        if (Directory.Exists(cache)) return fresh; // Refresh is preserved independently for repeatability review.
        try
        {
            Directory.Move(fresh, cache);
        }
        catch (IOException) when (Directory.Exists(cache))
        {
            return fresh;
        }

        return cache;
    }

    private static async Task<string> ProcessOutput(string executable, string[] arguments)
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

        var text = await stdout + await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} exited {process.ExitCode}: {text}");
        return text;
    }

    public static void CopyBundle(string source, string target)
    {
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(path, destination);
        }
    }
}