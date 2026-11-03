using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record RangeSliderInteractionConfig(string Action = "drag", bool Start = true, bool Rtl = false);

[ParitySuite("range-slider-interaction")]
public sealed class RangeSliderInteractionCases
{
    public static IEnumerable<CaseData<RangeSliderInteractionConfig>> Configurations()
    {
        foreach (var action in new[] { "tap", "drag", "vertical-cancel", "release-jump", "disable", "source", "detach", "external", "focus" })
        foreach (var start in new[] { true, false })
        foreach (var rtl in new[] { false, true })
            yield return new($"{action}.{(start ? "start" : "end")}.{(rtl ? "rtl" : "ltr")}", action, new(action, start, rtl));
    }

    [ParityScenario("interaction", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<RangeSliderInteractionConfig>();
        await using var host = new AvaloniaHost(writer, 320, 192);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseSurface] = Color.Parse("#322F35");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse("#FEF7FF");
        var sources = new[] { new MaterialInteractionSource(), new MaterialInteractionSource() };
        var logs = new[] { new EventLog(), new EventLog() };
        for (int i = 0; i < 2; i++) sources[i].Interaction += logs[i].Receive;
        var thumbs = sources.Select(source => new SliderThumb { Width = 4, Height = 44, InteractionSource = source }).ToArray();
        var tips = sources.Select(_ => new PlainTooltip { Content = new Border { Width = 32, Height = 8, Background = Brushes.Blue } }).ToArray();
        var labels = sources.Select((source, i) => new TooltipLabel { Content = thumbs[i], Label = tips[i], InteractionSource = source }).ToArray();
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            StartThumb = labels[0], EndThumb = labels[1], StartThumbInteractionSource = sources[0], EndThumbInteractionSource = sources[1] };
        var canvas = new Canvas { Background = Brushes.White, Children = { slider } };
        Canvas.SetLeft(slider, 20); Canvas.SetTop(slider, 96);
        host.Mount(canvas); host.Frame(0);
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("events", "events", null,
                [Measurement.Text("start", logs[0].Text, definition: "range-slider.interactions.paired-sequence"),
                 Measurement.Text("end", logs[1].Text, definition: "range-slider.interactions.paired-sequence")]) };
            if (slider.IsAttachedToVisualTree())
            {
                points.Add(new("slider", "slider", null, [
                    Measurement.Numeric("start", slider.ActualState.ActiveRangeStart, "value", definition: "slider.active-start"),
                    Measurement.Numeric("end", slider.ActualState.ActiveRangeEnd, "value", definition: "slider.active-end")]));
                for (int i = 0; i < 2; i++)
                {
                    points.Add(new($"thumb-{i}", "thumb", null, [.. AvaloniaMeasurements.Geometry(thumbs[i], host.Window).Where(m => m.Name is "box" or "size"),
                        Measurement.Flag("label", labels[i].IsPresented, definition: "label.popup.attached")]));
                    if (tips[i].IsAttachedToVisualTree()) points.Add(new($"label-{i}", "label", null,
                        AvaloniaMeasurements.Geometry(tips[i], host.Window).Where(m => m.Name is "box" or "size").ToArray()));
                }
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        long time = 0;
        void Change(string id, Action action) { time += 16000; host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        var index = c.Start ? 0 : 1;
        var point = thumbs[index].TranslatePoint(new Point(2, 22), host.Window)!.Value;
        var destination = point + new Vector((c.Start ? 24 : -24) * (c.Rtl ? -1 : 1), 0);
        void ReplaceSource(int endpoint)
        {
            sources[endpoint] = new(); sources[endpoint].Interaction += logs[endpoint].Receive;
            if (endpoint == 0) slider.StartThumbInteractionSource = sources[endpoint]; else slider.EndThumbInteractionSource = sources[endpoint];
            labels[endpoint].InteractionSource = sources[endpoint]; thumbs[endpoint].InteractionSource = sources[endpoint];
        }
        Sample("initial");
        if (c.Action == "focus")
        {
            Change("focus", () =>
            {
                slider.StartPresenter!.Focus(Avalonia.Input.NavigationMethod.Tab);
                if (index == 1) slider.EndPresenter!.Focus(Avalonia.Input.NavigationMethod.Tab);
            });
            Change("other-source", () => ReplaceSource(1 - index));
            Change("focused-source", () => ReplaceSource(index));
            return;
        }
        if (c.Action == "external")
        {
            var press = new MaterialInteractionEvent.Press(default); var drag = new MaterialInteractionEvent.DragStart();
            Change("press", () => sources[index].Emit(press)); Change("drag", () => sources[index].Emit(drag));
            Change("release", () => sources[index].Emit(new MaterialInteractionEvent.Release(press)));
            Change("cancel", () => sources[index].Emit(new MaterialInteractionEvent.DragCancel(drag))); return;
        }
        Change("hover", () => host.Input("move", point)); Change("press", () => host.Input("down", point));
        if (c.Action != "tap" && c.Action != "release-jump")
            Change("move", () => host.Input("move", c.Action == "vertical-cancel" ? point + new Vector(0, 32) : destination));
        if (c.Action == "disable") Change("disable", () => slider.IsEnabled = false);
        if (c.Action == "source") Change("source", () => ReplaceSource(index));
        if (c.Action == "detach") Change("detach", () => canvas.Children.Remove(slider));
        Change("release", () => host.Input("up", c.Action == "tap" ? point : destination));
        Change("exit", () => host.Input("move", new Point(4, 4)));
    }

    sealed class EventLog
    {
        readonly List<MaterialInteractionEvent> starts = [];
        readonly List<string> events = [];
        public string Text => string.Join(",", events);
        public void Receive(MaterialInteractionEvent interaction)
        {
            var (name, start) = interaction switch
            {
                MaterialInteractionEvent.Press => ("press", interaction),
                MaterialInteractionEvent.Release e => ("release", (MaterialInteractionEvent)e.Start),
                MaterialInteractionEvent.CancelPress e => ("cancel-press", e.Start),
                MaterialInteractionEvent.DragStart => ("drag", interaction),
                MaterialInteractionEvent.DragStop e => ("stop", e.Start),
                MaterialInteractionEvent.DragCancel e => ("cancel-drag", e.Start),
                MaterialInteractionEvent.Focus => ("focus", interaction),
                MaterialInteractionEvent.Unfocus e => ("unfocus", e.Start),
                MaterialInteractionEvent.HoverEnter => ("hover", interaction),
                MaterialInteractionEvent.HoverExit e => ("exit", e.Start),
                _ => throw new InvalidOperationException()
            };
            if (ReferenceEquals(start, interaction)) starts.Add(start);
            events.Add($"{name}:{starts.IndexOf(start)}");
        }
    }
}
