using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Parity;

public sealed record TimePickerAlphaConfig(double SelectorAlpha = 1, double TextAlpha = 1, double ParentAlpha = 1, bool ColorAlpha = false, bool Dynamic = false, bool HostGroup = false);

[ParitySuite("time-picker-alpha")]
public sealed class TimePickerAlphaCases
{
    public static IEnumerable<CaseData<TimePickerAlphaConfig>> Configurations()
    {
        yield return new("static.opaque", "opaque selector", new());
        yield return new("static.selector", "half opacity selector brush", new(SelectorAlpha: .5));
        yield return new("static.selected-text", "half opacity selected text brush", new(TextAlpha: .5));
        yield return new("static.both", "both brushes at half opacity", new(.5, .5));
        yield return new("static.color-alpha", "color alpha instead of brush opacity", new(.5, .5, ColorAlpha: true));
        yield return new("static.ancestor", "ancestor opacity", new(ParentAlpha: .5));
        yield return new("static.combined", "brush and ancestor opacity", new(.5, .5, .5));
        yield return new("static.ancestor-group", "host opts into full subtree opacity", new(ParentAlpha: .5, HostGroup: true));
        yield return new("static.combined-group", "brush alpha with host subtree opacity", new(.5, .5, .5, HostGroup: true));
        yield return new("dynamic.idle-brush", "idle mutable and replacement brushes", new(Dynamic: true));
    }
    [ParityScenario("render", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimePickerAlphaConfig>();
        await using var host = new AvaloniaHost(writer, 360, 480);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.PrimaryContainer] = Color.Parse("#EADDFF");
        host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse("#21005D");
        host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse("#E6E0E9");
        host.Window.Resources[Comp.TimePicker.ClockDialUnselectedLabelTextColor] = Colors.Red;
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = false, Margin = new Thickness(16), Opacity = c.ParentAlpha };
        var root = new StackPanel { Background = Brushes.White, Children = { picker } };
        if (c.HostGroup) RenderOptions.SetRequiresFullOpacityHandling(root, true);
        host.Mount(root);
        var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
        var selector = Brush(Colors.Black, c.SelectorAlpha); var text = Brush(Colors.Blue, c.TextAlpha);
        dial.DialBrush = Brushes.White; dial.SelectorBrush = selector; dial.SelectedTextBrush = text;
        host.Frame(0);
        SolidColorBrush Brush(Color color, double alpha) => c.ColorAlpha
            ? new(new Color((byte)Math.Round(alpha * 255, MidpointRounding.AwayFromZero), color.R, color.G, color.B)) : new(color, alpha);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("dial", "clock-dial", null,
            AvaloniaMeasurements.Geometry(dial, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
        void Change(long time, string id, Action action)
        { host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        if (!c.Dynamic) return;
        Change(16000, "brush-opacity", () => { selector.Opacity = .5; text.Opacity = .5; });
        Change(32000, "brush-color", () => selector.Color = Colors.Lime);
        Change(48000, "replacement", () => { dial.SelectorBrush = Brushes.Transparent; dial.SelectedTextBrush = Brushes.Red; });
        Change(64000, "ancestor", () => picker.Opacity = .5);
    }
}
