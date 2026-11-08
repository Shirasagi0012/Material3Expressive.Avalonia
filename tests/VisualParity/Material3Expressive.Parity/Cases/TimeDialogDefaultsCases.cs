using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record TimeDialogDefaultsConfig(string Kind = "clock", string Mode = "picker", bool Rtl = false,
    bool Dark = false, string Culture = "en-US", string Action = "none", bool PlatformAmbient = false);

[ParitySuite("time-dialog-defaults")]
public sealed class TimeDialogDefaultsCases
{
    public static IEnumerable<CaseData<TimeDialogDefaultsConfig>> Configurations()
    {
        foreach (var kind in new[] { "title", "clock", "scroll" })
        {
            foreach (var mode in new[] { "picker", "input", "scroll" })
            foreach (var rtl in new[] { false, true })
                yield return new($"static.{kind}.{mode}.{(rtl ? "rtl" : "ltr")}", "Default title or mode icon", new(kind, mode, rtl));
            foreach (var culture in new[] { "fr-FR", "zh-CN" })
                yield return new($"locale.{kind}.{culture.ToLowerInvariant()}", "Localized title and action description", new(kind, Culture: culture));
            yield return new($"diagnostic.ambient.{kind}", "Unmodified platform root content color", new(kind, Dark: true, PlatformAmbient: true));
            yield return new($"dark.{kind}", "Default content color", new(kind, Dark: true));
            yield return new($"dynamic.{kind}.mode", "Controlled mode updates", new(kind, Action: "mode"));
            if (kind == "title") continue;
            foreach (var action in new[] { "press", "hover", "focus", "reverse" })
                yield return new($"dynamic.{kind}.{action}", "Native input and real tooltip appearance", new(kind, Action: action));
        }
    }

    [ParityScenario("content", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TimeDialogDefaultsConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseSurface] = Color.Parse(c.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.InverseOnSurface] = Color.Parse(c.Dark ? "#322F35" : "#F5EFF7");
        if (!c.PlatformAmbient) Avalonia.Controls.Documents.TextElement.SetForeground(host.Window, Brushes.Black);
        var culture = CultureInfo.GetCultureInfo(c.Culture);
        var title = new TimePickerDialogTitle { Culture = culture, Width = 200 };
        TimePickerDisplayModeToggle toggle = c.Kind == "scroll" ? new TimeScrollDisplayModeToggle() : new TimePickerDisplayModeToggle();
        toggle.Culture = culture;
        void Mode(TimePickerDisplayMode mode) { title.DisplayMode = mode; toggle.DisplayMode = mode; }
        Mode(Enum.Parse<TimePickerDisplayMode>(c.Mode, true));
        Control control = c.Kind == "title" ? title : toggle;
        control.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
        control.Margin = new Thickness(60, 100, 0, 0);
        var clicks = 0; toggle.Click += (_, _) => clicks++;
        host.Mount(new Grid { Background = Brushes.White, Children = { control } }); host.Frame(-1000); host.Frame(0);
        var button = c.Kind == "title" ? null : toggle.GetVisualDescendants().OfType<IconButton>().Single();
        var next = 16000L;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new ProbePoint("control", "control", null, [.. AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size"),
            Measurement.Text("label", c.Kind == "title" ? title.Text! : AutomationProperties.GetName(button!)!, definition: "time-dialog-defaults.actual-label"),
            Measurement.Numeric("clicks", clicks, "count", definition: "time-dialog-defaults.click-count"),
            .. c.Kind == "title" ? new[] {
                Measurement.Numeric("fontSize", title.FontSize, "sp", category: "text", definition: "time-dialog-defaults.title-font-size"),
                Measurement.Numeric("fontWeight", (int)title.FontWeight, "weight", category: "text", definition: "time-dialog-defaults.title-font-weight"),
                Measurement.Numeric("lineHeight", title.LineHeight, "sp", category: "text", definition: "time-dialog-defaults.title-line-height"),
                Measurement.Numeric("letterSpacing", title.LetterSpacing, "sp", category: "text", definition: "time-dialog-defaults.title-letter-spacing") } : []])]));
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        if (c.Action == "mode")
        {
            Change(1000, "input", () => Mode(TimePickerDisplayMode.Input)); Times(16000);
            Change(17000, "scroll", () => Mode(TimePickerDisplayMode.Scroll)); Times(32000);
            Change(33000, "picker", () => Mode(TimePickerDisplayMode.Picker)); Times(48000);
        }
        else if (c.Action == "focus")
        {
            Change(1000, "focus", () => button!.Focus(NavigationMethod.Tab)); Times(16000, 64000, 224000, 400000);
            Change(401000, "escape", () => host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape")); Times(416000, 512000, 800000);
        }
        else if (c.Action is "hover" or "reverse")
        {
            Change(1000, "enter", () => host.Input("move", new(84, 124))); Times(16000, 64000);
            if (c.Action == "reverse")
            {
                Change(65000, "exit", () => host.Input("move", new(300, 220))); Times(80000);
                Change(81000, "enter-again", () => host.Input("move", new(84, 124)));
            }
            Times(224000, 400000); Change(401000, "exit-final", () => host.Input("move", new(300, 220))); Times(416000, 512000, 800000);
        }
        else if (c.Action == "press")
        {
            Change(1000, "press", () => { host.Input("move", new(84, 124)); host.Input("down", new(84, 124)); }); Times(16000, 64000, 160000);
            Change(161000, "release", () => host.Input("up", new(84, 124))); Times(176000, 400000);
        }
    }
}
