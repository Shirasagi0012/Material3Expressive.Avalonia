using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("surface")]
public sealed class SurfaceCases
{
    public static IEnumerable<CaseData<EmptyConfig>> Configurations() =>
        [new("static", "Surface with explicit fill", new())];


    [ParityScenario("static", Data = nameof(Configurations))]
    public async Task SurfaceStatic(CaptureWriter writer)
    {
        _ = writer.Metadata.ReadConfig<EmptyConfig>();
        await using var host = new AvaloniaHost(writer);
        var surface = new MaterialSurface { Width = 100, Height = 50, CornerRadius = new(12), Background = Brush.Parse("#6750A4") };
        host.Mount(CaseDefaults.Center(surface));
        host.Frame(-2000);
        host.Frame(-1000);
        host.Frame(0);
        writer.Submit(host.Snapshot("initial", "mounted", [
            new("surface", "container", null,
            [
                .. AvaloniaMeasurements.Geometry(surface, host.Window),
                Measurement.Vector("requestedColor", "color", [103, 80, 164, 255], "srgba8", "none", "properties", source: "declared")
            ])
        ]));
    }
}
