using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
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
