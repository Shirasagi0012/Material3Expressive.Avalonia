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
using NativeSlider = Avalonia.Controls.Slider;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

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
        // Keep the real pinned Compose component: direction-selected start drags
        // intentionally differ after the maintainer-approved overlap improvement.
        foreach (var rtl in new[] { false, true })
        foreach (var steps in new[] { 0, 3 })
        foreach (var end in new[] { "start", "end" })
            yield return Data($"dynamic.range.coincident.{(rtl ? "rtl" : "ltr")}.{end}.{steps}", new("range", steps, Value: 0.5f, End: 0.5f, Rtl: rtl, Action: $"coincident-{end}"));
        foreach (var rtl in new[] { false, true })
        foreach (var end in new[] { "start", "end" })
        foreach (var suffix in new[] { "reverse", "release" })
            yield return Data($"dynamic.range.coincident.{(rtl ? "rtl" : "ltr")}.{end}.{suffix}", new("range", 3, Value: 0.5f, End: 0.5f, Rtl: rtl, Action: $"coincident-{end}-{suffix}"));
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
        var range = new RangeSliderState(c.Value, c.End, c.Steps, c.Minimum, c.Maximum);
        range.ActiveRangeStart = c.Value; range.ActiveRangeEnd = c.End;
        var changes = 0; var finished = 0;
        range.OnValueChange = v => { changes++; range.ActiveRangeStart = v.Start; range.ActiveRangeEnd = v.EndInclusive; };
        range.OnValueChangeFinished = () => finished++;
        var intervals = c.Steps > 0 ? c.Steps + 1 : 100;
        var delta = (double)(c.Maximum - c.Minimum) / intervals;
        Control control = c.Kind == "range" ? new RangeSlider { State = range }
            : new NativeSlider { Minimum = c.Minimum, Maximum = c.Maximum, Value = c.Value,
                Orientation = c.Kind == "vertical" ? Orientation.Vertical : Orientation.Horizontal,
                IsDirectionReversed = c.Kind == "vertical" && c.TopToBottom,
                TickFrequency = c.Steps > 0 ? delta : 0, IsSnapToTickEnabled = c.Steps > 0,
                SmallChange = delta, LargeChange = delta * Math.Clamp(intervals / 10, 1, 10) };
        if (!c.AutoLength) { if (c.Kind == "vertical") control.Height = c.Length; else control.Width = c.Length; }
        if (c.CrossSize > 0) { if (c.Kind == "vertical") control.Width = c.CrossSize; else control.Height = c.CrossSize; }
        var colors = c.CustomColors ? Palette() : null;
        SliderThumb Thumb(bool end = false) => new() { Width = c.Kind == "vertical" ? (c.CustomThumbs ? 32 : 44) : c.CustomThumbs ? end ? 20 : 12 : 4,
            Height = c.Kind == "vertical" ? c.CustomThumbs ? 12 : 4 : c.CustomThumbs ? end ? 50 : 32 : 44,
            IsVertical = c.Kind == "vertical", Colors = colors };
        if (control is NativeSlider single)
        {
            single.ValueChanged += (_, _) => changes++;
            SliderAppearance.SetColors(single, colors);
            SliderAppearance.SetIsCentered(single, c.Centered);
            SliderAppearance.SetEnableCornerShrinking(single, c.Kind == "vertical" || c.Centered || c.CustomCorners);
            SliderAppearance.SetTrackCornerSize(single, c.CustomCorners ? 4 : double.NaN);
            SliderAppearance.SetThumbTrackGapSize(single, c.Gap);
            SliderAppearance.SetShowStopIndicator(single, c.Stop);
            if (c.CustomThumbs)
            {
                var theme = new ControlTheme(typeof(NativeSlider)) { BasedOn = (ControlTheme)host.Window.FindResource("MaterialSliderTheme")! };
                var style = new Style(s => s.Nesting().Template().OfType<Avalonia.Controls.Primitives.Thumb>().Name("PART_Thumb"));
                style.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
                    new FuncControlTemplate<Avalonia.Controls.Primitives.Thumb>((_, _) => Thumb())));
                theme.Children.Add(style); single.Theme = theme;
            }
        }
        else if (control is RangeSlider pair)
        {
            pair.Colors = colors;
            pair.Track = new SliderTrack { State = range, Colors = colors,
                TrackCornerSize = c.CustomCorners ? 4 : double.NaN, ThumbTrackGapSize = c.Gap, ShowStopIndicator = c.Stop };
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
        var track = control.GetVisualDescendants().OfType<SliderTrack>().Single();
        ContentPresenter Part(string name) => control.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == name);
        void Sample(string id)
        {
            var facts = new List<Measurement>(AvaloniaMeasurements.Geometry(control, host.Window))
            {
                Measurement.Numeric("value", c.Kind == "range" ? range.ActiveRangeStart : ((NativeSlider)control).Value, "value", definition: "slider.active-start"),
                Measurement.Numeric("end", c.Kind == "range" ? range.ActiveRangeEnd : ((NativeSlider)control).Value, "value", definition: "slider.active-end"),
                Measurement.Numeric("changes", changes, "count", definition: c.Kind == "range" ? "slider.value-change-callbacks" : "slider.native-value-changed-events"),
                c.Kind == "range" ? Measurement.Numeric("finished", finished, "count", definition: "slider.value-finished-callbacks")
                    : Measurement.Unavailable("finished", "properties", "number", "count", "none",
                        "Native Slider has no unified value-finished callback; this API was intentionally removed.", "slider.value-finished-callbacks"),
                Measurement.Numeric("feedback", (control is NativeSlider ? control.GetVisualDescendants().OfType<SliderThumb>().Single().Interaction : ((RangeSlider)control).StartInteraction | ((RangeSlider)control).EndInteraction) == Material3Expressive.Foundation.Interaction.InteractionState.None ? 0 : 1,
                    "boolean", definition: "slider.thumb-feedback-active"),
            };
            var points = new List<ProbePoint> { new("slider", "slider", null, facts.ToArray()), new("track", "track", "slider", AvaloniaMeasurements.Geometry(track, host.Window)) };
            foreach (var name in c.Kind == "range" ? new[] { "PART_StartThumb", "PART_EndThumb" } : new[] { "PART_Thumb" })
                points.Add(new(name == "PART_EndThumb" ? "end-thumb" : "thumb", "thumb", "slider", AvaloniaMeasurements.Geometry(control is NativeSlider
                    ? control.GetVisualDescendants().OfType<SliderThumb>().Single() : Part(name).Child!, host.Window)));
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
                if (control is NativeSlider native) native.Value = 0.86f;
                else { range.ActiveRangeEnd = 1; range.ActiveRangeStart = 0.6f; }
            }
            else if (c.Action == "corner") { if (control is NativeSlider native) SliderAppearance.SetTrackCornerSize(native, 8); else track.TrackCornerSize = 8; }
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
        if (c.Action.StartsWith("coincident-", StringComparison.Ordinal))
        {
            var direction = (c.Action.StartsWith("coincident-start", StringComparison.Ordinal) ? -1 : 1) * (c.Rtl ? -1 : 1);
            var thumb = Part("PART_StartThumb").Child!;
            a = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), host.Window)!.Value;
            b = new Point(a.X + direction * 74, a.Y);
        }
        Event(1000, "move", a); Event(2000, "down", a); Sample("pressed");
        if (c.Action is not ("tap" or "release-jump") && !c.Action.EndsWith("-release", StringComparison.Ordinal)) { Event(16000, "move", c.Action == "vertical-cancel" ? new(a.X, a.Y + 40) : b); Sample("moved"); }
        if (c.Action.EndsWith("-reverse", StringComparison.Ordinal)) { b = new Point(2 * a.X - b.X, a.Y); Event(24000, "move", b); Sample("reversed"); }
        if (c.Action == "disable") { host.AdvanceTo(17000); control.IsEnabled = false; host.Draw(); host.Frame(17000); Sample("disabled"); }
        Event(32000, "up", c.Action == "tap" ? a : b); Sample("released");
        host.Frame(48000); Sample("late");
    }
    private static SliderColors Palette() => new(Color.Parse("#126E60"), Color.Parse("#126E60"), Color.Parse("#B2E6D6"), Color.Parse("#B2E6D6"), Color.Parse("#126E60"),
        Color.Parse("#50365A80"), Color.Parse("#50365A80"), Color.Parse("#20456789"), Color.Parse("#20456789"), Color.Parse("#50365A80"));
}
