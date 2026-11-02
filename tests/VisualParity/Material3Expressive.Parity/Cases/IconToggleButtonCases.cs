using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using static Material3Expressive.Tokens.MaterialTokens;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record IconToggleConfig(bool Selected = false, bool Enabled = true, bool Dark = false, string Action = "none",
    string Style = "standard", string Size = "small", string WidthOption = "uniform", bool Morph = false, string Preset = "none");

[ParitySuite("icon-toggle-button")]
public sealed class IconToggleButtonCases
{
    public static IEnumerable<CaseData<IconToggleConfig>> StaticConfigurations()
    {
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{style}.{(selected ? "checked" : "unchecked")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}",
                "Controlled icon toggle colors", new(selected, enabled, dark, Style: style));
        // Size × width are shared with IconButton, but exercise each toggle theme and both checked
        // states through its real template; default size/width is already covered above.
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var size in new[] { "xsmall", "small", "medium", "large", "xlarge" })
        foreach (var width in new[] { "narrow", "uniform", "wide" })
        foreach (var selected in new[] { false, true })
            if (size != "small" || width != "uniform")
                yield return new($"static.layout.{style}.{size}.{width}.{(selected ? "checked" : "unchecked")}",
                    "Toggle dimensions", new(Selected: selected, Style: style, Size: size, WidthOption: width));
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
            yield return new($"static.shape.{style}", "Initially checked shape", new(Selected: true, Style: style, Morph: true));
        foreach (var size in new[] { "xsmall", "small", "medium", "large", "xlarge" })
        foreach (var preset in new[] { "round", "square" })
        foreach (var selected in new[] { false, true })
            yield return new($"static.preset.{size}.{preset}.{(selected ? "checked" : "unchecked")}",
                "Size-specific shape presets", new(Selected: selected, Style: "filled", Size: size, Preset: preset));
    }
    public static IEnumerable<CaseData<IconToggleConfig>> DynamicConfigurations()
    {
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var action in new[] { "accept", "ignore", "disable", "reverse", "cancel" })
            yield return new($"dynamic.{style}.{action}", "Real pointer input and controlled state", new(Action: action, Style: style));
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var action in new[] { "accept", "reverse", "cancel", "disable" })
            yield return new($"dynamic.shape.{style}.{action}", "Pressed/checked shape interruption", new(Action: action, Style: style, Morph: true));
        foreach (var style in new[] { "standard", "filled", "tonal", "outlined" })
        foreach (var size in new[] { "xsmall", "small", "large" })
            yield return new($"dynamic.width.{style}.{size}", "Resize a pressed toggle and restore after selection",
                new(Action: "width", Style: style, Size: size, Morph: true));
        foreach (var size in new[] { "xsmall", "small", "medium", "large", "xlarge" })
        foreach (var preset in new[] { "round", "square" })
            yield return new($"dynamic.preset.{size}.{preset}", "Resting, pressed and selected preset shapes",
                new(Action: "accept", Style: "filled", Size: size, Preset: preset));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<IconToggleConfig>();
        await using var host = new AvaloniaHost(writer, 280, 220);
        CaseDefaults.SetThemeColors(host, config.Dark);
        // The new themes consume these additional roles. Match the fixed Compose light/dark
        // ColorScheme inputs, as in ToggleButtonCases; do not compare generated MCU palettes here.
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(config.Dark ? "#211F26" : "#F3EDF7");
        host.Window.Resources[Sys.Color.Secondary] = Color.Parse(config.Dark ? "#CCC2DC" : "#625B71");
        host.Window.Resources[Sys.Color.OnSecondary] = Color.Parse(config.Dark ? "#332D41" : "#FFFFFF");
        host.Window.Resources[Sys.Color.InverseSurface] = Color.Parse(config.Dark ? "#E6E0E9" : "#322F35");
        host.Window.Resources[Sys.Color.InverseOnSurface] = Color.Parse(config.Dark ? "#322F35" : "#F5EFF7");
        var button = new IconToggleButton { Symbol = MaterialSymbols.MaterialSymbolCodepoint.Favorite,
            IsChecked = config.Selected, IsEnabled = config.Enabled,
            WidthOption = Enum.Parse<IconButtonWidthOption>(config.WidthOption, true) };
        button.Theme = (ControlTheme)host.Window.FindResource($"Material{char.ToUpperInvariant(config.Style[0]) + config.Style[1..]}IconToggleButtonTheme")!;
        if (config.Size != "small") button.Classes.Add(config.Size);
        if (config.Preset != "none") button.Classes.Add("morph");
        if (config.Preset == "square") button.Classes.Add("square");
        if (config.Morph) { button.CheckedCornerRadius = new CornerRadius(12); MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8)); }
        var root = CaseDefaults.Center(button);
        TextElement.SetForeground(root, new SolidColorBrush(Color.Parse("#FF245A80")));
        var requests = 0;
        button.CheckedChangeRequested += (_, e) => { requests++; if (config.Action != "ignore") button.IsChecked = e.Value; };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var symbol = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
        var vector = new VectorSymbol { Symbol = symbol.Symbol, HorizontalAlignment = symbol.HorizontalAlignment, VerticalAlignment = symbol.VerticalAlignment };
        typeof(StyledElement).GetProperty(nameof(StyledElement.TemplatedParent))!.SetValue(vector, symbol.TemplatedParent);
        vector.SetValue(MaterialSymbol.DefaultSizeProperty, MaterialSymbol.GetDefaultSize(symbol), BindingPriority.Template);
        surface.Child = vector; host.Draw();
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("button", "container", null, AvaloniaMeasurements.Geometry(button, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
             new("state", "state", null, [Measurement.Flag("checked", button.IsChecked, definition: "icon-toggle.checked"),
                 Measurement.Numeric("requests", requests, "count", definition: "icon-toggle.requests")])]));
        Sample("initial"); if (!dynamic) return;
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "input", id); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "down", () => { host.Input("move", center); host.Input("down", center); });
        Advance(64000); host.Draw(); Sample("pressed");
        if (config.Action == "width") Change(65000, "pressed-wide", () => button.WidthOption = IconButtonWidthOption.Wide);
        if (config.Action == "disable") Change(65000, "disabled", () => button.IsEnabled = false);
        if (config.Action == "cancel") Change(65000, "drag-out", () => host.Input("move", new Point(4, 4)));
        Change(80000, "up", () => host.Input("up", config.Action == "cancel" ? new Point(4, 4) : center));
        if (config.Action == "reverse")
        {
            Advance(96000); host.Draw(); Sample("checked-in-flight");
            Change(97000, "repress", () => host.Input("down", center));
            Advance(112000); host.Draw(); Sample("repress-in-flight");
            Change(113000, "reverse-up", () => host.Input("up", center));
        }
        foreach (var time in (config.Action == "reverse" ? new long[] { 128000, 160000, 320000, 800000 } : new long[] { 96000, 160000, 320000, 800000 })) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Change(801000, "leave", () => host.Input("move", new Point(4, 4)));
        Advance(1200000); host.Draw(); Sample("late");
        if (config.Action == "width")
        {
            Change(1201000, "checked-narrow", () => button.WidthOption = IconButtonWidthOption.Narrow);
            Change(1202000, "restore-uniform", () => button.WidthOption = IconButtonWidthOption.Uniform);
        }
    }

    private sealed class VectorSymbol : IconButtonSymbol
    {
        protected override Type StyleKeyOverride => typeof(MaterialSymbol);
        public override void Render(DrawingContext context) => context.FillRectangle(Foreground!, new Rect(4, 4, Bounds.Width - 8, Bounds.Height - 8));
    }
}
