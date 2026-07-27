using System.Runtime.InteropServices;

namespace Material3Expressive.VisualTesting.Storage;

/// <summary>Canonical repository paths used by both the tests and the baseline CLI.</summary>
public sealed class RepositoryPaths
{
    public RepositoryPaths(string? startDirectory = null)
    {
        Root = FindRoot(startDirectory ?? AppContext.BaseDirectory);
        PlatformMoniker = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"win-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}"
            : $"{GetOsName()}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        BaselineRoot = Path.Combine(
            Root,
            "tests",
            "Material3Expressive.VisualTesting",
            "Baselines",
            "avalonia-12.1-skia",
            PlatformMoniker,
            "1x");
        ArtifactRoot = Path.Combine(Root, "artifacts", "visual-diffs");
        ComposeReferenceRoot = Path.Combine(
            Root,
            "visual-references",
            "compose",
            "1.5.0-alpha24");
        ComposeArtifactRoot = Path.Combine(Root, "artifacts", "compose-parity");
    }

    public RepositoryPaths(
        string root,
        string platformMoniker,
        string baselineRoot,
        string artifactRoot)
    {
        Root = Path.GetFullPath(root);
        PlatformMoniker = platformMoniker;
        BaselineRoot = Path.GetFullPath(baselineRoot);
        ArtifactRoot = Path.GetFullPath(artifactRoot);
        ComposeReferenceRoot = Path.Combine(
            Root,
            "visual-references",
            "compose",
            "1.5.0-alpha24");
        ComposeArtifactRoot = Path.Combine(Root, "artifacts", "compose-parity");
    }

    public string Root { get; }

    public string PlatformMoniker { get; }

    public string BaselineRoot { get; }

    public string ArtifactRoot { get; }

    public string ComposeReferenceRoot { get; }

    public string ComposeArtifactRoot { get; }

    private static string FindRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Material3Expressive.slnx"))
                && File.Exists(Path.Combine(directory.FullName, "VISUAL_TEST_PLAN.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the Material3Expressive repository above '{startDirectory}'.");
    }

    private static string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";
        return "unknown";
    }
}
