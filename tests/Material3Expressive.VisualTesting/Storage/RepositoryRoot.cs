namespace Material3Expressive.VisualTesting.Storage;

/// <summary>Finds the repository root for tests that consume committed contracts.</summary>
public static class RepositoryRoot
{
    public static string Find(string? startDirectory = null)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory ?? AppContext.BaseDirectory));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Material3Expressive.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the Material3Expressive repository above '{startDirectory ?? AppContext.BaseDirectory}'.");
    }
}
