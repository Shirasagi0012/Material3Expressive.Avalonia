using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record SearchBarInputConfig(bool Dark = false, bool Enabled = true, bool Icons = true, bool Rtl = false,
    int HostWidth = 420, string Action = "none");

[ParitySuite("search-bar-input")]
public sealed class SearchBarInputFieldCases
{
    public static IEnumerable<CaseData<SearchBarInputConfig>> Configurations()
    {
        foreach (var dark in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var icons in new[] { false, true })
            yield return new($"static.{dark}.{enabled}.{icons}".ToLowerInvariant(), "input colors and slots", new(dark, enabled, icons));
        yield return new("static.rtl", "RTL icon offsets", new(Rtl: true));
        yield return new("static.narrow", "parent width constraint", new(HostWidth: 220));
        foreach (var action in new[] { "collapse", "reverse", "source-external", "source-suppressed", "source-replace" })
            yield return new($"dynamic.{action}", "delayed focus clearing", new(Action: action));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SearchBarInputConfig>();
        var delayTime = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        await using var host = new AvaloniaHost(writer, c.HostWidth, 160, delayTime);
        CaseDefaults.SetThemeColors(host, c.Dark);
        var field = new SearchBarInputField { IsEnabled = c.Enabled, Margin = new Thickness(16),
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var slots = new Dictionary<string, Control>();
        Control Slot(string name, int width, int height)
        {
            var marker = new Marker(width, height) { HorizontalAlignment = name == "placeholder" ? HorizontalAlignment.Left : HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center };
            slots.Add(name, marker); return marker;
        }
        MaterialTextField.SetPlaceholder(field, Slot("placeholder", 48, 16));
        if (c.Icons) { MaterialTextField.SetLeadingIcon(field, Slot("leading", 24, 24)); MaterialTextField.SetTrailingIcon(field, Slot("trailing", 24, 24)); }
        var requests = 0;
        var source = new MaterialInteractionSource();
        MaterialInteractionEvent.Focus? nativeFocus = null;
        source.Interaction += interaction =>
        {
            if (interaction is MaterialInteractionEvent.Focus focus) nativeFocus = focus;
        };
        if (c.Action.StartsWith("source-", StringComparison.Ordinal)) field.InteractionSource = source;
        var other = new Control { Focusable = true, Width = 8, Height = 8,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        field.ExpansionRequested += (_, _) => { requests++; field.IsExpanded = true; };
        host.Mount(new Grid { Children = { field, other } }); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("field", "search-input", null, AvaloniaMeasurements.Geometry(field, host.Window).ToArray()),
                new("state", "search-input-state", "field", [Measurement.Flag("focused", field.IsFocused, definition: "search-input.focused"),
                    Measurement.Flag("expanded", field.IsExpanded, definition: "search-input.expanded"),
                    Measurement.Numeric("requests", requests, "count", definition: "search-input.requests")]) };
            points.AddRange(slots.Select(pair => new ProbePoint(pair.Key, pair.Key, "field", AvaloniaMeasurements.Geometry(pair.Value, host.Window).ToArray())));
            if (c.Action.StartsWith("source-", StringComparison.Ordinal))
                points.Add(new("other", "focus-target", null, [Measurement.Flag("focused", other.IsFocused, definition: "search-input.other-focused")]));
            // Like the native TextField suite, focused carets use a separate platform clock.
            // Unfocused appearance cases retain full rasters; focus cases compare real state and geometry.
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: c.Action == "none"));
        }
        Sample("initial"); if (c.Action == "none") return;
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "focus", () =>
        {
            if (c.Action == "source-external") { other.Focus(); source.Emit(new MaterialInteractionEvent.Focus()); }
            else field.Focus();
        });
        Advance(800000); Sample("focused");
        if (c.Action == "source-external") Change(800000, "expand", () => field.IsExpanded = true);
        if (c.Action == "source-suppressed") Change(800000, "suppress", () => source.Emit(new MaterialInteractionEvent.Unfocus(nativeFocus!)));
        Change(800000, "collapse", () => field.IsExpanded = false);
        foreach (var time in new long[] { 816000, 864000, 899000, 900000, 912000, 1000000 })
        {
            Advance(time); Sample($"time-{time}");
            if (c.Action == "reverse" && time == 864000) Change(time, "reverse", () => field.IsExpanded = true);
            if (c.Action == "source-replace" && time == 864000)
            {
                Change(time, "replace", () => field.InteractionSource = new MaterialInteractionSource());
                Change(time, "clear-source-state", () => field.InteractionSource!.Emit(new MaterialInteractionEvent.HoverEnter()));
            }
        }
    }

    private sealed class Marker(int width, int height) : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}
