using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using System.Reflection;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Parity;
using MaterialTypography.Roboto;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DrawWaitsForSubmittedPixelsAndRenderCallbackChanges(bool changeAfterRender)
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 16, 16);
            var border = new Border { Background = Brushes.Red };
            host.Mount(border);
            host.Frame(0);
            var compositor = ElementComposition.GetElementVisual(border)!.Compositor;

            // Automatic wall-clock rendering must not accidentally repair an incomplete Draw.
            // Pause only the headless timer; explicit ForceRenderTimerTick remains operational.
            var timers = ((IEnumerable<DispatcherTimer>)typeof(Dispatcher)
                .GetField("_timers", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Dispatcher.UIThread)!).Where(t => Equals(t.Tag, "HeadlessRenderTimer")).ToArray();
            Assert.NotEmpty(timers);
            foreach (var timer in timers) timer.Stop();
            try
            {
                var previous = compositor.RequestCompositionBatchCommitAsync();
                Dispatcher.UIThread.RunJobs();
                Assert.False(previous.Rendered.IsCompleted);
                border.Background = Brushes.Blue;
                var blue = compositor.RequestCompositionBatchCommitAsync();
                CompositionBatch? green = null;
                Task? callback = null;
                if (changeAfterRender)
                    callback = blue.Rendered.ContinueWith(_ => Dispatcher.UIThread.Post(() =>
                    {
                        border.Background = Brushes.Lime;
                        green = compositor.RequestCompositionBatchCommitAsync();
                    }), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                host.Draw();
                Assert.True(blue.Rendered.IsCompletedSuccessfully);
                if (changeAfterRender)
                {
                    Assert.True(callback!.IsCompletedSuccessfully);
                    Assert.NotNull(green);
                    Assert.True(green.Rendered.IsCompletedSuccessfully);
                }
                var first = host.Snapshot("drawn", "fixture", []);
                var second = host.Snapshot("passive-repeat", "fixture", []);
                writer.Submit(first); writer.Submit(second);
                var pixels = PixelData.Read(writer.Output, first.Rasters.Single());
                Assert.Equal(changeAfterRender ? new byte[] { 0, 255, 0, 255 } : [0, 0, 255, 255], pixels[..4]);
                Assert.Equal(pixels, PixelData.Read(writer.Output, second.Rasters.Single()));
                Assert.Equal(0, host.FrameTimeUs);
            }
            finally
            {
                foreach (var timer in timers) timer.Start();
            }
        });
        Assert.Empty(result.Capture.Errors);
        Assert.Single(result.Capture.Trace, t => t.Operation == "frame");
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

    [AvaloniaFact]
    public async Task InterruptedMotionRepeatsAndExtraImagesDoNotAdvanceClock()
    {
        async Task Execute(CaptureWriter writer)
        {
            await using var host = new AvaloniaHost(writer);
            var marker = new Border { Width = 18, Height = 18, Background = Brushes.Black };
            host.Mount(new Canvas { Children = { marker } });
            host.Frame(0);
            var scope = VisualAnimationScope.Create(marker);
            var value = scope.CreateAnimatable(0d);
            scope.SubscribeValue(value, x => Canvas.SetLeft(marker, x));
            foreach (var target in new[] { 100d, 0d })
            {
                _ = host.Window.AnimationRoot.Launch(ct => value.AnimateToAsync(target,
                    new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: ComposeEasings.Linear), cancellationToken: ct));
                foreach (var offset in new long[] { 16000, 40000, 80000 })
                {
                    var time = target == 100 ? offset : 80000 + offset;
                    host.Frame(time);
                    writer.Submit(host.Snapshot("frame-" + time, "fixture", [new("marker", "fixture", null,
                        [Measurement.Numeric("value", value.Value), Measurement.Flag("running", value.IsRunning)])], image: offset != 40000));
                }
            }
        }
        var a = await Actual(Execute); var b = await Actual(Execute); var dense = await Actual(Execute, true);
        Assert.Empty(a.Capture.Errors); Assert.Empty(Program.Stability(a.Directory, b.Directory)); Assert.Empty(Program.Stability(a.Directory, dense.Directory, true));
        Assert.Equal(new long[] { 0, 16000, 40000, 80000, 96000, 120000, 160000 }, a.Capture.Trace.Where(t => t.Operation == "frame").Select(t => t.TimeUs));
        var values = a.Capture.Samples.Select(s => s.Points.Single().Measurements.Single(m => m.Name == "value").Numbers![0]).ToArray();
        Assert.True(values[2] > values[0]); Assert.True(values[^1] < values[2]);
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
    public async Task HostDeliversRealPointerInputAndCapturesNativeText()
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer);
            var label = new TextBlock { Text = "Test", FontFamily = new(RobotoFontCollection.FamilyUri), FontSize = 14 };
            var target = new Border { Background = Brushes.White, Child = label };
            var down = 0; var up = 0;
            target.PointerPressed += (_, _) => down++;
            target.PointerReleased += (_, _) => up++;
            host.Mount(target); host.Frame(0);
            host.Input("move", new(10, 10)); host.Input("down", new(10, 10));
            host.AdvanceTo(5000); host.Input("up", new(10, 10)); host.Draw();
            writer.Submit(host.Snapshot("input", "fixture", [
                new("target", "container", null, [Measurement.Numeric("down", down), Measurement.Numeric("up", up)]),
                AvaloniaMeasurements.Text("label", "target", label, host.Window)
            ], image: false));
        });
        Assert.Empty(result.Capture.Errors);
        var final = result.Capture.Samples.Single(); Assert.Contains(result.Capture.Trace,t=>t.Operation=="advance"&&t.TimeUs==5000);
        Assert.All(final.Points.Single(p => p.Id == "target").Measurements, m => Assert.Equal(1, m.Numbers![0]));
        var label = final.Points.Single(p => p.Id == "label"); Assert.Contains("Roboto", label.Measurements.Single(m => m.Name == "resolvedFonts").Strings!);
        Assert.Equal(new double[] { 0, 4 }, label.Measurements.Single(m => m.Name == "line.0.range").Numbers);
        Assert.True(label.Measurements.Single(m => m.Name == "line.0.nativeRange").Numbers![1] >= 4);
        Assert.Equal("notRequested", final.RasterCollection!.Single().Status);
    }

    [AvaloniaFact]
    public async Task ParagraphBreakReaderPreservesInputConsumptionAndExcludesBreaksFromContent()
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer);
            var text = new TextBlock { Text = "A \r\n\r\nB 😀  \nC\u2028D\u2029", Width = 120,
                FontFamily = new(RobotoFontCollection.FamilyUri), FontSize = 14, TextWrapping = TextWrapping.Wrap };
            host.Mount(text); host.Frame(0);
            writer.Submit(host.Snapshot("text", "fixture", [AvaloniaMeasurements.Text("label", null, text, host.Window)], image: false));
        });
        var fields = result.Capture.Samples.Single().Points.Single().Measurements;
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
}
