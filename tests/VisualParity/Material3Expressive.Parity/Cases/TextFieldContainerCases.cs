using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Tokens;

namespace Material3Expressive.Parity;

public sealed record TextFieldContainerConfig(bool Outlined, string Action, bool Slow = false, string Shape = "default");

[ParitySuite("text-field-container")]
public sealed class TextFieldContainerCases
{
    public static IEnumerable<CaseData<TextFieldContainerConfig>> Configurations()
    {
        foreach (var outlined in new[] { false, true })
        {
            foreach (var action in new[] { "focus", "reverse", "error", "disable", "replace", "theme" })
                yield return new($"dynamic.{(outlined ? "outlined" : "filled")}.{action}", "Container animation without editor or label pixels", new(outlined, action));
            yield return new($"dynamic.{(outlined ? "outlined" : "filled")}.slow-reverse", "Custom spring interruption", new(outlined, "reverse", true));
        }
        foreach (var shape in new[] { "square", "small", "asymmetric" })
            yield return new($"dynamic.outlined.shape-{shape}", "Physical stroke rounding and corner inset during reversal", new(true, "reverse", Shape: shape));
    }

    [ParityScenario("container", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TextFieldContainerConfig>();
        await using var host = new AvaloniaHost(writer, 320, 120);
        void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[MaterialTokens.Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[MaterialTokens.Sys.Color.Error] = Color.Parse(dark ? "#F2B8B5" : "#B3261E");
        }
        Theme(false);
        var field = new TextBox { Width = 280, Height = 80, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Theme = (ControlTheme)host.Window.FindResource(c.Outlined ? "MaterialOutlinedTextFieldTheme" : "MaterialFilledTextFieldTheme")! };
        if (c.Slow)
        {
            field.Resources[MaterialTokens.Comp.TextField.SpatialAnimation] = new SpringSpec(.5f, 100f);
            field.Resources[MaterialTokens.Comp.TextField.FastEffectsAnimation] = new SpringSpec(.7f, 100f);
        }
        if (c.Shape != "default") field.CornerRadius = c.Shape switch
        {
            "square" => new CornerRadius(0),
            "small" => new CornerRadius(.1),
            _ => new CornerRadius(.1, 4, 12, 0),
        };
        var source = new MaterialInteractionSource();
        MaterialTextField.SetInteractionSource(field, source);
        host.Mount(new Grid { Background = Brushes.White, Children = { field } });
        host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("container", "text-field-container", null, AvaloniaMeasurements.Geometry(field, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray())]));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        var first = new MaterialInteractionEvent.Focus();
        var second = new MaterialInteractionEvent.Focus();
        Sample("initial");
        Change(1000, "focus", () => source.Emit(first));
        At(16000); At(32000); At(64000);
        Change(65000, "change", () =>
        {
            switch (c.Action)
            {
                case "reverse": source.Emit(new MaterialInteractionEvent.Unfocus(first)); break;
                case "error": MaterialTextField.SetIsError(field, true); break;
                case "disable": field.IsEnabled = false; break;
                case "replace": source = new(); MaterialTextField.SetInteractionSource(field, source); break;
                case "theme": Theme(true); break;
            }
        });
        At(80000); At(96000);
        if (c.Action == "reverse") Change(97000, "refocus", () => source.Emit(second));
        if (c.Action == "replace") Change(97000, "new-source-event", () => source.Emit(new MaterialInteractionEvent.HoverEnter()));
        At(112000); At(160000); At(240000); At(400000); At(800000);
        Change(801000, "end", () =>
        {
            if (c.Action == "disable") field.IsEnabled = true;
            source.Emit(new MaterialInteractionEvent.Unfocus(c.Action == "reverse" ? second : first));
        });
        At(864000); At(960000); At(1200000); At(2000000);
    }
}
