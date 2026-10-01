using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.TextFormatting;
using Avalonia.Media;

namespace Material3Expressive.Parity;

public static class AvaloniaMeasurements
{
    public static Measurement[] Geometry(Control control, Window root)
    {
        var corners = new[]
            {
                new Point(), new Point(control.Bounds.Width, 0),
                new Point(control.Bounds.Width, control.Bounds.Height), new Point(0, control.Bounds.Height)
            }
            .Select(x => control.TranslatePoint(x, root) ?? throw new InvalidOperationException("Detached point.")).ToArray();
        var left = corners.Min(x => x.X);
        var top = corners.Min(x => x.Y);
        return
        [
            Measurement.Vector("box", "rect", [left, top, corners.Max(x => x.X) - left, corners.Max(x => x.Y) - top],
                "dip", "scene", "geometry", "layout.box.aabb.unclipped", "Control.Bounds + TranslatePoint"),
            Measurement.Vector("corners", "quad", corners.SelectMany(x => new[] { x.X, x.Y }).ToArray(),
                "dip", "scene", "geometry", "layout.box.corners.unclipped", "Control.Bounds + TranslatePoint"),
            Measurement.Vector("size", "point", [control.Bounds.Width, control.Bounds.Height], "dip", "local", "geometry",
                "layout.box.local-size",
                "Control.Bounds.Size", ["width", "height"])
        ];
    }

    public static ProbePoint Text(string id, string? parentId, TextBlock block, Window root)
    {
        if (block.Inlines is { Count: > 0 })
            throw new NotSupportedException("This text reader supports string content; supply a reader for rich inlines.");
        var layout = block.TextLayout;
        var fields = new List<Measurement>(Geometry(block, root))
        {
            Measurement.Text("content", block.Text ?? "", "text"),
            Measurement.Numeric("fontSize", block.FontSize, "sp", category: "text", source: "TextBlock.FontSize; host fontScale=1"),
            Measurement.Numeric("fontWeight", (int)block.FontWeight, "weight", category: "text", source: "TextBlock.FontWeight"),
            Measurement.Numeric("letterSpacing", block.LetterSpacing, "sp", category: "text",
                source: "TextBlock.LetterSpacing; host fontScale=1"),
            Measurement.Numeric("lineCount", layout.TextLines.Count, "count", category: "text"),
            new("resolvedFonts", "text", "strings", "none", "none", "text.resolved-font-families", "ShapedTextRun.GlyphTypeface",
                Strings: layout.TextLines.SelectMany(x => x.TextRuns).OfType<ShapedTextRun>()
                    .Select(x => x.GlyphRun.GlyphTypeface.FamilyName).Distinct().ToArray()),
            Measurement.Unavailable("drawOrigin", "text", "point", "dip", "scene",
                "TextBlock does not expose its actual RenderTextLayout origin."),
            Measurement.Unavailable("glyphInk", "text", "rect", "dip", "text",
                "No glyph ink reader installed; caret boxes are not ink boxes.")
        };
        fields.Add(block.Foreground is ISolidColorBrush brush
            ? Measurement.Vector("color", "color",
                [brush.Color.R / 255d, brush.Color.G / 255d, brush.Color.B / 255d, brush.Color.A / 255d * brush.Opacity],
                "srgb", "none", "text", "text.foreground.solid-srgba", "TextBlock.Foreground + Brush.Opacity")
            : Measurement.Unavailable("color", "text", "color", "srgb", "none", "Foreground is not a solid brush.",
                "text.foreground.solid-srgba"));
        double top = 0;
        for (var i = 0; i < layout.TextLines.Count; i++)
        {
            var line = layout.TextLines[i];
            var prefix = $"line.{i}.";
            fields.AddRange([
                // TextLine.Length may include a formatter end-of-paragraph run beyond the input string.
                // Retain that native range separately; canonical range is its intersection with the string.
                Measurement.Vector(prefix + "nativeRange", "range", [line.FirstTextSourceIndex, line.FirstTextSourceIndex + line.Length],
                    "formatter-index", "text", "text", "avalonia.line.native-range", "TextLine.FirstTextSourceIndex + Length"),
                Measurement.Vector(prefix + "range", "range",
                    TextRanges.Content(block.Text ?? "", line.FirstTextSourceIndex, line.FirstTextSourceIndex + line.Length),
                    "utf16", "text", "text", TextRanges.ContentDefinition,
                    "derived: native range clipped to input, then trailing CR/LF/U+2028/U+2029 removed; spaces retained"),
                Measurement.Vector(prefix + "consumedRange", "range", [
                        Math.Clamp(line.FirstTextSourceIndex, 0, (block.Text ?? "").Length),
                        i + 1 < layout.TextLines.Count
                            ? Math.Clamp(layout.TextLines[i + 1].FirstTextSourceIndex, 0, (block.Text ?? "").Length)
                            : (block.Text ?? "").Length
                    ],
                    "utf16", "text", "text", TextRanges.ConsumedDefinition,
                    "derived: line start to next line start, or input length for final line; includes paragraph breaks"),
                Measurement.Numeric(prefix + "top", top, "dip", "text", "text", "text.line.top"),
                Measurement.Numeric(prefix + "height", line.Height, "dip", "text", "text", "text.line.height"),
                Measurement.Numeric(prefix + "baseline", top + line.Baseline, "dip", "text", "text", "text.line.baseline"),
                Measurement.Numeric(prefix + "width", line.WidthIncludingTrailingWhitespace, "dip", "text", "text",
                    "avalonia.line.width-including-trailing"),
                Measurement.Flag(prefix + "ellipsized", line.HasCollapsed, "text", "text.line.ellipsized")
            ]);
            top += line.Height;
        }

        return new(id, "text-block", parentId, fields.ToArray());
    }
}

public static class TextRanges
{
    public const string ContentDefinition = "text.line.content-range.utf16.no-paragraph-break.v1";
    public const string ConsumedDefinition = "text.line.consumed-range.utf16.includes-paragraph-break.v1";

    public static double[] Content(string text, int start, int end)
    {
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        while (end > start && text[end - 1] is '\r' or '\n' or '\u2028' or '\u2029') end--;
        return [start, end];
    }
}

