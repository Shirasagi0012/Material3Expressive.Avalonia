using System.Text.Json;
using System.Text.Json.Nodes;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class ResultFilesTests
{
    static CaseMetadata Metadata => CaseMetadata.Create("test.files", "Files | <fixture>", "test", "unused", new FixtureConfig(false, "Save 😀\nnext"));
    static string Temp() => Path.Combine(Path.GetTempPath(), "m3-parity-files", Guid.NewGuid().ToString("N"));
    static ProbePoint Point(params Measurement[] fields) => new("target", "container", null, fields);
    static Capture Capture(string runner, params Sample[] samples) => new(2, runner, Metadata.Id, "metadata", "baseline",
        new() { ["density"] = "1", ["version"] = runner }, samples, [], []);
    static Sample Sample(string id, long time, params ProbePoint[] points) => new(id, time, "ready", points, [],
        RasterCollection: [new("full", "notRequested", "Measurement only.")]);

    static (string Directory, CaseResult Result, Comparison Original) RoundTrip(Capture? expected, Capture? actual,
        string? executionError = null)
    {
        var directory = Temp();
        var comparison = ParityComparison.Run(Metadata, expected, actual, "", "", Temp(), executionError: executionError);
        ResultFiles.Write(directory, Metadata, comparison, expected, actual, "", "", "");
        return (directory, ResultFiles.Read(directory), comparison);
    }

    [Fact]
    public void SampleFilesPreserveValuesDescriptorsAndDifferencesWithoutDuplicatedCaptures()
    {
        var fields = new[]
        {
            Measurement.Numeric("fraction", 1.0000000000000002, source: "declared") with { Reason = "Measured note retained." },
            Measurement.Vector("vector", "point", [-0.125, 1e-20], "dip", "scene", "geometry", components: ["x", "y"]),
            Measurement.Text("false", "on\nnull: true\r\n😀\u2028last"), Measurement.Flag("enabled", true),
            new Measurement("fonts", "text", "strings", "none", "none", "families", "runtime", Strings: ["Roboto", "null"]),
            Measurement.Unavailable("ink", "text", "rect", "dip", "text", "No reader.")
        };
        var expected = Capture("compose", Sample("initial", 0, Point(fields)));
        var changed = fields.Select(f => f.Name == "fraction" ? f with { Numbers = [2.25], Source = "runtime" } : f).ToArray();
        var actual = Capture("avalonia", Sample("initial", 0, Point(changed)));
        var (directory, result, original) = RoundTrip(expected, actual);
        Assert.Equal(Metadata.ReadConfig<FixtureConfig>(), result.Metadata.ReadConfig<FixtureConfig>());
        Assert.Equal(original.Coverage, result.Comparison.Coverage);
        foreach (var side in new[] { (expected, result.Expected!), (actual, result.Actual!) })
        foreach (var field in side.Item1.Samples[0].Points[0].Measurements)
        {
            var read = side.Item2.Samples[0].Points[0].Measurements.Single(f => f.Name == field.Name);
            Assert.Equal("equal", field.Status == "measured" ? ParityComparison.Field(field.Name, field, read).Status : "equal");
            Assert.Equal(field.Status, read.Status); Assert.Equal(field.Reason, read.Reason); Assert.Equal(field.Source, read.Source);
        }
        var sampleFile = Path.Combine(directory, result.Samples["initial"]);
        var yaml = File.ReadAllText(sampleFile);
        Assert.Contains("status: different", yaml);
        Assert.Contains("value: true", yaml);
        Assert.DoesNotContain("observations.json", yaml);
        Assert.False(File.Exists(Path.Combine(directory, "comparison.json")));
        Assert.False(File.Exists(Path.Combine(directory, "agent.json")));
        Assert.False(Directory.Exists(Path.Combine(directory, "expected")));
        Assert.Equal(new[] { 1.2499999999999998 }, result.Comparison.Samples[0].Points[0].Fields.Single(f => f.Name == "fraction").Delta);
    }

    [Fact]
    public void SemanticConflictsAndUnavailableSidesRemainExplicit()
    {
        var one = Measurement.Numeric("value", 42, "dip");
        var expected = Capture("compose", Sample("initial", 0, Point(one, Measurement.Flag("onlyCompose", false),
            Measurement.Unavailable("missing", "text", "string", "none", "none", "failed", status: "error"))));
        var actual = Capture("avalonia", Sample("initial", 0, Point(one with { Category = "motion", Unit = "px", Source = "different reader" },
            Measurement.Text("missing", "available", "text"))));
        var (_, result, original) = RoundTrip(expected, actual);
        Assert.Equal(original.Coverage, result.Comparison.Coverage);
        var fields = result.Comparison.Samples[0].Points[0].Fields;
        var conflict = fields.Single(f => f.Name == "value");
        Assert.Equal("notCompared", conflict.Status); Assert.Equal("dip", conflict.Expected!.Unit); Assert.Equal("px", conflict.Actual!.Unit);
        Assert.Equal("properties", conflict.Expected.Category); Assert.Equal("motion", conflict.Actual.Category);
        Assert.Equal(new[] { "category", "unit" }, conflict.Issue!.Members);
        Assert.Null(fields.Single(f => f.Name == "onlyCompose").Actual);
        Assert.Equal("error", fields.Single(f => f.Name == "missing").Expected!.Status);
    }

    [Fact]
    public void TimePointIdentityAndAbsentSamplesRoundTrip()
    {
        var value = Measurement.Numeric("value", 1);
        var expected = Capture("compose", Sample("initial", 0, Point(value)), Sample("compose-only", 20, Point(value)));
        var actual = Capture("avalonia", Sample("initial", 5, Point(value) with { Role = "allocation" }) with { FrameTimeUs = 3 },
            Sample("avalonia-only", 30, Point(value)));
        var (_, result, original) = RoundTrip(expected, actual);
        Assert.Equal(original.Coverage, result.Comparison.Coverage);
        Assert.Equal(5, result.Actual!.Samples[0].TimeUs); Assert.Equal(3, result.Actual.Samples[0].FrameTimeUs);
        Assert.Equal("container", result.Expected!.Samples[0].Points[0].Role);
        Assert.Equal("allocation", result.Actual.Samples[0].Points[0].Role);
        Assert.Equal("compose-only", result.Expected.Samples[1].Id);
        Assert.Equal("avalonia-only", result.Actual.Samples[1].Id);
        Assert.All(result.Comparison.Samples, s => Assert.Equal("notCompared", s.Status));
    }

    [Fact]
    public void OverviewListsEverySampleAndSeparatesUnavailableAndUnrequestedImages()
    {
        var e = Capture("compose", Sample("initial", 0, Point(Measurement.Numeric("x", 1, category: "geometry"))),
            Sample("later", 1000, Point(Measurement.Unavailable("ink", "text", "rect", "dip", "text", "No reader."))));
        var a = e with { Runner = "avalonia", Samples = [e.Samples[0] with { Points = [Point(Measurement.Numeric("x", 2, category: "geometry"))] }, e.Samples[1]] };
        var (_, result, _) = RoundTrip(e, a);
        var overview = ResultFiles.Overview(result);
        Assert.Contains("| initial | 0 | different | 1 / 0 |", overview);
        Assert.Contains("| later | 1000 | notCompared | 0 / 0 | 0 / 1 |", overview);
        Assert.Contains("full:notRequested", overview);
        Assert.Contains("samples/0001-later.yaml", overview);
        Assert.DoesNotContain("# Files | <fixture>", overview);
        Assert.Contains("repeatability: not checked", overview);
    }

    [Fact]
    public void TracesPreserveSubmissionOrderEvenWithSameTimeSamplesSortedDifferently()
    {
        var expected = Capture("compose", Sample("z-first", 0, Point()), Sample("a-second", 0, Point())) with
        {
            Trace = [new(0, -1, "mount", ""), new(1, 0, "sample", "z-first"),
                new(2, 0, "input", "press", JsonSerializer.SerializeToElement(new { x = 12.5, button = "primary" })),
                new(3, 0, "sample", "a-second"), new(4, 0, "dispose", "cleanup")]
        };
        var (_, result, _) = RoundTrip(expected, expected with { Runner = "avalonia" });
        Assert.Equal(new[] { "z-first", "a-second" }, result.Expected!.Samples.Select(s => s.Id));
        Assert.Equal(JsonSerializer.Serialize(expected.Trace), JsonSerializer.Serialize(result.Expected.Trace));
        Assert.Contains("Empty points/samples: 2", ResultFiles.Overview(result));
    }

    [Fact]
    public void ActualOnlyAndExecutionFailuresRemainReviewable()
    {
        var actual = Capture("avalonia", Sample("initial", 0, Point(Measurement.Numeric("x", 1))));
        var (_, single, _) = RoundTrip(null, actual);
        Assert.Null(single.Expected); Assert.Equal("notCompared", single.Comparison.Status);
        var (_, failed, _) = RoundTrip(null, actual, "Reference process timed out.");
        Assert.Equal("error", failed.Comparison.Status); Assert.Single(failed.Actual!.Samples);
        Assert.Contains("Reference process timed out.", ResultFiles.Overview(failed));
        var (_, empty, _) = RoundTrip(null, null, "Failed before capture.");
        Assert.Empty(empty.Comparison.Samples); Assert.Equal("error", empty.Comparison.Status);
    }

    [Fact]
    public void InvalidCaptureIdentityIsPreservedRatherThanReplacedByExpectedMetadata()
    {
        var expected = Capture("compose", Sample("initial", 0, Point(Measurement.Numeric("x", 1))));
        var wrong = expected with { CaseId = "wrong.case", MetadataSha256 = "other" };
        var (_, result, _) = RoundTrip(expected, wrong);
        Assert.Equal("error", result.Comparison.Status);
        Assert.Equal("compose", result.Actual!.Runner);
        Assert.Equal("wrong.case", result.Actual.CaseId);
        Assert.Equal("other", result.Actual.MetadataSha256);
        Assert.Equal("notCompared", result.Comparison.Samples[0].Status);
    }

    [Fact]
    public void ResultManifestDetectsMutationAndUnexpectedFiles()
    {
        var capture = Capture("compose", Sample("initial", 0, Point(Measurement.Numeric("x", 1))));
        var (directory, result, _) = RoundTrip(capture, capture with { Runner = "avalonia" });
        File.AppendAllText(Path.Combine(directory, result.Samples["initial"]), "# changed\n");
        Assert.Throws<InvalidDataException>(() => ResultFiles.Read(directory));
        ResultFiles.Seal(directory);
        File.WriteAllText(Path.Combine(directory, "unexpected.txt"), "x");
        Assert.Throws<InvalidDataException>(() => ResultFiles.Read(directory));
    }

    [Fact]
    public void YamlScalarTypesPrecisionAndReservedKeysRoundTrip()
    {
        var path = Path.Combine(Temp(), "values.yaml");
        var input = new JsonObject
        {
            ["yes"] = "true", ["false"] = "null", ["on"] = "no", ["exponent"] = 1e-20,
            ["precise"] = 1.0000000000000002, ["large"] = long.MaxValue, ["nil"] = null,
            ["newline"] = "A\r\n😀\u2028B\u2029", ["empty"] = ""
        };
        ResultYaml.Write(path, input);
        Assert.True(JsonNode.DeepEquals(input, ResultYaml.Read(path)));
        Assert.Contains("\"yes\": \"true\"", File.ReadAllText(path));
        Assert.Contains("1.0E-20", File.ReadAllText(path));
    }

    [Fact]
    public void RasterAssetsAndReportCanBeReadAfterCaptureSourcesAreMoved()
    {
        var source = Temp();
        var expectedDirectory = Path.Combine(source, "expected");
        var actualDirectory = Path.Combine(source, "actual");
        Capture Image(string runner, string directory, byte alpha)
        {
            Directory.CreateDirectory(directory);
            using var bitmap = new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(2, 1, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul));
            bitmap.SetPixel(0, 0, new SkiaSharp.SKColor(80, 120, 200, 0));
            bitmap.SetPixel(1, 0, new SkiaSharp.SKColor(81, 121, 201, alpha));
            using (var png = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) File.WriteAllBytes(Path.Combine(directory, "frame.png"), png.ToArray());
            var raster = PixelData.FromPng(directory, "full", "frame.png");
            return Capture(runner, Sample("initial", 0, Point()) with { Rasters = [raster], RasterCollection = [new("full", "captured")] });
        }
        var e = Image("compose", expectedDirectory, 128);
        var a = Image("avalonia", actualDirectory, 129);
        var diff = Path.Combine(source, "comparison");
        var comparison = ParityComparison.Run(Metadata, e, a, expectedDirectory, actualDirectory, diff);
        var directory = Temp();
        ResultFiles.Write(directory, Metadata, comparison, e, a, expectedDirectory, actualDirectory, diff);
        Directory.Move(source, source + "-unavailable");
        var result = Report.Write(directory);
        var raster = result.Comparison.Samples[0].Rasters.Single();
        Assert.Equal(1, raster.Metrics!.DifferentPixels);
        Assert.Equal(new[] { 0, 0, 0, 1 }, raster.Metrics.MaximumChannelDelta);
        Assert.Equal(new byte[] { 80, 120, 200, 0 }, PixelData.Read(directory, raster.Expected!)[..4]);
        Assert.True(File.Exists(Path.Combine(directory, raster.DifferencePixels!)));
        Assert.True(File.Exists(Path.Combine(directory, "overview.md")));
        var html = File.ReadAllText(Path.Combine(directory, "report.html"));
        Assert.Contains("sampleFiles", html); Assert.DoesNotContain("agent.json", html);
        var before = ResultFiles.Overview(result);
        Assert.Equal(before, ResultFiles.Overview(Report.Write(directory)));
    }
}
