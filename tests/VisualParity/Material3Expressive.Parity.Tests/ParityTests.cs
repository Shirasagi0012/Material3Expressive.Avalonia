using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using ComposeAnimation.TestSupport;
using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ParityAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Material3Expressive.Parity.Tests;

public class ParityTests
{
    private static ComparisonProfile Strict => new(1, "test", "Boundary/injection tests", .01, .001, 2, 0);
    private static Scenario Fixture => new() { Id = "test.fixture", Component = "surface", Required = ["layout", "behavior"] };
    private static readonly Dictionary<string, string> Environment = new()
    {
        ["platform"] = "windows", ["architecture"] = "x64", ["density"] = "1", ["fontScale"] = "1",
        ["fontSha256"] = "font", ["symbolSha256"] = "symbol", ["colorSpace"] = "sRGB"
    };
    private static Capture Capture(string runner, double x = 0) => new(runner, "scenario", "baseline", Environment,
        ["layout", "behavior", "motion"], [new(0, 0, null, [new("container", new(x, 0, 100, 50))],
            new() { ["value"] = 0, ["velocity"] = 0 }, 0, true)], []);
    private static Comparison Compare(Scenario scenario, Capture expected, Capture actual) =>
        ParityComparison.Run(scenario, expected, actual, Strict, "", "", Temp());
    private static string Temp() => Path.Combine(Path.GetTempPath(), "m3-parity-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void LayoutBoundaryAndInjectedPixelShift()
    {
        Assert.Equal("PASS", Compare(Fixture, Capture("compose"), Capture("avalonia", .009)).Status);
        var result = Compare(Fixture, Capture("compose"), Capture("avalonia", 1));
        Assert.Equal("FAIL", result.Status);
        Assert.Contains(result.Findings, x => x.Subject == "container" && x.Metric == "x" && x.Delta == 1);
    }
    [Fact]
    public void MissingCapabilityCannotPass()
    {
        Assert.Equal("UNVERIFIED", Compare(Fixture with { Required = ["resolvedFont"] }, Capture("compose"), Capture("avalonia")).Status);
    }
    [Fact]
    public void FontCapabilityWithoutResolvedFactsCannotPass()
    {
        var e = Capture("compose"); var a = Capture("avalonia");
        var node = new Node("label", new(0, 0, 100, 20), new("Save", 14, 400, [new(15, 30, 20, false)], []));
        e = e with { Capabilities = ["resolvedFont"], Frames = [e.Frames[0] with { Nodes = [node] }] };
        a = a with { Capabilities = ["resolvedFont"], Frames = [a.Frames[0] with { Nodes = [node] }] };
        Assert.Equal("UNVERIFIED", Compare(Fixture with { Required = ["resolvedFont"] }, e, a).Status);
    }
    [Fact]
    public void MissingSamplesCannotPass()
    {
        var actual = Capture("avalonia");
        actual = actual with { Frames = [actual.Frames[0] with { Values = [] }] };
        Assert.Equal("UNVERIFIED", Compare(Fixture with { Required = ["motion"] }, Capture("compose"), actual).Status);
    }
    [Fact]
    public void LateFrameAndWrongEnvironmentAreErrors()
    {
        var a = Capture("avalonia");
        Assert.Equal("ERROR", Compare(Fixture, Capture("compose"), a with { Frames = [a.Frames[0] with { TimeUs = 1 }] }).Status);
        Assert.Equal("ERROR", Compare(Fixture, Capture("compose"), a with { Environment = new(Environment) { ["fontSha256"] = "other" } }).Status);
    }
    [Fact]
    public void VelocityResetIsDetected()
    {
        var e = Capture("compose"); var a = Capture("avalonia");
        e = e with { Frames = [e.Frames[0] with { Values = new() { ["value"] = 10, ["velocity"] = 30 } }] };
        a = a with { Frames = [a.Frames[0] with { Values = new() { ["value"] = 10, ["velocity"] = 0 } }] };
        Assert.Contains(Compare(Fixture with { Required = ["motion"] }, e, a).Findings, x => x.Metric == "velocity" && x.Status == "FAIL");
    }
    [Fact]
    public void SmallMissingFeatureIsNotDilutedByBackground()
    {
        using var e = new SKBitmap(240, 160); e.Erase(SKColors.White);
        using var a = new SKBitmap(240, 160); a.Erase(SKColors.White);
        e.SetPixel(120, 80, SKColors.Black);
        var result = ImageComparison.Compare(e, a, SKColors.White, 2);
        Assert.Equal(1, result.DifferentPixels);
        Assert.Equal(255, result.MaximumChannelDelta);
        Assert.True(result.ContourDistance > 1);
    }
    [Fact]
    public void ChannelThresholdBoundary()
    {
        using var e = new SKBitmap(1, 1); e.Erase(new SKColor(100, 100, 100));
        using var a = new SKBitmap(1, 1); a.Erase(new SKColor(102, 100, 100));
        Assert.Equal(0, ImageComparison.Compare(e, a, SKColors.White, 2).DifferentPixels);
        a.Erase(new SKColor(103, 100, 100));
        Assert.Equal(1, ImageComparison.Compare(e, a, SKColors.White, 2).DifferentPixels);
    }
    [Fact]
    public void ManifestRejectsTampering()
    {
        var directory = Temp();
        ParityJson.Write(Path.Combine(directory, "observations.json"), Capture("compose"));
        ParityJson.Seal(directory, "compose", Environment);
        ParityJson.Verify(directory);
        File.AppendAllText(Path.Combine(directory, "observations.json"), " ");
        Assert.Throws<InvalidDataException>(() => ParityJson.Verify(directory));
    }
    [Fact]
    public void NestedManifestIsAlsoCoveredByArtifactIntegrity()
    {
        var directory = Temp();
        ParityJson.Write(Path.Combine(directory, "observations.json"), Capture("compose"));
        ParityJson.Write(Path.Combine(directory, "nested", "manifest.json"), new { input = 1 });
        ParityJson.Seal(directory, "compose", Environment);
        File.AppendAllText(Path.Combine(directory, "nested", "manifest.json"), " ");
        Assert.Throws<InvalidDataException>(() => ParityJson.Verify(directory));
    }
    [Fact]
    public async Task ManualClockDoesNotInsertFramesOrDeliverNewWaitersInSameBatch()
    {
        using var clock = new ManualAnimationFrameClock();
        var first = clock.NextFrameAsync(TestContext.Current.CancellationToken).AsTask();
        Task<TimeSpan>? second = null;
        async Task Consume() { await first; second = clock.NextFrameAsync(default).AsTask(); }
        var consuming = Consume();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(17));
        Assert.True(consuming.IsCompletedSuccessfully);
        Assert.Equal(TimeSpan.FromMilliseconds(17), await first);
        Assert.NotNull(second); Assert.False(second.IsCompleted);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(17));
        Assert.Equal(TimeSpan.FromMilliseconds(34), await second);
    }
    [AvaloniaFact]
    public async Task ActualCaptureRepeatsWithoutAdvancingMotionDuringScreenshots()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/motion.tween.reverse.json");
        var s = ParityJson.Read<Scenario>(input);
        var first = Temp(); var second = Temp();
        var a = await AvaloniaCapture.RunAsync(s, input, first, root);
        var b = await AvaloniaCapture.RunAsync(s, input, second, root);
        Assert.Empty(a.Errors); Assert.Empty(b.Errors);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(a, ParityJson.Options), System.Text.Json.JsonSerializer.Serialize(b, ParityJson.Options));
        foreach (var checkpoint in s.Checkpoints)
            Assert.Equal(ParityJson.Hash(Path.Combine(first, $"frames/{checkpoint:D4}.png")), ParityJson.Hash(Path.Combine(second, $"frames/{checkpoint:D4}.png")));
    }
    [AvaloniaFact]
    public async Task AddingScreenshotsDoesNotChangeAnimationSamples()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/motion.spring.reverse.json");
        var sparse = ParityJson.Read<Scenario>(input);
        var dense = sparse with { Checkpoints = Enumerable.Range(0, sparse.FramesUs.Length).ToArray() };
        var a = await AvaloniaCapture.RunAsync(sparse, input, Temp(), root);
        var b = await AvaloniaCapture.RunAsync(dense, input, Temp(), root);
        for (var i = 0; i < a.Frames.Length; i++)
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(a.Frames[i] with { Image = null }, ParityJson.Options),
                System.Text.Json.JsonSerializer.Serialize(b.Frames[i] with { Image = null }, ParityJson.Options));
    }
    [AvaloniaFact]
    public async Task ButtonUsesRealThemeAndInput()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/button.filled.quick-tap.json");
        var s = ParityJson.Read<Scenario>(input);
        var result = await AvaloniaCapture.RunAsync(s, input, Temp(), root);
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.Frames[^1].Clicks);
        Assert.Contains(result.Frames[0].Nodes, x => x.Id == "content");
        Assert.Equal(40, result.Frames[0].Nodes.First(x => x.Id == "allocation").Bounds.Height);
    }
    [AvaloniaFact]
    public async Task TextPaintSuppressionPreservesLayoutAndLeavesRealGlyphContribution()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/text.latin.json");
        var scenario = ParityJson.Read<Scenario>(input);
        var directory = Temp(); var result = await AvaloniaCapture.RunAsync(scenario, input, directory, root);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Frames[0].Nodes.Single(x => x.Id == "label").Text!.ResolvedFonts);
        using var full = SKBitmap.Decode(Path.Combine(directory, result.Frames[0].Image!));
        using var suppressed = SKBitmap.Decode(Path.Combine(directory, result.Frames[0].WithoutTextImage!));
        using var blank = new SKBitmap(full.Width, full.Height); blank.Erase(SKColors.White);
        Assert.Equal(0, ImageComparison.Compare(suppressed, blank, SKColors.White, 0).DifferentPixels);
        using var paint = ImageComparison.TextPaint(full, suppressed);
        blank.Erase(SKColors.Black);
        Assert.True(ImageComparison.Compare(paint, blank, SKColors.Black, 0).DifferentPixels > 0);
    }
    [AvaloniaFact]
    public async Task DiagnosticPaintsDoNotChangeTextButtonInteractionAnimation()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/button.filled.quick-tap.json");
        var scenario = ParityJson.Read<Scenario>(input) with { Content = "text", Required = ["layout", "raster", "behavior", "text", "textLayers"] };
        var dense = scenario with { Checkpoints = Enumerable.Range(0, scenario.FramesUs.Length).ToArray() };
        var first = Temp(); var second = Temp();
        await AvaloniaCapture.RunAsync(scenario, input, first, root);
        await AvaloniaCapture.RunAsync(dense, input, second, root);
        Assert.Empty(Program.CheckpointStability(first, second));
    }
    [AvaloniaFact]
    public async Task RealComponentMutationIsVisibleToComparer()
    {
        var root = Program.FindRoot();
        var input = Path.Combine(root, "tests/VisualParity/scenarios/surface.radius-12.json");
        var scenario = ParityJson.Read<Scenario>(input);
        var first = Temp(); var second = Temp();
        var e = (await AvaloniaCapture.RunAsync(scenario, input, first, root)) with { Runner = "compose" };
        var a = await AvaloniaCapture.RunAsync(scenario with { Radius = 0 }, input, second, root);
        var result = ParityComparison.Run(scenario, e, a, Strict, first, second, Temp());
        Assert.Equal("FAIL", result.Status);
        Assert.Contains(result.Findings, x => x.Subject == "raster" && x.Metric == "differentPixels");
    }
}
