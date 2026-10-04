using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ProgressIndicatorConfig(string Kind = "linear", double Progress = .5, bool Indeterminate = false,
    bool Dark = false, bool Rtl = false, string Cap = "round", double? StrokeWidth = null, double? TrackWidth = null,
    double? Gap = null, double? Stop = null, double? Amplitude = null, double? Wavelength = null, double? Speed = 0,
    double? Width = null, double? Height = null, double? ConstraintWidth = null, double? ConstraintHeight = null,
    string? Color = null, string? TrackColor = null, string Action = "none");

[ParitySuite("progress-indicator")]
public sealed class ProgressIndicatorCases
{
    static readonly string[] Kinds = ["linear", "circular", "linear-wavy", "circular-wavy"];
    public static IEnumerable<CaseData<ProgressIndicatorConfig>> StaticConfigurations()
    {
        foreach (var kind in Kinds)
        {
            // Every drawing branch gets endpoint, cap-entry, amplitude-threshold and clamping inputs.
            foreach (var (id, progress) in new[] { ("negative", -.2), ("zero", 0), ("tiny", .005), ("low", .1),
                ("wave-entry", .1001), ("half", .5), ("wave-exit-before", .9499), ("wave-exit", .95), ("full", 1), ("over", 1.2) })
                yield return Data($"static.progress.{kind}.{id}", new(kind, progress));
            yield return Data($"static.theme.{kind}.dark", new(kind, Dark: true));
            yield return Data($"static.rtl.{kind}", new(kind, Rtl: true));
            foreach (var cap in new[] { "butt", "square" })
                yield return Data($"static.cap.{kind}.{cap}", new(kind, Cap: cap));
            yield return Data($"static.gap.{kind}.zero", new(kind, Gap: 0));
            yield return Data($"static.gap.{kind}.large", new(kind, Gap: 12));
            yield return Data($"static.color.{kind}", new(kind, Color: "#A0126E60", TrackColor: "#70336699"));
            yield return Data($"static.layout.{kind}.small", new(kind, ConstraintWidth: 24, ConstraintHeight: 2));
            yield return Data($"static.layout.{kind}.zero", new(kind, ConstraintWidth: 0, ConstraintHeight: 0));
            yield return Data($"static.layout.{kind}.explicit", new(kind, Width: kind.StartsWith("linear") ? 120 : 64,
                Height: kind.StartsWith("linear") ? 16 : 64));
            yield return Data($"static.layout.{kind}.non-square", new(kind, Width: 32, Height: 64));
            if (kind.Contains("wavy"))
            {
                yield return Data($"static.stroke.{kind}.independent", new(kind, StrokeWidth: 6, TrackWidth: 2, Stop: 2));
                yield return Data($"static.wave.{kind}.flat", new(kind, Amplitude: 0));
                yield return Data($"static.wave.{kind}.partial", new(kind, Amplitude: .5));
                yield return Data($"static.wave.{kind}.wavelength", new(kind, Wavelength: 30));
            }
            else if (kind == "circular")
                yield return Data("static.stroke.circular.thick", new(kind, StrokeWidth: 8));
            if (kind.StartsWith("linear"))
                yield return Data($"static.stop.{kind}.none", new(kind, Stop: 0));
        }
    }

