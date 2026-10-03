using Avalonia.Headless.XUnit;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaTheory]
    [InlineData("xsmall", 20)]
    [InlineData("small", 24)]
    [InlineData("medium", 24)]
    [InlineData("large", 32)]
    [InlineData("xlarge", 40)]
    public async Task IconVectorRetainsActualTemplateSizeAndColorInheritance(string size, double side)
    {
        var result = await CaptureIconButton(new(Size: size));
        Assert.Empty(result.Capture.Errors);
        var sample = Assert.Single(result.Capture.Samples);
        var icon = sample.Points.Single(p => p.Id == "icon");
        Assert.Equal(new[] { side, side }, icon.Measurements.Single(m => m.Name == "size").Numbers);
        Assert.Equal(new double[] { 255, 255, 255, 255 }, icon.Measurements.Single(m => m.Name == "color").Numbers);
        Assert.DoesNotContain(sample.Points.SelectMany(p => p.Measurements), m => m.Category == "text");
        var box = icon.Measurements.Single(m => m.Name == "box").Numbers!;
        Assert.Equal(160, box[0] + box[2] / 2);
        Assert.Equal(120, box[1] + box[3] / 2);
        var raster = Assert.Single(sample.Rasters);
        var pixels = PixelData.Read(result.Directory, raster);
        var index = ((int)((box[1] + side * 0.35) * 4) * raster.Width + (int)((box[0] + side * 0.35) * 4)) * 4;
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, pixels[index..(index + 4)]);
    }

    [AvaloniaFact]
    public async Task DefaultIconRetainsShapeAndExplicitPressedRadiusChangesIt()
    {
        var fixedShape = await CaptureIconButton(new(Action: "press"));
        var animated = await CaptureIconButton(new(Action: "press", PressedRadius: 8));
        Assert.Empty(fixedShape.Capture.Errors);
        Assert.Empty(animated.Capture.Errors);
        var initial = Radii(fixedShape.Capture.Samples[0]);
        Assert.All(fixedShape.Capture.Samples, sample => Assert.Equal(initial, Radii(sample)));
        Assert.Contains(animated.Capture.Samples, sample => Radii(sample)[0] > 8 && Radii(sample)[0] < initial[0]);
        Assert.Equal(8, Radii(animated.Capture.Samples.Single(s => s.Id == "time-400000"))[0]);
        Assert.All(animated.Capture.Samples, sample => Assert.Equal(sample.TimeUs / 16000 * 16000, sample.FrameTimeUs));
        Assert.Equal(new double[] { 24, 32, 24 }, (await CaptureIconButton(new(Action: "size"))).Capture.Samples
            .Where(s => s.Id is "initial" or "mutation" or "restored")
            .Select(s => s.Points.Single(p => p.Id == "icon").Measurements.Single(m => m.Name == "size").Numbers![0]).ToArray());
    }

    [AvaloniaTheory]
    [InlineData("disabled", 0)]
    [InlineData("quick", 1)]
    [InlineData("cancel", 0)]
    [InlineData("drag-back", 0)]
    [InlineData("reverse", 2)]
    [InlineData("disable", 0)]
    public async Task IconPointerBranchesUseRealCallbacks(string action, int clicks)
    {
        var result = await CaptureIconButton(new(Action: action, Enabled: action != "disabled"));
        Assert.Empty(result.Capture.Errors);
        Assert.Equal((double)clicks, result.Capture.Samples[^1].Points.Single(p => p.Id == "button")
            .Measurements.Single(m => m.Name == "clickCount").Numbers!.Single());
    }

    [AvaloniaFact]
    public async Task AdditionalIconImagesDoNotChangeFrameHistory()
    {
        var normal = await CaptureIconButton(new(Action: "press", PressedRadius: 8));
        var extra = await CaptureIconButton(new(Action: "press", PressedRadius: 8), true);
        Assert.Empty(Program.Stability(normal.Directory, extra.Directory, extraImages: true));
    }

    static double[] Radii(Sample sample) => sample.Points.Single(p => p.Id == "container")
        .Measurements.Single(m => m.Name == "cornerRadii").Numbers!;

    static async Task<(Capture Capture, string Directory)> CaptureIconButton(IconButtonConfig config, bool extraImages = false)
    {
        var root = Program.FindRoot();
        var metadata = CaseMetadata.Create("icon-button.fixture", "IconButton vector fixture", "icon-button", config.Action == "none" ? "static" : "behavior", config);
        var input = Path.Combine(Temp(), "case.json"); ParityJson.Write(input, metadata);
        var directory = Temp(); var writer = new CaptureWriter(metadata, input, directory, root, extraImages);
        if (config.Action == "none") await new IconButtonCases().Static(writer); else await new IconButtonCases().Behavior(writer);
        var capture = writer.Finish(); ParityJson.Verify(directory); return (capture, directory);
    }
}
