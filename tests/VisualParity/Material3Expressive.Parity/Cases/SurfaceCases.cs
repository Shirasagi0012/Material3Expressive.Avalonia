using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record SurfaceConfig(string Shape = "round", string Layer = "fill", bool Dark = false,
    string Kind = "surface", double X = 130, double Y = 95, double Elevation = 0);

[ParitySuite("surface")]
public sealed class SurfaceCases
{
    public static IEnumerable<CaseData<SurfaceConfig>> Configurations()
    {
        foreach (var dark in new[] { false, true })
        foreach (var layer in new[] { "fill", "content", "border" })
            yield return new($"static.{layer}.{(dark ? "dark" : "light")}", "Background outside content clip", new(Layer: layer, Dark: dark));
        foreach (var shape in new[] { "asymmetric-large", "horizontal-overlap" })
            yield return new($"static.{shape}", "Large static corner normalization", new(Shape: shape, Layer: "border", Dark: true));
        foreach (var kind in new[] { "surface", "button", "fab" })
        foreach (var position in new[] { ("left", 32d, 48d), ("center", 130d, 95d), ("right", 228d, 142d) })
            yield return new($"shadow.{kind}.{position.Item1}", "Window-relative point light", new(Kind: kind, X: position.Item2, Y: position.Item3, Elevation: 6));
    }

    [ParityScenario("static", Data = nameof(Configurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SurfaceConfig>();
        await using var host = new AvaloniaHost(writer, 360, 240);
        var brush = new SolidColorBrush(Color.Parse(config.Dark ? "#211F26" : "#EADDFF"));
        Control control;
        if (config.Kind == "button")
        {
            var button = new Button { Background = brush, Padding = new Thickness(0), CornerRadius = new(16),
                Theme = (ControlTheme)host.Window.FindResource("MaterialElevatedButtonTheme")! };
            MaterialButton.SetElevation(button, new() { Default = config.Elevation });
            control = button;
        }
        else if (config.Kind == "fab")
            control = new FloatingActionButton { Background = brush, CornerRadius = new(16),
                Elevation = new(config.Elevation, config.Elevation, config.Elevation, config.Elevation) };
        else
            control = new MaterialSurface
            {
                Background = brush, ShadowElevation = config.Elevation,
                CornerRadius = config.Shape switch { "asymmetric-large" => new(80, 40, 0, 0), "horizontal-overlap" => new(80, 80, 0, 0), _ => new(16) },
                BorderBrush = config.Layer == "border" ? Brushes.Red : null,
                BorderThickness = new(config.Layer == "border" ? 1.1 : 0),
                Child = config.Layer == "content" ? new Border { Background = Brushes.Blue } : null
            };
        control.Width = 100; control.Height = 50;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        control.Margin = new Thickness(config.X, config.Y, 0, 0);
        host.Mount(new Grid { Background = Brushes.White, Children = { control } });
        host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", [new ProbePoint("container", "container", null,
            AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
    }
}
