using System.Text.Json;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class ConfigTests
{
    static CaseMetadata Fixture(object config) => CaseMetadata.Create("test.config", "Config", "test", "direct", config);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"enabled\":true}")]
    [InlineData("{\"enabled\":true,\"text\":null}")]
    [InlineData("{\"enabled\":\"true\",\"text\":\"Save\"}")]
    [InlineData("{\"enabled\":true,\"text\":\"Save\",\"unknown\":1}")]
    [InlineData("{\"Enabled\":true,\"text\":\"Save\"}")]
    public void ConfigDoesNotSilentlyDefaultOrIgnoreInvalidFields(string json)
    {
        var metadata = Fixture(new EmptyConfig()) with { Config = JsonSerializer.Deserialize<JsonElement>(json) };
        Assert.Throws<JsonException>(() => metadata.ReadConfig<FixtureConfig>());
    }

    [Fact]
    public void MetadataRejectsOldVersionAndMissingOrNonObjectConfig()
    {
        var metadata = Fixture(new EmptyConfig());
        Assert.Throws<InvalidDataException>(() => (metadata with { SchemaVersion = 3 }).Validate());
        Assert.Throws<InvalidDataException>(() => (metadata with { Config = default }).Validate());
        Assert.Throws<InvalidDataException>(() => Fixture(new[] { 1, 2 }).Validate());
    }

    [Fact]
    public void FrozenConfigIsDetachedAndActuallyControlsCapture()
    {
        var config = new Dictionary<string, double> { ["answer"] = 137.25 };
        var metadata = Fixture(config);
        config["answer"] = 99;
        Assert.Equal(137.25, metadata.ReadConfig<NumericConfig>().Answer);

        var directory = Path.Combine(Path.GetTempPath(), "m3-parity-config", Guid.NewGuid().ToString("N"));
        var input = Path.Combine(directory, "case.json");
        ParityJson.Write(input, metadata);
        var originalHash = ParityJson.Hash(input);
        var frozen = ParityJson.Read<CaseMetadata>(input);
        var writer = new CaptureWriter(frozen, input, Path.Combine(directory, "actual"), Program.FindRoot());
        writer.Submit(new("initial", 0, "fixture", [new("value", "fixture", null,
            [Measurement.Numeric("answer", writer.Metadata.ReadConfig<NumericConfig>().Answer)])], []));
        var capture = writer.Finish();
        Assert.Equal(137.25, capture.Samples.Single().Points.Single().Measurements.Single().Numbers!.Single());
        Assert.Equal(originalHash, capture.MetadataSha256);

        ParityJson.Write(input, Fixture(new NumericConfig(99)));
        Assert.NotEqual(originalHash, ParityJson.Hash(input));
        Assert.Equal(137.25, frozen.ReadConfig<NumericConfig>().Answer);
    }
}
