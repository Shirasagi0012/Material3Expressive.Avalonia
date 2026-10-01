using Avalonia.Headless.XUnit;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
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
}
