using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record NativeSliderSourceConfig(bool Vertical = false, string Action = "overlap");

[ParitySuite("native-slider-source")]
public sealed class NativeSliderSourceCases
{
    public static IEnumerable<CaseData<NativeSliderSourceConfig>> Configurations()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var action in new[] { "overlap", "focus", "cancel", "replace", "disable", "pointer" })
            yield return new($"{action}.{(vertical ? "vertical" : "horizontal")}", "Shared native slider feedback", new(vertical, action));
    }

    [ParityScenario("feedback", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<NativeSliderSourceConfig>();
        await using var host = new AvaloniaHost(writer, 360, 360);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Secondary] = Color.Parse("#625B71");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSecondary] = Color.Parse("#FFFFFF");
        var source = new MaterialInteractionSource();
        var slider = new Slider { Minimum = 0, Maximum = 1, Value = .25,
            Orientation = c.Vertical ? Orientation.Vertical : Orientation.Horizontal,
            Width = c.Vertical ? 48 : 240, Height = c.Vertical ? 240 : 48 };
        SliderAppearance.SetInteractionSource(slider, source);
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var label = new TooltipLabel { InteractionSource = source, Content = slider,
            Label = new Border { Width = 40, Height = 24, Background = Brushes.Blue } };
        host.Mount(CaseDefaults.Center(label)); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long at) { while (next <= at) { host.Frame(next); next += 16000; } host.AdvanceTo(at); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("slider", "slider", null, [.. AvaloniaMeasurements.Geometry(slider, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("value", slider.Value, definition: "slider.actual-value"),
                Measurement.Flag("label", label.IsPresented, definition: "label.popup.attached")])]));
        void Change(long at, string id, Action action) { Advance(at); action(); host.Draw(); host.Frame(at); Sample(id); }
        var press = new MaterialInteractionEvent.Press(default);
        var drag = new MaterialInteractionEvent.DragStart();
        var focus = new MaterialInteractionEvent.Focus();
        var old = source;
        var point = c.Vertical ? new Point(180, 239) : new Point(121, 180);
        Sample("initial");
        Change(1000, "start", () =>
        {
            if (c.Action == "focus") source.Emit(focus);
            else if (c.Action == "pointer") { host.Input("move", point); host.Input("down", point); }
            else source.Emit(press);
        });
        foreach (var at in new long[] { 16000, 64000, 160000 }) { Advance(at); Sample($"time-{at}"); }
        Change(161000, "change", () =>
        {
            switch (c.Action)
            {
                case "focus": source.Emit(new MaterialInteractionEvent.Unfocus(focus)); break;
                case "cancel": source.Emit(new MaterialInteractionEvent.CancelPress(press)); source.Emit(drag); break;
                case "replace": source = new(); SliderAppearance.SetInteractionSource(slider, source); label.InteractionSource = source; break;
                case "disable": slider.IsEnabled = false; break;
                case "pointer": host.Input("move", point + (c.Vertical ? new Vector(0, -24) : new Vector(24, 0))); break;
                default: source.Emit(drag); source.Emit(new MaterialInteractionEvent.Release(press)); break;
            }
        });
        foreach (var at in new long[] { 176000, 240000, 320000 }) { Advance(at); Sample($"time-{at}"); }
        Change(321000, "end", () =>
        {
            if (c.Action == "pointer") { host.Input("up", point + (c.Vertical ? new Vector(0, -24) : new Vector(24, 0))); host.Input("move", new(5, 5)); }
            else if (c.Action == "cancel") source.Emit(new MaterialInteractionEvent.DragCancel(drag));
            else if (c.Action == "replace") { old.Emit(new MaterialInteractionEvent.Release(press)); source.Emit(drag); }
            else { source.Emit(new MaterialInteractionEvent.Release(press)); source.Emit(new MaterialInteractionEvent.DragStop(drag)); }
        });
        foreach (var at in new long[] { 336000, 400000, 640000 }) { Advance(at); Sample($"time-{at}"); }
    }
}
