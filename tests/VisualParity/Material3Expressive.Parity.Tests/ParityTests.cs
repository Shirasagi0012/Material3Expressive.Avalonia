using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;
using System.Text.Json;
[assembly: AvaloniaTestApplication(typeof(ParityAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Material3Expressive.Parity.Tests;

public class ParityTests
{
    static CaseMetadata Metadata => new(2, "test.fixture", "Fixture", "test", "unused", "DirectObservation");
    static string Temp() => Path.Combine(Path.GetTempPath(), "m3-parity-v2", Guid.NewGuid().ToString("N"));
    static Capture Capture(string runner, params ProbePoint[] points) => new(2, runner, Metadata.Id, "metadata", "baseline", new() { ["density"] = "1" },
        [new("initial", 0, "mounted", points, [])], [], []);
    static ProbePoint Point(params Measurement[] fields) => new("target", "container", null, fields);
    static Comparison Compare(Capture e, Capture a) => ParityComparison.Run(Metadata, e, a, "", "", Temp());
    static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, ParityJson.Options);
    [Fact]
    public void StructuredIssuesListAllActualDescriptorConflicts()
    {
        var e = Measurement.Numeric("x", 1);
        var a = e with { Unit = "px", Space = "text" };
        var issue = ParityComparison.Field("x", e, a).Issue!;
        Assert.Equal("descriptorMismatch", issue.Code); Assert.Equal(new[] { "unit", "space" }, issue.Members);
        Assert.Equal("measurementAbsent", ParityComparison.Field("x", e, null).Issue!.Code);
        var c = Capture("avalonia", Point(e));
        var result = Compare(Capture("compose", Point(e)), c with { Samples = [c.Samples[0] with { FrameTimeUs = 12 }] });
        Assert.Equal(new[] { "frameTimeUs" }, result.Samples.Single().Issue!.Members);
        Assert.Equal("sampleMismatch", result.Samples.Single().Points.Single().Fields.Single().Issue!.Code);
    }
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
    [Theory]
    [InlineData("A \r\n", 0, 4, 0, 2)]
    [InlineData("\r\n", 0, 2, 0, 0)]
    [InlineData("A\u2028", 0, 2, 0, 1)]
    [InlineData("😀  \n", 0, 5, 0, 4)]
    [InlineData("Save", 0, 5, 0, 4)]
    [InlineData("A\nB", 0, 1, 0, 1)]
    public void ContentRangeRemovesOnlyRealParagraphBreaksAndPreservesUtf16(string input, int start, int end, int expectedStart, int expectedEnd) =>
        Assert.Equal(new double[] { expectedStart, expectedEnd }, TextRanges.Content(input, start, end));
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
    public void NoToleranceCanHideFractionalGeometryDifference()
    {
        var e = Capture("compose", Point(Measurement.Numeric("x", 0)));
        var a = Capture("avalonia", Point(Measurement.Numeric("x", .0000001)));
        var result = Compare(e, a); Assert.Equal(1, result.Coverage.Different); Assert.Equal(.0000001, result.Samples[0].Points[0].Fields[0].Delta![0]);
    }
    [Fact]
    public void UnavailableBothSidesRemainsNotCompared()
    {
        var field = Measurement.Unavailable("font", "text", "string", "none", "none", "No reader");
        Assert.Equal("notCompared", Compare(Capture("compose", Point(field)), Capture("avalonia", Point(field))).Status);
    }
    [Theory]
    [InlineData("unit")]
    [InlineData("space")]
    [InlineData("definition")]
    [InlineData("kind")]
    public void SemanticMismatchIsNotANumericDifference(string part)
    {
        var e = Measurement.Numeric("width", 10); var a = part switch
        {
            "unit" => e with { Unit = "px" },
            "space" => e with { Space = "text" },
            "definition" => e with { Definition = "glyph-ink-width" },
            _ => e with { Kind = "point", Numbers = [10, 10] }
        };
        Assert.Equal("notCompared", ParityComparison.Field("width", e, a).Status);
    }
    [Fact]
    public void MissingFieldAndMissingPointAreVisibleSeparately()
    {
        var e = Capture("compose", Point(Measurement.Numeric("width", 10)));
        var a = Capture("avalonia", Point()); Assert.Equal(1, Compare(e, a).Coverage.NotCompared);
        Assert.Equal("notCompared", Compare(e, Capture("avalonia")).Samples[0].Points.Single().Status);
    }
    [Fact]
    public void SampleMatchingUsesIdsAndChecksTimeAndStage()
    {
        var e = Capture("compose", Point()); var a = Capture("avalonia", Point());
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { TimeUs = 1 }] }).Samples.Single().Status);
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { FrameTimeUs = 1 }] }).Samples.Single().Status);
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { Stage = "pressed" }] }).Samples.Single().Status);
        Assert.Equal(2, Compare(e, a with { Samples = [a.Samples[0] with { Id = "other" }] }).Coverage.NotCompared);
    }
    [Fact]
    public void BooleanFactsUseNativeBooleanValues()
    {
        var e = Measurement.Flag("enabled", true); var a = Measurement.Flag("enabled", false);
        e.Validate(); a.Validate();
        Assert.Equal("different", ParityComparison.Field("enabled", e, a).Status);
        Assert.Null(ParityComparison.Field("enabled", e, a).Delta);
    }
    [Fact]
    public void MismatchedParentRetainsBothFactsWithoutPairingThem()
    {
        var e = Capture("compose", new("root", "container", null, []), new("target", "container", "root", [Measurement.Numeric("x", 1)]));
        var a = Capture("avalonia", Point(Measurement.Numeric("x", 2)));
        var point = Compare(e, a).Samples.Single().Points.Single(p => p.Id == "target");
        Assert.Equal("notCompared", point.Status); Assert.Equal("root", point.ExpectedParentId); Assert.Null(point.ActualParentId);
        Assert.Equal(1, point.Fields.Single().Expected!.Numbers![0]); Assert.Equal(2, point.Fields.Single().Actual!.Numbers![0]);
        Assert.Null(point.Fields.Single().Delta);
    }
    [Fact]
    public void NumericVectorsKeepSignedDelta()
    {
        var e = Measurement.Vector("box", "rect", [10, 20, 30, 40], "dip", "scene", "geometry");
        var a = e with { Numbers = [9, 20, 31, 39] };
        Assert.Equal(new double[] { -1, 0, 1, -1 }, ParityComparison.Field("box", e, a).Delta);
    }
    [Fact]
    public void DuplicateIdsCyclesAndNonfiniteValuesAreRejected()
    {
        var c = Capture("compose", Point(Measurement.Numeric("x", double.NaN))); Assert.Throws<InvalidDataException>(c.Validate);
        c = Capture("compose", Point(), Point()); Assert.Throws<InvalidDataException>(c.Validate);
        c = Capture("compose", new("a", "container", "b", []), new("b", "container", "a", [])); Assert.Throws<InvalidDataException>(c.Validate);
    }
    [Fact]
    public void ActualResolvedFontCannotBeSubstitutedForMissingFacts()
    {
        var e = Measurement.Unavailable("resolvedFonts", "text", "strings", "none", "none", "No API", "text.resolved-font-families");
        var a = new Measurement("resolvedFonts", "text", "strings", "none", "none", "text.resolved-font-families", "runtime", Strings: ["Roboto"]);
        Assert.Equal("notCompared", ParityComparison.Field("resolvedFonts", e, a).Status);
    }
    [Fact]
    public void OnePixelAndAlphaDifferencesAreNotDilutedOrDiscarded()
    {
        var e = new byte[240 * 160 * 4]; var a = (byte[])e.Clone(); a[(80 * 240 + 120) * 4 + 3] = 1;
        var r = ParityComparison.Pixels(e, a, 240, 160, [0, 0, 240, 160]);
        Assert.Equal(1, r.DifferentPixels); Assert.Equal(new[] { 120, 80, 1, 1 }, r.DifferenceBounds); Assert.Equal(1, r.MaximumChannelDelta[3]);
        Assert.True(r.MeanChannelDelta[3] > 0); Assert.Equal(0, ParityComparison.Pixels(e, a, 240, 160, [0, 0, 10, 10]).DifferentPixels);
    }
    [Fact]
    public void RasterRegionsUseUnionWithoutMovingImages()
    {
        var ed = Temp(); var ad = Temp(); Directory.CreateDirectory(ed); Directory.CreateDirectory(ad);
        using var bitmap = new SKBitmap(5, 5); bitmap.Erase(SKColors.White);
        void Save(string directory) => File.WriteAllBytes(Path.Combine(directory, "image.png"), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
        Save(ed); bitmap.SetPixel(4, 4, SKColors.Black); Save(ad);
        var er = PixelData.FromPng(ed, "full", "image.png"); var ar = PixelData.FromPng(ad, "full", "image.png");
        Measurement Box(double x) => Measurement.Vector("box", "rect", [x, 0, 1, 1], "dip", "scene", "geometry", "layout.box.aabb.unclipped");
        var e = Capture("compose", Point(Box(0))); var a = Capture("avalonia", Point(Box(2)));
        e = e with { Samples = [e.Samples[0] with { Rasters = [er] }] }; a = a with { Samples = [a.Samples[0] with { Rasters = [ar] }] };
        var r = ParityComparison.Run(Metadata, e, a, ed, ad, Temp()).Samples[0].Rasters.Single();
        Assert.Equal(1, r.Metrics!.DifferentPixels); Assert.Equal(new[] { 0, 0, 3, 1 }, r.Regions!.Single().Bounds); Assert.Equal(0, r.Regions!.Single().Metrics.DifferentPixels);
        var unpaired = a with { Samples = [a.Samples[0] with { Points = [a.Samples[0].Points[0] with { Role = "allocation" }] }] };
        var unpairedRaster = ParityComparison.Run(Metadata,e,unpaired,ed,ad,Temp()).Samples[0].Rasters.Single();
        Assert.Empty(unpairedRaster.Regions!); Assert.Equal(1,unpairedRaster.Metrics!.DifferentPixels);
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
    static async Task<(Capture Capture, string Directory)> Actual(string id, bool extra = false)
    {
        var root = Program.FindRoot(); var input = Path.Combine(root, $"tests/VisualParity/cases/{id}.json"); var m = ParityJson.Read<CaseMetadata>(input); var d = Temp(); var writer = new CaptureWriter(m, input, d, root, extra);
        await Cases.Entries[m.AvaloniaEntry](writer); var capture = writer.Finish(); ParityJson.Verify(d); return (capture, d);
    }
    [AvaloniaFact]
    public async Task InterruptedMotionRepeatsAndExtraImagesDoNotAdvanceClock()
    {
        var a = await Actual("motion.interrupted"); var b = await Actual("motion.interrupted"); var dense = await Actual("motion.interrupted", true);
        Assert.Empty(a.Capture.Errors); Assert.Empty(Program.Stability(a.Directory, b.Directory)); Assert.Empty(Program.Stability(a.Directory, dense.Directory, true));
        Assert.Equal(new long[] { -2000, -1000, 0, 16000, 40000, 56000, 80000, 140000, 160000 }, a.Capture.Trace.Where(t => t.Operation == "frame").Select(t => t.TimeUs));
    }
    [AvaloniaFact]
    public async Task InterframeEventTimeAndPassiveCaptureKeepLastDeliveredFrame()
    {
        var root = Program.FindRoot(); var directory = Temp(); var input = Path.Combine(directory,"case.json");
        ParityJson.Write(input,Metadata); var writer = new CaptureWriter(Metadata,input,directory,root);
        await using (var host = new AvaloniaHost(writer))
        {
            host.Mount(new Avalonia.Controls.Border()); host.Frame(0); host.AdvanceTo(5000); host.Draw();
            Assert.Equal(0,host.FrameTimeUs); Assert.Equal(5000,host.TimeUs);
            var sample = host.Snapshot("between", "between-frames", [], true);
            Assert.Equal(5000,sample.TimeUs); Assert.Equal(0,sample.FrameTimeUs); writer.Submit(sample);
            Assert.Equal(0,host.FrameTimeUs);
        }
        Assert.Single(writer.Finish().Trace,t=>t.Operation=="frame");
    }
    [AvaloniaFact]
    public async Task RealButtonUsesNativeLabelAndRealInput()
    {
        var result = await Actual("button.filled.press"); Assert.Empty(result.Capture.Errors);
        var final = result.Capture.Samples.Last(); Assert.Contains(result.Capture.Trace,t=>t.Operation=="advance"&&t.TimeUs==88000);
        Assert.Equal(1, final.Points.Single(p => p.Id == "button").Measurements.Single(m => m.Name == "clicks").Numbers![0]);
        var label = final.Points.Single(p => p.Id == "label"); Assert.Contains("Roboto", label.Measurements.Single(m => m.Name == "resolvedFonts").Strings!);
        Assert.Equal(new double[] { 0, 4 }, label.Measurements.Single(m => m.Name == "line.0.range").Numbers);
        Assert.True(label.Measurements.Single(m => m.Name == "line.0.nativeRange").Numbers![1] >= 4);
        var container = result.Capture.Samples[0].Points.Single(p => p.Id == "container");
        Assert.Equal("measured", container.Measurements.Single(m => m.Name == "cornerRadii").Status);
        Assert.Equal(20, container.Measurements.Single(m => m.Name == "cornerRadii").Numbers![0]);
        var pressed = result.Capture.Samples.Single(s => s.Id == "pressed-80000").Points.Single(p => p.Id == "interaction");
        Assert.True(pressed.Measurements.Single(m => m.Name == "pressed").Boolean);
        Assert.Equal(1, pressed.Measurements.Single(m => m.Name == "rippleCount").Numbers![0]);
        Assert.Equal("notRequested", result.Capture.Samples.Single(s => s.Id == "pressed-32000").RasterCollection!.Single().Status);
    }
    [AvaloniaFact]
    public async Task ParagraphBreakReaderPreservesInputConsumptionAndExcludesBreaksFromContent()
    {
        var result = await Actual("text.paragraph-breaks"); var fields = result.Capture.Samples.Single().Points.Single().Measurements;
        var input = fields.Single(f => f.Name == "content").Strings![0];
        var count = (int)fields.Single(f => f.Name == "lineCount").Numbers![0];
        var consumed = "";
        for (var i = 0; i < count; i++)
        {
            var range = fields.Single(f => f.Name == $"line.{i}.range").Numbers!;
            var text = input[(int)range[0]..(int)range[1]];
            Assert.False(text.EndsWith('\r') || text.EndsWith('\n') || text.EndsWith('\u2028') || text.EndsWith('\u2029'));
            var native = fields.Single(f => f.Name == $"line.{i}.consumedRange").Numbers!;
            consumed += input[(int)native[0]..(int)native[1]];
        }
        Assert.Equal(input, consumed);
    }
    [Fact]
    public void MetadataContainsEntriesInsteadOfAnExecutionLanguage()
    {
        var root = Program.FindRoot(); foreach (var file in Directory.GetFiles(Path.Combine(root, "tests/VisualParity/cases"), "*.json"))
        { var m = ParityJson.Read<CaseMetadata>(file); m.Validate(); Assert.True(Cases.Entries.ContainsKey(m.AvaloniaEntry)); }
        var path = Path.Combine(Temp(), "invalid.json"); ParityJson.Write(path, new { schemaVersion = 2, id = "test.fixture", title = "t", family = "f", composeEntry = "c", avaloniaEntry = "a", events = new[] { "down" } });
        Assert.Throws<JsonException>(() => ParityJson.Read<CaseMetadata>(path));
    }
}
