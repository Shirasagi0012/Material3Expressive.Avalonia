using Material3Expressive.VisualTesting;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class BaselineApprovalTests
{
    [Fact]
    public void RecordingCreatesAnUnapprovedCandidateAndApprovalBindsToItsHash()
    {
        using var directory = new TemporaryDirectory();
        var paths = CreatePaths(directory.Path);
        var store = new BaselineStore(paths);
        store.WriteEnvironment(VisualEnvironmentFingerprint.Capture());
        store.Write("case/light", Solid(10));

        Assert.Equal(
            BaselineApprovalState.AwaitingApproval,
            store.GetApprovalState("case/light"));

        store.Approve("case/light");
        Assert.Equal(BaselineApprovalState.Approved, store.GetApprovalState("case/light"));

        File.WriteAllBytes(store.GetBaselinePath("case/light"), Solid(20).EncodePng());
        Assert.Equal(
            BaselineApprovalState.ApprovedImageChanged,
            store.GetApprovalState("case/light"));
    }

    [Fact]
    public void RecordingAgainExplicitlyInvalidatesExistingApproval()
    {
        using var directory = new TemporaryDirectory();
        var paths = CreatePaths(directory.Path);
        var store = new BaselineStore(paths);
        store.WriteEnvironment(VisualEnvironmentFingerprint.Capture());
        store.Write("case/light", Solid(10));
        store.Approve("case/light");

        store.Write("case/light", Solid(10));

        Assert.Equal(
            BaselineApprovalState.AwaitingApproval,
            store.GetApprovalState("case/light"));
    }

    private static RepositoryPaths CreatePaths(string root) =>
        new(
            root,
            "test",
            Path.Combine(root, "baselines"),
            Path.Combine(root, "artifacts"));

    private static RenderedImage Solid(byte value) =>
        new(1, 1, [value, value, value, 255]);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Material3Expressive.VisualTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
