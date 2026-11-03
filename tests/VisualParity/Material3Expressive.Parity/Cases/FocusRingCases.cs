using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record FocusRingConfig(string Kind = "button", string Shape = "round", string Action = "focus",
    bool Dark = false, bool Custom = false, bool Checked = false);

[ParitySuite("focus-ring")]
public sealed class FocusRingCases
{
    public static IEnumerable<CaseData<FocusRingConfig>> Configurations()
    {
        foreach (var shape in new[] { "round", "asymmetric", "square", "small-radius" })
            yield return new($"dynamic.button.{shape}", "Inset ring geometry and entry/exit", new(Shape: shape));
        foreach (var action in new[] { "reverse", "hover-blur", "resize", "disable", "mode", "theme" })
            yield return new($"dynamic.button.{action}", "Inset ring interruption/configuration", new(Shape: "asymmetric", Action: action));
        yield return new("dynamic.button.custom", "Custom stroke metrics and translucent colors", new(Custom: true));
        foreach (var kind in new[] { "checkbox", "radio" })
        {
            yield return new($"dynamic.{kind}.focus", "Explicit focus shape independent of circular ripple", new(Kind: kind));
            foreach (var action in new[] { "press-to-ring", "press-to-opacity", "focus-mode" })
                yield return new($"dynamic.{kind}.{action}", "Focus-theme changes keep the existing indication", new(Kind: kind, Action: action));
        }
        yield return new("dynamic.button.source-old-end", "Recreated collector receives an old source end", new(Action: "source-old-end"));
        foreach (var kind in new[] { "list-item", "segmented-list-item" })
        {
            foreach (var action in new[] { "focus", "reverse", "hover-blur", "disable", "mode", "press-to-ring", "press-to-opacity", "theme" })
                yield return new($"dynamic.{kind}.{action}", "List item focus ring follows its animated shape", new(Kind: kind, Action: action));
            yield return new($"dynamic.{kind}.custom", "List item custom focus ring metrics", new(Kind: kind, Custom: true));
        }
        foreach (var check in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"dynamic.switch.{(check ? "on" : "off")}.{(dark ? "dark" : "light")}", "Track focus independent of thumb", new(Kind: "switch", Dark: dark, Checked: check));
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<FocusRingConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        void SetTheme(bool dark)
        {
            // Match the reference's fixed lightColorScheme/darkColorScheme inputs, as in
            // the other component suites; MCU's generated palette is a separate concern.
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.Secondary] = Color.Parse(dark ? "#CCC2DC" : "#625B71");
            host.Window.Resources[Sys.Color.OnSecondary] = Color.Parse(dark ? "#332D41" : "#FFFFFF");
            host.Window.Resources[Sys.Color.Surface] = Color.Parse(dark ? "#141218" : "#FEF7FF");
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[Sys.Color.OnPrimaryContainer] = Color.Parse(dark ? "#EADDFF" : "#21005D");
        }
        SetTheme(config.Dark);
        Control control = config.Kind switch
        {
            "switch" => new ToggleSwitch { IsChecked = config.Checked },
            "checkbox" => new CheckBox(),
            "radio" => new RadioButton(),
            "list-item" => new ListItem { Width = 180, InteractionMode = ListItemInteractionMode.Click,
                Content = new Border { Width = 32, Height = 16, Background = Brushes.Blue } },
            "segmented-list-item" => new SegmentedListItem { Width = 180, GroupIndex = 0, GroupCount = 3,
                InteractionMode = ListItemInteractionMode.Click,
                Content = new Border { Width = 32, Height = 16, Background = Brushes.Blue } },
            _ => new Button { Width = 100, Height = 50, Padding = new Thickness(0),
                CornerRadius = config.Shape switch { "asymmetric" => new(24, 8, 0, 16), "square" => new(0), "small-radius" => new(0.5), _ => new(16) },
                BorderBrush = Brushes.Red, BorderThickness = new Thickness(1.1),
                Theme = (ControlTheme)host.Window.FindResource("MaterialOutlinedButtonTheme")! }
        };
        MaterialInteraction.SetFocusFeedback(control, config.Action == "press-to-ring" ? FocusFeedback.Opacity : FocusFeedback.InsetRing);
        if (config.Custom)
        {
            MaterialInteraction.SetFocusRingMetrics(control, new(1.1, 1.3, 2.4, 2.7));
            MaterialInteraction.SetFocusRingOuterColor(control, Color.Parse("#806600CC"));
            MaterialInteraction.SetFocusRingInnerColor(control, Color.Parse("#C000CC66"));
        }
        host.Mount(new Grid { Background = Brushes.White, Children = { new Border
            { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = control } } });
        host.Frame(-1000); host.Frame(0);
        var source = new MaterialInteractionSource();
        if (config.Action == "source-old-end") MaterialInteraction.GetOrCreate(control).ConnectSource(source);
        var next = 16000L;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var measurements = AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToList();
            if (config.Action == "theme")
            {
                var ring = control.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
                measurements.Add(Measurement.Numeric("outerThemeColor", ring.DefaultOuterColor!.Value.ToUInt32(), "argb32",
                    definition: "focus-ring.current-theme.secondary", source: "InsetFocusRingPresenter.DefaultOuterColor effective Token binding"));
            }
            writer.Submit(host.Snapshot(id, id, [new ProbePoint("control", "control", null, measurements.ToArray())]));
        }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mutate(long time, string id, Action change)
        {
            Advance(time); change(); writer.Record(time, "mutation", id); host.Draw();
            // These mutations invalidate Compose composition. Send the same explicit frame on
            // both sides before observing the configured component (not a stale composition).
            if (id is "resize" or "disable" or "opacity" or "inset" or "theme") host.Frame(time);
            Sample(id);
        }
        Sample("initial");
        if (config.Action is "press-to-ring" or "press-to-opacity")
        {
            Mutate(1000, "hover", () => host.Input("move", new Point(120, 80)));
            Times(16000, 80000, 160000);
            Mutate(161000, "press", () => host.Input("down", new Point(120, 80)));
            Times(176000, 208000);
            var toRing = config.Action == "press-to-ring";
            Mutate(209000, toRing ? "inset" : "opacity", () =>
                MaterialInteraction.SetFocusFeedback(control, toRing ? FocusFeedback.InsetRing : FocusFeedback.Opacity));
            Times(224000, 256000, 400000);
            Mutate(401000, "cancel", () => { host.Input("move", new Point(8, 8)); host.Input("up", new Point(8, 8)); });
            Times(416000, 480000, 800000);
            return;
        }
        if (config.Action == "source-old-end")
        {
            var first = new MaterialInteractionEvent.Focus();
            Mutate(1000, "source-focus", () => source.Emit(first));
            Times(16000, 80000, 800000);
            Mutate(801000, "opacity", () => MaterialInteraction.SetFocusFeedback(control, FocusFeedback.Opacity));
            Mutate(817000, "inset", () => MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing));
            Mutate(833000, "old-unfocus", () => source.Emit(new MaterialInteractionEvent.Unfocus(first)));
            Mutate(849000, "new-focus", () => source.Emit(new MaterialInteractionEvent.Focus()));
            Times(864000, 960000, 1200000);
            Mutate(1201000, "second-new-focus", () => source.Emit(new MaterialInteractionEvent.Focus()));
            Times(1216000, 1328000, 1600000);
            return;
        }
        Mutate(1000, "focus", () => control.Focus(NavigationMethod.Tab));
        Times(16000, 32000, 48000);
        if (config.Action == "reverse")
        {
            Mutate(49000, "blur", () => host.Window.FocusManager!.Focus(null)); Times(64000);
            Mutate(65000, "refocus", () => control.Focus(NavigationMethod.Tab));
        }
        Times(80000, 112000, 160000, 240000, 400000, 800000);
        switch (config.Action)
        {
            case "hover-blur":
                Mutate(801000, "hover", () => host.Input("move", new Point(120, 80))); Times(816000, 960000);
                Mutate(961000, "blur-under-hover", () => host.Window.FocusManager!.Focus(null)); Times(976000, 1120000);
                Mutate(1121000, "hover-exit", () => host.Input("move", new Point(8, 8))); Times(1136000, 1500000); return;
            case "resize": Mutate(801000, "resize", () => control.Width = 140); break;
            case "disable": Mutate(801000, "disable", () => control.IsEnabled = false); break;
            case "mode": Mutate(801000, "opacity", () => MaterialInteraction.SetFocusFeedback(control, FocusFeedback.Opacity)); break;
            case "focus-mode":
                Mutate(801000, "opacity", () => MaterialInteraction.SetFocusFeedback(control, FocusFeedback.Opacity));
                Times(816000, 960000);
                Mutate(961000, "inset", () => MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing));
                Times(976000, 1200000);
                Mutate(1201000, "final-blur", () => host.Window.FocusManager!.Focus(null));
                Times(1216000, 1248000, 1328000, 1600000);
                return;
            case "theme": Mutate(801000, "theme", () => SetTheme(true)); break;
        }
        Times(816000, 960000, 1200000);
        Mutate(1201000, "final-blur", () => host.Window.FocusManager!.Focus(null));
        Times(1216000, 1248000, 1328000, 1600000);
    }
}
