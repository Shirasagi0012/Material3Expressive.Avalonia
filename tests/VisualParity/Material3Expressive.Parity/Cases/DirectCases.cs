namespace Material3Expressive.Parity;

public sealed record DirectConfig(double Answer);

[ParitySuite("protocol")]
public sealed class DirectCases
{
    public static IEnumerable<CaseData<DirectConfig>> Configurations() =>
        [new("direct", "Author-built observation without a host", new(42))];


    [ParityScenario("direct", Data = nameof(Configurations))]
    public Task DirectObservation(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<DirectConfig>();
        // Author-owned data and a transparent pixel fixture; no scene or animation host.
        var path = "images/pixels.png";
        Directory.CreateDirectory(Path.Combine(writer.Output, "images"));
        using var bitmap =
            new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(2, 1, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, new SkiaSharp.SKColor(80, 120, 200, 0));
        bitmap.SetPixel(1, 0, new SkiaSharp.SKColor(81, 121, 201, 128));
        using (var png = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            File.WriteAllBytes(Path.Combine(writer.Output, path), png.ToArray());
        var raster = PixelData.FromPng(writer.Output, "full", path) with { Contribution = "declared-pixel-fixture" };
        writer.Submit(new("initial", 0, "author-built", [
            new("custom", "measurement", null,
                [Measurement.Numeric("answer", config.Answer, "count", source: "declared")])
        ], [raster]));
        return Task.CompletedTask;
    }
}
