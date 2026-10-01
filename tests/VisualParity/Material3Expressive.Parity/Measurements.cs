using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.TextFormatting;
using Avalonia.Media;
using SkiaSharp;
namespace Material3Expressive.Parity;

public static class AvaloniaMeasurements
{
    public static Measurement[] Geometry(Control control, Window root)
    {
        var corners = new[] { new Point(), new Point(control.Bounds.Width, 0),
            new Point(control.Bounds.Width, control.Bounds.Height), new Point(0, control.Bounds.Height) }
            .Select(x => control.TranslatePoint(x, root) ?? throw new InvalidOperationException("Detached point.")).ToArray();
        var left = corners.Min(x => x.X); var top = corners.Min(x => x.Y);
        return [Measurement.Vector("box", "rect", [left, top, corners.Max(x => x.X) - left, corners.Max(x => x.Y) - top],
            "dip", "scene", "geometry", "layout.box.aabb.unclipped"),
            Measurement.Vector("corners", "quad", corners.SelectMany(x => new[] { x.X, x.Y }).ToArray(),
                "dip", "scene", "geometry", "layout.box.corners.unclipped"),
            Measurement.Vector("size", "point", [control.Bounds.Width, control.Bounds.Height], "dip", "local", "geometry", "layout.box.local-size")];
    }
    public static ProbePoint Text(string id, string? parentId, TextBlock block, Window root)
    {
        if (block.Inlines is { Count: > 0 }) throw new NotSupportedException("This text reader supports string content; supply a reader for rich inlines.");
        var layout = block.TextLayout;
        var fields = new List<Measurement>(Geometry(block, root))
        {
            Measurement.Text("content", block.Text ?? "", "text"),
            Measurement.Numeric("fontSize", block.FontSize, "sp", category: "text"),
            Measurement.Numeric("fontWeight", (int)block.FontWeight, "weight", category: "text"),
            Measurement.Numeric("letterSpacing", block.LetterSpacing, "sp", category: "text"),
            Measurement.Numeric("lineCount", layout.TextLines.Count, "count", category: "text"),
            new("resolvedFonts", "text", "strings", "none", "none", "text.resolved-font-families", "ShapedTextRun.GlyphTypeface",
                Strings: layout.TextLines.SelectMany(x => x.TextRuns).OfType<ShapedTextRun>()
                    .Select(x => x.GlyphRun.GlyphTypeface.FamilyName).Distinct().ToArray()),
            Measurement.Unavailable("drawOrigin", "text", "point", "dip", "scene", "TextBlock does not expose its actual RenderTextLayout origin."),
            Measurement.Unavailable("glyphInk", "text", "rect", "dip", "text", "No glyph ink reader installed; caret boxes are not ink boxes.")
        };
        fields.Add(block.Foreground is ISolidColorBrush brush
            ? Measurement.Vector("color", "color", [brush.Color.R / 255d, brush.Color.G / 255d, brush.Color.B / 255d, brush.Color.A / 255d * brush.Opacity],
                "srgb", "none", "text", "text.foreground.solid-srgba", "TextBlock.Foreground + Brush.Opacity")
            : Measurement.Unavailable("color", "text", "color", "srgb", "none", "Foreground is not a solid brush.", "text.foreground.solid-srgba"));
        double top = 0;
        for (var i = 0; i < layout.TextLines.Count; i++)
        {
            var line = layout.TextLines[i]; var prefix = $"line.{i}.";
            fields.AddRange([
                // TextLine.Length may include a formatter end-of-paragraph run beyond the input string.
                // Retain that native range separately; canonical range is its intersection with the string.
                Measurement.Vector(prefix + "nativeRange", "range", [line.FirstTextSourceIndex, line.FirstTextSourceIndex + line.Length],
                    "formatter-index", "text", "text", "avalonia.line.native-range", "TextLine.FirstTextSourceIndex + Length"),
                Measurement.Vector(prefix + "range", "range", [Math.Clamp(line.FirstTextSourceIndex, 0, (block.Text ?? "").Length),
                    Math.Clamp(line.FirstTextSourceIndex + line.Length, 0, (block.Text ?? "").Length)], "utf16", "text", "text", "text.line.source-range",
                    "derived: native range intersected with [0, input UTF16 length)"),
                Measurement.Numeric(prefix + "top", top, "dip", "text", "text", "text.line.top"),
                Measurement.Numeric(prefix + "height", line.Height, "dip", "text", "text", "text.line.height"),
                Measurement.Numeric(prefix + "baseline", top + line.Baseline, "dip", "text", "text", "text.line.baseline"),
                Measurement.Numeric(prefix + "width", line.WidthIncludingTrailingWhitespace, "dip", "text", "text", "avalonia.line.width-including-trailing"),
                Measurement.Flag(prefix + "ellipsized", line.HasCollapsed, "text", "text.line.ellipsized")]);
            top += line.Height;
        }
        return new(id, "text-block", parentId, fields.ToArray());
    }
}
public static class PixelData
{
    public static (int Width, int Height, byte[] Bytes) DecodePng(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Invalid PNG.");
        if (codec.EncodedFormat != SKEncodedImageFormat.Png) throw new InvalidDataException("Raster artifact must be PNG.");
        using var colorSpace = SKColorSpace.CreateSrgb();
        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height,
            SKColorType.Rgba8888, SKAlphaType.Unpremul, colorSpace));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new InvalidDataException("PNG decoding failed.");
        var bytes = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        System.Runtime.InteropServices.Marshal.Copy(bitmap.GetPixels(), bytes, 0, bytes.Length);
        return (bitmap.Width, bitmap.Height, bytes);
    }
    public static RasterObservation FromPng(string directory, string id, string image)
    {
        var decoded = DecodePng(Path.Combine(directory, image));
        var pixels = Path.ChangeExtension(image, ".rgba").Replace('\\', '/');
        File.WriteAllBytes(Path.Combine(directory, pixels), decoded.Bytes);
        return new(id, image, pixels, decoded.Width, decoded.Height);
    }
    public static byte[] Read(string directory, RasterObservation raster)
    {
        var path = Path.GetFullPath(Path.Combine(directory, raster.Pixels));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Pixel path outside bundle.");
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != checked(raster.Width * raster.Height * 4)) throw new InvalidDataException("Pixel buffer size mismatch.");
        return bytes;
    }
}
