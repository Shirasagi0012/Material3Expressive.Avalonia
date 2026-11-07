using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record IconConfig(string Source = "painter", string Intrinsic = "wide", bool Tint = true, bool Rtl = false,
    bool Mirror = false, double Width = 0, double Height = 0, double AvailableWidth = 100, double AvailableHeight = 100,
    double MinimumWidth = 0, double MinimumHeight = 0, string Action = "none");

[ParitySuite("icon")]
public sealed class IconCases
{
    public static IEnumerable<CaseData<IconConfig>> Configurations()
    {
        foreach (var source in new[] { "painter", "vector", "bitmap" })
        foreach (var tint in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.{source}.{tint}.{rtl}", new(source, Tint: tint, Rtl: rtl));
        foreach (var intrinsic in new[] { "none", "infinite", "width-only", "height-only", "zero", "fractional" })
            yield return Data($"intrinsic.{intrinsic}", new(Intrinsic: intrinsic));
        yield return Data("layout.width", new(Width: 60));
        yield return Data("layout.height", new(Height: 60));
        yield return Data("layout.fixed", new(Width: 53.25, Height: 39.75));
        yield return Data("layout.minimum", new(MinimumWidth: 60, MinimumHeight: 60));
        yield return Data("layout.narrow", new(AvailableWidth: 11, AvailableHeight: 80));
        yield return Data("layout.short", new(AvailableWidth: 80, AvailableHeight: 7));
        yield return Data("layout.unspecified-fixed", new(Intrinsic: "none", Width: 60, Height: 30));
        foreach (var tint in new[] { false, true })
            yield return Data($"mirror.{tint}", new("vector", Tint: tint, Rtl: true, Mirror: true));
        foreach (var action in new[] { "tint", "source", "size", "direction", "drawing" })
            yield return Data($"dynamic.{action}", new(Action: action));
    }
    private static CaseData<IconConfig> Data(string id, IconConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("render", Data = nameof(Configurations))]
    public async Task Render(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<IconConfig>();
        await using var host = new AvaloniaHost(writer, 160, 160);
        var intrinsic = c.Intrinsic switch
        {
            "none" => new Size(double.NaN, double.NaN), "infinite" => new Size(double.PositiveInfinity, double.PositiveInfinity),
            "width-only" => new Size(32, double.PositiveInfinity), "height-only" => new Size(double.PositiveInfinity, 16),
            "zero" => default, "fractional" => new Size(31.3, 15.7), _ => new Size(32, 16)
        };
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing { Brush = Brushes.Red, Geometry = new RectangleGeometry(new Rect(0, 0, 16, 16)) });
        drawing.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)), Geometry = new RectangleGeometry(new Rect(16, 0, 16, 8)) });
        var vector = new DrawingImage(drawing) { Viewbox = new Rect(0, 0, 32, 16) };
        using var bitmap = new WriteableBitmap(new PixelSize(128, 64), new Vector(384, 384), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (var pixels = bitmap.Lock())
        {
            var bytes = new byte[pixels.RowBytes * pixels.Size.Height];
            for (var y = 0; y < 64; y++) for (var x = 0; x < 128; x++)
            {
                var offset = y * pixels.RowBytes + x * 4;
                if (x < 64) { bytes[offset + 2] = 255; bytes[offset + 3] = 255; }
                else if (y < 32) { bytes[offset] = 255; bytes[offset + 3] = 128; }
            }
            Marshal.Copy(bytes, 0, pixels.Address, bytes.Length);
        }
        var icon = new MaterialIcon { Source = c.Source == "bitmap" ? bitmap : c.Source == "vector" || c.Action == "drawing" ? vector : new PatternImage(intrinsic),
            IsTintEnabled = c.Tint, AutoMirror = c.Mirror, MinWidth = c.MinimumWidth, MinHeight = c.MinimumHeight };
        if (c.Width > 0) icon.Width = c.Width;
        if (c.Height > 0) icon.Height = c.Height;
        var allocation = new Grid { Width = c.AvailableWidth, Height = c.AvailableHeight,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { icon } };
        TextElement.SetForeground(allocation, new SolidColorBrush(Color.FromArgb(192, 0, 128, 0)));
        host.Mount(CaseDefaults.Center(allocation)); host.Frame(0); Sample("initial");
        if (c.Action != "none")
        {
            host.AdvanceTo(1000);
            switch (c.Action)
            {
                case "tint": TextElement.SetForeground(allocation, Brushes.Purple); break;
                case "source": icon.Source = bitmap; break;
                case "size": icon.Width = 55.25; icon.Height = 33.75; break;
                case "direction": allocation.FlowDirection = FlowDirection.RightToLeft; icon.AutoMirror = true; break;
                case "drawing": ((GeometryDrawing)drawing.Children[0]).Brush = Brushes.Gold; break;
            }
            host.Frame(1000); Sample("changed");
            host.AdvanceTo(2000); icon.IsTintEnabled = false; host.Frame(2000); Sample("untinted");
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("icon", "icon", null, AvaloniaMeasurements.Geometry(icon, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
    }

    private sealed class PatternImage(Size size) : IImage
    {
        public Size Size => size;
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            context.FillRectangle(Brushes.Red, new Rect(destRect.X, destRect.Y, destRect.Width / 2, destRect.Height));
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
                new Rect(destRect.X + destRect.Width / 2, destRect.Y, destRect.Width / 2, destRect.Height / 2));
        }
    }
}