    public static IEnumerable<CaseData<ProgressIndicatorConfig>> DynamicConfigurations()
    {
        foreach (var kind in Kinds)
        {
            yield return Data($"dynamic.progress.{kind}", new(kind, Progress: .05, Action: "progress"));
            yield return Data($"dynamic.mode.{kind}", new(kind, Action: "mode", Speed: null));
            yield return Data($"dynamic.indeterminate.{kind}", new(kind, Indeterminate: true, Speed: null));
            yield return Data($"dynamic.indeterminate.{kind}.rtl-custom", new(kind, Indeterminate: true, Rtl: true,
                StrokeWidth: 6, TrackWidth: 2, Amplitude: .5, Wavelength: 30, TrackColor: "#E8DEF8", Speed: null));
            yield return Data($"dynamic.theme.{kind}", new(kind, Action: "theme", Speed: 0));
            if (kind.Contains("wavy"))
            {
                yield return Data($"dynamic.mode.{kind}.partial", new(kind, Action: "mode", Speed: null,
                    Amplitude: .5, StrokeWidth: 6, TrackWidth: 2, Wavelength: 30, Rtl: true));
                yield return Data($"dynamic.wave.{kind}", new(kind, Speed: null));
                yield return Data($"dynamic.speed.{kind}", new(kind, Action: "speed", Speed: null));
            }
        }
    }
    static CaseData<ProgressIndicatorConfig> Data(string id, ProgressIndicatorConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 320, 160);
        var scene = new Scene(host, writer.Metadata.ReadConfig<ProgressIndicatorConfig>());
        host.Mount(scene.Root); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ProgressIndicatorConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Frame(0);
        long frame = 0;
        void Advance(long time)
        {
            while (frame + 16000 <= time) { frame += 16000; host.Frame(frame); }
            host.AdvanceTo(time); host.Draw();
        }
        void Sample(long time, string id, bool image = true)
        { Advance(time); writer.Submit(host.Snapshot(id, id, scene.Measure(), image)); }
        void Change(long time, string id, Action action)
        {
            Advance(time); action(); writer.Record(time, "mutation", id); host.Draw();
            // Resolve recomposition, then resume animations created by that batch at the same
            // logical time. Both hosts use these two batches; no elapsed time is introduced.
            host.Frame(time); host.Frame(time);
            writer.Submit(host.Snapshot(id, id, scene.Measure()));
        }
        Sample(0, "initial");
        switch (config.Action)
        {
            case "progress":
                Change(200000, "progress-80", () => scene.Indicator.Progress = .8);
                Sample(280000, "amplitude-increasing");
                Change(350000, "progress-98-during-amplitude", () => scene.Indicator.Progress = .98);
                Sample(600000, "amplitude-late");
                Change(800000, "progress-40", () => scene.Indicator.Progress = .4);
                Change(1000000, "progress-98", () => scene.Indicator.Progress = .98);
                Sample(1200000, "amplitude-decreasing");
                Change(1250000, "progress-50-during-amplitude", () => scene.Indicator.Progress = .5);
                Change(1600000, "progress-60", () => scene.Indicator.Progress = .6);
                Sample(1900000, "amplitude-increasing-again"); Sample(2200000, "late", false); break;
            case "mode":
                Sample(320000, "determinate-wave");
                Change(400000, "indeterminate", () => scene.Indicator.IsIndeterminate = true);
                Sample(800000, "indeterminate-progress");
                Change(1000000, "determinate", () => scene.Indicator.IsIndeterminate = false);
                Sample(1200000, "determinate-again"); break;
            case "theme":
                Change(200000, "dark", () => CaseDefaults.SetThemeColors(host, true));
                Change(400000, "override", () => scene.Indicator.Color = Colors.Red);
                Change(600000, "light-under-override", () => CaseDefaults.SetThemeColors(host, false));
                Change(800000, "clear-override", () => scene.Indicator.ClearValue(ProgressIndicator.ColorProperty)); break;
            case "speed":
                Sample(320000, "moving");
                Change(400000, "speed-change", () => ((WavyProgressIndicator)scene.Indicator).WaveSpeed = 10);
                Sample(800000, "changed-speed");
                Change(1000000, "stopped", () => ((WavyProgressIndicator)scene.Indicator).WaveSpeed = 0);
                Sample(1200000, "stopped-late");
                Change(1400000, "restart", () => ((WavyProgressIndicator)scene.Indicator).ClearValue(WavyProgressIndicator.WaveSpeedProperty));
                Sample(1800000, "restarted"); break;
            default:
                foreach (var time in new long[] { 16000, 250000, 650000, 900000, 1000000, 1250000, 1500000,
                    1744000, 1750000, 1760000, 3000000, 4800000, 5984000, 6000000, 6016000,
                    9000000, 9024000 }) Sample(time, $"time-{time}"); break;
        }
    }

    sealed class Scene
    {
        readonly AvaloniaHost host;
        public ProgressIndicator Indicator { get; }
        public Control Root { get; }
        public Scene(AvaloniaHost host, ProgressIndicatorConfig config)
        {
            this.host = host;
            CaseDefaults.SetThemeColors(host, config.Dark);
            Indicator = config.Kind switch
            { "linear" => new LinearProgressIndicator(), "circular" => new CircularProgressIndicator(),
                "linear-wavy" => new LinearWavyProgressIndicator(), _ => new CircularWavyProgressIndicator() };
            Indicator.Progress = config.Progress; Indicator.IsIndeterminate = config.Indeterminate;
            if (config.Rtl) Indicator.FlowDirection = FlowDirection.RightToLeft;
            if (config.Width is { } width) Indicator.Width = width;
            if (config.Height is { } height) Indicator.Height = height;
            if (config.Gap is { } gap) Indicator.GapSize = gap;
            if (config.Color is { } color) Indicator.Color = Color.Parse(color);
            if (config.TrackColor is { } track) Indicator.TrackColor = Color.Parse(track);
            var cap = config.Cap switch { "butt" => PenLineCap.Flat, "square" => PenLineCap.Square, _ => PenLineCap.Round };
            if (Indicator is WavyProgressIndicator wave)
            {
                wave.Stroke = new(config.StrokeWidth ?? 4, cap); wave.TrackStroke = new(config.TrackWidth ?? 4, cap);
                wave.Amplitude = config.Amplitude; wave.Wavelength = config.Wavelength; wave.WaveSpeed = config.Speed;
            }
            if (Indicator is LinearProgressIndicator linear)
            { linear.StrokeCap = cap; if (config.Stop is { } stop) linear.StopSize = stop; }
            if (Indicator is CircularProgressIndicator circular)
            { circular.StrokeCap = cap; if (config.StrokeWidth is { } stroke) circular.StrokeWidth = stroke; }
            if (Indicator is LinearWavyProgressIndicator wavy && config.Stop is { } waveStop) wavy.StopSize = waveStop;
            if (config.ConstraintWidth is { } cw && config.ConstraintHeight is { } ch)
                Root = CaseDefaults.Center(new Grid { Width = cw, Height = ch, Children = { Indicator } });
            else Root = CaseDefaults.Center(Indicator);
        }
        public ProbePoint[] Measure() => [new("indicator", "indicator", null,
            AvaloniaMeasurements.Geometry(Indicator, host.Window).Where(m => m.Name is "box" or "size").ToArray())];
    }
}
