using Avalonia.Headless.XUnit;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaTheory]
    [InlineData("press")]
    [InlineData("space")]
    [InlineData("disable")]
    [InlineData("theme")]
    public async Task ButtonObservationsUseContinuousFramesWithoutChangingInputTimes(string action)
    {
        var result = await CaptureButtonTimeline(action);
        var capture = result.Capture;
        Assert.Empty(capture.Errors);

        (string Id, long TimeUs)[] expectedSamples = action switch
        {
            "press" =>
            [
                ("initial", 0), ("pointer-enter", 1000), ("down-event", 2000),
                ("time-16000", 16000), ("time-32000", 32000), ("time-75000", 75000),
                ("time-120000", 120000), ("time-225000", 225000), ("time-400000", 400000),
                ("release-event", 401000), ("time-416000", 416000), ("time-450000", 450000),
                ("time-551000", 551000), ("time-700000", 700000)
            ],
            "space" =>
            [
                ("initial", 0), ("time-16000", 16000), ("time-121000", 121000),
                ("key-down", 122000), ("time-138000", 138000), ("time-197000", 197000),
                ("time-347000", 347000), ("key-up", 348000), ("time-364000", 364000),
                ("time-498000", 498000), ("time-700000", 700000)
            ],
            "disable" =>
            [
                ("initial", 0), ("pointer-enter", 1000), ("down-event", 2000),
                ("time-16000", 16000), ("time-32000", 32000), ("time-75000", 75000),
                ("disabled-during-press", 76000), ("time-80000", 80000), ("time-92000", 92000),
                ("time-96000", 96000), ("time-112000", 112000), ("time-128000", 128000),
                ("time-225000", 225000), ("time-400000", 400000), ("disabled-release", 401000), ("reenabled", 402000),
                ("time-418000", 418000), ("time-600000", 600000)
            ],
            "theme" =>
            [
                ("initial", 0), ("mutation", 1000), ("time-16000", 16000), ("time-400000", 400000),
                ("restored", 401000), ("time-416000", 416000), ("time-800000", 800000)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        Assert.Equal(expectedSamples, capture.Samples.Select(s => (s.Id, s.TimeUs)).ToArray());

        // Observations between grid ticks retain their logical time and the last delivered frame.
        // In particular, taking the 75 ms observation must not inject a 75 ms animation frame.
        Assert.All(capture.Samples, sample =>
            Assert.Equal((long?)(sample.TimeUs / 16000 * 16000), sample.FrameTimeUs));
        var expectedFrames = new long[] { -1000, 0 }.Concat(
            Enumerable.Range(1, checked((int)(expectedSamples[^1].TimeUs / 16000)))
                .Select(index => index * 16000L)).ToArray();
        var frames = capture.Trace.Where(t => t.Operation == "frame").ToArray();
        Assert.Equal(expectedFrames, frames.Select(t => t.TimeUs).ToArray());

        long[] expectedInputTimes = action switch
        {
            "press" => [1000, 2000, 401000],
            "space" => [1000, 122000, 348000],
            "disable" => [1000, 2000, 76000, 401000, 402000],
            "theme" => [1000, 401000],
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        var inputs = capture.Trace.Where(t => t.Operation is "input" or "mutation").ToArray();
        Assert.Equal(expectedInputTimes, inputs.Select(t => t.TimeUs).ToArray());
        Assert.All(inputs, input =>
        {
            var precedingFrame = frames.Last(frame => frame.Sequence < input.Sequence);
            Assert.Equal(input.TimeUs / 16000 * 16000, precedingFrame.TimeUs);
        });
    }

    [AvaloniaFact]
    public async Task AdditionalButtonImagesDoNotChangeContinuousFrameOrAnimationHistory()
    {
        var ordinary = await CaptureButtonTimeline("press");
        var allImages = await CaptureButtonTimeline("press", extraImages: true);
        Assert.Empty(ordinary.Capture.Errors);
        Assert.Empty(allImages.Capture.Errors);
        Assert.Contains(ordinary.Capture.Samples, sample => sample.Rasters.Length == 0);
        Assert.All(allImages.Capture.Samples, sample => Assert.Single(sample.Rasters));
        Assert.Empty(Program.Stability(ordinary.Directory, allImages.Directory, extraImages: true));
    }

    [AvaloniaFact]
    public async Task BaselineButtonUsesOlderPaddingAndRetainsItsShapeDuringPress()
    {
        var result = await CaptureButtonTimeline("press", baseline: true);
        var capture = result.Capture;
        Assert.Empty(capture.Errors);
        var configuration = Assert.Single(capture.Trace, t => t.Operation == "configuration").Data!.Value;
        Assert.Equal(new double[] { 24, 8, 24, 8 },
            configuration.GetProperty("paddingDip").EnumerateArray().Select(x => x.GetDouble()).ToArray());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, configuration.GetProperty("pressedCornerRadius").ValueKind);
        var initial = capture.Samples[0].Points.Single(p => p.Id == "container")
            .Measurements.Single(m => m.Name == "cornerRadii").Numbers!;
        Assert.All(capture.Samples, sample => Assert.Equal(initial,
            sample.Points.Single(p => p.Id == "container").Measurements.Single(m => m.Name == "cornerRadii").Numbers));
        Assert.Equal(1d, capture.Samples[^1].Points.Single(p => p.Id == "button")
            .Measurements.Single(m => m.Name == "clickCount").Numbers!.Single());
    }

    [Fact]
    public void ButtonMatrixSeparatesAppearanceLayoutsAndIndependentBehavior()
    {
        var statics = ButtonCases.StaticConfigurations().ToArray();
        var dynamics = ButtonCases.DynamicConfigurations().ToArray();
        var all = statics.Concat(dynamics).ToArray();
        Assert.Equal(all.Length, all.Select(c => c.Id).Distinct().Count());
        Assert.All(statics, c => Assert.StartsWith("static.", c.Id));
        Assert.All(dynamics, c => Assert.StartsWith("dynamic.", c.Id));
        var appearance = statics.Where(c => c.Id.StartsWith("static.appearance.")).ToArray();
        Assert.Equal(50, appearance.Length);
        Assert.Equal(50, appearance.Select(c => (c.Config.Style, c.Config.Size, c.Config.Shape)).Distinct().Count());
        Assert.All(appearance, c =>
        {
            Assert.NotNull(c.Config.Width);
            Assert.Null(c.Config.Height);
            Assert.False(c.Config.Empty);
        });
        Assert.Equal(5, statics.Count(c => c.Id.StartsWith("static.layout.empty.") && c.Config.Empty && c.Config.Width == null));
        Assert.Equal(5, statics.Count(c => c.Id.StartsWith("static.layout.intrinsic.") && !c.Config.Empty && c.Config.Width == null));
        Assert.Equal(15, statics.Count(c => c.Id.StartsWith("static.layout.icons.")));
        Assert.Equal(10, dynamics.Count(c => c.Id.StartsWith("dynamic.press.filled.")));
        Assert.Equal(5, dynamics.Count(c => c.Id.StartsWith("dynamic.disable-during-press.")));
        Assert.Equal(5, dynamics.Count(c => c.Id.StartsWith("dynamic.change-theme.")));
        Assert.Contains(dynamics, c => c.Id == "dynamic.drag-out-back");
        Assert.Contains(dynamics, c => c.Id == "dynamic.disabled-keyboard.enter");
        Assert.Contains(dynamics, c => c.Id == "dynamic.baseline-press");
    }

    [AvaloniaTheory]
    [InlineData("static.layout.intrinsic.small", true)]
    [InlineData("static.layout.empty.small", false)]
    [InlineData("static.layout.icons.small.both", true)]
    [InlineData("static.layout.rtl-icons.both", true)]
    [InlineData("static.disabled.filled", true)]
    public async Task ButtonFixtureCapturesGeometricContentWithoutText(string id, bool hasContent)
    {
        var config = ButtonCases.StaticConfigurations().Single(c => c.Id == id).Config;
        var result = await CaptureButton(config);
        var sample = Assert.Single(result.Capture.Samples);
        Assert.Empty(result.Capture.Errors);
        Assert.DoesNotContain(sample.Points, p => p.Role == "text-block");
        Assert.DoesNotContain(sample.Points.SelectMany(p => p.Measurements), m => m.Category == "text");
        Assert.Equal(hasContent, sample.Points.Any(p => p.Id == "content"));
        if (id is "static.layout.icons.small.both" or "static.layout.rtl-icons.both")
        {
            Assert.Equal(new[] { "end-icon", "start-icon" }, sample.Points.Where(p => p.Role == "icon").Select(p => p.Id).Order().ToArray());
            var raster = Assert.Single(sample.Rasters);
            var pixels = PixelData.Read(result.Directory, raster);
            foreach (var icon in sample.Points.Where(p => p.Role == "icon"))
            {
                var box = icon.Measurements.Single(m => m.Name == "box").Numbers!;
                var paint = icon.Measurements.Single(m => m.Name == "color").Numbers!.Select(v => (byte)v).ToArray();
                // The arrow's upper-right head is filled; its upper-left stem is empty.
                // Read interior pixels to detect unintended automatic mirroring in either direction.
                Assert.Equal(paint, Pixel(11.5, 2.5));
                Assert.NotEqual(paint, Pixel(8.5, 2.5));
                byte[] Pixel(double x, double y)
                {
                    var px = (int)Math.Floor((box[0] + x / 20 * box[2]) * 4);
                    var py = (int)Math.Floor((box[1] + y / 20 * box[3]) * 4);
                    var index = (py * raster.Width + px) * 4;
                    return pixels[index..(index + 4)];
                }
            }
            if (id == "static.layout.rtl-icons.both")
            {
                double Left(string pointId) => sample.Points.Single(p => p.Id == pointId).Measurements.Single(m => m.Name == "box").Numbers![0];
                Assert.True(Left("start-icon") > Left("content"));
                Assert.True(Left("end-icon") < Left("content"));
            }
        }
        if (id == "static.layout.intrinsic.small")
            Assert.Equal(new double[] { 32, 16 }, sample.Points.Single(p => p.Id == "content").Measurements.Single(m => m.Name == "size").Numbers);
        Assert.DoesNotContain(sample.Points.SelectMany(p => p.Measurements), m => m.Name is "corners" or "nativeCornerRadii");
    }

    [AvaloniaFact]
    public async Task ButtonCustomPaddingPlacesContentFromLogicalStartInBothDirections()
    {
        var offsets = new List<double>();
        foreach (var rtl in new[] { false, true })
        {
            var result = await CaptureButton(new ButtonConfig(Custom: "padding", Rtl: rtl));
            Assert.Empty(result.Capture.Errors);
            var sample = Assert.Single(result.Capture.Samples);
            var container = sample.Points.Single(p => p.Id == "container").Measurements.Single(m => m.Name == "box").Numbers!;
            var content = sample.Points.Single(p => p.Id == "content");
            var box = content.Measurements.Single(m => m.Name == "box").Numbers!;
            var paint = content.Measurements.Single(m => m.Name == "color").Numbers!.Select(v => (byte)v).ToArray();
            var center = container[0] + container[2] / 2;
            var contentCenter = box[0] + box[2] / 2;
            var contentY = box[1] + box[3] / 2;
            offsets.Add(contentCenter - center);
            Assert.True(rtl ? contentCenter > center : contentCenter < center);
            var raster = Assert.Single(sample.Rasters);
            var pixels = PixelData.Read(result.Directory, raster);
            Assert.Equal(paint, Pixel(contentCenter, contentY));
            Assert.NotEqual(paint, Pixel(2 * center - contentCenter, contentY));

            byte[] Pixel(double x, double y)
            {
                var index = ((int)Math.Floor(y * 4) * raster.Width + (int)Math.Floor(x * 4)) * 4;
                return pixels[index..(index + 4)];
            }
        }
        Assert.Equal(-offsets[0], offsets[1]);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ButtonAsymmetricCornerMeasurementsMatchVisiblePhysicalPixels(bool rtl)
    {
        var result = await CaptureButton(new ButtonConfig(Shape: "asymmetric", Rtl: rtl));
        Assert.Empty(result.Capture.Errors);
        var sample = Assert.Single(result.Capture.Samples);
        var container = sample.Points.Single(p => p.Id == "container");
        var box = container.Measurements.Single(m => m.Name == "box").Numbers!;
        var shape = container.Measurements.Single(m => m.Name == "cornerRadii");
        Assert.Equal("button.container.normalized-visual-rounded-outline", shape.Definition);
        var radii = shape.Numbers!;
        Assert.True(rtl ? radii[0] > radii[2] : radii[0] < radii[2]);
        var raster = Assert.Single(sample.Rasters);
        var pixels = PixelData.Read(result.Directory, raster);
        var background = pixels[..4];
        // Interior to the small corner, exterior to the large corner, clear of antialiasing.
        // Validate physical direction against the captured bitmap, not only the reported radii.
        var left = Pixel(box[0] + 3, box[1] + 3);
        var right = Pixel(box[0] + box[2] - 3, box[1] + 3);
        Assert.Equal(background, rtl ? left : right);
        Assert.NotEqual(background, rtl ? right : left);

        byte[] Pixel(double x, double y)
        {
            var index = ((int)Math.Floor(y * 4) * raster.Width + (int)Math.Floor(x * 4)) * 4;
            return pixels[index..(index + 4)];
        }
    }

    [AvaloniaFact]
    public async Task ButtonContentWidthMutationAndRestorationActuallyChangeAutoLayout()
    {
        var result = await CaptureButton(new ButtonConfig(Action: "content", Width: null));
        Assert.Empty(result.Capture.Errors);
        var samples = result.Capture.Samples;
        double Width(string id) => samples.Single(s => s.Id == id).Points.Single(p => p.Id == "container")
            .Measurements.Single(m => m.Name == "size").Numbers![0];
        Assert.True(Width("mutation") > Width("initial"));
        Assert.Equal(Width("initial"), Width("restored"));
        Assert.Equal(new double[] { 32, 64, 32 }, new[] { "initial", "mutation", "restored" }.Select(id =>
            samples.Single(s => s.Id == id).Points.Single(p => p.Id == "content").Measurements.Single(m => m.Name == "size").Numbers![0]).ToArray());
    }

    [AvaloniaFact]
    public async Task ButtonPaintTracksThemeAndRestoresWithoutChangingFixedWidth()
    {
        var result = await CaptureButton(new ButtonConfig(Action: "theme"));
        Assert.Empty(result.Capture.Errors);
        double[] Color(string id) => result.Capture.Samples.Single(s => s.Id == id).Points.Single(p => p.Id == "content")
            .Measurements.Single(m => m.Name == "color").Numbers!;
        Assert.NotEqual(Color("initial"), Color("mutation"));
        Assert.Equal(Color("initial"), Color("restored"));
        var widths = result.Capture.Samples.Select(s => s.Points.Single(p => p.Id == "container")
            .Measurements.Single(m => m.Name == "size").Numbers![0]).Distinct();
        Assert.Single(widths);
    }

    [AvaloniaTheory]
    [InlineData("disabled", 0)]
    [InlineData("quick", 1)]
    [InlineData("cancel", 0)]
    [InlineData("drag-back", 0)]
    [InlineData("reverse", 2)]
    [InlineData("disable", 0)]
    public async Task ButtonPointerBranchesObserveRealClickCallbacks(string action, int expectedClicks)
    {
        var result = await CaptureButton(new ButtonConfig(Action: action, Enabled: action != "disabled"));
        Assert.Empty(result.Capture.Errors);
        Assert.Equal((double)expectedClicks, result.Capture.Samples[^1].Points.Single(p => p.Id == "button")
            .Measurements.Single(m => m.Name == "clickCount").Numbers!.Single());
        var trace = result.Capture.Trace;
        if (action == "disabled")
        {
            Assert.Contains(result.Capture.Samples, s => s.Id == "disabled-pointer-exit");
            Assert.False(result.Capture.Samples[^1].Points.Single(p => p.Id == "button").Measurements.Single(m => m.Name == "hovered").Boolean);
        }
        if (action == "quick")
        {
            var down = trace.Single(t => t.Operation == "input" && t.Data!.Value.GetProperty("action").GetString() == "down");
            var up = trace.Single(t => t.Operation == "input" && t.Data!.Value.GetProperty("action").GetString() == "up");
            Assert.DoesNotContain(trace, t => t.Operation == "frame" && t.Sequence > down.Sequence && t.Sequence < up.Sequence);
        }
    }

    static async Task<(Capture Capture, string Directory)> CaptureButtonTimeline(string action, bool extraImages = false, bool baseline = false)
        => await CaptureButton(new ButtonConfig(Action: action, Baseline: baseline), extraImages);

    static async Task<(Capture Capture, string Directory)> CaptureButton(ButtonConfig config, bool extraImages = false)
    {
        var root = Program.FindRoot();
        var metadata = CaseMetadata.Create("button.fixture", "Button non-text fixture",
            "button", config.Action == "none" ? "static" : "behavior", config);
        var input = Path.Combine(Temp(), "case.json");
        ParityJson.Write(input, metadata);
        var directory = Temp();
        var writer = new CaptureWriter(metadata, input, directory, root, extraImages);
        if (config.Action == "none") await new ButtonCases().Static(writer);
        else await new ButtonCases().Behavior(writer);
        var capture = writer.Finish();
        ParityJson.Verify(directory);
        return (capture, directory);
    }
}
