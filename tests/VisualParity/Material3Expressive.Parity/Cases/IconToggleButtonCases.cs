using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record IconToggleConfig(bool Selected = false, bool Enabled = true, bool Dark = false, string Action = "none");

[ParitySuite("icon-toggle-button")]
public sealed class IconToggleButtonCases
{
    public static IEnumerable<CaseData<IconToggleConfig>> StaticConfigurations()
    {
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{(selected ? "checked" : "unchecked")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}",
                "Standard controlled icon toggle", new(selected, enabled, dark));
    }
    public static IEnumerable<CaseData<IconToggleConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "accept", "ignore", "disable" })
            yield return new($"dynamic.{action}", "Real pointer input and controlled state", new(Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<IconToggleConfig>();
        await using var host = new AvaloniaHost(writer, 120, 100);
        CaseDefaults.SetThemeColors(host, config.Dark);
        var button = new IconToggleButton { Symbol = MaterialSymbols.MaterialSymbolCodepoint.Favorite,
            IsChecked = config.Selected, IsEnabled = config.Enabled };
        var root = CaseDefaults.Center(button);
        TextElement.SetForeground(root, new SolidColorBrush(Color.Parse("#FF245A80")));
        var requests = 0;
        button.CheckedChangeRequested += (_, e) => { requests++; if (config.Action != "ignore") button.IsChecked = e.Value; };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var symbol = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
        var vector = new VectorSymbol { Symbol = symbol.Symbol, HorizontalAlignment = symbol.HorizontalAlignment, VerticalAlignment = symbol.VerticalAlignment };
        typeof(StyledElement).GetProperty(nameof(StyledElement.TemplatedParent))!.SetValue(vector, symbol.TemplatedParent);
        vector.SetValue(Control.WidthProperty, symbol.Width, BindingPriority.Template);
        vector.SetValue(Control.HeightProperty, symbol.Height, BindingPriority.Template);
        surface.Child = vector; host.Draw();
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("button", "container", null, AvaloniaMeasurements.Geometry(button, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
             new("state", "state", null, [Measurement.Flag("checked", button.IsChecked, definition: "icon-toggle.checked"),
                 Measurement.Numeric("requests", requests, "count", definition: "icon-toggle.requests")])]));
        Sample("initial"); if (!dynamic) return;
        var center = button.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "input", id); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "down", () => { host.Input("move", center); host.Input("down", center); });
        Advance(64000); host.Draw(); Sample("pressed");
        if (config.Action == "disable") Change(65000, "disabled", () => button.IsEnabled = false);
        Change(80000, "up", () => host.Input("up", center));
        foreach (var time in new long[] { 96000, 160000, 320000, 800000 }) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        Change(801000, "leave", () => host.Input("move", new Point(4, 4)));
        Advance(1200000); host.Draw(); Sample("late");
    }

    private sealed class VectorSymbol : MaterialSymbol
    {
        protected override Type StyleKeyOverride => typeof(MaterialSymbol);
        public override void Render(DrawingContext context) => context.FillRectangle(Foreground!, new Rect(4, 4, Bounds.Width - 8, Bounds.Height - 8));
    }
}
