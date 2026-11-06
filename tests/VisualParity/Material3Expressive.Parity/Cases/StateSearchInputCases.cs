using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record StateSearchInputConfig(string Action = "none", bool Dark = false, bool Rtl = false,
    bool ReadOnly = false, bool Enabled = true, bool InitiallyExpanded = false, bool MatchedFont = false);

[ParitySuite("state-search-input")]
public sealed class StateSearchInputCases
{
    public static IEnumerable<CaseData<StateSearchInputConfig>> Configurations()
    {
        foreach (var dark in new[] { false, true })
        foreach (var enabled in new[] { false, true })
            yield return new($"static.{dark}.{enabled}".ToLowerInvariant(), "State input appearance", new(Dark: dark, Enabled: enabled));
        // Keep default-platform cases; the extra shared-font cases isolate the native
        // empty-line minimum from the input decoration instead of forcing its height.
        foreach (var dark in new[] { false, true })
        foreach (var enabled in new[] { false, true })
            yield return new($"font.{dark}.{enabled}".ToLowerInvariant(), "Shared Roboto input appearance", new(Dark: dark, Enabled: enabled, MatchedFont: true));
        yield return new("font.down", "Shared Roboto keyboard state", new("down", MatchedFont: true));
        yield return new("static.rtl", "State input RTL slots", new(Rtl: true));
        foreach (var action in new[] { "down", "click", "growth", "release", "cancel", "replace", "collapse-keyboard", "collapse-pointer", "reverse" })
            yield return new($"dynamic.{action}", "State-driven search input", new(action));
        yield return new("dynamic.readonly", "Read-only input still opens suggestions", new("down", ReadOnly: true));
        yield return new("dynamic.disabled", "Disabled input ignores Down", new("down", Enabled: false));
        yield return new("dynamic.initial-expanded", "Initially expanded input does not collect text growth", new("growth", InitiallyExpanded: true));
    }

    [ParityScenario("input", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<StateSearchInputConfig>();
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        await using var host = new AvaloniaHost(writer, 420, 180, time);
        CaseDefaults.SetThemeColors(host, c.Dark);
        var state = new SearchBarState(c.InitiallyExpanded ? SearchBarValue.Expanded : SearchBarValue.Collapsed);
        var source = new MaterialInteractionSource();
        var field = new SearchBarInputField { State = state, IsReadOnly = c.ReadOnly, IsEnabled = c.Enabled,
            InteractionSource = source, Margin = new(16), FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.MatchedFont) field.FontFamily = new FontFamily("fonts:Roboto#Roboto");
        MaterialTextField.SetPlaceholder(field, new Marker(48, 16));
        MaterialTextField.SetLeadingIcon(field, new Marker(24, 24));
        MaterialTextField.SetTrailingIcon(field, new Marker(24, 24));
        var suggestion = new Control { Focusable = true, Width = 32, Height = 24, Margin = new(16, 96, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        host.Mount(new Grid { Children = { field, suggestion } }); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [
            new("field", "input", null, AvaloniaMeasurements.Geometry(field, host.Window).ToArray()),
            new("state", "search-state", "field", [
                Measurement.Numeric("progress", state.Progress, "fraction", definition: "search-state.progress"),
                Measurement.Flag("expanded", state.CurrentValue == SearchBarValue.Expanded, definition: "search-state.expanded"),
                Measurement.Flag("targetExpanded", state.TargetValue == SearchBarValue.Expanded, definition: "search-state.target"),
                Measurement.Flag("focused", field.IsFocused, definition: "search-input.focused"),
                Measurement.Flag("suggestionFocused", suggestion.IsFocused, definition: "search-input.suggestion-focused"),
                Measurement.Numeric("length", field.Text?.Length ?? 0, "count", definition: "search-input.length")])
        ], image: c.Action == "none")); // Native carets have a separate platform clock.
        Sample("initial"); if (c.Action == "none") return;
        long next = 16000;
        void Advance(long target) { while (next <= target) { host.Frame(next); next += 16000; } host.AdvanceTo(target); }
        void Change(long at, string id, Action action) { Advance(at); action(); host.Draw(); host.Frame(at); Sample(id); }
        void Key() => host.Window.KeyPress(Avalonia.Input.Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Change(1000, "focus", () => field.Focus(NavigationMethod.Tab));
        if (c.InitiallyExpanded) Change(2000, "seed-collapse", () => _ = state.SnapToAsync(0));
        var press = new MaterialInteractionEvent.Press(new Point(30, 25));
        Change(81000, "trigger", () =>
        {
            switch (c.Action)
            {
                case "down": case "collapse-keyboard": Key(); break;
                case "click": case "collapse-pointer": case "reverse": host.Input("down", new(60, 40)); break;
                case "growth": field.Text = "x"; break;
                case "coalesced": field.Text = "x"; field.Text = ""; break;
                case "coalesced-grow": field.Text = "x"; field.Text = "xy"; break;
                case "state-growth": field.State = state = new(); field.Text = "x"; break;
                case "release": source.Emit(new MaterialInteractionEvent.Release(press)); break;
                case "cancel": source.Emit(press); source.Emit(new MaterialInteractionEvent.CancelPress(press)); break;
                case "replace": field.InteractionSource = new(); source.Emit(new MaterialInteractionEvent.Release(press)); break;
            }
        });
        if (c.Action is "click" or "collapse-pointer" or "reverse") Change(97000, "release", () => host.Input("up", new(60, 40)));
        foreach (var at in new long[] { 112000, 160000, 240000, 400000, 800000 }) { Advance(at); Sample($"time-{at}"); }
        if (c.Action == "down" && c.Enabled) Change(801000, "suggestions", Key);
        if (c.Action.StartsWith("collapse-", StringComparison.Ordinal) || c.Action == "reverse")
        {
            Change(801000, "collapse", () => _ = state.AnimateToCollapsedAsync());
            if (c.Action == "reverse") Change(881000, "reverse", () => _ = state.AnimateToExpandedAsync());
            foreach (var at in new long[] { 896000, 1008000, 1200000, 1600000, 1800000 }) { Advance(at); Sample($"time-{at}"); }
        }
    }

    private sealed class Marker(int width, int height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}


[ParitySuite("state-search-batched")]
public sealed class StateSearchBatchedCases
{
    public static IEnumerable<CaseData<StateSearchInputConfig>> Configurations() =>
        new[] { "coalesced", "coalesced-grow", "state-growth" }.Select(action =>
            new CaseData<StateSearchInputConfig>(action, "snapshot text observation and collector replacement", new(action)));
    [ParityScenario("input", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new StateSearchInputCases().Capture(writer);
}
