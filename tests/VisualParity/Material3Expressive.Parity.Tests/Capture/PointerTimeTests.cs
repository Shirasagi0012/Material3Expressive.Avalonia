using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaFact]
    public async Task PointerEventsUseLogicalTimeAndPreserveCaptureWithoutSendingFrames()
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 80, 80);
            var target = new Border { Width = 20, Height = 20, Background = Brushes.Red,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            var observed = new List<(string Kind, ulong Time, bool Pressed, IPointer Pointer, Point Position)>();
            void Record(string kind, PointerEventArgs e)
                => observed.Add((kind, e.Timestamp, e.GetCurrentPoint(target).Properties.IsLeftButtonPressed,
                    e.Pointer, e.GetPosition(host.Window)));
            target.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
            {
                Record("down", e);
                e.Pointer.Capture(target);
            }, RoutingStrategies.Bubble, true);
            target.AddHandler(InputElement.PointerMovedEvent, (_, e) => Record("move", e), RoutingStrategies.Bubble, true);
            target.AddHandler(InputElement.PointerReleasedEvent, (_, e) => Record("up", e), RoutingStrategies.Bubble, true);
            host.Mount(new Canvas { Children = { target } }); host.Frame(0);
            host.AdvanceTo(1000); host.Input("down", new Point(10, 10));
            // Passive work must not become event time or an extra animation frame.
            host.Draw(); writer.Submit(host.Snapshot("pressed", "pressed", [])); host.Draw();
            host.AdvanceTo(17000); host.Input("move", new Point(60, 60));
            host.AdvanceTo(33000); host.Input("up", new Point(60, 60));
            Assert.Null(observed[0].Pointer.Captured);
            host.AdvanceTo(49000); host.Input("move", new Point(60, 60));
            Assert.Equal(new[] { "down", "move", "up" }, observed.Select(x => x.Kind));
            Assert.Equal(new ulong[] { 1001, 1017, 1033 }, observed.Select(x => x.Time));
            Assert.Equal(new[] { true, true, false }, observed.Select(x => x.Pressed));
            Assert.All(observed, x => Assert.Same(observed[0].Pointer, x.Pointer));
            Assert.Equal(new Point(60, 60), observed[1].Position);
            Assert.Equal(0, host.FrameTimeUs);
        });
        Assert.Empty(result.Capture.Errors);
        Assert.Single(result.Capture.Trace, e => e.Operation == "frame");
    }
}
