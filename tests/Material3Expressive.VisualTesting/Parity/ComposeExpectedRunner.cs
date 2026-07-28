using System.Diagnostics;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Runs the CMP producer once for a parity run; no expected artifact is imported or retained.</summary>
public sealed class ComposeExpectedRunner
{
    public ComposeExpectedBundle Generate(string runDirectory, IReadOnlyCollection<string>? caseIds = null)
    {
        var root = RepositoryRoot.Find();
        var composeRoot = Path.Combine(root, "tests", "ComposeReference");
        var gradle = Path.Combine(composeRoot, "gradlew.bat");
        if (!File.Exists(gradle))
            throw new FileNotFoundException("Compose reference Gradle wrapper is missing.", gradle);
        var output = Path.GetFullPath(runDirectory);
        Directory.CreateDirectory(output);
        var arguments = new List<string> { ":capture-app:run", "--args=\"--output", output };
        foreach (var id in caseIds ?? [])
            arguments.Add("--case " + id);
        arguments[^1] += "\"";
        var start = new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = composeRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = "/c \"" + gradle + " " + string.Join(' ', arguments) + "\""
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the Compose reference producer.");
        var outputText = process.StandardOutput.ReadToEnd();
        var errorText = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Compose reference generation failed ({process.ExitCode}).{Environment.NewLine}{outputText}{errorText}");
        return ComposeExpectedBundle.Open(Path.Combine(output, "expected"));
    }
}
