using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using SkiaSharp;

namespace Material3Expressive.Parity;

public sealed record CheckBoxPathEffectConfig(string Effect, string State = "on", bool Rtl = false, string Action = "none");

[ParitySuite("checkbox-path-effect")]
public sealed class CheckBoxPathEffectCases
{
    public static IEnumerable<CaseData<CheckBoxPathEffectConfig>> Configurations()
    {
        foreach (var effect in new[] { "corner", "dash", "compose" })
        {
            foreach (var state in new[] { "on", "off", "indeterminate" })
            foreach (var rtl in new[] { false, true })
                yield return new($"{effect}.{state}.{(rtl ? "rtl" : "ltr")}", "Arbitrary stroke path effect", new(effect, state, rtl));
            yield return new($"{effect}.check", "Path effect during check animation", new(effect, "off", Action: "check"));
            yield return new($"{effect}.reverse", "Path effect during interrupted check animation", new(effect, "off", Action: "reverse"));
            yield return new($"{effect}.replace", "Replace and clear an active path effect", new(effect, Action: "replace"));
        }
    }

    [ParityScenario("capture", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<CheckBoxPathEffectConfig>();
        var scale = (float)AvaloniaHost.RenderScale;
        using var corner = SKPathEffect.CreateCorner(3 * scale);
        using var dash = SKPathEffect.CreateDash([2 * scale, 2 * scale], scale);
        using var composed = SKPathEffect.CreateCompose(dash, corner);
        // Effects outlive both the visual and its queued draw operations.
        await using var host = new AvaloniaHost(writer, 160, 128);
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.Primary] = Color.Parse("#6750A4");
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnPrimary] = Colors.White;
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse("#49454F");
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var box = new CheckBox { IsThreeState = true, IsChecked = config.State switch { "off" => false, "on" => true, _ => null } };
        var effect = config.Effect switch { "corner" => corner, "dash" => dash, _ => composed };
        void Stroke(SKPathEffect? value)
        {
            var stroke = new CheckBoxStroke { Width = 2, Cap = PenLineCap.Square, Join = PenLineJoin.Miter, PathEffect = value };
            CheckBoxAppearance.SetCheckmarkStroke(box, stroke);
            CheckBoxAppearance.SetOutlineStroke(box, stroke);
        }
        Stroke(effect);
        host.Mount(CaseDefaults.Center(box)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new ProbePoint("checkbox", "checkbox", null,
            [.. AvaloniaMeasurements.Geometry(box, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("state", box.IsChecked switch { false => 0, true => 1, null => 2 }, "enum", definition: "checkbox.toggle-state")]) ]));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        if (config.Action == "none") return;
        if (config.Action == "replace")
        {
            Change(1000, "replace", () => Stroke(config.Effect == "corner" ? dash : corner)); At(16000);
            Change(17000, "clear", () => Stroke(null)); At(32000, 160000); return;
        }
        Change(1000, "check", () => box.IsChecked = true); At(16000, 32000, 64000);
        if (config.Action == "reverse") Change(65000, "reverse", () => box.IsChecked = null);
        At(96000, 112000, 160000, 240000, 400000, 800000);
    }
}
