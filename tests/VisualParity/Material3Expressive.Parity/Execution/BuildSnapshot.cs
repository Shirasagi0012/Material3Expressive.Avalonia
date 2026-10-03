using System.Diagnostics;

namespace Material3Expressive.Parity;

// Build snapshots keep a concurrent rebuild from changing binaries underneath a running producer.
public static class BuildSnapshot
{
    public static string Create(string root, string source, string kind)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Build the producer first: " + source);
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(source, p).Replace('\\', '/'), ParityJson.Hash);
        var directory = Path.Combine(root, "artifacts/visual-parity/builds", kind, ParityJson.HashValue(files));
        if (Directory.Exists(directory))
        {
            if (Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() != files.Count ||
                files.Any(p => !File.Exists(Path.Combine(directory, p.Key)) || ParityJson.Hash(Path.Combine(directory, p.Key)) != p.Value))
                throw new InvalidDataException("Frozen build was modified: " + directory);
            return directory;
        }
        var staging = directory + "." + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        foreach (var (relative, hash) in files)
        {
            var target = Path.Combine(staging, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(source, relative), target);
            if (ParityJson.Hash(target) != hash) throw new IOException("Build changed while freezing: " + relative);
        }
        try { Directory.Move(staging, directory); }
        catch (IOException) when (Directory.Exists(directory))
        {
            // This freshly created sibling is the only directory this operation removes.
            Directory.Delete(staging, recursive: true);
        }
        return directory;
    }

    public static async Task<int?> ExecuteFrozen(string root, string[] args)
    {
        var builds = Path.GetFullPath(Path.Combine(root, "artifacts/visual-parity/builds/avalonia")) + Path.DirectorySeparatorChar;
        if (Path.GetFullPath(AppContext.BaseDirectory).StartsWith(builds, StringComparison.OrdinalIgnoreCase)) return null;
        var directory = Create(root, AppContext.BaseDirectory, "avalonia");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(directory, "Material3Expressive.Parity.dll"));
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start frozen runner.");
        async Task Forward(StreamReader source, TextWriter destination)
        {
            while (await source.ReadLineAsync() is { } line) await destination.WriteLineAsync(line);
        }
        var stdout = Forward(process.StandardOutput, Console.Out);
        var stderr = Forward(process.StandardError, Console.Error);
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        return process.ExitCode;
    }
}
