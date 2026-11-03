using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record DragHandleConfig(bool Dark = false, bool Rtl = false, string Appearance = "default",
    double? Width = null, double? Height = null, double Minimum = 48, string Mutation = "none");

[ParitySuite("drag-handle")]
public sealed class DragHandleCases
{
    public static IEnumerable<CaseData<DragHandleConfig>> StaticConfigurations()
    {
        foreach (var appearance in new[] { "default", "custom", "fractional" })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.{appearance}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", new(dark, rtl, appearance));
        foreach (var size in new[] { (2d, 12d), (20d, 20d), (100d, 100d), (48.25, 48.25) })
            yield return Data($"static.constraints.{size.Item1}-{size.Item2}", new(Width: size.Item1, Height: size.Item2));
        foreach (var minimum in new[] { 0d, 24d, 80d })
            yield return Data($"static.minimum.{minimum}", new(Minimum: minimum));
    }
    public static IEnumerable<CaseData<DragHandleConfig>> DynamicConfigurations()
    {
        foreach (var mutation in new[] { "press", "cancel", "drag", "source", "hover", "theme", "configuration", "external-press" })
            yield return Data($"dynamic.{mutation}", new(Mutation: mutation));
        yield return Data("dynamic.press.custom", new(Appearance: "custom", Mutation: "press"));
        yield return Data("dynamic.drag.custom", new(Appearance: "custom", Mutation: "drag"));
    }
    static CaseData<DragHandleConfig> Data(string id, DragHandleConfig config) => new(id, id.Replace('.', ' '), config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<DragHandleConfig>();
        await using var host = new AvaloniaHost(writer, 240, 180);
        var source = new MaterialInteractionSource();
        var handle = new VerticalDragHandle { InteractionSource = source };
        if (config.Width is { } width) handle.Width = width;
        if (config.Height is { } height) handle.Height = height;
        MaterialInteractiveSize.SetMinimum(handle, config.Minimum);
        host.Window.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Theme(config.Dark); Appearance(config.Appearance);
        host.Mount(CaseDefaults.Center(handle)); host.Frame(-1000); host.Frame(0);
        var presses = 0;
        void Observe(MaterialInteractionEvent e) { if (e is MaterialInteractionEvent.Press) presses++; }
        source.Interaction += Observe;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("handle", "handle", null, [.. AvaloniaMeasurements.Geometry(handle, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("presses", presses, "count", definition: "drag-handle.source.press-count")])]));
        Sample("initial"); if (!dynamic) return;
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        var center = handle.TranslatePoint(new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2), host.Window)!.Value;
        switch (config.Mutation)
        {
            case "press": case "cancel":
                Change(1000, "down", () => { host.Input("move", center); host.Input("down", center); }); Times(16000, 64000, 160000);
                if (config.Mutation == "cancel") Change(161000, "outside", () => host.Input("move", new Point(4, 4)));
                Change(162000, "up", () => host.Input("up", config.Mutation == "cancel" ? new Point(4, 4) : center));
                Times(176000, 240000, 400000); break;
            case "drag":
                var first = new MaterialInteractionEvent.DragStart(); var second = new MaterialInteractionEvent.DragStart();
                Change(1000, "start", () => source.Emit(first)); Times(16000, 64000);
                Change(65000, "overlap", () => source.Emit(second));
                Change(66000, "stop-first", () => source.Emit(new MaterialInteractionEvent.DragStop(first))); Times(80000);
                Change(81000, "cancel-last", () => source.Emit(new MaterialInteractionEvent.DragCancel(second))); Times(96000, 160000, 320000); break;
            case "source":
                var drag = new MaterialInteractionEvent.DragStart();
                Change(1000, "start", () => source.Emit(drag)); Times(16000, 64000);
                var replacement = new MaterialInteractionSource(); replacement.Interaction += Observe;
                Change(65000, "replace", () => handle.InteractionSource = replacement);
                Change(66000, "old-stop", () => source.Emit(new MaterialInteractionEvent.DragStop(drag)));
                Change(67000, "new-hover", () => replacement.Emit(new MaterialInteractionEvent.HoverEnter())); Times(80000, 160000, 320000); break;
            case "hover":
                Change(1000, "enter", () => host.Input("move", center)); Times(16000, 64000, 160000);
                Change(161000, "leave", () => host.Input("move", new Point(4, 4))); Times(176000, 240000, 400000); break;
            case "theme": Change(1000, "dark", () => Theme(true)); Change(2000, "light", () => Theme(false)); break;
            case "configuration": Change(1000, "custom", () => Appearance("custom")); Change(2000, "default", () => Appearance("default")); break;
            case "external-press":
                var press = new MaterialInteractionEvent.Press(new Point(2, 24));
                Change(1000, "source-down", () => source.Emit(press)); Times(16000, 64000, 160000);
                Change(161000, "source-up", () => source.Emit(new MaterialInteractionEvent.Release(press))); Times(176000, 240000, 400000); break;
        }
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            // Match the explicitly supplied Compose LocalContentColor, including theme changes.
            handle.Foreground = new SolidColorBrush(Color.Parse(dark ? "#E6E0E9" : "#1D1B20"));
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
        }
        void Appearance(string appearance)
        {
            handle.ClearValue(VerticalDragHandle.SizesProperty); handle.ClearValue(VerticalDragHandle.ColorsProperty); handle.ClearValue(VerticalDragHandle.ShapesProperty);
            if (appearance == "custom")
            {
                handle.Sizes = new(new(8, 40), new(14, 56), new(20, 64));
                handle.Colors = new(Colors.Red, Colors.Green, Colors.Blue);
                handle.Shapes = new(new(3), new(4), new(6));
            }
            if (appearance == "fractional") handle.Sizes = new(new(4.125, 47.625), new(12.125, 51.625), new(12.125, 51.625));
        }
    }
}
