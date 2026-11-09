using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Rendering.Composition;
using ComposeAnimation.TestSupport;
using Material3Expressive.Parity;
using SmoothScroll.Avalonia.Interaction;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaFact]
    public async Task DrawCompletesOrderedScrollInputBeforeTheNextExplicitAnimationFrame()
    {
        var result = await Actual(async writer =>
        {
            var time = new ManualTimeProvider();
            await using var host = new AvaloniaHost(writer, 16, 16, time);
            host.Mount(new Border()); host.Frame(0);
            var compositor = ElementComposition.GetElementVisual(host.Window)!.Compositor;
            var tracker = compositor.CreateInteractionTracker(null);
            try
            {
                tracker.MaxPosition = new Vector3D(0, 1000, 0);
                var calls = new List<string>();
                tracker.ConfigureVerticalScroll(new FrameOrderParticipant(() =>
                    tracker.DispatchToUI(() => calls.Add("movement"))), null, async (_, token) =>
                {
                    calls.Add("release");
                    Assert.True(await host.Window.AnimationRoot.WaitForNextFrameAsync(token));
                    calls.Add("animation-frame");
                    return 0;
                });
                host.Draw();
                host.AdvanceTo(16000);
                tracker.ApplyVerticalScrollDelta(10);
                tracker.CompleteVerticalScroll();
                Assert.Empty(calls);

                host.Draw();
                Assert.Equal(new[] { "movement", "release" }, calls);
                Assert.Equal(10, tracker.Position.Y);
                Assert.Equal(0, host.FrameTimeUs);
                Assert.Equal(TimeSpan.FromMilliseconds(16), time.Elapsed);
                host.Draw();
                Assert.Equal(new[] { "movement", "release" }, calls);
                writer.Submit(host.Snapshot("input-drained", "input before frame", [], image: false));

                host.Frame(16000);
                Assert.Equal(new[] { "movement", "release", "animation-frame" }, calls);
                Assert.Equal(16000, host.FrameTimeUs);
                writer.Submit(host.Snapshot("next-frame", "first release frame", [], image: false));
            }
            finally
            {
                tracker.Dispose();
                host.Draw();
            }
        });
        Assert.Empty(result.Capture.Errors);
        Assert.Equal(new long[] { 0, 16000 }, result.Capture.Trace.Where(t => t.Operation == "frame").Select(t => t.TimeUs));
    }

    private sealed class FrameOrderParticipant(Action publish) : IScrollMovementParticipantFactory, IScrollMovementParticipant
    {
        public IScrollMovementParticipant Create() => this;
        public double PreScroll(in ScrollMovementContext context) => 0;
        public double PostScroll(in ScrollMovementContext context, double selfConsumed) => 0;
        public void OnScrollCompleted(in ScrollMovementResult result) => publish();
    }
}
