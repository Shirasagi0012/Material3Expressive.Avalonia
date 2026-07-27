namespace Material3Expressive.VisualTesting.Storage;

/// <summary>Canonical repository paths used by Compose reference capture and parity reporting.</summary>
public sealed class RepositoryPaths
{
    public RepositoryPaths(string? startDirectory = null)
    {
        Root = FindRoot(startDirectory ?? AppContext.BaseDirectory);
        InitializeComposePaths();
    }

    public RepositoryPaths(string root, bool useAsRepositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        InitializeComposePaths();
    }

    public string Root { get; }

    public string ComposeReferenceRoot { get; private set; } = null!;

    public string ComposeArtifactRoot { get; private set; } = null!;

    private void InitializeComposePaths()
    {
        ComposeReferenceRoot = Path.Combine(
            Root,
            "visual-references",
            "compose",
            "1.5.0-alpha24");
        ComposeArtifactRoot = Path.Combine(Root, "artifacts", "compose-parity");
    }

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
}
