using Avalonia;
using Avalonia.Controls;
using Material3Expressive.VisualTesting.Comparison;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Reporting;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class DiffArtifactWriterTests
{
    [Fact]
    public void AVisualFailureProducesEveryReviewArtifact()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "Material3Expressive.VisualTests",
            Guid.NewGuid().ToString("N"));
        var artifactRoot = Path.Combine(tempRoot, "artifacts");
        var paths = new RepositoryPaths(
            tempRoot,
            "test-x64",
            Path.Combine(tempRoot, "baselines"),
            artifactRoot);

        try
        {
            var expected = Solid(4, 4, 20);
            var actual = Solid(4, 4, 20);
            actual.Pixels[0] = 80;
            var comparer = new PixelComparer();
            var result = comparer.Compare(expected, actual, ComparisonProfile.Exact);
            var visualCase = new VisualCase(
                "harness/failure-artifacts",
                new PixelSize(4, 4),
                VisualTheme.Light,
                _ => new Border(),
                ComparisonProfile.Exact,
                ["harness"]);

            var directory = new DiffArtifactWriter(paths, comparer).Write(
                visualCase,
                actual,
                expected,
                result,
                result.Summary);

            Assert.False(result.Passed);
            Assert.True(File.Exists(Path.Combine(directory, "expected.png")));
            Assert.True(File.Exists(Path.Combine(directory, "actual.png")));
            Assert.True(File.Exists(Path.Combine(directory, "diff.png")));
            Assert.True(File.Exists(Path.Combine(directory, "overlay.png")));
            Assert.True(File.Exists(Path.Combine(directory, "metrics.json")));
            Assert.True(File.Exists(Path.Combine(directory, "index.html")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static RenderedImage Solid(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = value;
            pixels[index + 1] = value;
            pixels[index + 2] = value;
            pixels[index + 3] = byte.MaxValue;
        }

        return new RenderedImage(width, height, pixels);
    }
}
