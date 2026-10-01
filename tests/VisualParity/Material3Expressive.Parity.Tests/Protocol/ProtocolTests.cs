using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;
using System.Text.Json;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [Fact]
    public void NumericComponentMeaningParticipatesInPairing()
    {
        var coordinate = Measurement.Vector("value", "point", [20, 40], "dip", "local", "geometry");
        var size = coordinate with { Components = ["width", "height"] };
        Assert.Equal(new[] { "x", "y" }, coordinate.ComponentNames());
        Assert.Equal("notCompared", ParityComparison.Field("value", coordinate, size).Status);
        Assert.Contains("components", ParityComparison.Field("value", coordinate, size).Issue!.Members);
        Assert.Throws<InvalidDataException>(() => (size with { Components = ["width", "width"] }).Validate());
    }

    [Fact]
    public void CollectionIntentMustAgreeWithRastersAndExplainOmissions()
    {
        var c = Capture("compose", Point());
        var sample = c.Samples[0] with { RasterCollection = [new("full", "notRequested", "No image at this step.")] };
        (c with { Samples = [sample] }).Validate();
        Assert.Throws<InvalidDataException>(() => (c with { Samples = [sample with { RasterCollection = [new("full", "captured")] }] }).Validate());
        Assert.Throws<InvalidDataException>(() => (c with { Samples = [sample with { RasterCollection = [new("full", "notRequested")] }] }).Validate());
    }

    [Fact]
    public void DuplicateIdsCyclesAndNonfiniteValuesAreRejected()
    {
        var c = Capture("compose", Point(Measurement.Numeric("x", double.NaN))); Assert.Throws<InvalidDataException>(c.Validate);
        c = Capture("compose", Point(), Point()); Assert.Throws<InvalidDataException>(c.Validate);
        c = Capture("compose", new("a", "container", "b", []), new("b", "container", "a", [])); Assert.Throws<InvalidDataException>(c.Validate);
    }

    [Fact]
    public void ManifestRejectsTamperedOrAddedFiles()
    {
        var d = Temp(); var c = Capture("compose", Point()); ParityJson.Write(Path.Combine(d, "observations.json"), c); ParityJson.Seal(d, c.Runner, c.Environment); ParityJson.Verify(d);
        File.WriteAllText(Path.Combine(d, "extra.txt"), "new"); Assert.Throws<InvalidDataException>(() => ParityJson.Verify(d)); File.Delete(Path.Combine(d, "extra.txt"));
        File.AppendAllText(Path.Combine(d, "observations.json"), " "); Assert.Throws<InvalidDataException>(() => ParityJson.Verify(d));
    }

    [Fact]
    public void ManifestRejectsInternallyInconsistentPixelsEvenWhenResealed()
    {
        var d = Temp(); Directory.CreateDirectory(d); using var bitmap = new SKBitmap(1, 1); bitmap.Erase(new SKColor(80, 120, 200, 128));
        File.WriteAllBytes(Path.Combine(d, "image.png"), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray()); var raster = PixelData.FromPng(d, "full", "image.png");
        var c = Capture("compose", Point()); c = c with { Samples = [c.Samples[0] with { Rasters = [raster] }] };
        ParityJson.Write(Path.Combine(d, "observations.json"), c); ParityJson.Seal(d, c.Runner, c.Environment); ParityJson.Verify(d);
        var bytes = File.ReadAllBytes(Path.Combine(d, raster.Pixels)); bytes[0]++; File.WriteAllBytes(Path.Combine(d, raster.Pixels), bytes); ParityJson.Seal(d, c.Runner, c.Environment);
        Assert.Throws<InvalidDataException>(() => ParityJson.Verify(d));
    }

    [Fact]
    public void CanonicalDecodePreservesStraightRgbUnderZeroAlpha()
    {
        var directory=Temp();Directory.CreateDirectory(directory);
        using var bitmap=new SKBitmap(new SKImageInfo(2,1,SKColorType.Rgba8888,SKAlphaType.Unpremul));
        bitmap.SetPixel(0,0,new SKColor(80,120,200,0)); bitmap.SetPixel(1,0,new SKColor(81,121,201,128));
        var path=Path.Combine(directory,"pixels.png");File.WriteAllBytes(path,bitmap.Encode(SKEncodedImageFormat.Png,100).ToArray());
        var decoded=PixelData.DecodePng(path);
        Assert.Equal(new byte[] {80,120,200,0,81,121,201,128},decoded.Bytes);
    }

    [Fact]
    public void SubmitDetachesMutableCollections()
    {
        var root = Program.FindRoot(); var d = Temp(); var input = Path.Combine(d, "case.json"); ParityJson.Write(input, Metadata);
        var writer = new CaptureWriter(Metadata, input, d, root); var field = Measurement.Numeric("value", 1); writer.Submit(new("initial", 0, "custom", [Point(field)], [])); field.Numbers![0] = 2;
        Assert.Equal(1, writer.Finish().Samples.Single().Points.Single().Measurements.Single().Numbers![0]);
    }

    [Fact]
    public void MetadataContainsEntriesInsteadOfAnExecutionLanguage()
    {
        var scenarios = ScenarioDiscovery.Discover(typeof(Program).Assembly.GetTypes());
        foreach (var m in ScenarioDiscovery.Expand(scenarios.Values))
        { m.Validate(); Assert.True(scenarios.ContainsKey(new(m.Suite, m.Scenario))); }
        var path = Path.Combine(Temp(), "invalid.json"); ParityJson.Write(path, new { schemaVersion = 4, id = "test.fixture", title = "t", suite = "test", scenario = "a", config = new { }, events = new[] { "down" } });
        Assert.Throws<JsonException>(() => ParityJson.Read<CaseMetadata>(path));
    }
}
