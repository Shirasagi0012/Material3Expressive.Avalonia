using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [Fact]
    public void DifferenceImagePreservesMaximumRgbaDeltaAndPixelPositions()
    {
        var ed = Temp(); var ad = Temp(); var output = Temp();
        Capture Image(string runner, string directory, SKColor[] pixels)
        {
            Directory.CreateDirectory(directory);
            using var bitmap = new SKBitmap(new SKImageInfo(3, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            for (var i = 0; i < pixels.Length; i++) bitmap.SetPixel(i % 3, i / 3, pixels[i]);
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(directory, "image.png"), png.ToArray());
            var capture = Capture(runner);
            return capture with { Samples = [capture.Samples[0] with
                { Rasters = [PixelData.FromPng(directory, "full", "image.png")] }] };
        }

        var e = Image("compose", ed,
        [
            new(10, 20, 30, 255), new(10, 100, 30, 255), new(10, 20, 30, 255),
            new(80, 120, 200, 0), new(0, 0, 0, 0), new(81, 121, 201, 128)
        ]);
        var a = Image("avalonia", ad,
        [
            new(51, 20, 30, 255), new(10, 27, 30, 255), new(10, 20, 139, 255),
            new(80, 120, 200, 151), new(211, 17, 19, 0), new(81, 121, 201, 128)
        ]);
        var raster = ParityComparison.Run(Metadata, e, a, ed, ad, output).Samples[0].Rasters.Single();
        var image = PixelData.DecodePng(Path.Combine(output, raster.DifferenceImage!));
        Assert.Equal((3, 2), (image.Width, image.Height));
        // R, negative G, B, alpha-only, hidden RGB under zero alpha, and an equal pixel.
        Assert.Equal(new byte[]
        {
            41, 41, 41, 255, 73, 73, 73, 255, 109, 109, 109, 255,
            151, 151, 151, 255, 211, 211, 211, 255, 0, 0, 0, 255
        }, image.Bytes);
        Assert.Equal(5, raster.Metrics!.DifferentPixels);
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
}
