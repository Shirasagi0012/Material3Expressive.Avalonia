using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record LegacySliderTrackConfig(string Mode = "standalone", float Start = .25f, float End = .75f,
    int Steps = 0, bool Enabled = true, bool Rtl = false, bool Translucent = false, bool Dynamic = false,
    bool CustomColors = true);

[ParitySuite("legacy-slider-track")]
public sealed class LegacySliderTrackCases
{
    public static IEnumerable<CaseData<LegacySliderTrackConfig>> Cases()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var steps in new[] { 0, 3 })
        foreach (var (id, start, end) in new[] { ("single", 0f, .25f), ("range", .25f, .75f),
            ("empty", .5f, .5f), ("zero", 0f, 0f), ("full", 0f, 1f) })
            yield return new($"static.{id}.{(steps == 0 ? "continuous" : "ticks")}.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}",
                "Overlapping round-cap strokes and grouped ticks", new(Start: start, End: end, Steps: steps, Enabled: enabled, Rtl: rtl));
        foreach (var rtl in new[] { false, true })
        {
            foreach (var enabled in new[] { false, true })
                yield return new($"static.default-colors.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}",
                    "Default token colors", new(Steps: 3, Enabled: enabled, Rtl: rtl, CustomColors: false));
            yield return new($"static.translucent.{(rtl ? "rtl" : "ltr")}", "Inactive under active alpha compositing", new(Rtl: rtl, Translucent: true));
            foreach (var mode in new[] { "native", "range", "standalone" })
                yield return new($"dynamic.{mode}.{(rtl ? "rtl" : "ltr")}", "State, enabled, colors, direction and remount", new(mode, Steps: 3, Rtl: rtl, Dynamic: true));
        }
    }

    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<LegacySliderTrackConfig>();
        await using var host = new AvaloniaHost(writer, 240, 96);
        CaseDefaults.SetThemeColors(host, false);
        var state = new RangeSliderState(c.Start, c.End, c.Steps);
        var colors = c.CustomColors ? Palette(c.Translucent) : null;
        var standalone = new LegacySliderTrack { State = state, Width = 180, Colors = colors };
        var native = new Slider { Width = 180, Minimum = 0, Maximum = 1, Value = c.End,
            TickFrequency = c.Steps == 0 ? 0 : 1d / (c.Steps + 1) };
        SliderAppearance.SetColors(native, colors);
        var range = new RangeSlider { Width = 180, State = state, Colors = colors,
            TrackTemplate = new FuncDataTemplate<RangeSliderState>((value, _) => new LegacySliderTrack { State = value }) };
        Control target = c.Mode switch { "native" => native, "range" => range, _ => standalone };
        target.IsEnabled = c.Enabled;
        var root = (Grid)CaseDefaults.Center(target); root.Background = Brushes.White;
        root.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        host.Mount(root);
        if (c.Mode == "native") native.Theme = (ControlTheme)native.FindResource("MaterialLegacySliderTheme")!;
        host.Frame(0);
        LegacySliderTrack Track() => target is LegacySliderTrack track ? track : target.GetVisualDescendants().OfType<LegacySliderTrack>().Single();
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("track", "legacy-slider-track", null,
                AvaloniaMeasurements.Geometry(Track(), host.Window).Where(m => m.Name is "box" or "size").ToArray())], true));
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action)
        { host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        Change(16000, "value", () => { state.ActiveRangeEnd = 1; state.ActiveRangeStart = .5f; native.Value = .5; });
        Change(32000, "disabled", () => target.IsEnabled = false);
        Change(48000, "translucent", () =>
        {
            colors = Palette(true); standalone.Colors = colors; range.Colors = colors; SliderAppearance.SetColors(native, colors);
            target.IsEnabled = true;
        });
        Change(64000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
        Change(80000, "replace-state", () => { state = new RangeSliderState(.125f, .875f); standalone.State = state; range.State = state;
            native.Ticks = new Avalonia.Collections.AvaloniaList<double> { 0, .125, .875, 1 }; native.Value = .75; });
        Change(96000, "remount", () => { root.Children.Remove(target); host.Draw(); root.Children.Add(target); });
    }

    private static SliderColors Palette(bool translucent) => new(Colors.Black,
        Color.Parse(translucent ? "#804488CC" : "#FF4488CC"), Color.Parse("#FFFFDD55"),
        Color.Parse(translucent ? "#80775533" : "#FF775533"), Color.Parse("#FFDD4466"),
        Color.Parse("#FF999999"), Color.Parse("#FF777777"), Color.Parse("#FFDDDDDD"),
        Color.Parse("#FFCCCCCC"), Color.Parse("#FF555555"));
}
