using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ToggleFabIconConfig(string Size = "default", string Source = "vector", bool Checked = false,
    bool Rtl = false, bool Custom = false, bool Constrained = false, string Action = "none");

[ParitySuite("toggle-fab-icon")]
public sealed class ToggleFabIconCases
{
    public static IEnumerable<CaseData<ToggleFabIconConfig>> Configurations()
    {
        foreach (var size in new[] { "default", "medium", "large" })
        foreach (var source in new[] { "vector", "bitmap", "painter" })
        foreach (var check in new[] { false, true })
            yield return Data($"static.{size}.{source}.{check}".ToLowerInvariant(), new(size, source, check));
        foreach (var source in new[] { "vector", "bitmap", "painter" })
        {
            yield return Data($"static.rtl.{source}", new(Source: source, Rtl: true));
            yield return Data($"dynamic.reverse.{source}", new("large", source, Action: "reverse"));
        }
        yield return Data("static.constrained", new("large", Constrained: true));
        yield return Data("static.custom", new(Custom: true));
        yield return Data("dynamic.custom", new(Custom: true, Action: "reverse"));
        yield return Data("dynamic.check", new(Action: "check"));
        yield return Data("dynamic.uncheck", new(Checked: true, Action: "check"));
        yield return Data("dynamic.source", new(Action: "source"));
        yield return Data("dynamic.drawing", new(Action: "drawing"));
        yield return Data("dynamic.drawing-repaint", new(Action: "drawing-repaint"));
    }
    private static CaseData<ToggleFabIconConfig> Data(string id, ToggleFabIconConfig c) => new(id, id, c);

    [ParityScenario("render", Data = nameof(Configurations))]
    public async Task Render(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<ToggleFabIconConfig>();
        await using var host = new AvaloniaHost(writer, 200, 180);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.PrimaryContainer] = Color.Parse("#EADDFF");
        host.Window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnPrimaryContainer] = Color.Parse("#21005D");
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing { Brush = Brushes.Red, Geometry = new RectangleGeometry(new Rect(0, 0, 20, 16)) });
        drawing.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)), Geometry = new RectangleGeometry(new Rect(12, 0, 20, 8)) });
        var vector = new DrawingImage(drawing) { Viewbox = new Rect(0, 0, 32, 16) };
        using var bitmap = new WriteableBitmap(new PixelSize(128, 64), new Vector(384, 384), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (var pixels = bitmap.Lock())
        {
            var bytes = new byte[pixels.RowBytes * pixels.Size.Height];
            for (var y = 0; y < 64; y++) for (var x = 0; x < 128; x++)
            {
                var i = y * pixels.RowBytes + x * 4;
                if (x < 80) { bytes[i + 2] = 255; bytes[i + 3] = 255; }
                if (x >= 48 && y < 32)
                { bytes[i] = x < 80 ? (byte)128 : (byte)255; bytes[i + 2] = x < 80 ? (byte)127 : (byte)0; bytes[i + 3] = x < 80 ? (byte)255 : (byte)128; }
            }
            Marshal.Copy(bytes, 0, pixels.Address, bytes.Length);
        }
        var icon = new ToggleFloatingActionButtonIcon { Source = c.Source switch { "bitmap" => bitmap, "painter" => new PatternImage(), _ => vector }, AutoMirror = c.Rtl };
        if (c.Custom) { icon.Size = p => 25.25 + 8 * p; icon.Color = p => Color.FromArgb(128, (byte)(40 + 80 * p), 140, 30); }
        var toggle = new ToggleFloatingActionButton { Size = Enum.Parse<FloatingActionButtonSize>(c.Size, true), IsChecked = c.Checked, Icon = icon,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Constrained) { toggle.Width = 24; toggle.Height = 24; }
        host.Mount(CaseDefaults.Center(toggle)); host.Frame(-1000); host.Frame(0); Sample("initial");
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        if (c.Action != "none")
        {
            Advance(1000);
            if (c.Action == "source") icon.Source = bitmap;
            else if (c.Action.StartsWith("drawing")) ((GeometryDrawing)drawing.Children[0]).Geometry = new RectangleGeometry(new Rect(0, 0, 8, 16));
            else toggle.IsChecked = !toggle.IsChecked;
            host.Frame(1000); Sample("changed"); At(16000); At(64000);
            if (c.Action == "reverse") { Advance(65000); toggle.IsChecked = !toggle.IsChecked; host.Frame(65000); Sample("reversed"); }
            if (c.Action == "drawing-repaint") { Advance(65000); toggle.IsChecked = true; host.Frame(65000); Sample("repaint"); }
            At(96000); At(160000); At(320000); At(800000);
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("fab", "fab", null, [.. AvaloniaMeasurements.Geometry(toggle, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Numeric("checkedProgress", toggle.CheckedProgress, "ratio", category: "motion", definition: "fab.checked-progress")]),
             new("icon", "icon", "fab", [.. AvaloniaMeasurements.Geometry(icon, host.Window).Where(m => m.Name is "box" or "size"),
                 Measurement.Numeric("drawingWidth", ((GeometryDrawing)drawing.Children[0]).Geometry!.Bounds.Width, "dip", definition: "icon.source.red-rectangle-width")])]));
    }

    private sealed class PatternImage : IImage
    {
        public Size Size => new(32, 16);
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            context.FillRectangle(Brushes.Red, new Rect(destRect.X, destRect.Y, destRect.Width * 20 / 32, destRect.Height));
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
                new Rect(destRect.X + destRect.Width * 12 / 32, destRect.Y, destRect.Width * 20 / 32, destRect.Height / 2));
        }
    }
}
