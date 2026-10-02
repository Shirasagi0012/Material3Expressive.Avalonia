using System.Text.Json.Nodes;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class RunResultsTests
{
    [Fact]
    public void RunIndexPublishesProgressAndStableCountsWithoutSampleFacts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "parity-run-index", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var run = new RunResults(directory, 2, "self-test");
        run.Save("running");
        var index = ResultYaml.Read(Path.Combine(directory, "run.yaml"));
        Assert.Equal("running", index["state"]!.GetValue<string>());
        Assert.Null(index["finishedAt"]);
        Assert.Empty(index["cases"]!.AsArray());
        var metadata = CaseMetadata.Create("test.example", "Example", "test", "direct", new NumericConfig(42));
        var comparison = new Comparison(2, metadata.Id, "different", new(3, 2, 1), [], [], [], "parity");
        run.Add(new(metadata, comparison, null, null, [], new JsonObject
        { ["results"] = new JsonObject { ["compose.repeat"] = new JsonArray() } }));
        run.Save("completed");
        index = ResultYaml.Read(Path.Combine(directory, "run.yaml"));
        Assert.Equal(2, index["selectedCases"]!.GetValue<int>());
        Assert.Equal("stable", index["cases"]![0]!["repeatability"]!.GetValue<string>());
        Assert.Equal(2, index["cases"]![0]!["coverage"]!["different"]!.GetValue<int>());
        Assert.NotNull(index["finishedAt"]);
        Assert.False(File.Exists(Path.Combine(directory, "run.yaml.tmp")));
        Assert.False(File.Exists(Path.Combine(directory, "report.html")));
    }
}
