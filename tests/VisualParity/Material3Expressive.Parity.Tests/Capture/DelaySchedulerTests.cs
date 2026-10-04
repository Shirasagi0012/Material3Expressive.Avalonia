using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ComposeAnimation.TestSupport;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaFact]
    public async Task DelayScheduler_AdvancesBeforeFramesWhileDrawAndSnapshotRemainPassive()
    {
        var result = await Actual(async writer =>
        {
            var time = new ManualTimeProvider();
            await using var host = new AvaloniaHost(writer, 16, 16, time);
            host.Mount(new Border()); host.Frame(0);
            var completions = new List<double>();
            var waiting = host.Window.AnimationRoot.Launch(async ct =>
            {
                await host.Window.AnimationRoot.DelayAsync(TimeSpan.FromMilliseconds(50), ct);
                completions.Add(time.Elapsed.TotalMilliseconds);
                await host.Window.AnimationRoot.DelayAsync(TimeSpan.FromMilliseconds(50), ct);
                completions.Add(time.Elapsed.TotalMilliseconds);
            });
            host.Draw(); writer.Submit(host.Snapshot("passive", "passive", [], image: false)); host.Draw();
            Assert.Equal(TimeSpan.Zero, time.Elapsed); Assert.Empty(completions);
            host.AdvanceTo(125000);
            Assert.Equal(new double[] { 50, 100 }, completions);
            Assert.Equal(0, host.FrameTimeUs); Assert.True(waiting.IsCompletedSuccessfully);
            writer.Submit(host.Snapshot("delays", "delays without frames", [], image: false));
            host.Frame(200000);
            Assert.Equal(TimeSpan.FromMilliseconds(200), time.Elapsed); Assert.Equal(200000, host.FrameTimeUs);
            writer.Submit(host.Snapshot("frame", "frame after delays", [], image: false));
        });
        Assert.Empty(result.Capture.Errors);
    }
}
