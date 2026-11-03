using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record TimeInputPresentationConfig(bool Vibrant = false, bool Clock24 = false,
    bool Dark = false, bool Dynamic = false, bool Error = false, string Action = "selection");

[ParitySuite("time-input-presentation")]
public sealed class TimeInputPresentationCases
{
    public static IEnumerable<CaseData<TimeInputPresentationConfig>> Configurations()
    {
        foreach (var vibrant in new[] { false, true })
        foreach (var clock24 in new[] { false, true })
        {
            foreach (var dark in new[] { false, true })
            {
                yield return new($"static.{(vibrant ? "vibrant" : "standard")}.{(clock24 ? "24h" : "12h")}.{(dark ? "dark" : "light")}",
                    "selected time editor and supporting text", new(vibrant, clock24, dark));
                yield return new($"error.{(vibrant ? "vibrant" : "standard")}.{(clock24 ? "24h" : "12h")}.{(dark ? "dark" : "light")}",
                    "invalid time editor and live supporting text", new(vibrant, clock24, dark, Error: true));
            }
            yield return new($"changes.{(vibrant ? "vibrant" : "standard")}.{(clock24 ? "24h" : "12h")}",
                "invalid drafts and selection focus", new(vibrant, clock24, Dynamic: true));
            yield return new($"editing.{(vibrant ? "vibrant" : "standard")}.{(clock24 ? "24h" : "12h")}",
                "numeric parsing, leading zeros and rejected replacement", new(vibrant, clock24, Dynamic: true, Action: "editing"));
            yield return new($"external.{(vibrant ? "vibrant" : "standard")}.{(clock24 ? "24h" : "12h")}",
                "external field changes preserve independent invalid drafts", new(vibrant, clock24, Dynamic: true, Action: "external"));
        }
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimeInputPresentationConfig>();
        await using var host = new AvaloniaHost(writer, 420, 280);
        CaseDefaults.SetThemeColors(host, c.Dark);
        var picker = new TimeInput { SelectedTime = new(2, 30), Is24Hour = c.Clock24,
            Variant = c.Vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard,
            Margin = new Thickness(16), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        host.Mount(new Grid { Background = Brushes.White, Children = { picker } }); host.Frame(0);
        TextBox Editor() => picker.GetVisualDescendants().OfType<TextBox>().Single(f => f.IsEffectivelyVisible);
        void Sample(string id)
        {
            var editor = Editor();
            var points = new List<ProbePoint> { new("picker", "time-input", null,
                AvaloniaMeasurements.Geometry(picker, host.Window).Concat([
                    Measurement.Text("selection", picker.Selection.ToString().ToLowerInvariant(), definition: "time-picker.selection"),
                    Measurement.Numeric("hour", picker.SelectedTime.Hour, "hour", definition: "time-picker.selected-hour"),
                    Measurement.Numeric("minute", picker.SelectedTime.Minute, "minute", definition: "time-picker.selected-minute"),
                    Measurement.Flag("valid", picker.IsInputValid, definition: "time-input.valid"),
                    Measurement.Numeric("liveErrors", picker.GetVisualDescendants().OfType<TextBlock>().Count(t =>
                        t.IsEffectivelyVisible && AutomationProperties.GetLiveSetting(t) == AutomationLiveSetting.Polite), "count", definition: "time-input.visible-polite-errors")]).ToArray()),
                new("editor", "time-editor", "picker", [
                    Measurement.Flag("focused", editor.IsFocused, definition: "time-input.editor-focused"),
                    Measurement.Text("text", editor.Text ?? "", definition: "time-input.editor-text")]) };
            // Native caret clocks are not virtualized. Keep full static images; dynamic cases
            // compare the actual state, focus, live error count and component layout.
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: !c.Dynamic));
        }
        long time = 0;
        void Change(string id, Action action)
        {
            time += 16000; host.AdvanceTo(time); action(); writer.Record(time, "mutation", id);
            host.Draw(); host.Frame(time); Sample(id);
        }
        if (c.Error) Change("invalid-hour", () => Editor().Text = "99");
        else Sample("initial");
        if (!c.Dynamic) return;
        if (c.Action == "editing")
        {
            Change("select-minute", () => picker.Selection = TimeSelection.Minute);
            foreach (var text in new[] { "007", "0001", "-1", "75", "100", "hello", "١", "１２", "05" })
                Change("replace-" + (text switch { "-1" => "negative", "١" => "arabic", "１２" => "fullwidth", _ => text }), () => Editor().Text = text);
            return;
        }
        Change("invalid-hour", () => Editor().Text = "99");
        Change("minute-selection", () => picker.Selection = TimeSelection.Minute);
        Change("invalid-minute", () => Editor().Text = "75");
        if (c.Action == "external")
        {
            Change("external-hour", () => picker.SelectedTime = new(5, 30));
            Change("hour-selection", () => picker.Selection = TimeSelection.Hour);
            Change("external-minute", () => picker.SelectedTime = new(5, 45));
            Change("minute-selection-again", () => picker.Selection = TimeSelection.Minute);
            return;
        }
        Change("hour-draft", () => picker.Selection = TimeSelection.Hour);
        Change("correct-hour", () => Editor().Text = "01");
        Change("correct-minute", () => Editor().Text = "05");
        for (var i = 0; i < 32; i++) { time += 16000; host.Frame(time); }
        Sample("settled");
    }
}
