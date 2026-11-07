using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PrecisionButtonConfig(bool Toggle = false, string Style = "filled", string Size = "small",
    bool Icon = false, bool Rtl = false, bool Dynamic = false, bool CustomPadding = false);

[ParitySuite("precision-button")]
public sealed class PrecisionButtonCases
{
    public static IEnumerable<CaseData<PrecisionButtonConfig>> Configurations()
    {
        foreach (var toggle in new[] { false, true })
        {
            foreach (var style in toggle ? new[] { "filled", "tonal", "elevated", "outlined" } : new[] { "filled", "tonal", "elevated", "outlined", "text" })
            foreach (var icon in new[] { false, true })
            foreach (var rtl in new[] { false, true })
                yield return Data($"{(toggle ? "toggle" : "button")}.{style}.{icon}.{rtl}", new(toggle, style, Icon: icon, Rtl: rtl));
            foreach (var size in new[] { "xsmall", "medium", "large", "xlarge" })
                yield return Data($"{(toggle ? "toggle" : "button")}.{size}", new(toggle, Size: size, Icon: true));
            foreach (var rtl in new[] { false, true })
            foreach (var custom in new[] { false, true })
                yield return Data($"{(toggle ? "toggle" : "button")}.dynamic.{rtl}.{custom}", new(toggle, Icon: true, Rtl: rtl, Dynamic: true, CustomPadding: custom));
        }
    }
    private static CaseData<PrecisionButtonConfig> Data(string id, PrecisionButtonConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("sizing", Data = nameof(Configurations))]
    public async Task Sizing(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<PrecisionButtonConfig>();
        await using var host = new AvaloniaHost(writer, 400, 240);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse("#F3EDF7");
        host.Window.Resources[Sys.Color.Secondary] = Color.Parse("#625B71");
        host.Window.Resources[Sys.Color.OnSecondary] = Colors.White;
        var label = new Border { Width = 32, Height = 16, Background = Brushes.Navy };
        var iconSize = c.Size switch { "medium" => 24, "large" => 32, "xlarge" => 40, _ => 20 };
        var icon = new Border { Width = iconSize, Height = iconSize, Background = Brushes.Gold };
        Button button = c.Toggle ? new ToggleButton() : new Button();
        button.Content = label;
        button.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        button.Theme = (ControlTheme)host.Window.FindResource($"Material{char.ToUpperInvariant(c.Style[0])}{c.Style[1..]}{(c.Toggle ? "ToggleButton" : "Button")}Theme")!;
        if (c.Size != "small") button.Classes.Add(c.Size);
        if (c.Icon) MaterialButton.SetIcon(button, icon);
        if (c.CustomPadding) button.Padding = new(5, 3, 27, 9);
        var root = CaseDefaults.Center(button);
        MaterialPrecisionPointer.SetIsEnabled(root, !c.Dynamic);
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        Sample("initial");
        if (c.Dynamic)
            foreach (var time in new long[] { 16000, 32000, 64000, 80000, 96000, 160000 })
            {
                host.AdvanceTo(time);
                if (time is 16000 or 80000) MaterialPrecisionPointer.SetIsEnabled(root, time == 16000);
                host.Frame(time); Sample($"time-{time}");
            }

        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("button", "button", null, Geometry(button)), new("content", "content", "button", Geometry(label)) };
            if (c.Icon) points.Add(new("icon", "icon", "button", Geometry(icon)));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
    }
}
