using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record SearchInputFocusRingConfig(bool Dark = false, bool Rtl = false, bool Asymmetric = false,
    bool Custom = false, string Action = "focus", bool ReadOnly = false, string Shape = "none");

[ParitySuite("search-input-focus-ring")]
public sealed class SearchInputFocusRingCases
{
    public static IEnumerable<CaseData<SearchInputFocusRingConfig>> Configurations()
    {
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var asymmetric in new[] { false, true })
            yield return new($"focus.{dark}.{rtl}.{asymmetric}".ToLowerInvariant(), "State input focus ring", new(dark, rtl, asymmetric));
        foreach (var action in new[] { "reverse", "mode", "pointer", "replace" })
            yield return new(action, "Focus indication lifecycle", new(Action: action));
        // Read-only is a supported variant and suppresses the editor's platform caret.
        // Retain editable cases as evidence of external-source/native-caret differences.
        foreach (var action in new[] { "focus", "reverse", "mode", "pointer", "replace" })
            yield return new($"readonly.{action}", "Read-only ring without a native caret", new(Action: action, ReadOnly: true));
        yield return new("readonly.custom", "Read-only translucent ring", new(Custom: true, ReadOnly: true));
        yield return new("custom", "Custom stroke widths, insets and translucent colors", new(Custom: true));
    }

    [ParityScenario("ring", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SearchInputFocusRingConfig>();
        await using var host = new AvaloniaHost(writer, 420, 160);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Secondary] = Color.Parse(c.Dark ? "#CCC2DC" : "#625B71");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSecondary] = Color.Parse(c.Dark ? "#332D41" : "#FFFFFF");
        var source = new MaterialInteractionSource();
        var field = new SearchBarInputField { State = new(), InteractionSource = source, IsReadOnly = c.ReadOnly, Width = 360, Height = 56,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Shape != "none") field.Shape = MaterialShapeCases.CreateShape(c.Shape);
        if (c.Asymmetric) field.CornerRadius = new(24, 8, 0, 16);
        MaterialInteraction.SetFocusFeedback(field, FocusFeedback.InsetRing);
        if (c.Custom)
        {
            MaterialInteraction.SetFocusRingMetrics(field, new(1.1, 1.3, 2.4, 2.7));
            MaterialInteraction.SetFocusRingOuterColor(field, Color.Parse("#806600CC"));
            MaterialInteraction.SetFocusRingInnerColor(field, Color.Parse("#C000CC66"));
        }
        MaterialTextField.SetPlaceholder(field, new Border { Width = 48, Height = 16, Background = Brushes.Blue,
            HorizontalAlignment = HorizontalAlignment.Left });
        host.Mount(CaseDefaults.Center(field)); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long target) { while (next <= target) { host.Frame(next); next += 16000; } host.AdvanceTo(target); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("input", "search-input", null, AvaloniaMeasurements.Geometry(field, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
        void Change(long at, string id, Action action) { Advance(at); action(); host.Draw(); host.Frame(at); Sample(id); }
        var focus = new MaterialInteractionEvent.Focus();
        Sample("initial"); Change(1000, "focus", () => source.Emit(focus));
        foreach (var at in new long[] { 16000, 32000, 64000, 96000, 160000 }) { Advance(at); Sample($"time-{at}"); }
        Change(161000, "change", () =>
        {
            switch (c.Action)
            {
                case "mode": MaterialInteraction.SetFocusFeedback(field, FocusFeedback.Opacity); break;
                case "pointer": host.Input("down", new(5, 5)); host.Input("up", new(5, 5)); break;
                case "replace": field.InteractionSource = new(); break;
                default: source.Emit(new MaterialInteractionEvent.Unfocus(focus)); break;
            }
        });
        foreach (var at in new long[] { 176000, 208000, 240000 }) { Advance(at); Sample($"time-{at}"); }
        if (c.Action == "reverse") Change(241000, "refocus", () => source.Emit(new MaterialInteractionEvent.Focus()));
        if (c.Action == "mode") Change(241000, "ring-mode", () => MaterialInteraction.SetFocusFeedback(field, FocusFeedback.InsetRing));
        foreach (var at in new long[] { 256000, 320000, 480000, 800000 }) { Advance(at); Sample($"time-{at}"); }
    }
}
