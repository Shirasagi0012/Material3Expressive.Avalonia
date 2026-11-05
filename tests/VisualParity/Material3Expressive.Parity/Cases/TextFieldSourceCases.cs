using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record TextFieldSourceConfig(bool Outlined, string Action, bool Rtl = false, bool Legacy = false);

[ParitySuite("text-field-source")]
public sealed class TextFieldSourceCases
{
    public static IEnumerable<CaseData<TextFieldSourceConfig>> Configurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var action in new[] { "overlap", "reverse", "replace", "native", "native-replace", "disable",
                     "pointer", "pointer-cancel", "pointer-replace" })
            yield return new($"dynamic.{(outlined ? "outlined" : "filled")}.{action}", action, new(outlined, action));
        foreach (var outlined in new[] { false, true })
            yield return new($"dynamic.{(outlined ? "outlined" : "filled")}.rtl", "RTL external focus", new(outlined, "overlap", true));
        foreach (var outlined in new[] { false, true })
        foreach (var action in new[] { "overlap", "reverse", "replace", "native", "native-replace", "disable",
                     "pointer", "pointer-cancel", "pointer-replace" })
            yield return new($"dynamic.legacy.{(outlined ? "outlined" : "filled")}.{action}", "String-value overload interaction lifecycle", new(outlined, action, Legacy: true));
    }

    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<TextFieldSourceConfig>();
        await using var host = new AvaloniaHost(writer, 400, 200);
        var scene = new TextFieldCases.FieldScene(host, new(config.Outlined,
            config.Outlined ? "Cutout" : "Inside", Slots: true, Height: 96, Rtl: config.Rtl));
        var source = new MaterialInteractionSource();
        var starts = new List<MaterialInteractionEvent>();
        var events = new List<string>();
        void Observe(MaterialInteractionSource observed) => observed.Interaction += interaction =>
        {
            var (name, start) = interaction switch
            {
                MaterialInteractionEvent.Focus => ("focus", interaction),
                MaterialInteractionEvent.Unfocus end => ("unfocus", end.Start),
                MaterialInteractionEvent.Press => ("press", interaction),
                MaterialInteractionEvent.Release end => ("release", end.Start),
                MaterialInteractionEvent.CancelPress end => ("cancel", end.Start),
                _ => ("", interaction),
            };
            if (name.Length == 0) return;
            if (ReferenceEquals(start, interaction)) starts.Add(start);
            events.Add($"{name}:{starts.IndexOf(start)}");
        };
        Observe(source);
        MaterialTextField.SetInteractionSource(scene.Box, source);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        var first = new MaterialInteractionEvent.Focus();
        var second = new MaterialInteractionEvent.Focus();
        var native = config.Action.StartsWith("native") || config.Action.StartsWith("pointer");
        // External focus retains full images, including Core's source-driven caret difference.
        // Native focus scenarios retain geometry/motion; native caret timing is not virtualized.
        // The legacy label lambda does not expose minimized progress. Compare actual slot geometry
        // and images for that overload, retaining the public progress probe for state-based fields.
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure().Where(p => !config.Legacy || p.Id != "motion").Append(
            new ProbePoint("events", "events", null, [Measurement.Text("sequence", string.Join(",", events),
                definition: "text-field.interactions.paired-sequence")])).ToArray(), image: !native));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "source", id); host.Draw(); host.Frame(time); Sample(id);
        }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Sample("initial");
        if (config.Action.StartsWith("pointer"))
        {
            var point = scene.Box.TranslatePoint(new Point(140, 40), host.Window)!.Value;
            Change(1000, "press", () => { host.Input("move", point); host.Input("down", point); });
            At(16000); At(64000);
            if (config.Action == "pointer-cancel") Change(65000, "outside", () => host.Input("move", new Point(380, 190)));
            if (config.Action == "pointer-replace") Change(65000, "replace", () =>
            {
                source = new(); Observe(source); MaterialTextField.SetInteractionSource(scene.Box, source);
            });
            At(96000);
            Change(97000, "release", () => host.Input("up", point));
            At(160000); At(400000);
            Change(401000, "blur", () => scene.FocusTarget.Focus());
            At(800000);
            return;
        }
        Change(1000, "focus", () => { if (native) scene.Box.Focus(); else source.Emit(first); });
        At(16000); At(64000);
        switch (config.Action)
        {
            case "reverse":
                Change(65000, "unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(first)));
                At(96000);
                Change(97000, "refocus", () => source.Emit(second));
                break;
            case "replace":
            case "native-replace":
                Change(65000, "replace", () =>
                {
                    source = new(); Observe(source); MaterialTextField.SetInteractionSource(scene.Box, source);
                });
                At(96000);
                Change(97000, "new-source-event", () => source.Emit(new MaterialInteractionEvent.HoverEnter()));
                break;
            case "disable":
                Change(65000, "disable", () => scene.Box.IsEnabled = false);
                At(96000);
                break;
            case "overlap":
                Change(65000, "overlap", () => source.Emit(second));
                Change(97000, "first-end", () => source.Emit(new MaterialInteractionEvent.Unfocus(first)));
                break;
        }
        At(160000); At(400000); At(800000);
        Change(801000, "end", () =>
        {
            if (native) scene.FocusTarget.Focus();
            else if (config.Action == "disable") { scene.Box.IsEnabled = true; source.Emit(new MaterialInteractionEvent.Unfocus(first)); }
            else source.Emit(new MaterialInteractionEvent.Unfocus(second));
        });
        At(864000); At(960000); At(1200000); At(1600000);
    }
}
