using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record ElevationCommitConfig(string Kind, string Action);

[ParitySuite("elevation-commit")]
public sealed class ElevationCommitCases
{
    public static IEnumerable<CaseData<ElevationCommitConfig>> Configurations()
    {
        foreach (var kind in new[] { "button", "chip", "card" })
        foreach (var action in new[] { "configuration", "disable", "coalesce" })
            yield return new($"dynamic.{kind}.{action}", "Elevation effects commit after displayed values", new(kind, action));
    }

    [ParityScenario("change", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ElevationCommitConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        CaseDefaults.SetThemeColors(host, false);
        var source = new MaterialInteractionSource();
        Button control = config.Kind switch
        {
            "chip" => new InputChip { InteractionSource = source, Shapes = null },
            "card" => new Card { InteractionSource = source, IsClickable = true },
            _ => new Button { Padding = new Thickness(0) }
        };
        if (config.Kind == "button")
        {
            MaterialButton.SetInteractionSource(control, source);
            MaterialButton.SetPressedCornerRadius(control, null);
        }
        control.Width = 104; control.Height = 48; control.CornerRadius = new(8);
        control.Background = Brushes.White; control.Foreground = Brushes.Black;
        var marker = new Border { Width = 32, Height = 16, Background = Brushes.Black };
        control.Content = config.Kind == "card" ? new CardColumn { Children = { marker } } : marker;
        control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        void Configure(int step)
        {
            var hover = step > 0 ? 7.25 : 5;
            var drag = step > 1 ? 12.5 : 9;
            var disabled = step > 1 ? 4.75 : 1;
            if (control is Chip chip) chip.Elevation = new(2, 3, 4, hover, drag, disabled);
            else if (control is Card card) card.Elevation = new() { Default = 2, Pressed = 3, Focused = 4, Hovered = hover, Dragged = drag, Disabled = disabled };
            else MaterialButton.SetElevation(control, new() { Default = 2, Pressed = drag, Focused = 4, Hovered = hover, Disabled = disabled });
        }
        Configure(0);
        host.Mount(new Grid { Background = Brushes.White, Children = { control } });
        host.Frame(-1000); host.Frame(0);
        var surface = control.GetVisualDescendants().OfType<MaterialSurface>().Single();
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("container", "container", null,
                [Measurement.Numeric("shadowElevation", surface.ShadowElevation, "dip", category: "motion", definition: "surface.shadow-elevation")])],
            image: id != "time-32000"));
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        MaterialInteractionEvent active = config.Kind == "button" ? new MaterialInteractionEvent.Press(new Point(52, 24)) : new MaterialInteractionEvent.DragStart();
        Sample("initial"); Change(1000, "active", () => source.Emit(active)); Times(16000, 32000, 64000);
        if (config.Action == "configuration")
        {
            Change(65000, "unused-hover", () => Configure(1)); Times(80000, 96000);
            Change(97000, "active-drag", () => Configure(2)); Times(112000, 160000, 240000);
        }
        else if (config.Action == "disable")
        {
            Change(65000, "disable", () => control.IsEnabled = false); Times(80000);
            Change(81000, "disabled-config", () => Configure(2)); Times(96000);
            Change(97000, "enable", () => control.IsEnabled = true); Times(112000, 160000, 240000);
        }
        else
        {
            Change(65000, "disable", () => control.IsEnabled = false);
            Change(66000, "config", () => Configure(2));
            Change(67000, "enable", () => control.IsEnabled = true);
            Times(80000, 96000, 112000, 160000, 240000);
        }
        Change(241000, "stop", () => source.Emit(active is MaterialInteractionEvent.Press press
            ? new MaterialInteractionEvent.Release(press) : new MaterialInteractionEvent.DragStop((MaterialInteractionEvent.DragStart)active)));
        Times(256000, 320000, 400000, 640000);
    }
}
