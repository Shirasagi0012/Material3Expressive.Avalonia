using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaTheory]
    [InlineData("assist")]
    [InlineData("filter")]
    [InlineData("input")]
    [InlineData("elevated-assist")]
    [InlineData("suggestion")]
    public async Task ChipFixtureUsesDisabledContainerOverride(string kind)
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 320, 160);
            var sceneType = typeof(ChipCases).GetNestedType("ChipScene", BindingFlags.NonPublic)!;
            var scene = Activator.CreateInstance(sceneType, host, new ChipConfig(kind, Enabled: false, Icons: "both", Custom: "colors"))!;
            var chip = (Chip)sceneType.GetProperty("Chip")!.GetValue(scene)!;
            host.Mount(CaseDefaults.Center(chip)); host.Frame(-1000); host.Frame(0);
            Assert.Equal(Color.Parse("#50336699"), ((ISolidColorBrush)chip.Background!).Color);
            var surface = chip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(Color.Parse("#50336699"), ((ISolidColorBrush)surface.Background!).Color);
            var effective = (IBrush)typeof(MaterialSurface).GetProperty("EffectiveBackground", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(surface)!;
            Assert.Equal(Color.Parse("#50336699"), ((ISolidColorBrush)effective).Color);
            // A state selector must target the Chip, not every disabled template descendant.
            Assert.All(chip.GetVisualDescendants().OfType<Border>(), border => Assert.Null(border.Background));
            writer.Submit(host.Snapshot("disabled", "fixture", []));
        });
        Assert.Empty(result.Capture.Errors);
    }

    [AvaloniaFact]
    public async Task ButtonGroupMarkerRepaintsInheritedInkWithoutLayoutOrAnimationFrames()
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 16, 16);
            // Exercise the actual fixture, independently of ButtonGroup, ToggleButton and ripple.
            var markerType = typeof(ButtonGroupCases).GetNestedType("Marker", BindingFlags.NonPublic)!;
            var marker = (Control)Activator.CreateInstance(markerType, 16d, 16d, null)!;
            var parent = new Border { Child = marker };
            TextElement.SetForeground(parent, Brushes.Red);
            host.Mount(parent); host.Frame(0);
            var size = marker.Bounds;
            var red = host.Snapshot("red", "fixture", []); writer.Submit(red);
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, PixelData.Read(writer.Output, red.Rasters.Single())[..4]);
            TextElement.SetForeground(parent, Brushes.Blue);
            host.Draw();
            var blue = host.Snapshot("blue", "fixture", []); writer.Submit(blue);
            Assert.Equal(size, marker.Bounds);
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, PixelData.Read(writer.Output, blue.Rasters.Single())[..4]);
            Assert.Equal(0, host.FrameTimeUs);
        });
        Assert.Empty(result.Capture.Errors);
        Assert.Single(result.Capture.Trace, e => e.Operation == "frame");
    }
}
