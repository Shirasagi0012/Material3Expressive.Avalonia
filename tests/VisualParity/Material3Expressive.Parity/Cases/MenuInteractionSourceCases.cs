using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Styling;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record MenuSourceConfig(string Kind = "baseline", string Action = "press");

[ParitySuite("menu-source")]
public sealed class MenuInteractionSourceCases
{
    public static IEnumerable<CaseData<MenuSourceConfig>> Configurations()
    {
        foreach (var kind in new[] { "baseline", "expressive", "checkable", "selectable", "native-baseline", "native-expressive" })
        foreach (var action in new[] { "press", "overlap", "replace", "disable", "hover-drag", "reattach", "null-native" })
        {
            // Native activation is exercised in a real MenuFlyout by the behavior tests.
            if (kind.StartsWith("native-") && action == "null-native") continue;
            yield return new($"{kind}.{action}", $"{kind} external source {action}", new(kind, action));
        }
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<MenuSourceConfig>();
        await using var host = new AvaloniaHost(writer, 240, 128);
        MenuCases.SetColors(host, false);
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse("#49454F");
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurface] = Color.Parse("#1D1B20");
        Control control = c.Kind switch
        {
            "native-baseline" => new MenuItem { Theme = (ControlTheme)host.Window.FindResource("MaterialBaselineMenuItemTheme")! },
            "native-expressive" => new MenuItem { Theme = (ControlTheme)host.Window.FindResource("MaterialVerticalMenuItemTheme")! },
            "expressive" => new DropdownMenuItem { Shapes = MenuDefaults.ItemShape(0, 1) },
            "checkable" => new CheckableDropdownMenuItem { IsChecked = true },
            "selectable" => new SelectableDropdownMenuItem { IsSelected = true },
            _ => new DropdownMenuItem()
        };
        // Native Margin is outside Bounds; Compose's outer modifier includes item padding.
        Control allocation = control is MenuItem ? new Grid { Width = 180, Children = { control } } : control;
        if (control is not MenuItem) control.Width = 180;
        if (c.Kind == "expressive") control.Theme = (ControlTheme)host.Window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
        var marker = new Border { Width = 32, Height = 16, Background = Brushes.Blue };
        var clicks = 0;
        if (control is MenuItem native) { native.Header = marker; native.Click += (_, _) => clicks++; }
        else { var item = (DropdownMenuItem)control; item.Content = marker; item.Click += (_, _) => clicks++; }
        var source = new MaterialInteractionSource();
        void SetSource(MaterialInteractionSource? value)
        {
            if (control is MenuItem native) MenuAppearance.SetInteractionSource(native, value);
            else ((DropdownMenuItem)control).InteractionSource = value;
        }
        SetSource(source);
        var content = CaseDefaults.Center(allocation);
        var root = new Border { Background = Brushes.White, Child = content };
        TextElement.SetForeground(root, new SolidColorBrush(Color.Parse("#1D1B20")));
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long t) { while (next <= t) { host.Frame(next); next += 16000; } host.AdvanceTo(t); }
        bool Selected() => control is MenuItem native ? native.IsChecked : ((DropdownMenuItem)control).Selected;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("control", c.Kind, null,
            [.. AvaloniaMeasurements.Geometry(allocation, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("state", Selected() ? 1 : 0, "enum", definition: "menu-source.state"),
                Measurement.Flag("enabled", control.IsEffectivelyEnabled, definition: "menu-source.enabled"),
                Measurement.Numeric("clicks", clicks, "count", definition: "menu-source.clicks")])]));
        void Times(params long[] ts) { foreach (var t in ts) { Advance(t); host.Draw(); Sample($"time-{t}"); } }
        void Mutate(long t, string id, Action action, bool sample = true)
        { Advance(t); action(); writer.Record(t, "mutation", id); host.Draw(); host.Frame(t); if (sample) Sample(id); }
        var press = new MaterialInteractionEvent.Press(new Point(40, 24));
        var second = new MaterialInteractionEvent.Press(new Point(40, 24));
        var hover = new MaterialInteractionEvent.HoverEnter();
        var drag = new MaterialInteractionEvent.DragStart();
        Sample("initial");
        if (c.Action == "hover-drag")
        {
            Mutate(1000, "hover", () => source.Emit(hover)); Times(16000, 64000);
            Mutate(65000, "drag", () => source.Emit(drag)); Times(80000, 160000);
            Mutate(161000, "drag-stop", () => source.Emit(new MaterialInteractionEvent.DragStop(drag))); Times(176000, 240000);
            Mutate(241000, "hover-exit", () => source.Emit(new MaterialInteractionEvent.HoverExit(hover))); Times(256000, 400000);
            return;
        }
        if (c.Action == "null-native")
        {
            Mutate(1000, "local-source", () => SetSource(null));
            var center = control.TranslatePoint(new Rect(control.Bounds.Size).Center, host.Window)!.Value;
            Mutate(17000, "native-down", () => { host.Input("move", center); host.Input("down", center); }); Times(32000, 64000);
            Mutate(65000, "native-up", () => host.Input("up", center)); Times(80000, 240000, 800000);
            return;
        }
        Mutate(1000, "press", () => source.Emit(press)); Times(16000, 64000);
        switch (c.Action)
        {
            case "overlap":
                Mutate(65000, "second-press", () => source.Emit(second));
                Mutate(66000, "first-release", () => source.Emit(new MaterialInteractionEvent.Release(press))); Times(80000, 160000, 240000);
                Mutate(241000, "second-cancel", () => source.Emit(new MaterialInteractionEvent.CancelPress(second))); break;
            case "replace":
                var old = source; source = new(); source.Emit(second);
                Mutate(65000, "replace", () => SetSource(source));
                Mutate(66000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(press))); Times(80000, 160000);
                Mutate(161000, "new-press", () => source.Emit(press)); Times(176000, 240000);
                Mutate(241000, "new-release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
            case "disable":
                Mutate(65000, "disable", () => control.IsEnabled = false); Times(80000, 160000);
                Mutate(161000, "external-release", () => source.Emit(new MaterialInteractionEvent.Release(press)));
                Mutate(177000, "disabled-press", () => source.Emit(second)); Times(192000, 240000);
                Mutate(241000, "enable", () => control.IsEnabled = true);
                Mutate(242000, "release", () => source.Emit(new MaterialInteractionEvent.Release(second))); break;
            case "reattach":
                Mutate(65000, "detach", () => root.Child = null, false);
                Mutate(66000, "unmounted-press", () => source.Emit(second), false);
                Mutate(81000, "reattach", () => root.Child = content); Times(96000, 160000);
                Mutate(161000, "old-release", () => source.Emit(new MaterialInteractionEvent.Release(second)));
                Mutate(177000, "new-press", () => source.Emit(press)); Times(192000, 240000);
                Mutate(241000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
            default:
                Times(160000, 240000);
                Mutate(241000, "release", () => source.Emit(new MaterialInteractionEvent.Release(press))); break;
        }
        Times(256000, 320000, 400000, 800000);
    }
}
