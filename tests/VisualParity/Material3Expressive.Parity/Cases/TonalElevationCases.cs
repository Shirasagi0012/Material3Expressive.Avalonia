using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TonalElevationConfig(string Kind = "surface", bool Enabled = true, bool? InnerEnabled = null,
    bool Dark = false, bool CustomColor = false, double Shadow = 0, bool Dynamic = false);

[ParitySuite("tonal-elevation")]
public sealed class TonalElevationCases
{
    public static IEnumerable<CaseData<TonalElevationConfig>> Cases()
    {
        foreach (var kind in new[] { "surface", "clickable", "selectable", "toggleable" })
        foreach (var enabled in new[] { false, true })
        foreach (var inner in new bool?[] { null, false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{kind}.{enabled}.{inner?.ToString() ?? "inherit"}.{dark}".ToLowerInvariant(),
                "Inherited tonal elevation gate", new(kind, enabled, inner, dark));
        foreach (var enabled in new[] { false, true })
        {
            yield return new($"static.custom.{enabled}".ToLowerInvariant(), "Custom color bypasses tint", new(Enabled: enabled, CustomColor: true));
            yield return new($"static.shadow.{enabled}".ToLowerInvariant(), "Shadow is independent from tonal tint", new(Enabled: enabled, Shadow: 6));
        }
        foreach (var kind in new[] { "surface", "clickable", "selectable", "toggleable" })
            yield return new($"dynamic.{kind}", "Live gate, nested override and elevation updates", new(kind, Dynamic: true));
    }

    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<TonalElevationConfig>();
        await using var host = new AvaloniaHost(writer, 240, 190);
        CaseDefaults.SetThemeColors(host, config.Dark);
        var surfaceColor = Color.Parse(config.Dark ? "#141218" : "#FFF7FF");
        host.Window.Resources[Sys.Color.Surface] = surfaceColor;
        host.Window.Resources[Sys.Color.SurfaceTint] = Color.Parse(config.Dark ? "#D0BCFF" : "#6750A4");
        Control inner = config.Kind switch
        {
            "clickable" => new ClickableSurface { TonalElevation = 3, ShadowElevation = config.Shadow },
            "selectable" => new SelectableSurface { TonalElevation = 3, ShadowElevation = config.Shadow },
            "toggleable" => new ToggleableSurface { TonalElevation = 3, ShadowElevation = config.Shadow },
            _ => new MaterialSurface { TonalElevation = 3, ShadowElevation = config.Shadow }
        };
        var marker = new Border { Width = 24, Height = 12, Background = Brushes.Black };
        if (inner is MaterialSurface s) { s.Child = marker; s.CornerRadius = new(12); }
        else { ((ContentControl)inner).Content = marker; ((TemplatedControl)inner).CornerRadius = new(12); }
        inner.Width = 100; inner.Height = 60;
        inner.HorizontalAlignment = HorizontalAlignment.Center; inner.VerticalAlignment = VerticalAlignment.Center;
        if (config.CustomColor)
        {
            if (inner is MaterialSurface surface) surface.Background = Brushes.Coral;
            else ((TemplatedControl)inner).Background = Brushes.Coral;
        }
        if (config.InnerEnabled is { } enabled) MaterialTonalElevation.SetIsEnabled(inner, enabled);
        var outer = new MaterialSurface { Width = 180, Height = 130, CornerRadius = new(16), TonalElevation = 2,
            Child = new Grid { Children = { inner } } };
        var root = (Grid)CaseDefaults.Center(outer);
        root.Background = Brushes.White;
        MaterialTonalElevation.SetIsEnabled(root, config.Enabled);
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        MaterialSurface InnerSurface() => inner as MaterialSurface ?? inner.GetVisualDescendants().OfType<MaterialSurface>().First();
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new("outer", "outer", null, [Measurement.Numeric("absoluteElevation", outer.AbsoluteTonalElevation, "dip", definition: "surface.absolute-tonal-elevation")]),
         new("inner", "inner", "outer", [Measurement.Numeric("absoluteElevation", InnerSurface().AbsoluteTonalElevation, "dip", definition: "surface.absolute-tonal-elevation"),
             Measurement.Flag("enabled", MaterialTonalElevation.GetIsEnabled(InnerSurface()), definition: "surface.tonal-enabled")])]));
        Sample("initial");
        if (!config.Dynamic) return;
        void Change(long time, string id, Action action)
        { host.AdvanceTo(time); action(); writer.Record(time, "mutation", id); host.Frame(time); Sample(id); }
        Change(1000, "disabled", () => MaterialTonalElevation.SetIsEnabled(root, false));
        Change(17000, "inner-enabled", () => MaterialTonalElevation.SetIsEnabled(inner, true));
        Change(33000, "ancestor-elevation", () => outer.TonalElevation = 8);
        Change(49000, "inherit-disabled", () => inner.ClearValue(MaterialTonalElevation.IsEnabledProperty));
        Change(65000, "enabled", () => MaterialTonalElevation.SetIsEnabled(root, true));
        Change(81000, "restore-elevation", () => outer.TonalElevation = 2);
    }
}
