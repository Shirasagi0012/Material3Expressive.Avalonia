using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record ButtonInkConfig(string Style);

[ParitySuite("button-ink")]
public sealed class ButtonInkCases
{
    public static IEnumerable<CaseData<ButtonInkConfig>> Configurations() =>
        new[] { "Filled", "Elevated", "Tonal", "Outlined", "Text" }
            .Select(style => new CaseData<ButtonInkConfig>($"dynamic.{style.ToLowerInvariant()}", "Current content color drives ripple and state layer", new(style)));

    [ParityScenario("color", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonInkConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        CaseDefaults.SetThemeColors(host, false);
        var source = new MaterialInteractionSource();
        var marker = new ButtonCases.ButtonMarker { PreferredSize = new(32, 16) };
        var button = new Button { Width = 104, Height = 48, Padding = new(0), CornerRadius = new(8),
            Content = marker, Foreground = new SolidColorBrush(Color.Parse("#803050")) };
        button.Theme = (ControlTheme)host.Window.FindResource($"Material{config.Style}ButtonTheme")!;
        MaterialButton.SetInteractionSource(button, source);
        host.Mount(CaseDefaults.Center(button)); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var brush = (ISolidColorBrush)marker.Paint!;
            writer.Submit(host.Snapshot(id, id, [new("content", "content", null,
                [Measurement.Vector("color", "color", [brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A],
                    "srgba8", "none", "properties", "button.content.painted-solid-srgba8")])]));
        }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        var hover = new MaterialInteractionEvent.HoverEnter();
        var press = new MaterialInteractionEvent.Press(new Point(52, 24));
        Sample("initial"); Change(1000, "hover", () => source.Emit(hover)); At(16000); At(64000);
        Change(65000, "press", () => source.Emit(press)); At(80000); At(128000);
        Change(129000, "replace-color", () => button.Foreground = new SolidColorBrush(Color.Parse("#804080C0"))); At(144000); At(192000);
        Change(193000, "clear-color", () => button.ClearValue(Button.ForegroundProperty)); At(208000); At(256000);
        Change(257000, "disable", () => button.IsEnabled = false); At(272000); At(320000);
        Change(321000, "enable", () => button.IsEnabled = true); At(336000);
        Change(337000, "release", () => { source.Emit(new MaterialInteractionEvent.Release(press)); source.Emit(new MaterialInteractionEvent.HoverExit(hover)); });
        At(352000); At(480000); At(640000);
    }
}
