using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaFact]
    public async Task HostAlwaysRendersAtFourTimesScaleWithLayoutAndPointerCoordinatesInDip()
    {
        const int scale = 4;
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 64, 48);
            var target = new Border { Width = 20, Height = 10, Background = Brushes.Red };
            Canvas.SetLeft(target, 12); Canvas.SetTop(target, 8);
            var presses = 0;
            target.PointerPressed += (_, e) =>
            {
                presses++;
                Assert.Equal(new Avalonia.Point(2, 2), e.GetPosition(target));
            };
            host.Mount(new Canvas { Children = { target } });
            host.Frame(0);
            Assert.Equal(scale, host.Window.RenderScaling);
            Assert.Equal(new Avalonia.Size(64, 48), host.Window.ClientSize);
            Assert.Equal(new Avalonia.Rect(12, 8, 20, 10), target.Bounds);
            host.Input("move", new(14, 10)); host.Input("down", new(14, 10));
            host.Input("up", new(14, 10)); host.Draw();
            Assert.Equal(1, presses);
            var sample = host.Snapshot("scaled", "fixture", []);
            var raster = Assert.Single(sample.Rasters);
            Assert.Equal(64 * scale, raster.Width); Assert.Equal(48 * scale, raster.Height);
            var pixels = PixelData.Read(writer.Output, raster);
            byte[] Pixel(int x, int y) => pixels[((y * raster.Width + x) * 4)..((y * raster.Width + x) * 4 + 4)];
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(12 * scale, 8 * scale));
            Assert.Equal(new byte[] { 255, 255, 255, 255 }, Pixel(12 * scale - 1, 8 * scale));
            writer.Submit(sample);
        });
        Assert.Equal(scale.ToString(), result.Capture.Environment["density"]);
        Assert.Empty(result.Capture.Errors);
    }

    [Fact]
    public void RasterRegionsConvertDipToPixelsBeforeRoundingAndRejectMismatchedDensity()
    {
        var ed = Temp(); var ad = Temp(); Directory.CreateDirectory(ed); Directory.CreateDirectory(ad);
        using var bitmap = new SKBitmap(20, 20); bitmap.Erase(SKColors.White);
        void Save(string directory) => File.WriteAllBytes(Path.Combine(directory, "image.png"), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
        Save(ed); bitmap.SetPixel(12, 9, SKColors.Black); Save(ad);
        Measurement Box(double x, double width) => Measurement.Vector("box", "rect", [x, 0.375, width, 2],
            "dip", "scene", "geometry", "layout.box.aabb.unclipped");
        var e = Capture("compose", Point(Box(-0.125, 3)));
        var a = Capture("avalonia", Point(Box(2, 1.125)));
        e = e with { Environment = new() { ["density"] = "4" }, Samples = [e.Samples[0] with { Rasters = [PixelData.FromPng(ed, "full", "image.png")] }] };
        a = a with { Environment = new() { ["density"] = "4" }, Samples = [a.Samples[0] with { Rasters = [PixelData.FromPng(ad, "full", "image.png")] }] };
        var raster = ParityComparison.Run(Metadata, e, a, ed, ad, Temp()).Samples[0].Rasters.Single();
        var region = Assert.Single(raster.Regions!);
        Assert.Equal(new[] { 0, 1, 13, 9 }, region.Bounds);
        Assert.Equal(1, region.Metrics.DifferentPixels);
        Assert.Equal(new[] { 12, 9, 1, 1 }, region.Metrics.DifferenceBounds);

        var differentDensity = a with { Environment = new() { ["density"] = "1" } };
        var unpaired = ParityComparison.Run(Metadata, e, differentDensity, ed, ad, Temp()).Samples[0].Rasters.Single();
        Assert.Equal("notCompared", unpaired.Status);
        Assert.Contains("density", unpaired.Issue!.Members);
        Assert.Null(unpaired.Metrics);
    }
}
