using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [Theory]
    [InlineData(20, 1, true)] // Both limits are inclusive: 20 / 100 = 0.2.
    [InlineData(21, 1, false)]
    [InlineData(1, 21, false)] // Low peak, but mean 0.21.
    public void RasterToleranceEnforcesPeakAndMean(int delta, int pixels, bool accepted)
    {
        var expected = new byte[400 * 4 * 4];
        var actual = (byte[])expected.Clone();
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < pixels * 4; x++) actual[(y * 400 + x) * 4] = (byte)delta;
        var result = ParityComparison.AssessRaster(expected, actual, 400, 4, []);
        Assert.Equal(delta, result.Full.MaximumChannelDelta);
        Assert.Equal(delta * pixels / 100.0, result.Full.MeanMaximumChannelDelta);
        Assert.Equal(accepted, result.Full.Accepted);
        Assert.Equal(accepted ? "withinTolerance" : "different", result.Status);
    }

    [Fact]
    public void LocalRegionPreventsBlankCanvasDilutingColorDifference()
    {
        var expected = new byte[400 * 4 * 4];
        var actual = (byte[])expected.Clone();
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++) actual[(y * 400 + x) * 4] = 1;
        var regions = new[] { new RasterRegion("content", [1, 0, 2, 4], ParityComparison.Pixels(expected, actual, 400, 4, [1, 0, 2, 4])) };
        var result = ParityComparison.AssessRaster(expected, actual, 400, 4, regions);
        Assert.True(result.Full.Accepted);
        Assert.Equal(new[] { 0, 0, 1, 1 }, result.Regions[0].Bounds);
        Assert.Equal(1, result.Regions[0].MeanMaximumChannelDelta);
        Assert.False(result.Regions[0].Accepted);
        Assert.Equal("different", result.Status);
    }

    [Fact]
    public void PeakStillRejectsLocalizedErrorOnLargeCanvas()
    {
        var expected = new byte[800 * 4 * 4];
        var actual = (byte[])expected.Clone();
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++) actual[(y * 800 + x) * 4] = 21;
        var result = ParityComparison.AssessRaster(expected, actual, 800, 4, []);
        Assert.Equal(0.105, result.Full.MeanMaximumChannelDelta);
        Assert.False(result.Full.Accepted);
    }

    [Fact]
    public void BoxAverageHasDeterministicRoundingAndIncludesEveryChannel()
    {
        var expected = new byte[8 * 4 * 4];
        var actual = (byte[])expected.Clone();
        actual[0] = 7; // 7/16 rounds down.
        actual[4 * 4 + 3] = 8; // 8/16 rounds up, alpha included.
        var result = ParityComparison.AssessRaster(expected, actual, 8, 4, []);
        Assert.Equal(1, result.Full.MaximumChannelDelta);
        Assert.Equal(0.5, result.Full.MeanMaximumChannelDelta);
        Assert.Throws<InvalidDataException>(() => ParityComparison.AssessRaster(new byte[5 * 4 * 4], new byte[5 * 4 * 4], 5, 4, []));
    }

    [Fact]
    public void ToleratedBlackWhiteEdgeRetainsExactEvidenceAndSurvivesResultRoundTrip()
    {
        var ed = Temp(); var ad = Temp(); var diff = Temp(); var output = Temp();
        Directory.CreateDirectory(ed); Directory.CreateDirectory(ad);
        using var bitmap = new SKBitmap(40, 40);
        bitmap.Erase(SKColors.White);
        void Save(string directory) => File.WriteAllBytes(Path.Combine(directory, "image.png"), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
        Save(ed);
        // One physical row of coverage difference on a black/white edge: raw 64, reduced 16.
        for (var x = 0; x < 4; x++) bitmap.SetPixel(x, 0, new SKColor(191, 191, 191));
        Save(ad);
        var box = Measurement.Vector("box", "rect", [0, 0, 10, 10], "dip", "scene", "geometry", "layout.box.aabb.unclipped");
        Capture Image(string runner, string directory)
        {
            var capture = Capture(runner, Point(box));
            return capture with { Environment = new() { ["density"] = "4" }, Samples = [capture.Samples[0] with
            { Rasters = [PixelData.FromPng(directory, "full", "image.png")], RasterCollection = [new("full", "captured")] }] };
        }
        var e = Image("compose", ed); var a = Image("avalonia", ad);
        var result = ParityComparison.Run(Metadata, e, a, ed, ad, diff);
        Assert.Equal("withinTolerance", result.Status);
        Assert.Equal(1, result.Coverage.WithinTolerance);
        Assert.Equal(0, result.Coverage.Different);
        var raster = result.Samples[0].Rasters.Single();
        Assert.Equal("different", raster.ExactStatus);
        Assert.Equal(4, raster.Metrics!.DifferentPixels);
        Assert.Equal(64, raster.Metrics.MaximumChannelDelta[0]);
        Assert.Equal(16, raster.Tolerance!.Full.MaximumChannelDelta);
        Assert.Equal(0.16, raster.Tolerance.Full.MeanMaximumChannelDelta);
        ResultFiles.Write(output, Metadata, result, e, a, ed, ad, diff);
        var read = ResultFiles.Read(output).Comparison;
        Assert.Equal(result.Coverage, read.Coverage);
        Assert.Equal("withinTolerance", read.Status);
        Assert.Equal("different", read.Samples[0].Rasters[0].ExactStatus);
        Assert.Equal(0.16, read.Samples[0].Rasters[0].Tolerance!.Regions.Single().MeanMaximumChannelDelta);
        Assert.Equal(File.ReadAllBytes(Path.Combine(ed, "image.png")), File.ReadAllBytes(Path.Combine(output, read.Samples[0].Rasters[0].Expected!.Image)));
        Assert.Equal("different", ParityComparison.Status(["withinTolerance", "different"]));
        Assert.Equal("notCompared", ParityComparison.Status(["withinTolerance", "notCompared"]));
    }
}
