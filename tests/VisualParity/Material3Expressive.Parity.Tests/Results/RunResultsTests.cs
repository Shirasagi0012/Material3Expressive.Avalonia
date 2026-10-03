using System.Text.Json.Nodes;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class RunResultsTests
{
    [Fact]
    public void ThousandCasesAppendProgressWithoutGrowingRunningIndex()
    {
        var directory = Path.Combine(Path.GetTempPath(), "parity-run-index", Guid.NewGuid().ToString("N"));
        var run = new RunResults(directory, 1000, "run");
        run.Save("running");
        for (var i = 0; i < 1000; i++)
        {
            var metadata = CaseMetadata.Create($"test.case-{i}", "Case", "test", "direct", new NumericConfig(i));
            var comparison = new Comparison(2, metadata.Id, "equal", new(1, 0, 0), [], [], [], "parity");
            run.Add(new(metadata, comparison, null, null, [], null), new() { ["actualCapture"] = 2 });
            run.Save("running");
        }
        var index = ResultYaml.Read(Path.Combine(directory, "run.yaml"));
        Assert.Equal(1000, index["completedCases"]!.GetValue<int>());
        Assert.Empty(index["cases"]!.AsArray());
        Assert.True(new FileInfo(Path.Combine(directory, "run.yaml")).Length < 1024);
        Assert.Equal(1000, File.ReadLines(Path.Combine(directory, "progress.jsonl")).Count());
        Assert.Equal(2000, index["timingsMs"]!["actualCapture"]!.GetValue<double>());
        run.Save("completed");
        Assert.Equal(1000, ResultYaml.Read(Path.Combine(directory, "run.yaml"))["cases"]!.AsArray().Count);
    }

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
