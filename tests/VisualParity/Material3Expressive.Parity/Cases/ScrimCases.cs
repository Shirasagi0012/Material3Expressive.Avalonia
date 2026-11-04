using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ScrimConfig(float Alpha = 1, string Color = "default", bool Dismiss = true, bool Dark = false, string Action = "none");
[ParitySuite("scrim")]
public sealed class ScrimCases
{
    public static IEnumerable<CaseData<ScrimConfig>> StaticConfigurations()
    {
        foreach (var color in new[] { "default", "#70336699", "#00336699", "none" })
        foreach (var alpha in new[] { 0f, .5f, 1f })
            yield return new($"static.{color.Replace('#', 'c')}.{alpha}", "Color and alpha", new(alpha, color));
        yield return new("static.alpha-negative", "Alpha below zero", new(-1));
        yield return new("static.alpha-excess", "Alpha above one", new(2));
        yield return new("static.decorative", "No click callback", new(Dismiss: false));
        yield return new("static.dark", "Dark theme default", new(Dark: true));
    }
    public static IEnumerable<CaseData<ScrimConfig>> Behaviors() =>
        new[] { "tap", "inside-travel", "outside-return", "disable-press", "alpha", "color" }
            .Select(action => new CaseData<ScrimConfig>($"dynamic.{action}", action, new(Action: action)));
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("behavior", Data = nameof(Behaviors))]
    public Task Behavior(CaptureWriter writer) => Capture(writer);
    private static async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ScrimConfig>(); await using var host = new AvaloniaHost(writer, 200, 160);
        CaseDefaults.SetThemeColors(host, config.Dark);
        var scrim = new Scrim { Alpha = config.Alpha, IsDismissEnabled = config.Dismiss };
        if (config.Color != "default") scrim.Color = config.Color == "none" ? null : Color.Parse(config.Color);
        var clicks = 0; scrim.Click += (_, _) => clicks++;
        host.Mount(new Border { Background = Brushes.White, Padding = new Thickness(20), Child = new Grid { Background = Brush.Parse("#FFD166"), Children = { scrim } } });
        host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("state", "state", null,
            [Measurement.Numeric("clicks", clicks, "count", definition: "scrim.clicks")])]));
        void Change(long time, string id, Action action)
        { host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        if (config.Action == "none") return;
        if (config.Action == "alpha")
        {
            Change(1000, "half", () => scrim.Alpha = .5f); Change(2000, "zero", () => scrim.Alpha = 0);
            Change(3000, "excess", () => scrim.Alpha = 2); return;
        }
        if (config.Action == "color")
        {
            Change(1000, "absent", () => scrim.Color = null); Change(2000, "custom", () => scrim.Color = Color.Parse("#70336699"));
            Change(3000, "default", () => scrim.ClearValue(Scrim.ColorProperty)); return;
        }
        Change(1000, "down", () => host.Window.MouseDown(new Point(40, 40), MouseButton.Left));
        if (config.Action == "disable-press") Change(2000, "disabled", () => scrim.IsDismissEnabled = false);
        else if (config.Action != "tap") Change(2000, "move", () => host.Window.MouseMove(config.Action == "outside-return" ? new Point(190, 150) : new Point(160, 100)));
        Change(3000, "up", () => { host.Window.MouseMove(new Point(40, 40)); host.Window.MouseUp(new Point(40, 40), MouseButton.Left); });
    }
}
