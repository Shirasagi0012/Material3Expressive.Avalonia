using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record ButtonGroupSourceConfig(string Action, bool Rtl);

[ParitySuite("button-group-source")]
public sealed class ButtonGroupSourceCases
{
    public static IEnumerable<CaseData<ButtonGroupSourceConfig>> Configurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "multiple", "quick", "repress", "replace", "replace-release", "detach" })
            yield return new($"dynamic.{(rtl ? "rtl" : "ltr")}.{action}", "External width interaction lifecycle", new(action, rtl));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonGroupSourceConfig>();
        await using var host = new AvaloniaHost(writer, 360, 128);
        CaseDefaults.SetThemeColors(host, false);
        var source = new MaterialInteractionSource();
        var group = new ButtonGroup { ExpandedRatio = .5, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var items = Enumerable.Range(0, 3).Select(index => new ButtonGroupCustomItem
        {
            Content = new Marker(new[] { Brushes.Red, Brushes.Blue, Brushes.Green }[index]),
            InteractionSource = index == 1 ? source : new(), CompressionLimit = 24
        }).ToArray();
        foreach (var item in items) group.Items.Add(item);
        var canvas = new Canvas { Background = Brushes.White, Children = { group } };
        Canvas.SetLeft(group, 32); Canvas.SetTop(group, 32);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time)
        {
            while (next <= time) { host.Frame(next); next += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id)
        {
            var points = new List<ProbePoint>();
            if (canvas.Children.Contains(group))
            {
                points.Add(new("group", "container", null, AvaloniaMeasurements.Geometry(group, host.Window)
                    .Where(m => m.Name is "box" or "size").ToArray()));
                for (var index = 0; index < items.Length; index++)
                    points.Add(new($"item-{index}", "item", "group", AvaloniaMeasurements.Geometry(items[index], host.Window)
                        .Where(m => m.Name is "box" or "size").ToArray()));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: id != "time-32000"));
        }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "source", id); host.Draw(); host.Frame(time); Sample(id);
        }
        void Times(params long[] times)
        {
            foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        }
        var press = new MaterialInteractionEvent.Press(default);
        var second = new MaterialInteractionEvent.Press(default);
        Sample("initial");
        Change(1000, "press", () => source.Emit(press));
        if (config.Action == "quick")
        {
            Change(2000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
            Times(16000, 32000, 64000, 96000, 160000, 400000, 800000);
            return;
        }
        Times(16000, 32000, 64000);
        switch (config.Action)
        {
            case "multiple":
                Change(65000, "second", () => source.Emit(second));
                Change(81000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
                Change(97000, "unmatched", () => source.Emit(new MaterialInteractionEvent.CancelPress(press)));
                Times(160000, 400000);
                Change(401000, "last-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second)));
                Times(416000, 480000, 640000, 1000000); break;
            case "repress":
                Change(65000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
                Times(96000, 160000);
                Change(161000, "repress", () => source.Emit(second));
                Times(176000, 240000, 400000);
                Change(401000, "cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second)));
                Times(480000, 640000, 1000000); break;
            case "replace":
            case "replace-release":
                var old = source;
                Change(65000, "replace", () => items[1].InteractionSource = source = new());
                Change(81000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(press)));
                Times(160000);
                Change(161000, "first-new-event", () => source.Emit(config.Action == "replace"
                    ? second : new MaterialInteractionEvent.HoverEnter()));
                Times(176000, 240000, 400000, 800000, 1200000);
                if (config.Action == "replace")
                    Change(1201000, "release", () => source.Emit(new MaterialInteractionEvent.Release(second)));
                Times(1216000, 1400000, 1800000); break;
            case "detach":
                Change(65000, "detach", () => canvas.Children.Remove(group));
                Change(81000, "detached-press", () => source.Emit(second));
                Change(97000, "reattach", () => canvas.Children.Add(group));
                Times(160000);
                Change(161000, "new-press", () => source.Emit(press));
                Times(240000, 400000);
                Change(401000, "cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(press)));
                Times(480000, 640000, 1000000); break;
        }
    }

    private sealed class Marker(IBrush brush) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(80, 40).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.FillRectangle(brush, new Rect(Bounds.Size));
    }
}
