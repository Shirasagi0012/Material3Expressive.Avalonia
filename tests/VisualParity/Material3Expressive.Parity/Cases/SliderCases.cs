using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using MSlider = Material3Expressive.Components.Slider;

namespace Material3Expressive.Parity;

public sealed record SliderConfig(string Kind = "horizontal", int Steps = 0, float Value = 0.5f, float End = 0.75f,
    bool Enabled = true, bool Rtl = false, bool TopToBottom = true, bool Dark = false,
    bool Centered = false, bool CustomCorners = false, bool Stop = true, double Gap = 6,
    double Length = 280, bool CustomThumbs = false, bool CustomColors = false, bool Hoisted = false,
    string Action = "none", bool AutoLength = false, double CrossSize = 0, float Minimum = 0, float Maximum = 1);

[ParitySuite("slider")]
public sealed class SliderCases
{
    private static CaseData<SliderConfig> Data(string id, SliderConfig c) => new(id, id, c);
    public static IEnumerable<CaseData<SliderConfig>> StaticConfigurations()
    {
        // The three forms and continuous/discrete tracks have distinct geometry. Cross
        // the complete core appearance matrix with enabled and RTL; value boundaries follow below.
        foreach (var kind in new[] { "horizontal", "vertical", "range" })
        foreach (var steps in new[] { 0, 3 })
        foreach (var enabled in new[] { true, false })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.appearance.{kind}.{steps}.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}", new(kind, steps, Enabled: enabled, Rtl: rtl));
        foreach (var kind in new[] { "horizontal", "vertical", "range" })
        {
            foreach (var value in new[] { 0f, 0.25f, 0.99f, 1f })
                yield return Data($"static.boundary.{kind}.{value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}", new(kind, Value: value, End: Math.Max(value, 0.75f)));
            yield return Data($"static.dark.{kind}", new(kind, Steps: 3, Dark: true));
            yield return Data($"static.custom-colors.{kind}", new(kind, CustomColors: true));
            yield return Data($"static.custom-thumbs.{kind}", new(kind, Steps: 3, CustomThumbs: true));
            yield return Data($"static.short.{kind}", new(kind, Length: 36));
            yield return Data($"static.auto-length.{kind}", new(kind, AutoLength: true));
            yield return Data($"static.cross-constrained.{kind}", new(kind, CrossSize: 20));
            yield return Data($"static.below-track.{kind}", new(kind, CrossSize: 8));
            yield return Data($"static.below-thumb.{kind}", new(kind, Length: 2));
            yield return Data($"static.cross-expanded.{kind}", new(kind, CrossSize: 100));
            yield return Data($"static.negative-range.{kind}", new(kind, Minimum: -1, Maximum: 1, Steps: 4));
            yield return Data($"static.custom-corners.{kind}", new(kind, CustomCorners: true));
            yield return Data($"static.zero-gap.{kind}", new(kind, Gap: 0, Stop: false, Steps: 3));
        }
        foreach (var kind in new[] { "horizontal", "vertical" })
        foreach (var value in new[] { 0.25f, 0.5f, 0.75f })
            yield return Data($"static.centered.{kind}.{value}", new(kind, Value: value, Centered: true));
        yield return Data("static.vertical.reverse", new("vertical", TopToBottom: false, Steps: 3));
        yield return Data("static.range.collapsed", new("range", Value: 0.5f, End: 0.5f, Steps: 3));
        yield return Data("static.single.hoisted-unsnapped", new(Steps: 3, Value: 0.36f, Hoisted: true));
        yield return Data("static.single.value-snapped", new(Steps: 3, Value: 0.36f));
    }
    public static IEnumerable<CaseData<SliderConfig>> InteractionConfigurations()
    {
        foreach (var kind in new[] { "horizontal", "vertical", "range" })
        foreach (var steps in new[] { 0, 3 })
        foreach (var action in new[] { "tap", "drag", "disable", "keyboard" })
            yield return Data($"dynamic.{action}.{kind}.{steps}", new(kind, steps, Value: 0.25f, Action: action));
        yield return Data("dynamic.drag.horizontal.rtl", new(Rtl: true, Action: "drag"));
        yield return Data("dynamic.drag.vertical.reverse", new("vertical", TopToBottom: false, Action: "drag"));
        yield return Data("dynamic.range.start-crossing", new("range", Value: 0.25f, Action: "cross-start"));
        yield return Data("dynamic.range.end-crossing", new("range", Value: 0.25f, Action: "cross-end"));
        yield return Data("dynamic.range.coincident", new("range", Value: 0.5f, End: 0.5f, Action: "drag"));
        yield return Data("dynamic.range.vertical-cancel", new("range", Action: "vertical-cancel"));
        yield return Data("dynamic.range.release-jump.ltr", new("range", Value: 0.25f, Action: "release-jump"));
        yield return Data("dynamic.range.release-jump.rtl", new("range", Value: 0.25f, Rtl: true, Action: "release-jump"));
        yield return Data("dynamic.disabled-input", new(Enabled: false, Action: "tap"));
        foreach (var kind in new[] { "horizontal", "vertical", "range" })
        {
            yield return Data($"dynamic.custom-thumb.{kind}", new(kind, CustomThumbs: true, Action: "drag"));
            yield return Data($"dynamic.programmatic.{kind}", new(kind, Steps: 3, Action: "program"));
            yield return Data($"dynamic.theme.{kind}", new(kind, Action: "theme"));
            yield return Data($"dynamic.corner.{kind}", new(kind, Steps: 3, Value: 0.25f, CustomCorners: true, Action: "corner"));
        }
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("interaction", Data = nameof(InteractionConfigurations))]
    public Task Interaction(CaptureWriter writer) => Capture(writer);

    private static async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<SliderConfig>();
        await using var host = new AvaloniaHost(writer, 320, 320);
        host.Window.RequestedThemeVariant = c.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        // Pair the stock Compose scheme explicitly. The product's seed-generated MCU
        // default has a different Primary; that is independent of Slider's color consumption.
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Primary] = Color.Parse(c.Dark ? "#D0BCFF" : "#6750A4");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.SecondaryContainer] = Color.Parse(c.Dark ? "#4A4458" : "#E8DEF8");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSurface] = Color.Parse(c.Dark ? "#E6E0E9" : "#1D1B20");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse(c.Dark ? "#141218" : "#FEF7FF");
        var value = new SliderState(c.Value, c.Steps, c.Minimum, c.Maximum); var range = new RangeSliderState(c.Value, c.End, c.Steps, c.Minimum, c.Maximum);
        if (!c.Hoisted) value.Value = c.Value;
        range.ActiveRangeStart = c.Value; range.ActiveRangeEnd = c.End;
        var changes = 0; var finished = 0;
        value.OnValueChange = v => { changes++; value.Value = v; };
        range.OnValueChange = v => { changes++; range.ActiveRangeStart = v.Start; range.ActiveRangeEnd = v.EndInclusive; };
        value.OnValueChangeFinished = range.OnValueChangeFinished = () => finished++;
        Control control = c.Kind switch
        {
            "vertical" => new VerticalSlider { State = value, TopToBottom = c.TopToBottom },
            "range" => new RangeSlider { State = range },
            _ => new MSlider { State = value },
        };
        if (!c.AutoLength) { if (c.Kind == "vertical") control.Height = c.Length; else control.Width = c.Length; }
        if (c.CrossSize > 0) { if (c.Kind == "vertical") control.Width = c.CrossSize; else control.Height = c.CrossSize; }
        var colors = c.CustomColors ? Palette() : null;
        var track = new SliderTrack { State = c.Kind == "range" ? range : value, Colors = colors,
            IsCentered = c.Centered, EnableCornerShrinking = c.Kind == "vertical" || c.Centered || c.CustomCorners,
            TrackCornerSize = c.CustomCorners ? 4 : double.NaN, ThumbTrackGapSize = c.Gap, ShowStopIndicator = c.Stop };
        SliderThumb Thumb(bool end = false) => new() { Width = c.Kind == "vertical" ? (c.CustomThumbs ? 32 : 44) : c.CustomThumbs ? end ? 20 : 12 : 4,
            Height = c.Kind == "vertical" ? c.CustomThumbs ? 12 : 4 : c.CustomThumbs ? end ? 50 : 32 : 44,
            IsVertical = c.Kind == "vertical", Colors = colors };
        // Default thumbs are product-owned, so input feedback remains part of the component.
        if (control is MSlider single)
        {
            single.Colors = colors; single.Track = track;
            if (c.CustomThumbs)
            {
                var custom = Thumb(); custom.Bind(SliderThumb.InteractionProperty, new Avalonia.Data.Binding(nameof(MSlider.Interaction)) { Source = single }); single.Thumb = custom;
            }
        }
        else if (control is RangeSlider pair)
        {
            pair.Colors = colors; pair.Track = track;
            if (c.CustomThumbs)
            {
                var start = Thumb(); var end = Thumb(true);
                start.Bind(SliderThumb.InteractionProperty, new Avalonia.Data.Binding(nameof(RangeSlider.StartInteraction)) { Source = pair });
                end.Bind(SliderThumb.InteractionProperty, new Avalonia.Data.Binding(nameof(RangeSlider.EndInteraction)) { Source = pair });
                pair.StartThumb = start; pair.EndThumb = end;
            }
        }
        control.IsEnabled = c.Enabled;
        control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
        control.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        host.Mount(new Panel { Margin = new Thickness(16), FlowDirection = control.FlowDirection, Children = { control } }); host.Frame(0);
        ContentPresenter Part(string name) => control.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == name);
        void Sample(string id)
        {
            var facts = new List<Measurement>(AvaloniaMeasurements.Geometry(control, host.Window))
            {
                Measurement.Numeric("value", c.Kind == "range" ? range.ActiveRangeStart : value.Value, "value", definition: "slider.active-start"),
                Measurement.Numeric("end", c.Kind == "range" ? range.ActiveRangeEnd : value.Value, "value", definition: "slider.active-end"),
                Measurement.Numeric("changes", changes, "count", definition: "slider.value-change-callbacks"),
                Measurement.Numeric("finished", finished, "count", definition: "slider.value-finished-callbacks"),
                Measurement.Numeric("feedback", (control is MSlider one ? one.Interaction : ((RangeSlider)control).StartInteraction | ((RangeSlider)control).EndInteraction) == Material3Expressive.Foundation.Interaction.InteractionState.None ? 0 : 1,
                    "boolean", definition: "slider.thumb-feedback-active"),
            };
            var points = new List<ProbePoint> { new("slider", "slider", null, facts.ToArray()), new("track", "track", "slider", AvaloniaMeasurements.Geometry(track, host.Window)) };
            foreach (var name in c.Kind == "range" ? new[] { "PART_StartThumb", "PART_EndThumb" } : new[] { "PART_Thumb" })
                points.Add(new(name == "PART_EndThumb" ? "end-thumb" : "thumb", "thumb", "slider", AvaloniaMeasurements.Geometry(Part(name).Child!, host.Window)));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        void Event(long time, string action, Point p) { host.AdvanceTo(time); host.Input(action, p); host.Draw(); host.Frame(time); }
        var a = c.Kind == "vertical" ? new Point(40, 80) : new Point(80, 40);
        var b = c.Kind == "vertical" ? new Point(40, 230) : new Point(230, 40);
        Sample("initial");
        if (c.Action == "none") return;
        if (c.Action is "program" or "theme" or "corner")
        {
            host.AdvanceTo(16000);
            if (c.Action == "program")
            {
                value.Value = 0.86f; range.ActiveRangeEnd = 1; range.ActiveRangeStart = 0.6f;
            }
            else if (c.Action == "corner") track.TrackCornerSize = 8;
            else
            {
                host.Window.RequestedThemeVariant = ThemeVariant.Dark;
                host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Primary] = Color.Parse("#D0BCFF");
                host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.SecondaryContainer] = Color.Parse("#4A4458");
                host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSurface] = Color.Parse("#E6E0E9");
                host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.Surface] = Color.Parse("#141218");
            }
            host.Draw(); host.Frame(16000); Sample("changed"); host.Frame(32000); Sample("late"); return;
        }
        if (c.Action == "keyboard")
        {
            host.AdvanceTo(1000);
            if (control is RangeSlider) Part("PART_StartThumb").Focus(); else control.Focus();
            host.Draw(); host.Frame(1000); Sample("focus");
            foreach (var (key, i) in new[] { c.Kind == "vertical" ? Key.Down : Key.Right, Key.PageUp, Key.End, Key.Home }.Select((key, i) => (key, i)))
            {
                host.AdvanceTo(2000 + i * 2000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(host.TimeUs); Sample($"key-{i}-down");
                host.AdvanceTo(3000 + i * 2000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(host.TimeUs); Sample($"key-{i}-up");
            }
            return;
        }
        if (c.Action == "cross-end") (a, b) = (b, a);
        Event(1000, "move", a); Event(2000, "down", a); Sample("pressed");
        if (c.Action is not ("tap" or "release-jump")) { Event(16000, "move", c.Action == "vertical-cancel" ? new(a.X, a.Y + 40) : b); Sample("moved"); }
        if (c.Action == "disable") { host.AdvanceTo(17000); control.IsEnabled = false; host.Draw(); host.Frame(17000); Sample("disabled"); }
        Event(32000, "up", c.Action == "tap" ? a : b); Sample("released");
        host.Frame(48000); Sample("late");
    }
    private static SliderColors Palette() => new(Color.Parse("#126E60"), Color.Parse("#126E60"), Color.Parse("#B2E6D6"), Color.Parse("#B2E6D6"), Color.Parse("#126E60"),
        Color.Parse("#50365A80"), Color.Parse("#50365A80"), Color.Parse("#20456789"), Color.Parse("#20456789"), Color.Parse("#50365A80"));
}
